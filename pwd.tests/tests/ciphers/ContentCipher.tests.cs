namespace pwd.tests.ciphers;

public sealed class ContentCipher_Tests
{
   [Test]
   public async Task the_cipher_encrypts_the_message_correctly()
   {
      var (password, text, _) = Shared.ContentEncryptionTestData();
      var cipher = new ContentCipher(password);
      var encrypted = await cipher.EncryptAsync(text);
      // to update NameEncryptionTestData uncomment the following line and update the method:
      // Console.WriteLine(Convert.ToHexString(encrypted));
      Assert.That(await cipher.IsEncryptedAsync(encrypted));
   }

   [Test]
   public async Task the_cipher_decrypts_the_message_correctly()
   {
      var (password, text, encrypted) = Shared.ContentEncryptionTestData();
      var cipher = new ContentCipher(password);
      using var stream = new MemoryStream(encrypted);
      var decrypted = await cipher.DecryptStringAsync(stream);
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase('a', 2_000)]
   [TestCase('Z', 100_000)]
   [TestCase('1', 1)]
   public async Task the_cipher_decrypts_what_it_encrypted(
      char symbol,
      int length)
   {
      var cipher = new ContentCipher("pa$$w0rd");
      var text = new string(symbol, length);
      var stream = new MemoryStream();
      await cipher.EncryptAsync(new string(symbol, length), stream);
      stream.Position = 0;
      var decrypted = await cipher.DecryptStringAsync(stream);
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [Test]
   public async Task the_cipher_detects_encrypted_stream_correctly()
   {
      var (password, _, encrypted) = Shared.ContentEncryptionTestData();
      var cipher = new ContentCipher(password);
      using var stream = new MemoryStream(encrypted);
      Assert.That(await cipher.IsEncryptedAsync(stream));
   }

   [TestCase("")]
   [TestCase("53616C7465645F5F")]
   [TestCase("53616C7465645F5F010203040506")]
   public async Task the_cipher_detects_unencrypted_stream_correctly(
      string data)
   {
      var cipher = new ContentCipher("pa$$w0rd");
      using var stream = new MemoryStream(Convert.FromHexString(data));
      Assert.That(await cipher.IsEncryptedAsync(stream), Is.False);
   }
}