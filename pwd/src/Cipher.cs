using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd;

/// <summary>
///   Provides methods for encrypting and decrypting data streams.
/// </summary>
/// <remarks>
///   Implementers are responsible for ensuring that methods are thread-safe if instances
///   of implementations are expected to be called from multiple threads.
///
///   The methods do not close or flush the output streams.
/// </remarks>
public interface ICipher
{
   /// <summary>
   ///   Encrypts the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   void Encrypt(
      Stream input,
      Stream output);

   /// <summary>
   ///   Encrypts the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>
   ///   Does not close or flush the output stream. Canceling the task may
   ///   result in only a portion of the data being encrypted and written
   ///   to the output stream.
   /// </remarks>
   Task EncryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default);

   /// <summary>
   ///   Decrypts the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   void Decrypt(
      Stream input,
      Stream output);

   /// <summary>
   ///   Decrypts the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   Task DecryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default);
}

internal static class CipherExtensions
{
   /// <summary>
   ///   Encrypts the specified text.
   /// </summary>
   /// <returns>A byte array containing the encrypted data.</returns>
   /// <remarks>
   ///   Converts the input text to a byte array using UTF8 encoding, then
   ///   encrypts the data using the Encrypt method of the ICipher interface.
   /// </remarks>
   public static byte[] Encrypt(
      this ICipher cipher,
      string input)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      cipher.Encrypt(inputStream, outputStream);
      return outputStream.ToArray();
   }

   /// <summary>
   ///   Encrypts the specified text.
   /// </summary>
   /// <returns>A byte array containing the encrypted data.</returns>
   /// <remarks>
   ///   Converts the input text to a byte array using UTF8 encoding, then
   ///   encrypts the data using the Encrypt method of the ICipher interface.
   /// </remarks>
   public static async Task<byte[]> EncryptAsync(
      this ICipher cipher,
      string input,
      CancellationToken cancellationToken = default)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      await cipher.EncryptAsync(inputStream, outputStream, cancellationToken);
      return outputStream.ToArray();
   }

   /// <summary>
   ///   Decrypts a byte array into a string.
   /// </summary>
   /// <returns>A string containing the decrypted data.</returns>
   /// <exception cref="System.ArgumentException">
   ///   Thrown when the decrypted data is not valid UTF-8.
   /// </exception>
   /// <remarks>
   ///   If the decrypted data is not valid UTF-8, an exception will be thrown, indicating
   ///   that the data may be corrupted or may have been encrypted with a different encoding.
   /// </remarks>
   public static string DecryptString(
      this ICipher cipher,
      byte[] data)
   {
      using var inputStream = data.AsStream();
      using var outputStream = new MemoryStream();
      cipher.Decrypt(inputStream, outputStream);

      var utf8WithException =
         new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

      return utf8WithException.GetString(outputStream.ToArray());
   }

   /// <summary>
   ///   Decrypts a byte array into a string.
   /// </summary>
   /// <returns>A string containing the decrypted data.</returns>
   /// <exception cref="System.ArgumentException">
   ///   Thrown when the decrypted data is not valid UTF-8.
   /// </exception>
   /// <remarks>
   ///   If the decrypted data is not valid UTF-8, an exception will be thrown, indicating
   ///   that the data may be corrupted or may have been encrypted with a different encoding.
   /// </remarks>
   public static async Task<string> DecryptStringAsync(
      this ICipher cipher,
      byte[] data,
      CancellationToken cancellationToken = default)
   {
      using var inputStream = data.AsStream();
      using var outputStream = new MemoryStream();
      await cipher.DecryptAsync(inputStream, outputStream, cancellationToken);

      var utf8WithException =
         new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

      return utf8WithException.GetString(outputStream.ToArray());
   }

   public static bool TryDecryptString(
      this ICipher cipher,
      byte[] data,
      out string output)
   {
      try
      {
         output = DecryptString(cipher, data);
         return true;
      }
      catch
      {
         output = string.Empty;
         return false;
      }
   }

   public static async Task<(bool, string?)> TryDecryptStringAsync(
      this ICipher cipher,
      byte[] data,
      CancellationToken token = default)
   {
      try
      {
         var output = await DecryptStringAsync(cipher, data, token);
         return (true, output);
      }
      catch
      {
         return (false, null);
      }
   }
}

public interface ICipherFactory
{
   ICipher Create(
      string password);
}

/// <summary>
///   Defines an encryption and decryption provider that uses
///   AES encryption for securing content within data streams.
/// </summary>
public sealed class Cipher
   : ICipher
{
   /// <summary>Size of the salt in bytes.</summary>
   private const int SaltSize = 8;

   private readonly string _password;

    /// <summary>
    ///  Initializes a new instance of the <see cref="Cipher"/> class
    ///  with the specified password.
    /// </summary>
   public Cipher(
      string password)
   {
      _password = password;
   }

   public void Encrypt(
      Stream input,
      Stream output)
   {
      VerifyStreams(input, output);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      output.Write(salt);

      using var aes = CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

      using var cryptoStream =
         new CryptoStream(
            output,
            encryptor,
            CryptoStreamMode.Write,
            leaveOpen: true);

      input.CopyTo(cryptoStream);

      cryptoStream.Flush();

      if (cryptoStream.HasFlushedFinalBlock == false)
         cryptoStream.FlushFinalBlock();
   }

   public async Task EncryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default)
   {
      VerifyStreams(input, output);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      await output.WriteAsync(salt, cancellationToken);

      using var aes = CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

      await using var cryptoStream =
         new CryptoStream(
            output,
            encryptor,
            CryptoStreamMode.Write,
            leaveOpen: true);

      await input.CopyToAsync(cryptoStream, cancellationToken);

      await cryptoStream.FlushAsync(cancellationToken);

      if (cryptoStream.HasFlushedFinalBlock == false)
         await cryptoStream.FlushFinalBlockAsync(cancellationToken);
   }

   public void Decrypt(
      Stream input,
      Stream output)
   {
      VerifyStreams(input, output);

      var salt = input.ReadBytes(SaltSize);
      if (salt.Length != SaltSize)
         throw new("Unexpected end of input.");

      using var aes = CreateAes(_password, salt);
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

      using var cryptoStream =
         new CryptoStream(
            input,
            decryptor,
            CryptoStreamMode.Read,
            leaveOpen: true);

      cryptoStream.CopyTo(output);
   }

   public async Task DecryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default)
   {
      VerifyStreams(input, output);

      var salt = await input.ReadBytesAsync(8, cancellationToken);
      if (salt.Length != SaltSize)
         throw new("Unexpected end of input.");

      using var aes = CreateAes(_password, salt);
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

      await using var cryptoStream =
         new CryptoStream(
            input,
            decryptor,
            CryptoStreamMode.Read,
            leaveOpen: true);

      await cryptoStream.CopyToAsync(output, cancellationToken);
   }

   private static void VerifyStreams(
      Stream input,
      Stream output)
   {
      if (input?.CanRead != true)
      {
         throw new ArgumentException(
            "Input stream must not be null and should be readable.",
            nameof(input));
      }

      if (output?.CanWrite != true)
      {
         throw new ArgumentException(
            "Output stream must not be null and should be writable.",
            nameof(output));
      }
   }

   private static Aes CreateAes(
      string password,
      byte[] salt)
   {
      var aes =
         Aes.Create()
         ?? throw new("Cannot create AES encryption object.");

        aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;

      // SHA256 are defaults for pbkdf2 in openssl
      // 600000 is a recommended number of iterations
      using var rfc2898 =
         new Rfc2898DeriveBytes(
            password,
            salt,
            600000,
            HashAlgorithmName.SHA256);

      aes.Key = rfc2898.GetBytes(32);
      aes.IV = rfc2898.GetBytes(16);

      return aes;
   }
}

public sealed class CipherFactory
   : ICipherFactory
{
   public ICipher Create(
      string password)
   {
      return new Cipher(password);
   }
}
