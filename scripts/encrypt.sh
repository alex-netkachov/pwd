#!/bin/bash
#
# Encrypts the in_file and writes the encrypted contents to the out_file.
#
# Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
#
# Requires: openssl, base64, tr
#

if [[ -z "$1" ]] || [[ -z "$2" ]]; then
  echo "Usage: $0 <in_file> <out_file>"
  echo "Examples: $0 data.txt data.pwd"
  exit 1
fi

in_file="$1"
out_file="$2"

if [ ! -f "$in_file" ]; then
  echo "Input file does not exist."
  exit 1
fi

if [ -f "$out_file" ]; then
  echo -n "Output file already exists. Overwrite? (y/n) "
  read -r confirm
  if [[ $confirm != [yY] ]]; then
    echo "Exiting without overwriting file."
    exit 1
  fi
fi

read -s -e -p "Password: " password
echo -ne "\r\033[K"
if [[ -z "$password" ]]; then
  echo "Password cannot be empty."
  exit 1
fi

read -s -e -p "Confirm password: " password_confirm
echo -ne "\r\033[K"
if [[ "$password" != "$password_confirm" ]]; then
  echo "The passwords do not match."
  exit 1
fi

openssl aes-256-cbc -e -salt -pbkdf2 -iter 600000 -in "$in_file" -pass file:<( echo -n "$password" ) \
| dd iflag=skip_bytes skip=8 \
| base64 -w 0 \
| tr "\53" "\55" \
| tr "\57" "\137" \
| cat > "$out_file"
