using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

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
         var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -e -salt -pbkdf2 -iter 600000 -pass stdin")
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
      var cipher = new Cipher(Password);
      var data = File.ReadAllBytes(path);
      var body = data["Salted__".Length..];
      var decrypted = await cipher.DecryptStringAsync(body);
      File.Delete(path);
      Assert.That(decrypted, Is.EqualTo(Text));
   }

   [Test]
   public async Task Decrypt()
   {
      async Task<string?> OpensslDecrypt(
         string path,
         string pwd)
      {
         var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -d -salt -pbkdf2 -iter 600000 -pass stdin")
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
         var salted = Encoding.ASCII.GetBytes("Salted__");
         writer.Write(salted, 0, salted.Length);
         var encrypted = await File.ReadAllBytesAsync(path);
         writer.Write(encrypted, 0, encrypted.Length);
         writer.Close();
         return await process.StandardOutput.ReadToEndAsync();
      }

      var path = Path.GetTempFileName();
      var data = new Cipher(Password).Encrypt(Text);
      File.WriteAllBytes(path, data);
      var decrypted = await OpensslDecrypt(path, Password);
      File.Delete(path);
      Assert.That(decrypted, Is.EqualTo(Text));
   }
}