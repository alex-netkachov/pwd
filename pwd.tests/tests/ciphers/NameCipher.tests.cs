using pwd.ciphers;

namespace pwd.tests.ciphers;

public sealed class NameCipher_Tests
{
   [Test]
   public async Task the_cipher_encrypts_the_message_correctly()
   {
      var (password, text, _) = NameEncryptionTestData();
      var cipher = new NameCipher(password);
      var encrypted = await cipher.EncryptAsync(text);
      // to update NameEncryptionTestData uncomment the following line and update the method:
      // Console.WriteLine(Convert.ToHexString(encrypted));
      Assert.That(encrypted, Is.Not.Null);
   }

   [Test]
   public async Task the_cipher_decrypts_the_message_correctly()
   {
      var (password, text, encrypted) = NameEncryptionTestData();
      var cipher = new NameCipher(password);
      using var stream = new MemoryStream(encrypted);
      var decrypted = (await cipher.DecryptStringAsync(stream)).Text;
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase('a', 2_000)]
   [TestCase('Z', 100_000)]
   [TestCase('1', 1)]
   public async Task the_cipher_decrypts_what_it_encrypted(
      char symbol,
      int length)
   {
      var cipher = new NameCipher("pa$$w0rd");
      var text = new string(symbol, length);
      var stream = new MemoryStream();
      await cipher.EncryptAsync(new string(symbol, length), stream);
      stream.Position = 0;
      var decrypted = (await cipher.DecryptStringAsync(stream)).Item2;
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [Test]
   public void the_cipher_decrypt_encrypted_stream()
   {
      var (password, expected, encrypted) = NameEncryptionTestData();
      var cipher = new NameCipher(password);
      using var stream = new MemoryStream(encrypted);
      var (decrypted, actual) = cipher.DecryptString(stream);
      Assert.That(decrypted);
      Assert.That(actual, Is.EqualTo(expected));
   }

   [TestCase("")]
   public void the_cipher_does_not_decrypt_unencrypted_stream(
      string data)
   {
      var cipher = new NameCipher("pa$$w0rd");
      using var stream = new MemoryStream(Convert.FromHexString(data));
      var (decrypted, _) = cipher.DecryptString(stream);
      Assert.That(decrypted, Is.False);
   }
   
   private static (string pwd, string text, byte[] encrypted) NameEncryptionTestData()
   {
      return (
         "secret",
         "only you can protect what is yours",
         Convert.FromHexString(
            "475349596B69396453506F675378444A525F73396D6D636E616D6A746A3734616E4D43793255324B6A464B48345F335234477859675452326C446E726778352B694E654A573375474F63737E"));
   }
}