namespace pwd.tests;

public sealed class Cipher_Tests
{
    [Test]
    public async Task the_cipher_decrypts_the_message_correctly()
    {
        var (password, text, encrypted) = Shared.EncryptionTestData();
        var cipher = new Cipher(password);
        using var stream = new MemoryStream(encrypted);
        var decrypted = await cipher.Decrypt(stream);
        Assert.That(text, Is.EqualTo(decrypted));
    }

    [TestCase('a', 2_000)]
    [TestCase('Z', 100_000)]
    [TestCase('1', 1)]
    public async Task the_cipher_decrypts_what_it_encrypted(
        char symbol,
        int length)
    {
        var cipher = new Cipher("pa$$w0rd");
        var text = new string(symbol, length);
        var stream = new MemoryStream();
        await cipher.Encrypt(new string(symbol, length), stream);
        stream.Position = 0;
        var decrypted = await cipher.Decrypt(stream);
        Assert.That(text, Is.EqualTo(decrypted));
    }

    [Test]
    public async Task the_cipher_detects_encrypted_stream_correctly()
    {
        var (password, _, encrypted) = Shared.EncryptionTestData();
        var cipher = new Cipher(password);
        using var stream = new MemoryStream(encrypted);
        Assert.That(await cipher.IsEncrypted(stream));
    }
    
    [TestCase("")]
    [TestCase("53616C7465645F5F")]
    [TestCase("53616C7465645F5F010203040506")]
    public async Task the_cipher_detects_unencrypted_stream_correctly(
        string data)
    {
        var cipher = new Cipher("pa$$w0rd");
        using var stream = new MemoryStream(Convert.FromHexString(data));
        Assert.That(await cipher.IsEncrypted(stream), Is.False);
    }
}
