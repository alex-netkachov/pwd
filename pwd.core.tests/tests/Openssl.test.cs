using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwd.core.abstractions;
using pwd.core.mocks;

namespace pwd.core.tests;

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
         AesInitialisationData initialisationData,
         string txt)
      {
         var args =
            string.Format(
               "aes-256-cbc -e -S {0} -iv {1} -pbkdf2 -iter 600000 -pass stdin",
               initialisationData.Salt.ToHexString(),
               initialisationData.InitialisationVector.ToHexString());

         var info =
            new ProcessStartInfo(LocateOpenssl(), args)
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

      var aesInitialisationData = AesInitialisationData.Random();

      var path = Path.GetTempFileName();

      await OpensslEncrypt(path, Password, aesInitialisationData, Text);

      var cipher = new AesCipher(Password, aesInitialisationData.ToArray());
      var data = await File.ReadAllBytesAsync(path);
      var decrypted = await cipher.DecryptStringAsync(data);
      File.Delete(path);
      Assert.That(decrypted, Is.EqualTo(Text));
   }

   [Test]
   public async Task Decrypt()
   {
      async Task<string?> OpensslDecrypt(
         string path,
         string pwd,
         AesInitialisationData initialisationData)
      {
         var args =
            string.Format(
               "aes-256-cbc -d -S {0} -iv {1} -pbkdf2 -iter 600000 -pass stdin",
               initialisationData.Salt.ToHexString(),
               initialisationData.InitialisationVector.ToHexString());

         var info =
            new ProcessStartInfo(LocateOpenssl(), args)
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
      var cipher = new AesCipher(Password);

      var initialisationData =
         AesInitialisationData.FromArray(
            cipher.GetInitialisationData());
      
      var data = await cipher.EncryptAsync(Text);
      await File.WriteAllBytesAsync(path, data);

      var decrypted = await OpensslDecrypt(path, Password, initialisationData);

      File.Delete(path);

      Assert.That(decrypted, Is.EqualTo(Text));
   }
}