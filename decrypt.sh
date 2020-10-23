#!/bin/bash
#
# Decrypts the specified file and prints the content.
#
# Usage:
#
#     ./decrypt.sh example.com
#
# If there is an environment variable PWDPWD, reads the password from it.
# Otherwise prompts for the password.
#
# Set the password environment variable:
#
#     read -s PWDPWD && export PWDPWD
#

if [[ -z "${PWDPWD}" ]]; then
    openssl aes-256-cbc -d -salt -pbkdf2 -in $1
else
    openssl aes-256-cbc -d -salt -pbkdf2 -in $1 -pass env:PWDPWD
fi
