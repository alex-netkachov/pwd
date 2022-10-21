using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.ciphers;

public interface ICipher
{
   /// <summary>Encrypts the string and saves it into the stream.</summary>
   /// <remarks>Does not close nor flush the stream.</remarks>
   /// <returns>Number of bytes written to the stream.</returns>
   int EncryptString(
      string text,
      Stream stream);

   /// <summary>Encrypts the string and saves it into the stream.</summary>
   /// <remarks>
   ///    Does not close nor flush the stream. Cancelling the task may result with only part of the encrypted
   ///    text written to the stream.
   /// </remarks>
   /// <returns>Number of bytes written to the stream.</returns>
   Task<int> EncryptStringAsync(
      string text,
      Stream stream,
      CancellationToken cancellationToken = default);

   /// <summary>Decrypts content of the stream into a string.</summary>
   /// <remarks>Does not close the stream. Throws an exception when the stream encoding is not UTF-8.</remarks>
   string DecryptString(
      Stream stream);

   /// <summary>Decrypts content of the stream into a string.</summary>
   /// <remarks>Does not close the stream. Throws an exception when the stream encoding is not UTF-8.</remarks>
   Task<string> DecryptStringAsync(
      Stream stream,
      CancellationToken cancellationToken = default);
}

public static class CipherExtensions
{
   public static byte[] Encrypt(
      this ICipher cipher,
      string text)
   {
      using var stream = new MemoryStream();
      cipher.EncryptString(text, stream);
      return stream.ToArray();
   }

   public static async Task<byte[]> EncryptAsync(
      this ICipher cipher,
      string text,
      CancellationToken cancellationToken = default)
   {
      using var stream = new MemoryStream();
      await cipher.EncryptStringAsync(text, stream, cancellationToken);
      return stream.ToArray();
   }

   public static string DecryptString(
      this ICipher cipher,
      byte[] data)
   {
      using var stream = new MemoryStream(data);
      return cipher.DecryptString(stream);
   }

   public static Task<string> DecryptStringAsync(
      this ICipher cipher,
      byte[] data,
      CancellationToken cancellationToken = default)
   {
      using var stream = new MemoryStream(data);
      return cipher.DecryptStringAsync(stream, cancellationToken);
   }
}

internal static class CipherShared
{
   public static Aes CreateAes(
      string password,
      byte[] salt)
   {
      var aes = Aes.Create();

      if (aes == null)
         throw new("Cannot create AES encryption object.");

      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;

      // 10000 and SHA256 are defaults for pbkdf2 in openssl
      using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);

      aes.Key = rfc2898.GetBytes(32);
      aes.IV = rfc2898.GetBytes(16);

      return aes;
   }
}