using pwd.ciphers;

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
      Assert.That(encrypted, Is.Not.Null);
   }

   [Test]
   public async Task the_cipher_decrypts_the_message_correctly()
   {
      var (password, text, encrypted) = Shared.ContentEncryptionTestData();
      var cipher = new ContentCipher(password);
      using var stream = new MemoryStream(encrypted);
      var decrypted = (await cipher.DecryptStringAsync(stream)).Text;
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase('a', 2_000)]
   [TestCase('Z', 100_000)]
   [TestCase('1', 1)]
   public void the_cipher_decrypts_what_it_encrypted(
      char symbol,
      int length)
   {
      var cipher = new ContentCipher("pa$$w0rd");
      var expected = new string(symbol, length);
      var stream = new MemoryStream();
      cipher.Encrypt(new string(symbol, length), stream);
      stream.Position = 0;
      var (decrypted, actual) = cipher.DecryptString(stream);
      Assert.That(decrypted);
      Assert.That(actual, Is.EqualTo(expected));

   }

   [Test]
   public void the_cipher_decrypt_encrypted_stream()
   {
      var (password, expected, encrypted) = Shared.ContentEncryptionTestData();
      var cipher = new ContentCipher(password);
      using var stream = new MemoryStream(encrypted);
      var (decrypted, actual) = cipher.DecryptString(stream);
      Assert.That(decrypted);
      Assert.That(actual, Is.EqualTo(expected));
   }

   [TestCase("")]
   [TestCase("53616C7465645F5F")]
   [TestCase("53616C7465645F5F010203040506")]
   public void the_cipher_does_not_decrypt_unencrypted_stream(
      string data)
   {
      var cipher = new ContentCipher("pa$$w0rd");
      using var stream = new MemoryStream(Convert.FromHexString(data));
      var (decrypted, _) = cipher.DecryptString(stream);
      Assert.That(decrypted, Is.False);
   }
}