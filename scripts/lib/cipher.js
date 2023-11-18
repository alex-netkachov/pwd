'use strict';

const crypto = require('crypto');

const base64Url = {
  encode : buffer => buffer.toString('base64').replace(/\//g, '_').replace(/\+/g, '-'),
  decode : string => Buffer.from(string.replace(/_/g, '/').replace(/\-/g, '+'), 'base64')
};

function pbkdf2(password, salt, iterations, keylen, digest) {
  return new Promise((resolve, reject) =>
    crypto.pbkdf2(password, salt, iterations, keylen, digest,
      (err, key) => err ? reject(err) : resolve(key)));
}


/**
 * Decrypts the input using the password.
 *
 * @param {string} password 
 * @param {Buffer} input 
 * @returns Buffer
 */
async function decrypt(password, input) {
  if (!Buffer.isBuffer(input)) throw new Error("input must be a Buffer");
  const encoded = input.toString('ascii');
  const data = base64Url.decode(encoded);
  const salt = data.subarray(0, 8);
  const derived = await pbkdf2(password, salt, 600000, 48, 'sha256');
  const [ key, iv ] = [ derived.subarray(0, 32), derived.subarray(32, 48) ];
  const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
  const encrypted = data.subarray(8);
  return Buffer.concat([decipher.update(encrypted), decipher.final()]);
}

/**
 * Encrypts the input using the password.
 *
 * @param {string} password 
 * @param {Buffer} input 
 * @returns Buffer
 */
async function encrypt(password, input) {
  if (!Buffer.isBuffer(input)) throw new Error("input must be a Buffer");
  const salt = crypto.randomBytes(8);
  const derived = await pbkdf2(password, salt, 600000, 48, 'sha256');
  const [ key, iv ] = [ derived.subarray(0, 32), derived.subarray(32, 48) ];
  const cipher = crypto.createCipheriv('aes-256-cbc', key, iv);
  const encrypted = Buffer.concat([cipher.update(input), cipher.final()]);
  const encoded = base64Url.encode(Buffer.concat([salt, encrypted]));
  return Buffer.from(encoded, 'ascii');
}

function isEncrypted(input) {
  return /^[\w\-]+=*$/.test(input)
         && (input.length === 32
             || input.length === 54
             || input.length === 75
             || input.length > 75);
}

function findEncryptedRegion(input, offset) {
  offset = offset || 0;

  const content = input.substring(offset);

  const items = content.split(/([\w\-]+=*)/g);
  if (!items)
    return null;

  let position = offset;
  for (let index = 0; index < items.length; index++) {
    const text = items[index];

    if (index % 2 === 0) {
      position += text.length;
      continue;
    }

    const start = position;
    const end = start + text.length;

    const padding = (text.match(/=*$/)[0] || '').length;

    const tailLength = (text.length - 32) % 64;
  
    const found =
      (tailLength === 0 && padding === 0)
      || (tailLength === 24 && padding === 2)
      || (tailLength === 44 && padding === 1);

    if (!found) {
      position += text.length;
      continue;
    }
  
    return { start, end, text };
  }

  return null;
}

function cipher(password) {
  return { decrypt : input => decrypt(password, input),
           encrypt : input => encrypt(password, input) };
}

cipher.isEncrypted = isEncrypted;
cipher.findEncryptedRegion = findEncryptedRegion;

module.exports = cipher;
