'use strict';

const crypto = require('crypto');

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
  const salt = input.subarray(8, 16);
  const derived = await pbkdf2(password, salt, 10000, 48, 'sha256');
  const [ key, iv ] = [ derived.subarray(0, 32), derived.subarray(32, 48) ];
  const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
  const encrypted = input.subarray(16);
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
  throw new Error('Not implemented.');
}

function isEncrypted(input) {
  throw new Error('Not implemented.');
}

function findEncryptedRegion(input, offset) {
  throw new Error('Not implemented.');
}

function cipher(password) {
  return { decrypt : input => decrypt(password, input),
           encrypt : input => encrypt(password, input) };
}

cipher.isEncrypted = isEncrypted;
cipher.findEncryptedRegion = findEncryptedRegion;

module.exports = cipher;
