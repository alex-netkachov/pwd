/**
 * Prints the decrypted contents of the file.
 *
 * Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
 *
 * Usage:
 *
 * ```
 * node reveal.js data.txt
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
      console.error('Usage: node reveal.js <in_file>');
      console.error('Example: node reveal.js data.txt');
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

  let cipher = mods.cipher(password);
  try {
    let text = await mods.fsp.readFile(inFile, 'utf-8');
    let region = mods.cipher.findEncryptedRegion(text);
    while (region !== null) {
      const decrypted = (await cipher.decrypt(Buffer.from(region.text))).toString('utf-8');
      text = text.substring(0, region.start) + decrypted + text.substring(region.end);
      region = mods.cipher.findEncryptedRegion(text, region.start + decrypted.length);
    }
    console.log(text);
  } catch (err) {
      console.error('Error during decryption:', err.message);
      process.exit(1);
  }
}
