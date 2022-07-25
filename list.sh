#!/bin/bash
#
# Lists the filesystem items (files, folders) that are in the current working folder.
#
# Usage:
#
#     ./list.sh
#

if [[ -z "${PWDPWD}" ]]; then
  echo -n Password:
  read -r -s PWDPWD
  export PWDPWD
  echo ""
fi

for i in *; do
  TMP1="${i//_/\/}"
  ENCRYPTED="${TMP1//~/=}"
  NAME=$(echo -n "$ENCRYPTED" | base64 -d 2> /dev/null | cat <(echo -n "Salted__") - | openssl aes-256-cbc -d -salt -pbkdf2 -pass env:PWDPWD 2> /dev/null)
  echo "${NAME} - ${i}"
done