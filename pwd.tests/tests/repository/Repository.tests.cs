using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using pwd.mocks;
using pwd.repository;
using pwd.repository.implementation;

namespace pwd.tests.repository;

public sealed class Repository_Tests
{
   [Test]
   public void List_root_of_empty_repository_returns_no_items()
   {
      var fs = Shared.GetMockFs();
      var repository = SimpleRepository(fs);
      Assert.That(!repository.Root.List().Any());
   }

   [TestCase(true)]
   [TestCase(false)]
   public void List_root_of_non_empty_repository_returns_items(
      bool withPlain)
   {
      var cipher = FastTestCipher.Instance;
      var encoder = Base64Url.Instance;

      var fs = Shared.GetMockFs();

      var plain = "test";
      var encrypted = encoder.Encode(cipher.Encrypt(plain));
      fs.File.WriteAllText(encrypted, encrypted);

      if (withPlain)
         fs.File.WriteAllText(plain, plain);

      var repository = new Repository(fs, cipher, encoder, ".");
      Assert.That(
         repository.List(repository.Root).Single().Name.Value,
         Is.EqualTo(plain));
   }

   [TestCase("test", ".", false, false, false, "")]
   [TestCase("@test", ".", false, false, false, "")]
   [TestCase("^test", ".", false, false, false, "")]
   [TestCase("*test", ".", false, false, false, "test")]
   [TestCase("*_test", ".", false, false, false, "")]
   [TestCase("*_test", ".", false, false, true, "_test")]
   [TestCase("*.test", ".", false, false, false, "")]
   [TestCase("*.test", ".", false, false, true, ".test")]
   [TestCase("@f/*test", ".", false, false, false, "")]
   [TestCase("@f/*test", ".", true, false, false, "f/test")]
   [TestCase("@f/*test", ".", false, true, false, "f")]
   [TestCase("@f/@test", ".", false, true, false, "f")]
   [TestCase("f/test", ".", false, true, false, "")]
   [TestCase("@f/*_test", ".", true, false, false, "")]
   [TestCase("@f/*_test", ".", true, false, true, "f/_test")]
   [TestCase("@f/*test", "f", true, false, false, "f/test")]
   public async Task List_repository(
      string path,
      string listPath,
      bool recursive,
      bool includeFolders,
      bool includeDottedFilesAndFolders,
      string expected)
   {
      // @ - encrypt name
      // ^ - encrypt content
      // * - encrypt name and content

      var fs = Shared.GetMockFs();

      var cipher = FastTestCipher.Instance;
      var encoder = Base64Url.Instance;

      var filePath =
         string.Join(
            '/',
            path.Split('/')
               .Select(item => item[0] switch
               {
                  '@' or '*' => encoder.Encode(cipher.Encrypt(item[1..])),
                  '^' => item[1..],
                  _ => item
               }));

      var fileContent =
         path.Split('/')[^1][0] is '^' or '*'
            ? encoder.Encode(cipher.Encrypt("test"))
            : "test";

      var completeFilePath = fs.Path.Combine("folder1/folder2", filePath);
      var folder = fs.Path.GetDirectoryName(completeFilePath);
      if (folder == null)
         throw new Exception("folder is null");
      fs.Directory.CreateDirectory(folder);
      fs.File.WriteAllText(completeFilePath, fileContent);

      var repository = new Repository(fs, cipher, Base64Url.Instance, "folder1/folder2");

      var items =
         ((IFolder)repository.Get(Path.Parse(fs, listPath))!)
            .List(
               new ListOptions(
                  recursive,
                  includeFolders,
                  includeDottedFilesAndFolders))
            .ToList();

      var actual = ItemsToPaths(items);

      Assert.That(
         actual,
         Is.EqualTo(expected));
   }

   [Test]
   public void List_special_case1()
   {
      var fs = Shared.GetMockFs();

      var f = Encrypt("f");
      var test11 = Encrypt("test1");
      var test21 = Encrypt("test2");
      var test12 = Encrypt("test1");
      var test22 = Encrypt("test2");
      var test = Encrypt("test");

      fs.Directory.CreateDirectory(f);
      fs.File.WriteAllText($"{f}/{test12}", test);
      fs.File.WriteAllText($"{f}/{test22}", test);
      fs.File.WriteAllText($"{test11}", test);
      fs.File.WriteAllText($"{test21}", test);

      var repository = new Repository(fs, FastTestCipher.Instance, Base64Url.Instance, ".");

      var items =
         repository
            .Root
            .List(new ListOptions(true, false, false))
            .ToList();

      Assert.That(
         ItemsToPaths(items),
         Is.EqualTo("f\\test1;f\\test2;test1;test2"));
   }

   [TestCase(true)]
   [TestCase(false)]
   public async Task CreateFile_creates_a_file(
      bool async)
   {
      var fs = Shared.GetMockFs();
      var repository = SimpleRepository(fs);

      var path = Path.Parse(fs, "test");
      var file =
         async switch
         {
            true => await repository.CreateFileAsync(path),
            _ => repository.CreateFile(path)
         };

      Assert.That(
         file,
         Is.Not.Null);

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(1));

      Assert.That(
         repository.Root.List().Count(),
         Is.EqualTo(1));
   }

   [Test]
   public void Delete_deletes_a_file()
   {
      var fs = Shared.GetMockFs();

      fs.File.WriteAllText("test", "test");

      var repository = SimpleRepository(fs);

      var file = repository.Get(Path.Parse(fs, "test")) as INamedItem;

      Assert.That(
         file,
         Is.Not.Null);

      Assert.That(
         repository.Root.List().Count(),
         Is.EqualTo(1));

      // deleting a file removes it
      repository.Delete(file);

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(0));

      Assert.That(
         repository.Root.List().Count(),
         Is.EqualTo(0));
   }

   private static string Encrypt(
      string input)
   {
      return Base64Url.Instance.Encode(
         FastTestCipher.Instance.Encrypt(
            input));
   }

   private static IRepository SimpleRepository(
      IFileSystem fs)
   {
      return new Repository(
         fs,
         ZeroCipher.Instance,
         ZeroEncoder.Instance,
         ".");
   }

   private static string ItemsToPaths(
      IEnumerable<INamedItem> items)
   {
      var paths =
         items
            .Select(item => item switch
                           {
                                 File file => file.GetPath().ToString(),
                                 Folder folder => folder.GetPath().ToString(),
                                 _ => ""
                              })
            .OrderBy(item => item)
            .ToList();
      return string.Join(";", paths);
   }
}