using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd.ciphers;

public interface IContentCipher
   : ICipher
{
}

public interface IContentCipherFactory
{
   IContentCipher Create(
      string password);
}

public sealed class ContentCipher
   : IContentCipher
{
   /// <summary>"Salted__" encoded in ASCII (UTF8).</summary>
   private static readonly byte[] Salted = {0x53, 0x61, 0x6c, 0x74, 0x65, 0x64, 0x5f, 0x5f};

   private static readonly int SaltSize = 8;

   private readonly string _password;

   public ContentCipher(
      string password)
   {
      _password = password;
   }

   public int Encrypt(
      string text,
      Stream stream)
   {
      stream.Write(Salted);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      stream.Write(salt);

      using var aes = CipherShared.CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
      using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write, true);
      var data = Encoding.UTF8.GetBytes(text);
      cryptoStream.Write(data);

      return Salted.Length + salt.Length + data.Length;
   }

   public async Task<int> EncryptAsync(
      string text,
      Stream stream)
   {
      await stream.WriteAsync(Salted);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      await stream.WriteAsync(salt);

      using var aes = CipherShared.CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
      await using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write, true);
      var data = Encoding.UTF8.GetBytes(text);
      await cryptoStream.WriteAsync(data);

      return Salted.Length + salt.Length + data.Length;
   }

   public (bool Success, string Text) DecryptString(
      Stream stream)
   {
      var octet = stream.ReadBytes(Salted.Length);
      if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
         return (false, "");

      var salt = stream.ReadBytes(SaltSize);
      if (salt.Length != SaltSize)
         return (false, "");

      using var aes = CipherShared.CreateAes(_password, salt);
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
      using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
      var data = cryptoStream.ReadAllBytes();
      return (true, Encoding.UTF8.GetString(data));
   }

   public async Task<(bool Success, string Text)> DecryptStringAsync(
      Stream stream)
   {
      var octet = await stream.ReadBytesAsync(8);
      if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
         throw new FormatException("The data stream is not encrypted.");

      using var aes = CipherShared.CreateAes(_password, await stream.ReadBytesAsync(8));
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
      await using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
      var data = await cryptoStream.ReadAllBytesAsync();
      return (true, Encoding.UTF8.GetString(data));
   }
}

public sealed class ContentCipherFactory
   : IContentCipherFactory
{
   public IContentCipher Create(
      string password)
   {
      return new ContentCipher(password);
   }
}
