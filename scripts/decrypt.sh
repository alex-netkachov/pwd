#!/bin/bash
#
# Decrypts the file and displays the decrypted contents.
#
# Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
#
# Requires: openssl, base64, tr
#

if [[ -z "$1" ]]; then
  echo "Usage: $0 <in_file>"
  echo "Example: $0 data.pwd > data.txt"
  exit 1
fi

in_file="$1"

if [ ! -f "$in_file" ]; then
  echo "Input file does not exist."
  exit 1
fi

read -s -e -p "Password: " password
echo -ne "\r\033[K"
if [[ -z "$password" ]]; then
  echo "Password cannot be empty."
  exit 1
fi

cat "$in_file" \
| tr "\55" "\53" \
| tr "\137" "\57" \
| base64 -d \
| cat <(echo -n "Salted__") - \
| openssl aes-256-cbc -d -salt -pbkdf2 -iter 600000 -pass file:<( echo -n $password )
