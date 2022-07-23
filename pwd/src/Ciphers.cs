using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace pwd;

public interface ICipher
{
    /// <summary>Checks whether the array contains encrypted data.</summary>
    /// <remarks>Does not close the stream.</remarks>
    bool IsEncrypted(
        Stream stream);

    /// <summary>Checks whether the stream contains encrypted data.</summary>
    /// <remarks>Does not close the stream.</remarks>
    Task<bool> IsEncryptedAsync(
        Stream stream);
    
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
    /// <remarks>Does not close the stream. Throws an exception when the steam's content is not encrypted.</remarks>
    string DecryptString(
        Stream stream);
    
    /// <summary>Decrypts content of the stream into a string.</summary>
    /// <remarks>Does not close the stream. Throws an exception when the steam's content is not encrypted.</remarks>
    Task<string> DecryptStringAsync(
        Stream stream);
}

public static class CipherExtensions
{
    public static bool IsEncrypted(
        this ICipher cipher,
        byte[] data)
    {
        using var stream = new MemoryStream(data);
        return cipher.IsEncrypted(stream);
    }
    
    public static Task<bool> IsEncryptedAsync(
        this ICipher cipher,
        byte[] data)
    {
        using var stream = new MemoryStream(data);
        return cipher.IsEncryptedAsync(stream);
    }

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

    public static string DecryptString(
        this ICipher cipher,
        byte[] data)
    {
        using var stream = new MemoryStream(data);
        return cipher.DecryptString(stream);
    }

    public static Task<string> DecryptStringAsync(
        this ICipher cipher,
        byte[] data)
    {
        using var stream = new MemoryStream(data);
        return cipher.DecryptStringAsync(stream);
    }
}

public sealed class ZeroCipher
    : ICipher
{
    public bool IsEncrypted(
        Stream stream)
    {
        return true;
    }

    public Task<bool> IsEncryptedAsync(
        Stream stream)
    {
        return Task.FromResult(true);
    }

    public int Encrypt(
        string text,
        Stream stream)
    {
        var data = Encoding.UTF8.GetBytes(text);
        stream.Write(data);
        return data.Length;
    }

    public Task<int> EncryptAsync(
        string text,
        Stream stream)
    {
        var data = Encoding.UTF8.GetBytes(text);
        stream.Write(data);
        return Task.FromResult(data.Length);
    }

    public string DecryptString(
        Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<string> DecryptStringAsync(
        Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}


public sealed class NameCipher
    : ICipher
{
    private readonly string _password;

    public NameCipher(
        string password)
    {
        _password = password;
    }

    public bool IsEncrypted(
        Stream stream)
    {
        return TryDecrypt(stream) != null;
    }

    public async Task<bool> IsEncryptedAsync(
        Stream stream)
    {
        return await TryDecryptAsync(stream) != null;
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
        var data = Encoding.UTF8.GetBytes($":{text}");
        cryptoStream.Write(data);
        cryptoStream.FlushFinalBlock();

        var encryptedBase64Name = Convert.ToBase64String(dataStream.ToArray());
        var encryptedFileName = Subst(encryptedBase64Name);
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
        var data = Encoding.UTF8.GetBytes($":{text}");
        await cryptoStream.WriteAsync(data);
        await cryptoStream.FlushFinalBlockAsync();

        var encryptedBase64Name = Convert.ToBase64String(dataStream.ToArray());
        var encryptedFileName = Subst(encryptedBase64Name);
        var base64 = Encoding.UTF8.GetBytes(encryptedFileName);
        await stream.WriteAsync(base64);
        
        return base64.Length;
    }

    public string DecryptString(
        Stream stream)
    {
        var text = TryDecrypt(stream);
        if (text == null)
            throw new FormatException("The data stream is not encrypted name.");
        return text;
    }

    public async Task<string> DecryptStringAsync(
        Stream stream)
    {
        var text = await TryDecryptAsync(stream);
        if (text == null)
            throw new FormatException("The data stream is not encrypted name.");
        return text;
    }

    private string? TryDecrypt(
        Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var encryptedFileName = reader.ReadToEnd();
            var encryptedBase64Name = Restore(encryptedFileName);
            var encryptedName = Convert.FromBase64String(encryptedBase64Name);
            
            var encryptedNameStream = new MemoryStream(encryptedName);

            var salt = CipherShared.ReadBytes(encryptedNameStream, 8);

            using var aes = CipherShared.CreateAes(_password, salt);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var cryptoStream =
                new CryptoStream(
                    encryptedNameStream,
                    decryptor,
                    CryptoStreamMode.Read,
                    true);

            using var cryptoStreamReader = new StreamReader(cryptoStream);
            var text = cryptoStreamReader.ReadToEnd();
            return text.StartsWith(':') ? text[1..] : default;
        }
        catch
        {
            return default;
        }
    }

    private async Task<string?> TryDecryptAsync(
        Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var encryptedFileName = await reader.ReadToEndAsync();
            var encryptedBase64Name = Restore(encryptedFileName);
            var encryptedName = Convert.FromBase64String(encryptedBase64Name);
            
            var encryptedNameStream = new MemoryStream(encryptedName);

            var salt = await CipherShared.ReadBytesAsync(encryptedNameStream, 8);

            using var aes = CipherShared.CreateAes(_password, salt);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            await using var cryptoStream =
                new CryptoStream(
                    encryptedNameStream,
                    decryptor,
                    CryptoStreamMode.Read,
                    true);

            using var cryptoStreamReader = new StreamReader(cryptoStream);
            var text = await cryptoStreamReader.ReadToEndAsync();
            return text.StartsWith(':') ? text[1..] : default;
        }
        catch
        {
            return default;
        }
    }

    private static string Subst(
        string text)
    {
        return text.Replace('/', '_').Replace('=', '~');
    }
    
    private static string Restore(
        string text)
    {
        return text.Replace('_', '/').Replace('~', '=');
    }
}

public sealed class ContentCipher
    : ICipher
{
    /// <summary>"Salted__" encoded in ASCII (UTF8).</summary>
    private static readonly byte[] Salted = {0x53, 0x61, 0x6c, 0x74, 0x65, 0x64, 0x5f, 0x5f};

    private readonly string _password;

    public ContentCipher(
        string password)
    {
        _password = password;
    }

    public bool IsEncrypted(
        Stream stream)
    {
        var octet = CipherShared.ReadBytes(stream, 8);
        var salt = CipherShared.ReadBytes(stream, 8);
        return octet.Length == Salted.Length &&
               !Salted.Where((t, i) => octet[i] != t).Any() &&
               salt.Length == 8;
    }

    public async Task<bool> IsEncryptedAsync(
        Stream stream)
    {
        var octet = await CipherShared.ReadBytesAsync(stream, 8);
        var salt = await CipherShared.ReadBytesAsync(stream, 8);
        return octet.Length == Salted.Length &&
               !Salted.Where((t, i) => octet[i] != t).Any() &&
               salt.Length == 8;
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
    
    public string DecryptString(
        Stream stream)
    {
        var octet = CipherShared.ReadBytes(stream, 8);
        if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
            throw new FormatException("The data stream is not encrypted.");

        using var aes = CipherShared.CreateAes(_password, CipherShared.ReadBytes(stream, 8));
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
        using var cryptoStreamReader = new StreamReader(cryptoStream);
        return cryptoStreamReader.ReadToEnd();
    }

    public async Task<string> DecryptStringAsync(
        Stream stream)
    {
        var octet = await CipherShared.ReadBytesAsync(stream, 8);
        if (octet.Length != Salted.Length || Salted.Where((t, i) => octet[i] != t).Any())
            throw new FormatException("The data stream is not encrypted.");

        using var aes = CipherShared.CreateAes(_password, await CipherShared.ReadBytesAsync(stream, 8));
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        await using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, true);
        using var cryptoStreamReader = new StreamReader(cryptoStream);
        return await cryptoStreamReader.ReadToEndAsync();
    }
}

public static class CipherShared
{
    public static async Task<byte[]> ReadBytesAsync(
        Stream stream,
        int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset != length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset));
            offset += read;
            if (read == 0)
                return buffer[..offset];
        }

        return buffer;
    }

    public static byte[] ReadBytes(
        Stream stream,
        int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset != length)
        {
            var read = stream.Read(buffer.AsSpan(offset, length - offset));
            offset += read;
            if (read == 0)
                return buffer[..offset];
        }

        return buffer;
    }
    
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