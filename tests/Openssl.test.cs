using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwd;

// ReSharper disable UnusedMember.Local because the tests are called through reflection

public static partial class Program
{
    private static string LocateOpenssl()
    {
        return new[]
        {
            Environment.GetEnvironmentVariable("ProgramFiles") + @"\Git\usr\bin\openssl.exe",
            Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\Programs\Git\usr\bin\openssl.exe"
        }.FirstOrDefault(System.IO.File.Exists) ?? "openssl";
    }


    private static async Task Test_Openssl_Encrypt()
    {
        void OpensslEncrypt(string path, string pwd, string txt)
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
            using var writer = new BinaryWriter(process.StandardInput.BaseStream);
            var passwordData = Encoding.ASCII.GetBytes(pwd + "\n");
            writer.Write(passwordData, 0, passwordData.Length);
            var data = Encoding.UTF8.GetBytes(txt);
            writer.Write(data, 0, data.Length);
            writer.Close();
            using var stream = System.IO.File.OpenWrite(path);
            process.StandardOutput.BaseStream.CopyTo(stream);
        }

        var (password, text) = EncryptionTestData();

        var path = Path.GetTempFileName();
        OpensslEncrypt(path, password, text);
        var cipher = new Cipher(password);
        var data = System.IO.File.ReadAllBytes(path);
        var decrypted = await cipher.Decrypt(data);
        System.IO.File.Delete(path);
        Assert(text == decrypted);
    }

    private static async Task Test_Openssl_Decrypt()
    {
        string? OpensslDecrypt(string path, string pwd)
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
            using var writer = new BinaryWriter(process.StandardInput.BaseStream);
            var passwordData = Encoding.ASCII.GetBytes(pwd + "\n");
            writer.Write(passwordData, 0, passwordData.Length);
            var encrypted = System.IO.File.ReadAllBytes(path);
            writer.Write(encrypted, 0, encrypted.Length);
            writer.Close();
            return process.StandardOutput.ReadToEnd();
        }

        var (password, text) = EncryptionTestData();

        var path = Path.GetTempFileName();
        System.IO.File.WriteAllBytes(path, await new Cipher(password).Encrypt(text));
        var decrypted = OpensslDecrypt(path, password);
        System.IO.File.Delete(path);
        Assert(text == decrypted);
    }
}