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

/** Lists the encrypted files. */
async function main() {
  let path = process.argv[2];
  path = !!path ? path : '.';
  if (!$.fs.existsSync(path))
    throw new Error('Folder does not exist.');

  const password = await $.readPassword('Password: ');
  if (!password)
    throw new Error('Password cannot be empty.');

  const cf = $.cipher(password);
  const input = await $.fsp.readdir(path);
  for (const file of input) {
    const region = $.cipher.findEncryptedRegion(file);
    if (region === null || region.text !== file)
      continue;
    const name = (await cf.decrypt(Buffer.from(file))).toString('utf-8');
    console.log(`${file} : ${name}`);
  }
}
