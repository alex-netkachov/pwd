namespace pwd.tests;

public sealed class Cipher_Tests
{
    [Test]
    public async Task Roundup_works_well()
    {
        var (password, text) = Shared.EncryptionTestData();
        var cipher = new Cipher(password);
        var stream = new MemoryStream();
        await cipher.Encrypt(text, stream);
        stream.Position = 0;
        var decrypted = await cipher.Decrypt(stream);
        Assert.That(text, Is.EqualTo(decrypted));
    }
}
