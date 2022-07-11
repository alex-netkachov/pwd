#!/bin/bash
#
# Encrypts the stdin into the specified file.
#
# Usage:
#
#     ./encrypt.sh example.com
#
# Reads the password from the environment variable `PWDPWD` if it does exist.
# Prompts for the password otherwise.
#
# Securely set the password environment variable:
#
#     read -s PWDPWD && export PWDPWD
#

if [[ -z "${PWDPWD}" ]]; then
    openssl aes-256-cbc -e -salt -pbkdf2 -out $1
else
    openssl aes-256-cbc -e -salt -pbkdf2 -out $1 -pass env:PWDPWD
fi
