#!/bin/bash
#
# Creates a new password encrypted file.
#
# Creates a temporary file, edits it, encrypts into a file in
# the current folder, removes temporary file.  
#
# Usage:
#
#     ./add.sh name
#

if [[ -z "$1" ]]; then
  echo "Usage: add.sh name"
  exit
fi

if [[ -z "${PWDPWD}" ]]; then
  echo -n Password:
  read -r -s PWDPWD
  export PWDPWD
  echo ""
fi

ENCRYPTED_NAME=$(echo -n "$1" | openssl aes-256-cbc -e -salt -pbkdf2 -pass env:PWDPWD  | dd bs=1 skip=8 status=none | base64)
TMP1="${ENCRYPTED_NAME//\//_}"
FILE_NAME="${TMP1//=/'~'}"
echo "${FILE_NAME}"

TEMPORARY_FILE=$(mktemp /tmp/pwd.XXXXXXXX)
vi "${TEMPORARY_FILE}" < /dev/tty > /dev/tty
openssl aes-256-cbc -e -salt -pbkdf2 -out "${FILE_NAME}" -pass env:PWDPWD < "${TEMPORARY_FILE}"
rm "${TEMPORARY_FILE}"
