#!/bin/bash
#
# Decrypts the specified file and prints the content.
#
# Usage:
#
#     ./decrypt.sh example.com
#
# Reads the password from the environment variable `PWDPWD` if it does exist.
# Prompts for the password otherwise.
#
# Securely set the password environment variable:
#
#     read -s PWDPWD && export PWDPWD
#

if [[ -z "${PWDPWD}" ]]; then
    openssl aes-256-cbc -d -salt -pbkdf2 -in $1
else
    openssl aes-256-cbc -d -salt -pbkdf2 -in $1 -pass env:PWDPWD
fi
