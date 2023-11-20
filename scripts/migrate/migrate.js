/**
 * Migrates repository to a new format.
 *
 * Usage:
 *
 * ```
 * node migrate.js source destination
 * ```
 */

'use strict';

const mods =
  { fs : require('fs'),
    fsp : require('fs/promises'),
    path : require('path'),
    readPassword : require('../lib/readPassword'),
    cipher : require('../lib/cipher'),
    contentCipher : require('./cipher.old.content'),
    nameCipher : require('./cipher.old.name') };

main().catch(error => console.error(error));

async function main() {
  const source = process.argv[2];
  if (!source) {
      console.error('Usage: node migrate.js <source> <destination>');
      console.error('Example: node migrate.js . ..\\new');
      process.exit(1);
  }
  if (!mods.fs.existsSync(source)) {
      console.error('Source location does not exist.');
      process.exit(1);
  }

  const destination = process.argv[3];
  if (!destination) {
      console.error('Usage: node migrate.js <source> <destination>');
      console.error('Example: node migrate.js . ..\\new');
      process.exit(1);
  }
  if (!mods.fs.existsSync(destination)) {
      console.error('Destination location does not exist.');
      process.exit(1);
  }

  const password = await mods.readPassword('Password: ');
  if (!password) {
    console.error('Password cannot be empty.');
    process.exit(1);
  }

  const cipher = mods.cipher(password);
  const nameCipher = mods.nameCipher(password);
  const contentCipher = mods.contentCipher(password);

  const folders = {};

  const entries = await mods.fsp.readdir(source, { withFileTypes: true, recursive: true });
  for (const item of entries) {
    if (mods.cipher.findEncryptedRegion(item.name) === null)
      continue;

    try {
      const sourcePath = mods.path.join(source, item.path, item.name);
      const encrypted = await mods.fsp.readFile(sourcePath);
      const decryptedContent = await contentCipher.decrypt(encrypted);
      const decryptedName = await nameCipher.decrypt(Buffer.from(item.name));

      let destinationPath = '';
      if (item.path === '.') {
        destinationPath = destination;
      } else if (mods.nameCipher.findEncryptedRegion(item.path) === null) {
        console.log(item.path, mods.cipher.findEncryptedRegion(item.path));
        destinationPath = mods.path.join(destination, item.path);
        await mods.fsp.mkdir(destinationPath, { recursive: true });
      } else {
        if (folders[item.path] === undefined) {
          const decryptedPath = await nameCipher.decrypt(Buffer.from(item.path));
          const encryptedPath = (await cipher.encrypt(Buffer.from(decryptedPath))).toString('ascii');
          folders[item.path] = encryptedPath;
          await mods.fsp.mkdir(mods.path.join(destination, encryptedPath), { recursive: true });
        }
        destinationPath = mods.path.join(destination, folders[item.path]);
      }

      const newName = (await cipher.encrypt(Buffer.from(decryptedName))).toString('ascii');

      console.log(mods.path.join(item.path, item.name), '->', mods.path.join(destinationPath, newName));

      const newContent = (await cipher.encrypt(Buffer.from(decryptedContent))).toString('ascii');
      await mods.fsp.writeFile(mods.path.join(destinationPath, newName), newContent);
    } catch (err) {
        console.error('Error during listing files:', err.message);
        process.exit(1);
    }
  }
}
