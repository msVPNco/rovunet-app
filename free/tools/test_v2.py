import datetime as dt
import importlib.util
import json
from pathlib import Path
import os
import tempfile
import unittest
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
import base64

spec = importlib.util.spec_from_file_location("v2", Path(__file__).with_name("v2.py"))
v2 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v2)

class EncryptedFreeTests(unittest.TestCase):
    def test_six_separate_items_one_shared_budget(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root)
            key = os.urandom(32)
            keyfile = root / "key"
            keyfile.write_bytes(key)
            keyfile.chmod(0o600)
            t = dt.datetime.now(dt.timezone.utc).replace(microsecond=0) - dt.timedelta(minutes=1)
            start = t.isoformat().replace("+00:00", "Z")
            expires = (t + dt.timedelta(days=30)).isoformat().replace("+00:00", "Z")
            feed = {"version":1,"items":[{"id":f"vless-{i}","link":"rovunet://testonly-not-an-actual-secret"} for i in range(6)]}
            inp = root / "private.json"
            inp.write_text(json.dumps(feed))
            args = type("Args", (), dict(input=str(inp),output=str(root/"v2"/"configs"),key_file=str(keyfile),key_id="free-test-001",donation_id="a"*32,created_at=start,expires_at=expires))
            v2.import_feed(args)
            entries = v2.build_catalog(root / "v2")
            self.assertEqual(6, len(entries))
            self.assertEqual({"a"*32}, {x["donation_id"] for x in entries})
            self.assertEqual({50}, {x["quota_gib"] for x in entries})
            env = json.loads((root/"v2"/entries[0]["path"]).read_text())
            plain = AESGCM(key).decrypt(base64.b64decode(env["nonce"]),base64.b64decode(env["ciphertext"]),v2.associated(env))
            self.assertIn("rovunet://",plain.decode())
            changed = dict(env, expires_at="2029-01-01T00:00:00Z")
            with self.assertRaises(Exception):
                AESGCM(key).decrypt(base64.b64decode(env["nonce"]),base64.b64decode(env["ciphertext"]),v2.associated(changed))
            (root/"v2"/"revoked.json").write_text(json.dumps(["a"*32]))
            self.assertEqual([],v2.build_catalog(root/"v2"))

    def test_reject_raw_subscription_and_wrong_expiry(self):
        self.assertFalse("https://example.test/sub/token.windows.json".startswith(v2.SCHEMES))
        with self.assertRaises(ValueError):
            v2.time_window("2026-11-23T00:00:00Z","2026-11-25T00:00:00Z")

if __name__ == "__main__":
    unittest.main()
