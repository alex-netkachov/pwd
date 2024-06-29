using System.ComponentModel;
using pwd.core.abstractions;

namespace pwd.core.tests;

public sealed class FolderRepository_Tests
{
   private static readonly string TestName = Support.Encrypt("test");
   private static readonly string TestContent = Support.Encrypt("test");

   [Test]
   public void List_root_of_empty_repository_returns_no_items()
   {
      var fs = Support.GetMockFs();
      var repository = Support.CreateRepository(fs);
      Assert.That(!repository.List(repository.Root).Any());
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_files()
   {
      var fs = Support.GetMockFs("*test");
      var repository = Support.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(2));
      var location = repository.Root;
      var items = repository.List(location).ToList();
      var file = items.Single();
      Assert.That(
         file.Name!.Value,
         Is.EqualTo("test"));
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_folders()
   {
      var fs = Support.GetMockFs("@test1/*test2");
      var repository = Support.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateDirectories(".").Count(),
         Is.EqualTo(1));
      var folder = repository.List(repository.Root, new ListOptions(false, true, false)).Single();
      Assert.That(
         folder.Name!.Value,
         Is.EqualTo("test1"));
   }

   [TestCase("test", ".", false, false, false, "")]
   [TestCase("@test", ".", false, false, false, "test")]
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
      var fs = Support.GetMockFs(files);

      var repository = Support.CreateRepository(fs);
      var repositoryFolder =
         listPath == "."
            ? repository.Root
            : repository.TryParseLocation(listPath, out var value)
               ? value!
               : throw new Exception();

      var items =
         repository
            .List(
               repositoryFolder!,
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
      var fs = Support.GetMockFs();

      var f = Support.Encrypt("f");
      var test11 = Support.Encrypt("test1");
      var test21 = Support.Encrypt("test2");
      var test12 = Support.Encrypt("test1");
      var test22 = Support.Encrypt("test2");
      var test = Support.Encrypt("test");

      fs.Directory.CreateDirectory(f);
      fs.File.WriteAllText($"{f}/{test12}", test);
      fs.File.WriteAllText($"{f}/{test22}", test);
      fs.File.WriteAllText($"{test11}", test);
      fs.File.WriteAllText($"{test21}", test);

      var repository = Support.CreateRepository(fs);

      var items =
         repository
            .List(
               repository.Root,
               new ListOptions(true, false, false))
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
      var fs = Support.GetMockFs();
      var repository = Support.CreateRepository(fs);

      Assert.That(repository.TryParseLocation(TestName, out var path));

      if (async)
         repository.Write(path, TestContent);
      else
         await repository.WriteAsync(path, TestContent);

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(2));

      Assert.That(
         repository.List(repository.Root).Count(),
         Is.EqualTo(1));
   }

   [Test]
   public void Delete_deletes_a_file()
   {
      var fs = Support.GetMockFs("*test");
      var repository = Support.CreateRepository(fs);

      Assert.That(repository.TryParseLocation("test", out var path));
      Assert.That(repository.FileExist(path));

      Assert.That(
         repository.List(repository.Root).Count(),
         Is.EqualTo(1));

      // deleting a file removes it
      repository.Delete(path);

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(1));

      Assert.That(
         repository.List(repository.Root).Count(),
         Is.EqualTo(0));
   }

   private static string ItemsToPaths(
      IEnumerable<Location> items)
   {
      var paths =
         items
            .Select(item => item.Repository.ToString(item))
            .OrderBy(item => item)
            .ToList();
      return string.Join(";", paths);
   }
}