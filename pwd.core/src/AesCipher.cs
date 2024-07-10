using System.Security.Cryptography;
using pwd.core.abstractions;

namespace pwd.core;

public sealed class AesInitialisationData
{
   private const int InitialisationVectorSize = 16;
   private const int SaltSize = 8;

   private readonly byte[] _salt;
   private readonly byte[] _initialisationVector;

   public static AesInitialisationData Zero =
      new(
         new byte[SaltSize],
         new byte[InitialisationVectorSize]);
   
   private AesInitialisationData(
      byte[] salt,
      byte[] initialisationVector)
   {
      _salt = salt;
      _initialisationVector = initialisationVector;
   }

   public ReadOnlySpan<byte> Salt => _salt.AsSpan();
   
   public ReadOnlySpan<byte> InitialisationVector => _initialisationVector.AsSpan();
   
   public byte[] ToArray()
   {
      var data = new byte[SaltSize + InitialisationVectorSize];
      Array.Copy(_salt, data, SaltSize);
      Array.Copy(_initialisationVector, 0, data, SaltSize, InitialisationVectorSize);
      return data;
   }

   public static AesInitialisationData FromArray(
      byte[] data)
   {
      if (data.Length != SaltSize + InitialisationVectorSize)
         throw new ArgumentException("Invalid length of the initialisation data.", nameof(data));

      var salt = new byte[SaltSize];
      var initialisationVector = new byte[InitialisationVectorSize];
      Array.Copy(data, salt, SaltSize);
      Array.Copy(data, SaltSize, initialisationVector, 0, InitialisationVectorSize);

      return new AesInitialisationData(salt, initialisationVector);
   }
   
   public static AesInitialisationData Random()
   {
      using var rng = RandomNumberGenerator.Create();
      var salt = new byte[SaltSize];
      var initialisationVector = new byte[InitialisationVectorSize];
      rng.GetBytes(salt);
      rng.GetBytes(initialisationVector);
      return new AesInitialisationData(salt, initialisationVector);
   }
}

/// <summary>
///   Defines an encryption and decryption provider that uses
///   AES encryption for securing content within data streams.
/// </summary>
public sealed class AesCipher
   : ICipher
{
   private const int InitialisationVectorSize = 16;
   private const int KeySize = 32;
   private const int SaltSize = 8;

   private readonly byte[] _salt = new byte[SaltSize];
   private readonly byte[] _initialisationVector = new byte[InitialisationVectorSize];
   private readonly Lazy<byte[]> _key;

   /// <summary>
   ///  Initializes a new instance of the AesCipher class with the password.
   /// </summary>
   public AesCipher(
      string password,
      byte[]? initialisationData = null)
   {
      _key =
         new(() =>
         {
            // SHA256 are defaults for pbkdf2 in openssl
            // 600000 is a recommended number of iterations
            using var rfc2898 =
               new Rfc2898DeriveBytes(
                  password,
                  _salt,
                  600000,
                  HashAlgorithmName.SHA256);

            return rfc2898.GetBytes(KeySize);
         });
      
      ApplyInitialisationData(
         initialisationData
         ?? AesInitialisationData.Random().ToArray());
   }
   
   /// <summary>
   ///  Initializes a new instance of the AesCipher class with the key.
   /// </summary>
   public AesCipher(
      byte[] key,
      byte[]? initialisationData = null)
   {
      if (key.Length != KeySize)
         throw new ArgumentException("Invalid length of the key.", nameof(key));

      _key = new(() => key);

      ApplyInitialisationData(
         initialisationData
         ?? AesInitialisationData.Random().ToArray());
   }

   private void ApplyInitialisationData(
      byte[] initialisationData)
   {
      var data = AesInitialisationData.FromArray(initialisationData);
      Array.Copy(data.Salt.ToArray(), _salt, SaltSize);
      Array.Copy(data.InitialisationVector.ToArray(), _initialisationVector, InitialisationVectorSize);
   }

   public byte[] GetInitialisationData()
   {
      var data = new byte[SaltSize + InitialisationVectorSize];
      Array.Copy(_salt, data, SaltSize);
      Array.Copy(_initialisationVector, 0, data, SaltSize, InitialisationVectorSize);
      return data;
   }

   public void Encrypt(
      Stream input,
      Stream output)
   {
      VerifyStreams(input, output);
      
      using var aes = CreateAes();
      using var encryptor = aes.CreateEncryptor();
      
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
      CancellationToken token = default)
   {
      VerifyStreams(input, output);

      using var aes = CreateAes();
      using var encryptor = aes.CreateEncryptor();

      await using var cryptoStream =
         new CryptoStream(
            output,
            encryptor,
            CryptoStreamMode.Write,
            leaveOpen: true);

      await input.CopyToAsync(cryptoStream, token);

      await cryptoStream.FlushAsync(token);

      if (cryptoStream.HasFlushedFinalBlock == false)
         await cryptoStream.FlushFinalBlockAsync(token);
   }

   public void Decrypt(
      Stream input,
      Stream output)
   {
      VerifyStreams(input, output);

      using var aes = CreateAes();
      using var decryptor = aes.CreateDecryptor();

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
      CancellationToken token = default)
   {
      VerifyStreams(input, output);

      using var aes = CreateAes();
      using var decryptor = aes.CreateDecryptor();

      await using var cryptoStream =
         new CryptoStream(
            input,
            decryptor,
            CryptoStreamMode.Read,
            leaveOpen: true);

      await cryptoStream.CopyToAsync(output, token);
   }

   private static void VerifyStreams(
      Stream input,
      Stream output)
   {
      if (!input.CanRead)
      {
         throw new ArgumentException(
            "Input stream must not be null and should be readable.",
            nameof(input));
      }

      if (!output.CanWrite)
      {
         throw new ArgumentException(
            "Output stream must not be null and should be writable.",
            nameof(output));
      }
   }

   private Aes CreateAes()
   {
      var aes =
         Aes.Create()
         ?? throw new("Cannot create AES encryption object.");

      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;
      aes.Key = _key.Value;
      aes.IV = _initialisationVector;

      return aes;
   }

   public void Dispose()
   {
      // do nothing
   }
}
