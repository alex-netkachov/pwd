using System.Text;
using pwd.core.extensions;

namespace pwd.core.abstractions;

/// <summary>
///   Creates cipher with optional initialisation data.
/// </summary>
public delegate ICipher CipherFactory(
   string password,
   byte[]? initialisationData = null);

/// <summary>
///   Provides methods for encrypting and decrypting data streams.
/// </summary>
/// <remarks>
///   Implementers are responsible for ensuring that methods are thread-safe if instances
///   of implementations are expected to be called from multiple threads.
///
///   The methods do not close or flush the output streams.
///
///   The encryption is deterministic, meaning that the same input will always produce
///   the same output.
/// </remarks>
public interface ICipher
   : IDisposable
{
   /// <summary>Writes initialisation data into the specified stream.</summary>
   /// <remarks>
   ///   These initialisation data is supposed to be provided to a cipher's
   ///   constructor. Two ciphers initialised with the same data should produce
   ///   the same ciphertext for the same input.
   /// </remarks>
   byte[] GetInitialisationData();

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

public static class CipherExtensions
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
