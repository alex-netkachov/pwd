using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
   private const int SaltSize = 8;

   /// <summary>"Salted__" encoded in ASCII (UTF8).</summary>
   private static readonly byte[] Salted = { 0x53, 0x61, 0x6c, 0x74, 0x65, 0x64, 0x5f, 0x5f };

   private readonly string _password;

   public ContentCipher(
      string password)
   {
      _password = password;
   }

   public int EncryptString(
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

   public async Task<int> EncryptStringAsync(
      string text,
      Stream stream,
      CancellationToken cancellationToken = default)
   {
      await stream.WriteAsync(Salted, cancellationToken);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      await stream.WriteAsync(salt, cancellationToken);

      using var aes = CipherShared.CreateAes(_password, salt);
      using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
      await using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write, true);
      var data = Encoding.UTF8.GetBytes(text);
      await cryptoStream.WriteAsync(data, cancellationToken);

      return Salted.Length + salt.Length + data.Length;
   }

   public string DecryptString(
      Stream stream)
   {
      var octet = stream.ReadBytes(Salted.Length);
      if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
         throw ThrowNotEncryptedException();

      var salt = stream.ReadBytes(SaltSize);
      if (salt.Length != SaltSize)
         throw ThrowNotEncryptedException();

      using var aes = CipherShared.CreateAes(_password, salt);
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
      using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
      var data = cryptoStream.ReadAllBytes();

      if (!TextExtensions.IsUtf8(data))
         throw ThrowNotEncryptedException();

      return Encoding.UTF8.GetString(data);
   }

   public async Task<string> DecryptStringAsync(
      Stream stream,
      CancellationToken cancellationToken = default)
   {
      var octet = await stream.ReadBytesAsync(8, cancellationToken);
      if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
         throw ThrowNotEncryptedException();

      using var aes = CipherShared.CreateAes(_password, await stream.ReadBytesAsync(8, cancellationToken));
      using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
      await using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
      var data = await cryptoStream.ReadAllBytesAsync(cancellationToken);

      if (!TextExtensions.IsUtf8(data))
         throw ThrowNotEncryptedException();

      return Encoding.UTF8.GetString(data);
   }

   private static Exception ThrowNotEncryptedException()
   {
      return new("The data stream does not contain an encrypted string.");
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