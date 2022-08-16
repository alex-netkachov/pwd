using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace pwd.ciphers;

public interface ICipher
{
   /// <summary>Encrypts the string and saves it into the stream.</summary>
   /// <remarks>Does not close the stream.</remarks>
   /// <returns>Number of bytes written to the stream.</returns>
   int Encrypt(
      string text,
      Stream stream);

   /// <summary>Encrypts the string and saves it into the stream.</summary>
   /// <remarks>Does not close the stream.</remarks>
   /// <returns>Number of bytes written to the stream.</returns>
   Task<int> EncryptAsync(
      string text,
      Stream stream);

   /// <summary>Decrypts content of the stream into a string.</summary>
   /// <remarks>Does not close the stream.</remarks>
   (bool Success, string Text) DecryptString(
      Stream stream);

   /// <summary>Decrypts content of the stream into a string.</summary>
   /// <remarks>Does not close the stream.</remarks>
   Task<(bool Success, string Text)> DecryptStringAsync(
      Stream stream);
}

public static class CipherExtensions
{
   public static byte[] Encrypt(
      this ICipher cipher,
      string text)
   {
      using var stream = new MemoryStream();
      cipher.Encrypt(text, stream);
      return stream.ToArray();
   }

   public static async Task<byte[]> EncryptAsync(
      this ICipher cipher,
      string text)
   {
      using var stream = new MemoryStream();
      await cipher.EncryptAsync(text, stream);
      return stream.ToArray();
   }

   public static Task<(bool Success, string Text)> DecryptStringAsync(
      this ICipher cipher,
      byte[] data)
   {
      using var stream = new MemoryStream(data);
      return cipher.DecryptStringAsync(stream);
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
         throw new Exception("Cannot create AES encryption object.");

      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;

      // 10000 and SHA256 are defaults for pbkdf2 in openssl
      using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);

      aes.Key = rfc2898.GetBytes(32);
      aes.IV = rfc2898.GetBytes(16);

      return aes;
   }
}
