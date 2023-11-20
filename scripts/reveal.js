'use strict';

const $ =
  { fs : require('fs'),
    fsp : require('fs/promises'),
    readPassword : require('./lib/readPassword'),
    cipher : require('./lib/cipher') };

main().catch(error => { console.error(error); process.exit(1); });

/** Prints the decrypted contents of the file. */
async function main() {
  const inFile = process.argv[2];
  if (!inFile)
    throw new Error('Usage: node reveal.js <in_file>');
  if (!$.fs.existsSync(inFile))
    throw new Error('Input file does not exist.');

  const password = await $.readPassword('Password: ');
  if (!password)
    throw new Error('Password cannot be empty.');

  let cipher = $.cipher(password);
  let text = await $.fsp.readFile(inFile, 'utf-8');
  let region = $.cipher.findEncryptedRegion(text);
  while (region !== null) {
    const decrypted = (await cipher.decrypt(Buffer.from(region.text))).toString('utf-8');
    text = text.substring(0, region.start) + decrypted + text.substring(region.end);
    region = $.cipher.findEncryptedRegion(text, region.start + decrypted.length);
  }
  console.log(text);
}
