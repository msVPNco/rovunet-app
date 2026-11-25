#!/usr/bin/env python3
"""Rovunet Free v2: offline individual AES-GCM envelopes + signed index.
Never place source subscription URL, plaintext links, or private keys in GitHub.
"""
import argparse
import base64
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.serialization import load_pem_private_key
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey

ID = re.compile(r"[0-9a-f]{32}\Z")
KEY_ID = re.compile(r"[a-z0-9][a-z0-9_-]{0,63}\Z")
SCHEMES = ("rovunet://", "vless://", "ss://", "trojan://", "hy2://", "hysteria2://")
UTC = dt.timezone.utc

def when(value):
    if not isinstance(value, str):
        raise ValueError("missing UTC timestamp")
    t = dt.datetime.fromisoformat(value.replace("Z", "+00:00"))
    if t.tzinfo is None:
        raise ValueError("timestamp must have timezone")
    return t.astimezone(UTC)

def time_window(created, expires):
    a, b = when(created), when(expires)
    if b - a != dt.timedelta(days=30) or a > dt.datetime.now(UTC) + dt.timedelta(minutes=5):
        raise ValueError("invalid 30-day window")
    return a, b

def json_bytes(value):
    return (json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False) + "\n").encode()

def read_key(path):
    p = Path(path)
    if not p.is_file() or p.stat().st_size != 32:
        raise ValueError("AES-256-GCM requires a 32-byte key file")
    if os.name != "nt" and (p.stat().st_mode & 0o077):
        raise ValueError("encryption key must be owner-only (chmod 600)")
    return p.read_bytes()

def associated(e):
    fields = ("id", "donation_id", "created_at", "expires_at", "key_id")
    return ("rovunet-free-v2|" + "|".join(e[x] for x in fields)).encode()

def encrypt_one(item, donation_id, created, expires, key_id, key):
    env = {
        "version": 2, "id": secrets.token_hex(16),
        "donation_id": donation_id, "created_at": created, "expires_at": expires,
        "quota_gib": 50, "key_id": key_id, "algorithm": "AES-256-GCM",
    }
    nonce = os.urandom(12)
    env["nonce"] = base64.b64encode(nonce).decode()
    env["ciphertext"] = base64.b64encode(
        AESGCM(key).encrypt(nonce, json_bytes(item), associated(env))
    ).decode()
    return env

def import_feed(args):
    if not ID.fullmatch(args.donation_id) or not KEY_ID.fullmatch(args.key_id):
        raise ValueError("invalid donation_id or key_id")
    _, end = time_window(args.created_at, args.expires_at)
    if end <= dt.datetime.now(UTC):
        raise ValueError("donation already expired")
    p = Path(args.input)
    if not p.is_file() or p.stat().st_size > 1024 * 1024:
        raise ValueError("private feed missing or too large")
    feed = json.loads(p.read_bytes())
    if not isinstance(feed, dict) or feed.get("version") != 1:
        raise ValueError("expected strict subscription feed version 1")
    items = feed.get("items")
    if not isinstance(items, list) or not 1 <= len(items) <= 128:
        raise ValueError("expected 1-128 independent items")
    seen = set()
    for item in items:
        if not isinstance(item, dict) or set(item) != {"id", "link"}:
            raise ValueError("invalid item shape")
        uid, uri = item["id"], item["link"]
        if not isinstance(uid, str) or not 1 <= len(uid) <= 200 or uid in seen:
            raise ValueError("duplicate/invalid item id")
        if not isinstance(uri, str) or not 16 <= len(uri) <= 65536 or not uri.startswith(SCHEMES) or "\n" in uri or "\r" in uri:
            raise ValueError("invalid/unsupported item URI")
        seen.add(uid)
    key = read_key(args.key_file)
    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=True, mode=0o700)
    # Refuse partial overwrite of an existing donation to avoid ambiguous quota groups.
    if any(out.glob("*.json")):
        raise ValueError("output must be a new empty directory; avoid duplicates")
    for item in items:
        env = encrypt_one(item, args.donation_id, args.created_at, args.expires_at, args.key_id, key)
        target = out / (env["id"] + ".json")
        with target.open("xb") as file:
            file.write(json_bytes(env))
    print(f"Encrypted {len(items)} independent items in shared group {args.donation_id}; no subscription URL stored.")

def build_catalog(root):
    files = sorted((root / "configs").glob("*.json"))
    if len(files) > 500:
        raise ValueError("too many items")
    revoked = root / "revoked.json"
    denied = set(json.loads(revoked.read_text())) if revoked.is_file() else set()
    if not all(isinstance(x, str) and ID.fullmatch(x) for x in denied):
        raise ValueError("invalid revocation list")
    entries, seen = [], set()
    for p in files:
        if p.is_symlink() or not p.is_file() or p.stat().st_size > 200000 or not ID.fullmatch(p.stem):
            raise ValueError("unsafe file in configs")
        raw = p.read_bytes()
        env = json.loads(raw)
        if set(env) != {"version", "id", "donation_id", "created_at", "expires_at", "quota_gib", "key_id", "algorithm", "nonce", "ciphertext"}:
            raise ValueError("unexpected envelope fields")
        if env["version"] != 2 or env["algorithm"] != "AES-256-GCM" or env["quota_gib"] != 50:
            raise ValueError("bad envelope parameters")
        if env["id"] != p.stem or not ID.fullmatch(env["donation_id"]) or env["id"] in seen or not KEY_ID.fullmatch(env["key_id"]):
            raise ValueError("bad envelope identity")
        seen.add(env["id"])
        _, expires = time_window(env["created_at"], env["expires_at"])
        nonce = base64.b64decode(env["nonce"], validate=True)
        cipher = base64.b64decode(env["ciphertext"], validate=True)
        if len(nonce) != 12 or len(cipher) < 17:
            raise ValueError("bad AES-GCM envelope")
        if b"rovunet://" in raw or b".windows.json" in raw or b"https://" in raw:
            raise ValueError("plaintext credential or subscription URL found")
        if expires <= dt.datetime.now(UTC) or env["id"] in denied or env["donation_id"] in denied:
            continue
        entries.append({
            "id": env["id"], "donation_id": env["donation_id"],
            "created_at": env["created_at"], "expires_at": env["expires_at"],
            "quota_gib": 50, "key_id": env["key_id"],
            "path": "configs/" + p.name,
            "sha256": hashlib.sha256(raw).hexdigest(),
        })
    return entries

def publish(root, check):
    entries = build_catalog(root)
    print(f"Valid encrypted items: {len(entries)}")
    if check:
        return
    secret = os.environ.get("ROVUNET_FREE_SIGNING_KEY_PEM_B64", "")
    if not secret:
        raise ValueError("dedicated Ed25519 signer missing; refusing unsigned publication")
    private = load_pem_private_key(base64.b64decode(secret, validate=True), password=None)
    if not isinstance(private, Ed25519PrivateKey):
        raise ValueError("dedicated key must be Ed25519")
    body = json_bytes({"version": 2, "generated_at": dt.datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"), "entries": entries})
    signature = base64.b64encode(private.sign(body)) + b"\n"
    root.mkdir(parents=True, exist_ok=True)
    for name, value in (("catalog.json", body), ("catalog.sig", signature)):
        temp = root / ("." + name + ".tmp")
        temp.write_bytes(value)
        temp.replace(root / name)
    print("Signed v2 catalog. Client activation requires pinned public key and approved key-broker.")

def main():
    p = argparse.ArgumentParser()
    p.add_argument("action", choices=("import", "check", "publish"))
    p.add_argument("--input")
    p.add_argument("--output", default="free/v2/configs")
    p.add_argument("--root", default="free/v2")
    p.add_argument("--key-file")
    p.add_argument("--key-id")
    p.add_argument("--donation-id")
    p.add_argument("--created-at")
    p.add_argument("--expires-at")
    args = p.parse_args()
    if args.action == "import":
        if not all((args.input, args.key_file, args.key_id, args.donation_id, args.created_at, args.expires_at)):
            p.error("import requires input, key-file, key-id, donation-id, created-at and expires-at")
        import_feed(args)
    else:
        publish(Path(args.root), check=args.action == "check")

if __name__ == "__main__":
    main()
