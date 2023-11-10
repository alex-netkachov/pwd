# Migration

Scripts and tools to migrate to the new database format.

## Old format

- Encryption: AES 256-bit CBC
- File content encoding: none, prefixed with Salted__
- File name encoding: custom base64, no prefix
- Key derivation: PBKDF2 with sha256, 10K iterations

## New format

- Encryption: AES 256-bit CBC
- File content encoding: base64url, no prefix
- File name encoding: base64url, no prefix
- Key derivation: PBKDF2 with sha256, 600K iterations

## Progress

- decrypt.sh - new
- encrypt.sh - new
- reveal.sh - new
- migration.sh - not started
- decrypt.ps1 - not started
- encrypt.ps1 - not started
- reveal.ps1 - not started
- pwd - not started
