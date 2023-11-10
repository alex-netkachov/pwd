#!/bin/bash
#
# Looks for the encrypted text in the input and decrypts it.
#
# Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
#
# This script is meant to be used with the output of a filesystem command or
# for a text file containing relatively short encrypted chunks (password).
#
# Therefore, it looks for encrypted chunks of 32, 56, and 76 characters long.
#
# Requires: perl, IPC::Open2, openssl, base64
#

if [[ -z "$1" ]]; then
  echo "Usage: $0 <in_file>"
  echo "Example: $0 data.pwd"
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

decrypt=$(cat <<'EOF'
use strict;
use warnings;
use IPC::Open2;

sub exec2 {
  my ($input, $cmd, @args) = @_;
  my $pid = open2(my $out, my $in, $cmd, @args) or die "open2 failed: $!";
  binmode($in);
  binmode($out);
  print $in $input;
  close $in;
  my $buffer;
  my $byte_array = '';
  while (my $bytes_read = read($out, $buffer, 1024)) { $byte_array .= $buffer; }
  waitpid($pid, 0);
  return $byte_array;
}

sub decrypt {
  my ($v) = @_;
  $v =~ s/-/+/g;
  $v =~ s/_/\//g;
  $v = exec2($v, 'base64', '-d');
  $v = exec2('Salted__' . $v, 'openssl', 'aes-256-cbc', '-d', '-salt', '-pbkdf2', '-iter', '600000', '-pass', 'env:PWD747');
  return $v;
}

s/(?<![^\w\-\_])([\w\-\_]{32}|[\w\-\_]{54}==|[\w\-\_]{75}=)(?![^\w\-\_])/decrypt($1)/ge
EOF
)

cat "$in_file" \
| PWD747="$password" perl -pe "$decrypt"
