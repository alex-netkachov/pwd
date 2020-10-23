#!/bin/bash
#
# Encrypts the stdin into the specified file.
#
# Usage:
#
#     ./encrypt.sh example.com
#
# If there is an environment variable PWDPWD, reads the password from it.
# Otherwise prompts for the password.
#
# Set the password environment variable:
#
#     read -s PWDPWD && export PWDPWD
#

if [[ -z "${PWDPWD}" ]]; then
    openssl aes-256-cbc -e -salt -pbkdf2 -out $1
else
    openssl aes-256-cbc -e -salt -pbkdf2 -out $1 -pass env:PWDPWD
fi
