using System.IO.Abstractions;
using pwd.core;
using pwd.core.abstractions;

namespace pwd.mocks;

public static class FilesystemExtensions
{
   public static IFileSystem FileLayout1(
      this IFileSystem fs)
   {
      const string text = "test"; 
      var encrypted = Encrypt(text);

      fs.File.WriteAllText("file", text);
      fs.File.WriteAllText(".hidden", text);
      fs.Directory.CreateDirectory("regular_dir");
      fs.File.WriteAllText("regular_dir/file", text);
      fs.File.WriteAllText("regular_dir/.hidden", text);
      fs.Directory.CreateDirectory(".hidden_dir");
      fs.File.WriteAllText(".hidden_dir/file", text);
      fs.File.WriteAllText(".hidden_dir/.hidden", text);
      fs.File.WriteAllText("encrypted", encrypted);
      fs.File.WriteAllText(".hidden_encrypted", encrypted);
      fs.File.WriteAllText("regular_dir/encrypted", encrypted);
      fs.File.WriteAllText("regular_dir/.hidden_encrypted", encrypted);
      fs.File.WriteAllText(".hidden_dir/encrypted", encrypted);
      fs.File.WriteAllText(".hidden_dir/.hidden_encrypted", encrypted);

      var encryptedFile = fs.Path.GetFileName("file");
      var encryptedDotHidden = fs.Path.GetFileName(".hidden");
      var encryptedRegularDir = fs.Path.GetFileName(".regular_dir");

      return fs;
   }

   private static string Encrypt(
      string input)
   {
      var cipher = Shared.GetTestCipher();
      var encrypted = cipher.Encrypt(input);
      var encoded = Base64Url.Instance.Encode(encrypted);
      return encoded;
   }

}