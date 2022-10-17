using System.Diagnostics;
using System.Text;
using pwd.ciphers;

namespace pwd.tests;

public sealed class Openssl_Tests
{
   private const string Password = "secret";
   private const string Text = "test";

   private static string LocateOpenssl()
   {
      return new[]
      {
         Environment.GetEnvironmentVariable("ProgramFiles") + @"\Git\usr\bin\openssl.exe",
         Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\Programs\Git\usr\bin\openssl.exe"
      }.FirstOrDefault(File.Exists) ?? "openssl";
   }

   [Test]
   public async Task Encrypt()
   {
      async Task OpensslEncrypt(
         string path,
         string pwd,
         string txt)
      {
         var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -e -salt -pbkdf2 -pass stdin")
         {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
         };
         var process = Process.Start(info);
         if (process == null)
            return;
         await using var writer = new BinaryWriter(process.StandardInput.BaseStream);
         var passwordData = Encoding.ASCII.GetBytes(pwd + "\n");
         writer.Write(passwordData, 0, passwordData.Length);
         var data = Encoding.UTF8.GetBytes(txt);
         writer.Write(data, 0, data.Length);
         writer.Close();
         await using var stream = File.OpenWrite(path);
         await process.StandardOutput.BaseStream.CopyToAsync(stream);
      }

      var path = Path.GetTempFileName();
      await OpensslEncrypt(path, Password, Text);
      var cipher = new ContentCipher(Password);
      await using var stream = File.OpenRead(path);
      var decrypted = await cipher.DecryptStringAsync(stream);
      stream.Close();
      File.Delete(path);
      Assert.That(decrypted.Item2, Is.EqualTo(Text));
   }

   [Test]
   public async Task Decrypt()
   {
      async Task<string?> OpensslDecrypt(
         string path,
         string pwd)
      {
         var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -d -salt -pbkdf2 -pass stdin")
         {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
         };
         var process = Process.Start(info);
         if (process == null)
            return null;
         await using var writer = new BinaryWriter(process.StandardInput.BaseStream);
         var passwordData = Encoding.ASCII.GetBytes(pwd + "\n");
         writer.Write(passwordData, 0, passwordData.Length);
         var encrypted = await File.ReadAllBytesAsync(path);
         writer.Write(encrypted, 0, encrypted.Length);
         writer.Close();
         return await process.StandardOutput.ReadToEndAsync();
      }

      var path = Path.GetTempFileName();
      await using var stream = File.OpenWrite(path);
      await new ContentCipher(Password).EncryptAsync(Text, stream);
      stream.Close();
      var decrypted = await OpensslDecrypt(path, Password);
      File.Delete(path);
      Assert.That(decrypted, Is.EqualTo(Text));
   }
}