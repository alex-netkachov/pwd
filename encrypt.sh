#!/bin/bash
#
# Encrypts the stdin into the specified file.
#
# Usage:
#
#     cat file | ./encrypt.sh example.com
#

if [[ -z "$1" ]]; then
  echo "Usage: encrypt.sh name"
  exit
fi

if [[ -z "${PWDPWD}" ]]; then
  echo -n Password:
  read -r -s PWDPWD
  export PWDPWD
  echo ""
fi

openssl aes-256-cbc -e -salt -pbkdf2 -out "$1" -pass env:PWDPWD
