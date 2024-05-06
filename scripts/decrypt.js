'use strict';

const $ =
  { fs : require('fs'),
    fsp : require('fs/promises'),
    readPassword : require('./lib/readPassword'),
    cipher : require('./lib/cipher') };

main().catch(error => {
  console.error(error);
  process.exit(1);
});

/** Prints the decrypted contents of the file. */
async function main() {
  const inFile = process.argv[2];
  if (!inFile)
    throw new Error('Usage: node decrypt.js <in_file>');
  if (!$.fs.existsSync(inFile))
    throw new Error('Input file does not exist.');

  const password = await $.readPassword('Password: ');
  if (!password)
    throw new Error('Password cannot be empty.');

  const input = await $.fsp.readFile(inFile);
  const decrypted = (await $.cipher(password).decrypt(input)).toString('utf-8');
  console.log(decrypted);
}
