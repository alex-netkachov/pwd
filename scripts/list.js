/**
 * Lists the encrypted files.
 *
 * Encrypted text is a AES encrypted, base64url (see RFC4648) encoded string.
 *
 * Usage:
 *
 * ```
 * node list.js [path]
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
  let path = process.argv[2];
  path = !!path ? path : '.';
  if (!mods.fs.existsSync(path)) {
      console.error('Path does not exist.');
      process.exit(1);
  }

  const password = await mods.readPassword('Password: ');
  if (!password) {
    console.error('Password cannot be empty.');
    process.exit(1);
  }

  const cf = mods.cipher(password);
  try {
    const input = await mods.fsp.readdir(path);
    for (const file of input) {
      const region = mods.cipher.findEncryptedRegion(file);
      if (region === null || region.text !== file)
        continue;
      try {
        const name = (await cf.decrypt(Buffer.from(file))).toString('utf-8');
        console.log(`${file} -> ${name}`);
      } catch (err) {
        console.error('Error during decryption:', err.message);
      }
    }
  } catch (err) {
      console.error('Error during listing files:', err.message);
      process.exit(1);
  }
}
