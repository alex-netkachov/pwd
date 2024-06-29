using System.Security.Cryptography;
using System.Text;

namespace pwd.core.tests;

public sealed class Cipher_Tests
{
   [TestCase(true)]
   [TestCase(false)]
   public async Task Encrypt_encrypts_the_message_as_expected(
      bool async)
   {
      var (password, text, _) = NameEncryptionTestData();
      var cipher = new AesCipher(password, new byte[24]);
      using var input = new MemoryStream(Encoding.UTF8.GetBytes(text));
      using var output = new MemoryStream();

      if (async)
         await cipher.EncryptAsync(input, output);
      else
         cipher.Encrypt(input, output);

      var data = output.ToArray();
      // to update NameEncryptionTestData uncomment the following line and update the method:
      //Console.WriteLine(Convert.ToHexString(data));
      Assert.That(data, Is.Not.Null);
   }

   [TestCase(true)]
   [TestCase(false)]
   public async Task Decrypt_decrypts_the_message_correctly(
      bool async)
   {
      var (password, text, encrypted) = NameEncryptionTestData();
      var cipher = new AesCipher(password, new byte[24]);
      using var input = new MemoryStream(encrypted);
      using var output = new MemoryStream();

      if (async)
         await cipher.DecryptAsync(input, output);
      else
         cipher.Decrypt(input, output);

      var decrypted = Encoding.UTF8.GetString(output.ToArray());
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase('a', 2_000)]
   [TestCase('Z', 100_000)]
   [TestCase('1', 1)]
   public async Task Decrypt_decrypts_encrypted_text(
      char symbol,
      int length)
   {
      var cipher = new AesCipher("pa$$w0rd");
      var text = new string(symbol, length);

      using var input = new MemoryStream(Encoding.UTF8.GetBytes(text));
      using var output1 = new MemoryStream();
      await cipher.EncryptAsync(input, output1);
      output1.Position = 0;

      using var output2 = new MemoryStream();
      await cipher.DecryptAsync(output1, output2);

      var decrypted = Encoding.UTF8.GetString(output2.ToArray());
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase("00FFFF00")]
   public void Decrypt_fails_when_the_input_is_not_encrypted_stream(
      string input)
   {
      var cipher = new AesCipher("pa$$w0rd", new byte[24]);
      using var inputStream = new MemoryStream(Convert.FromHexString(input));
      using var outputStream = new MemoryStream();
      Assert.Throws<CryptographicException>(() => cipher.Decrypt(inputStream, outputStream));
   }

   private static (string pwd, string text, byte[] encrypted) NameEncryptionTestData()
   {
      return (
         "secret",
         "only you can protect what is yours",
         Convert.FromHexString(
            "8FAEE41BCA64A2D1F62E9D187D855637BB4082C9FB2EE7256CE3ADECD9F7FB4CDE91CB7F50F3E5849908EF277576D274"));
   }
}