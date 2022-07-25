#!/bin/bash
#
# Decrypts the specified file and outputs its content.
#
# Example:
#
#     ./decrypt.sh example.com
#

if [[ -z "$1" ]]; then
  echo "Usage: decrypt.sh name"
  exit
fi

if [[ -z "${PWDPWD}" ]]; then
  echo -n Password:
  read -r -s PWDPWD
  export PWDPWD
  echo ""
fi

openssl aes-256-cbc -d -salt -pbkdf2 -in "$1" -pass env:PWDPWD
