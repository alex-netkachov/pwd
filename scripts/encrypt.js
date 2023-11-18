/**
 * Prints the encrypted contents of the file.
 *
 * Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
 *
 * Usage:
 *
 * ```
 * node encrypt.js data.txt data.pwd
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
  const outFile = process.argv[3];
  if (!inFile || !outFile) {
      console.error('Usage: node encrypt.js <in_file> <out_file>');
      console.error('Example: node encrypt.js data.txt data.pwd');
      process.exit(1);
  }
  if (!fs.existsSync(inFile)) {
      console.error('Input file does not exist.');
      process.exit(1);
  }
  if (fs.existsSync(outFile)) {
    console.error('Output file exists.');
    process.exit(1);
}

  const password = await readPassword('Password: ');
  if (!password) {
    console.error('Password cannot be empty.');
    process.exit(1);
  }

  const confirmedPassword = await readPassword('Confirm password: ');
  if (!confirmedPassword) {
    console.error('Password cannot be empty.');
    process.exit(1);
  }
  if (confirmedPassword !== password) {
    console.error('Passwords do not match.');
    process.exit(1);
  }

  try {
    const input = await fsp.readFile(inFile);
    const encrypted = await cipher(password).encrypt(input);
    await fsp.writeFile(outFile, encrypted);
    console.log('Encrypted file written to ' + outFile);
  } catch (err) {
      console.error('Error during encryption:', err.message);
      process.exit(1);
  }
}
