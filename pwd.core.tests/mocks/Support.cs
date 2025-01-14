using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using pwd.core.abstractions;

namespace pwd.core.mocks;

public static class Support
{
   public static IFileSystem GetMockFs(
      string entries = "")
   {
      var fs = new MockFileSystem();
      fs.Directory.CreateDirectory("container/test");
      var dir = fs.DirectoryInfo.New("container/test").FullName;
      fs.Directory.SetCurrentDirectory(dir);

      // entries := item [; item] *
      // item := [ @ | ^ | * ] name [ / item ] *
      // @ - encrypt name (for folders)
      // ^ - encrypt content
      // * - encrypt name and content

      if (entries != "")
      {
         foreach (var entry in entries.Split(';'))
         {
            var filePath =
               string.Join(
                  '/',
                  entry.Split('/')
                     .Select(item => item[0] switch
                     {
                        '@' or '*' => Encrypt(item[1..]),
                        '^' => item[1..],
                        _ => item
                     }));

            var fileContent =
               entry.Split('/')[^1][0] is '^' or '*'
                  ? Encrypt("test")
                  : "test";

            var folder = fs.Path.GetDirectoryName(filePath)!;
            if (folder != "")
               fs.Directory.CreateDirectory(folder);
            fs.File.WriteAllText(filePath, fileContent);
         }
      }

      return fs;
   }
   
   public static string Encrypt(
      string input)
   {
      var cipher = GetTestCipher();
      var encrypted = cipher.Encrypt(input);
      var encoded = Base64Url.Instance.Encode(encrypted);
      return encoded;
   }
   
   public static IRepository CreateRepository(
      IFileSystem? fs = null,
      ILogger<FolderRepository>? logger = null)
   {
      return new FolderRepository(
         logger ?? Mock.Of<ILogger<FolderRepository>>(),
         fs ?? Mock.Of<IFileSystem>(),
         (_, _) => GetTestCipher(),
         Base64Url.Instance,
         ".",
         "");
   }

   public static ICipher GetTestCipher()
   {
      return new AesCipher(
         new byte[32],
         AesInitialisationData.Zero.ToArray());
   }
}

public static class HexStringConverterExtensions
{
   public static string ToHexString(
      this byte[] bytes)
   {
      return BitConverter.ToString(bytes).Replace("-", "");
   }
   
   public static string ToHexString(
      this ReadOnlySpan<byte> bytes)
   {
      return BitConverter.ToString(bytes.ToArray()).Replace("-", "");
   }
}