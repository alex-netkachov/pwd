using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using pwd.mocks;
using pwd.repository;
using pwd.repository.implementation;
using pwd.repository.interfaces;

namespace pwd.tests.repository;

public sealed class Repository_Tests
{
   private static readonly string _testName = Encrypt("test");
   private static readonly string _testContent = Encrypt("test");

   [Test]
   public void List_root_of_empty_repository_returns_no_items()
   {
      var fs = Shared.GetMockFs();
      var repository = Shared.CreateRepository(fs);
      Assert.That(!repository.Root.List().Any());
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_files()
   {
      var fs = Shared.GetMockFs("*test");
      var repository = Shared.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(1));
      var file = (pwd.repository.interfaces.IFile)repository.Root.List().Single();
      Assert.That(
         file.Name.Value,
         Is.EqualTo("test"));
      Assert.That(
         repository.Get(Path.From(file.Name)),
         Is.Not.Null);
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_folders()
   {
      var fs = Shared.GetMockFs("@test1/*test2");
      var repository = Shared.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateDirectories(".").Count(),
         Is.EqualTo(1));
      var folder = (IFolder)repository.Root.List(new ListOptions(false, true, false)).Single();
      Assert.That(
         folder.Name.Value,
         Is.EqualTo("test1"));
      Assert.That(
         repository.Get(Path.From(folder.Name)),
         Is.Not.Null);
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
   [TestCase("@f/*test", ".", true, false, false, "f\\test")]
   [TestCase("@f/*test", ".", false, true, false, "f")]
   [TestCase("@f/@test", ".", false, true, false, "f")]
   [TestCase("f/test", ".", false, true, false, "")]
   [TestCase("@f/*_test", ".", true, false, false, "")]
   [TestCase("@f/*_test", ".", true, false, true, "f\\_test")]
   [TestCase("@f/*test", "f", true, false, false, "f\\test")]
   public void List_repository(
      string files,
      string listPath,
      bool recursive,
      bool includeFolders,
      bool includeDottedFilesAndFolders,
      string expected)
   {
      var fs = Shared.GetMockFs(files);

      var repository = Shared.CreateRepository(fs);
      var repositoryFolderPath = Path.Parse(fs, listPath);
      var repositoryFolder = (IContainer)repository.Get(repositoryFolderPath)!;
      Assert.That(repositoryFolder, Is.Not.Null);

      var items =
         repositoryFolder
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

      var repository = Shared.CreateRepository(fs);

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
      var repository = Shared.CreateRepository(fs);

      var path = Path.Parse(fs, _testName);
      var file =
         async switch
         {
            true => await repository.CreateFileAsync(path),
            _ => repository.CreateFile(path)
         };

      await file.WriteAsync(_testContent);

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
      var fs = Shared.GetMockFs("*test");
      var repository = Shared.CreateRepository(fs);

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