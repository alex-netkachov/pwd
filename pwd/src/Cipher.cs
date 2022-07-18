using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd;

public interface ICipher
{
    /// <summary>Checks whether the stream contains encrypted data.</summary>
    /// <remarks>Does not close the stream.</remarks>
    Task<bool> IsEncrypted(
        Stream stream);

    /// <summary>Encrypts the string and saves it into the stream.</summary>
    /// <remarks>Does not close the stream.</remarks>
    /// <returns>Number of bytes written to the stream.</returns>
    Task<int> Encrypt(
        string text,
        Stream stream);

    /// <summary>Decrypts content of the stream into a string.</summary>
    /// <remarks>Does not close the stream. Throws an exception when the steam's content is not encrypted.</remarks>
    Task<string> Decrypt(
        Stream stream);
}

public sealed class Cipher
    : ICipher
{
    /// <summary>"Salted__" encoded in ASCII (UTF8).</summary>
    private static readonly byte[] Salted = {0x53, 0x61, 0x6c, 0x74, 0x65, 0x64, 0x5f, 0x5f};

    private readonly string _password;

    public Cipher(
        string password)
    {
        _password = password;
    }
    
    public async Task<bool> IsEncrypted(
        Stream stream)
    {
        var octet = await stream.ReadBytesAsync(8);
        return octet.Length == Salted.Length &&
               !Salted.Where((t, i) => octet[i] != t).Any();
    }

    public async Task<int> Encrypt(
        string text,
        Stream stream)
    {
        await stream.WriteAsync(Salted);

        var salt = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        await stream.WriteAsync(salt);

        using var aes = CreateAes(salt);
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        await using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write, true);
        var data = Encoding.UTF8.GetBytes(text);
        await cryptoStream.WriteAsync(data);

        return Salted.Length + salt.Length + data.Length;
    }

    public async Task<string> Decrypt(
        Stream stream)
    {
        var octet = await stream.ReadBytesAsync(8);
        if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
            throw new FormatException($"The data stream is not encrypted.");

        using var aes = CreateAes(await stream.ReadBytesAsync(8));
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        await using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
        using var reader = new StreamReader(cryptoStream);
        return await reader.ReadToEndAsync();
    }

    private Aes CreateAes(
        byte[] salt)
    {
        var aes = Aes.Create();

        if (aes == null)
            throw new Exception("Cannot create AES encryption object.");

        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // 10000 and SHA256 are defaults for pbkdf2 in openssl
        using var rfc2898 = new Rfc2898DeriveBytes(_password, salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = rfc2898.GetBytes(32);
        aes.IV = rfc2898.GetBytes(16);

        return aes;
    }
}