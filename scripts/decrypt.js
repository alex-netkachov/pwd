/**
 * Prints the decrypted contents of the file.
 *
 * Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
 *
 * Usage:
 *
 * ```
 * node decrypt.js data.pwd > data.txt
 * ```
 */

'use strict';

const mods =
  { fs : require('fs'),
    fsp : require('fs/promises'),
    readPassword : require('./lib/readPassword'),
    cipher : require('./lib/cipher') };

main().catch(error => console.error(error));

async function main() {
  const inFile = process.argv[2];
  if (!inFile) {
      console.error('Usage: node decrypt.js <in_file>');
      console.error('Example: node decrypt.js data.pwd > data.txt');
      process.exit(1);
  }
  if (!mods.fs.existsSync(inFile)) {
      console.error('Input file does not exist.');
      process.exit(1);
  }

  const password = await mods.readPassword('Password: ');
  if (!password) {
    console.error('Password cannot be empty.');
    process.exit(1);
  }

  try {
    const input = await mods.fsp.readFile(inFile);
    const decrypted = (await mods.cipher(password).decrypt(input)).toString('utf-8');
    console.log(decrypted);
  } catch (err) {
      console.error('Error during decryption:', err.message);
      process.exit(1);
  }
}
