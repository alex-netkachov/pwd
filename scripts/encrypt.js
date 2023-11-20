'use strict';

const $ =
  { fs : require('fs'),
    fsp : require('fs/promises'),
    readPassword : require('./lib/readPassword'),
    cipher : require('./lib/cipher') };

main().catch(error => { console.error(error); process.exit(1); });

/** Prints the encrypted contents of the file. */
async function main() {
  const [ inFile, outFile ] = process.argv.slice(2, 3);
  if (!inFile || !outFile)
      throw new Error('Usage: node encrypt.js <in_file> <out_file>');
  if (!$.fs.existsSync(inFile))
    throw new Error('Input file does not exist.');
  if ($.fs.existsSync(outFile))
    throw new Error('Output file exists.');

  const password = await $.readPassword('Password: ');
  if (!password)
    throw new Error('Password cannot be empty.');

  const confirmedPassword = await $.readPassword('Confirm password: ');
  if (confirmedPassword !== password)
    throw new Error('Passwords do not match.');

  const input = await $.fsp.readFile(inFile);
  const encrypted = await $.cipher(password).encrypt(input);
  await fsp.writeFile(outFile, encrypted);
  console.log('Encrypted file written to ' + outFile);
}
