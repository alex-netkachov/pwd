using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using pwd.extensions;

namespace pwd.tests.ciphers;

public sealed class Cipher_Tests
{
   [TestCase(true)]
   [TestCase(false)]
   public async Task Encrypt_encrypts_the_message_as_expected(
      bool async)
   {
      var (password, text, _) = NameEncryptionTestData();
      var cipher = new Cipher(password);
      using var input = text.AsStream();
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
      var cipher = new Cipher(password);
      using var input = encrypted.AsStream();
      using var output = new MemoryStream();

      if (async)
         await cipher.DecryptAsync(input, output);
      else
         cipher.Decrypt(input, output);

      var decrypted = output.AsString();
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase('a', 2_000)]
   [TestCase('Z', 100_000)]
   [TestCase('1', 1)]
   public async Task Decrypt_decrypts_encrypted_text(
      char symbol,
      int length)
   {
      var cipher = new Cipher("pa$$w0rd");
      var text = new string(symbol, length);

      using var input = text.AsStream();
      using var output1 = new MemoryStream();
      await cipher.EncryptAsync(input, output1);
      output1.Position = 0;

      using var output2 = new MemoryStream();
      await cipher.DecryptAsync(output1, output2);

      var decrypted = output2.AsString();
      Assert.That(text, Is.EqualTo(decrypted));
   }

   [TestCase("")]
   [TestCase("00FFFF00")]
   public void Decrypt_fails_when_the_input_is_not_encrypted_stream(
      string input)
   {
      var cipher = new Cipher("pa$$w0rd");
      using var inputStream = Convert.FromHexString(input).AsStream();
      using var outputStream = new MemoryStream();
      Assert.Throws<Exception>(() => cipher.Decrypt(inputStream, outputStream));
   }

   private static (string pwd, string text, byte[] encrypted) NameEncryptionTestData()
   {
      return (
         "secret",
         "only you can protect what is yours",
         Convert.FromHexString(
            "F4426E32C0DCB5D9F5DB60A6EEB43288F2EF59CF85D76F3E3ECE574903DC3D1A206374B5B720B225655C102E0CB865CA6379CD59DF07BBAE"));
   }
}