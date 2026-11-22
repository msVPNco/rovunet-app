import base64
import datetime as dt
import json
import pathlib
import shutil
import subprocess
import tempfile
import unittest

import publish


NOW = dt.datetime(2026, 11, 22, 12, 0, tzinfo=dt.timezone.utc)


def candidate(created=NOW - dt.timedelta(days=1), link="vless://abcd@example.com:443?security=tls&type=ws"):
    return {
        "version": 1,
        "kind": "rovunet-donation",
        "donation_id": "a" * 32,
        "created_at": publish.canonical_timestamp(created),
        "expires_at": publish.canonical_timestamp(created + dt.timedelta(days=30)),
        "quota_gb": 50,
        "items": [{"id": "vless", "link": link}],
    }


class DonationPublisherTests(unittest.TestCase):
    def test_50_gib_30_day_contract(self):
        row = publish.validate_candidate(candidate(), NOW)
        self.assertEqual(row["quota_gb"], 50)
        self.assertEqual(len(row["items"]), 1)

    def test_expired_and_future_are_not_published(self):
        self.assertIsNone(publish.validate_candidate(candidate(NOW - dt.timedelta(days=31)), NOW))
        with self.assertRaisesRegex(ValueError, "future"):
            publish.validate_candidate(candidate(NOW + dt.timedelta(days=1)), NOW)

    def test_expiry_cannot_be_refreshed_by_github_upload(self):
        row = candidate()
        row["expires_at"] = publish.canonical_timestamp(NOW + dt.timedelta(days=31))
        with self.assertRaisesRegex(ValueError, "30 days"):
            publish.validate_candidate(row, NOW)

    def test_quota_and_insecure_links_rejected(self):
        for link in ["rovunet://base64notasecret", "vless://id@host:80?security=none",
                     "vless://id@host:443?security=tls&allowInsecure=1",
                     "https://host/private-subscription"]:
            with self.subTest(link=link), self.assertRaisesRegex(ValueError, "insecure"):
                publish.validate_candidate(candidate(link=link), NOW)
        wrong = candidate()
        wrong["quota_gb"] = 0
        with self.assertRaisesRegex(ValueError, "50 GiB"):
            publish.validate_candidate(wrong, NOW)

    def test_catalog_contains_active_only_and_expired_file_is_listed_for_cleanup(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder = pathlib.Path(temporary)
            folder.joinpath("a.json").write_text(json.dumps(candidate()))
            old = candidate(NOW - dt.timedelta(days=31))
            old["donation_id"] = "b" * 32
            folder.joinpath("b.json").write_text(json.dumps(old))
            blob, expired = publish.build_catalog(folder, NOW)
            parsed = json.loads(blob)
            self.assertEqual(parsed["version"], 1)
            self.assertEqual(len(parsed["donations"]), 1)
            self.assertEqual(parsed["donations"][0]["donation_id"], "a" * 32)
            self.assertEqual([file.name for file in expired], ["b.json"])

    @unittest.skipUnless(shutil.which("openssl"), "OpenSSL required for signing smoke test")
    def test_ed25519_signature_checks_exact_catalog_bytes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            key, public, message, signature = (root / n for n in ("key.pem", "pub.pem", "catalog.json", "catalog.sig"))
            subprocess.run(["openssl", "genpkey", "-algorithm", "Ed25519", "-out", str(key)], check=True, capture_output=True)
            subprocess.run(["openssl", "pkey", "-in", str(key), "-pubout", "-out", str(public)], check=True, capture_output=True)
            blob = b'{"version":1,"donations":[]}\n'
            message.write_bytes(blob)
            signature.write_bytes(publish.sign(blob, key.read_bytes()))
            accepted = subprocess.run(["openssl", "pkeyutl", "-verify", "-pubin", "-inkey", str(public),
                                       "-rawin", "-in", str(message), "-sigfile", str(signature)], capture_output=True)
            self.assertEqual(accepted.returncode, 0, accepted.stderr)
            message.write_bytes(blob + b"tampered")
            rejected = subprocess.run(["openssl", "pkeyutl", "-verify", "-pubin", "-inkey", str(public),
                                       "-rawin", "-in", str(message), "-sigfile", str(signature)], capture_output=True)
            self.assertNotEqual(rejected.returncode, 0)


if __name__ == "__main__":
    unittest.main()
