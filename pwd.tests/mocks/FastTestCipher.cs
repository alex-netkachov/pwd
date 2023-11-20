using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd.mocks;

public sealed class FastTestCipherFactory
   : ICipherFactory
{
   public ICipher Create(string password)
      => FastTestCipher.Instance;
}

public sealed class FastTestCipher
   : ICipher
{
   private const string Password = "pwd$";

   public static readonly FastTestCipher Instance = new();

   /// <summary>Size of the salt in bytes.</summary>
   private const int SaltSize = 8;

   public void Encrypt(
      Stream input,
      Stream output)
   {
      VerifyStreams(input, output);

      var salt = new byte[8];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      output.Write(salt);

      using var aes = CreateAes(Password, salt);
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

      using var aes = CreateAes(Password, salt);
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

      using var aes = CreateAes(Password, salt);
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

      using var aes = CreateAes(Password, salt);
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

      // pbkdf2 openssl defaults: SHA256, 10,000 iterations
      // 10K iterations is not a lot, but it's fast enough for tests
      using var rfc2898 =
         new Rfc2898DeriveBytes(
            password,
            salt,
            10000,
            HashAlgorithmName.SHA256);

      aes.Key = rfc2898.GetBytes(32);
      aes.IV = rfc2898.GetBytes(16);

      return aes;
   }
}
