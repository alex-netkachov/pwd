using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pwd;

public interface ICipher
{
    byte[] Encrypt(
        string text);

    string Decrypt(
        byte[] data);
}

public sealed class Cipher
    : ICipher
{
    private readonly string _password;

    public Cipher(
        string password)
    {
        _password = password;
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

    public byte[] Encrypt(
        string text)
    {
        using var stream = new MemoryStream();

        stream.Write(Salted.Bytes);

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[8];
        rng.GetBytes(salt);
        stream.Write(salt);

        var data = Encoding.UTF8.GetBytes(text);
        using (var aes = CreateAes(salt))
        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(data, 0, data.Length);
        }

        return stream.ToArray();
    }

    public string Decrypt(
        byte[] data)
    {
        using var stream = new MemoryStream(data);

        if (!Salted.Equals(stream.ReadBytes(8)))
            throw new FormatException($"Expecting the data stream to begin with {Salted.Text}.");

        using var aes = CreateAes(stream.ReadBytes(8));
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }
}