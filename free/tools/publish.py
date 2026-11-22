#!/usr/bin/env python3
"""Build a signed Rovunet Free catalog from reviewed 50 GiB / 30-day donations.

Private signing material must enter only through the publisher environment.
Never commit the signing key, private panel config or personal subscriptions.
"""

import argparse
import base64
import datetime as dt
import json
import os
import pathlib
import re
import subprocess
import tempfile
import urllib.parse

UTC = dt.timezone.utc
MAX_DONATIONS = 500
MAX_LINKS_PER_DONATION = 128
MAX_LINK_LENGTH = 65536
SCHEMES = {"vless", "ss", "trojan", "hy2", "hysteria2"}
ID_RE = re.compile(r"[0-9a-f]{32}\Z")


def parse_time(value: str) -> dt.datetime:
    if not isinstance(value, str):
        raise ValueError("timestamp must be RFC3339")
    value = value.replace("Z", "+00:00")
    parsed = dt.datetime.fromisoformat(value)
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise ValueError("timestamp must include a UTC offset")
    return parsed.astimezone(UTC)


def canonical_timestamp(value: dt.datetime) -> str:
    return value.astimezone(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def valid_link(link: str) -> bool:
    if not isinstance(link, str) or len(link) > MAX_LINK_LENGTH:
        return False
    if "\n" in link or "\r" in link:
        return False
    parsed = urllib.parse.urlsplit(link)
    if parsed.scheme.lower() not in SCHEMES or not parsed.netloc:
        return False
    if parsed.scheme.lower() == "vless":
        q = urllib.parse.parse_qs(parsed.query)
        if q.get("security", [""])[0].lower() not in ("tls", "reality"):
            return False
        if q.get("allowInsecure", ["0"])[0] not in ("", "0", "false"):
            return False
    return True


def validate_candidate(raw: object, now: dt.datetime) -> dict | None:
    if not isinstance(raw, dict):
        raise ValueError("candidate must be an object")
    if raw.get("version") != 1 or raw.get("kind") != "rovunet-donation":
        raise ValueError("unexpected candidate schema")
    donation_id = raw.get("donation_id")
    if not isinstance(donation_id, str) or not ID_RE.fullmatch(donation_id):
        raise ValueError("invalid donation ID")
    if raw.get("quota_gb") != 50:
        raise ValueError("donation must share a 50 GiB server-side quota")
    created = parse_time(raw.get("created_at"))
    expires = parse_time(raw.get("expires_at"))
    if expires - created != dt.timedelta(days=30):
        raise ValueError("donation expiry must be exactly 30 days after creation")
    if created > now + dt.timedelta(minutes=5):
        raise ValueError("donation created in the future")
    items = raw.get("items")
    if not isinstance(items, list) or len(items) > MAX_LINKS_PER_DONATION:
        raise ValueError("invalid donation item count")
    seen = set()
    normalized = []
    for item in items:
        if not isinstance(item, dict) or set(item) != {"id", "link"}:
            raise ValueError("invalid donation item")
        item_id, link = item["id"], item["link"]
        if not isinstance(item_id, str) or not 1 <= len(item_id) <= 120 or item_id in seen:
            raise ValueError("duplicate or invalid item ID")
        if not valid_link(link):
            raise ValueError("unsupported or insecure public link")
        seen.add(item_id)
        normalized.append({"id": item_id, "link": link})
    if expires <= now:
        return None
    if not normalized:
        raise ValueError("active donation has no usable links")
    return {
        "donation_id": donation_id,
        "created_at": canonical_timestamp(created),
        "expires_at": canonical_timestamp(expires),
        "quota_gb": 50,
        "items": normalized,
    }


def build_catalog(donations_dir: pathlib.Path, now: dt.datetime) -> tuple[bytes, list[pathlib.Path]]:
    entries = []
    expired = []
    seen = set()
    files = sorted(donations_dir.glob("*.json")) if donations_dir.exists() else []
    if len(files) > MAX_DONATIONS:
        raise ValueError("too many donation files")
    for file in files:
        if file.is_symlink() or not file.is_file() or file.stat().st_size > 1024 * 1024:
            raise ValueError("unsafe donation file: " + file.name)
        raw = json.loads(file.read_text(encoding="utf-8"))
        donation = validate_candidate(raw, now)
        if donation is None:
            expired.append(file)
            continue
        if donation["donation_id"] in seen:
            raise ValueError("duplicate donation ID")
        seen.add(donation["donation_id"])
        entries.append(donation)
    entries.sort(key=lambda e: e["donation_id"])
    catalog = {"version": 1, "generated_at": canonical_timestamp(now), "donations": entries}
    data = (json.dumps(catalog, ensure_ascii=False, sort_keys=True, separators=(",", ":")) + "\n").encode("utf-8")
    if len(data) > 8 * 1024 * 1024:
        raise ValueError("catalog exceeds client download budget")
    return data, expired


def sign(data: bytes, pem: bytes) -> bytes:
    with tempfile.TemporaryDirectory(prefix="rovunet-free-sign-") as directory:
        root = pathlib.Path(directory)
        key, message, signature = root / "key.pem", root / "message", root / "signature"
        key.write_bytes(pem)
        key.chmod(0o600)
        message.write_bytes(data)
        result = subprocess.run(
            ["openssl", "pkeyutl", "-sign", "-rawin", "-inkey", str(key), "-in", str(message), "-out", str(signature)],
            capture_output=True, check=False,
        )
        if result.returncode != 0:
            raise RuntimeError("Ed25519 signing failed (verify publisher key and OpenSSL)")
        value = signature.read_bytes()
        if len(value) != 64:
            raise RuntimeError("publisher key is not a valid Ed25519 signing key")
        return value


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=pathlib.Path, default=pathlib.Path("free"))
    parser.add_argument("--check", action="store_true", help="validate inputs but never write or remove files")
    args = parser.parse_args()
    now = dt.datetime.now(UTC).replace(microsecond=0)
    data, expired = build_catalog(args.root / "donations", now)
    if args.check:
        print(f"Validated {len(json.loads(data)['donations'])} active donations; {len(expired)} expired")
        return
    encoded = os.environ.get("ROVUNET_FREE_SIGNING_KEY_PEM_B64", "")
    if not encoded:
        raise SystemExit("Signing key not configured; refusing unsigned publication")
    try:
        key = base64.b64decode(encoded, validate=True)
    except Exception as exc:
        raise SystemExit("Invalid signing key encoding") from exc
    signature = sign(data, key)
    args.root.mkdir(parents=True, exist_ok=True)
    # A single Git commit atomically publishes both files. Clients verify the
    # exact downloaded bytes and retain their last-good catalog on mismatch.
    (args.root / "catalog.json").write_bytes(data)
    (args.root / "catalog.sig").write_text(base64.b64encode(signature).decode("ascii") + "\n", encoding="ascii")
    for file in expired:
        file.unlink()
    print(f"Signed {len(json.loads(data)['donations'])} active donations; retired {len(expired)} expired")


if __name__ == "__main__":
    main()
