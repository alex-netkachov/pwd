using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd.ciphers;

public interface INameCipher
   : ICipher
{
}

public interface INameCipherFactory
{
   INameCipher Create(
      byte[] password);
}

public sealed class NameCipher
   : INameCipher
{
   private readonly byte[] _password;

   public NameCipher(
      byte[] password)
   {
      _password = password;
   }

   public int Encrypt(
      string text,
      Stream stream)
   {
      using var dataStream = new MemoryStream();

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      dataStream.Write(salt);

      using var aes = CipherShared.CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
      using var cryptoStream = new CryptoStream(dataStream, encryptor, CryptoStreamMode.Write, true);
      var data = Encoding.UTF8.GetBytes(text);
      cryptoStream.Write(data);
      cryptoStream.FlushFinalBlock();

      var encryptedBase64Name = Convert.ToBase64String(dataStream.ToArray());
      var encryptedFileName = ToFileName(encryptedBase64Name);
      var base64 = Encoding.UTF8.GetBytes(encryptedFileName);
      stream.Write(base64);

      return base64.Length;
   }

   public async Task<int> EncryptAsync(
      string text,
      Stream stream)
   {
      using var dataStream = new MemoryStream();

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      await dataStream.WriteAsync(salt);

      using var aes = CipherShared.CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
      await using var cryptoStream = new CryptoStream(dataStream, encryptor, CryptoStreamMode.Write, true);
      var data = Encoding.UTF8.GetBytes(text);
      await cryptoStream.WriteAsync(data);
      await cryptoStream.FlushFinalBlockAsync();

      var encryptedBase64Name = Convert.ToBase64String(dataStream.ToArray());
      var encryptedFileName = ToFileName(encryptedBase64Name);
      var base64 = Encoding.UTF8.GetBytes(encryptedFileName);
      await stream.WriteAsync(base64);

      return base64.Length;
   }

   public (bool Success, string Text) DecryptString(
      Stream stream)
   {
      try
      {
         using var reader = new StreamReader(stream, leaveOpen: true);
         var encryptedFileName = reader.ReadToEnd();
         if (string.IsNullOrEmpty(encryptedFileName))
            return (false, "");
            
         var encryptedBase64Name = ToBase64(encryptedFileName);
         var encryptedName = Convert.FromBase64String(encryptedBase64Name);
         
         // the data is an encrypted name when it is AES-encrypted 16-byte size blocks with 8 bytes salt, 
         // i.e. length(concat(salt, join(aes_blocks)))
         if (encryptedName.Length < 8 || 0 != (encryptedName.Length - 8) % 16)
            return (false, "");
         
         var encryptedNameStream = new MemoryStream(encryptedName);

         var salt = encryptedNameStream.ReadBytes(8);

         using var aes = CipherShared.CreateAes(_password, salt);

         using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

         using var cryptoStream =
            new CryptoStream(
               encryptedNameStream,
               decryptor,
               CryptoStreamMode.Read,
               true);

         var data = cryptoStream.ReadAllBytes();

         return TextExtensions.IsUtf8(data) switch
         {
            true => (true, Encoding.UTF8.GetString(data)),
            false => (false, "")
         };
      }
      catch
      {
         throw new FormatException("The data stream is not encrypted name.");
      }
   }

   public async Task<(bool Success, string Text)> DecryptStringAsync(
      Stream stream)
   {
      try
      {
         using var reader = new StreamReader(stream, leaveOpen: true);
         var encryptedFileName = await reader.ReadToEndAsync();
         var encryptedBase64Name = ToBase64(encryptedFileName);
         var encryptedName = Convert.FromBase64String(encryptedBase64Name);

         // the data is an encrypted name when it is AES-encrypted 16-byte size blocks with 8 bytes salt, 
         // i.e. length(concat(salt, join(aes_blocks)))
         if (encryptedName.Length < 8 || 0 != (encryptedName.Length - 8) % 16)
            return (false, "");

         var encryptedNameStream = new MemoryStream(encryptedName);

         var salt = await encryptedNameStream.ReadBytesAsync(8);

         using var aes = CipherShared.CreateAes(_password, salt);

         using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

         await using var cryptoStream =
            new CryptoStream(
               encryptedNameStream,
               decryptor,
               CryptoStreamMode.Read,
               true);

         var data = await cryptoStream.ReadAllBytesAsync();

         return TextExtensions.IsUtf8(data) switch
         {
            true => (true, Encoding.UTF8.GetString(data)),
            false => (false, "")
         };
      }
      catch
      {
         return (false, "");
      }
   }

   /// <summary>Converts a base64-encoded string to the file name.</summary>
   /// <remarks>'/' is an invalid character for the file name, '=' wraps file names in quotes in bash.</remarks>
   private static string ToFileName(
      string base64Encoded)
   {
      return base64Encoded.Replace('/', '_').Replace('=', '~');
   }

   /// <summary>Converts a file name to the base64-encoded string.</summary>
   private static string ToBase64(
      string fileName)
   {
      return fileName.Replace('_', '/').Replace('~', '=');
   }
}

public sealed class NameCipherFactory
   : INameCipherFactory
{
   public INameCipher Create(
      byte[] password)
   {
      return new NameCipher(password);
   }
}