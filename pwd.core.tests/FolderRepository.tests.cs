using System.IO.Abstractions.TestingHelpers;
using pwd.core.abstractions;

namespace pwd.core.tests;

public sealed class FolderRepository_Tests
{
   private static readonly string TestName = Support.Encrypt("test");
   private static readonly string TestContent = Support.Encrypt("test");
   
   [TestCase("", "/")]
   [TestCase("/", "/")]
   [TestCase("/test", "/test")]
   [TestCase("/test1/test2", "/test1/test2")]
   [TestCase("test1", "/test1")]
   [TestCase("test1/.//../test2", "/test2")]
   [TestCase("test1;test2", "/test1/test2")]
   public void Repository_sets_and_gets_the_working_folder_as_expected(
      string paths,
      string expectedPath)
   {
      var fs = new MockFileSystem();
      var repository = Support.CreateRepository(fs);
      foreach (var item in paths.Split(';'))
         repository.SetWorkingFolder(item);
      Assert.That(
         repository.GetWorkingFolder(),
         Is.EqualTo(expectedPath));
   }

   [Test]
   public void GetWorkingFolder_is_root_for_new_repository()
   {
      var fs = new MockFileSystem();
      var repository = Support.CreateRepository(fs);
      var workingFolder = repository.GetWorkingFolder();
      Assert.That(workingFolder, Is.EqualTo("/"));
   }

   [TestCase(null, "test", "/test")]
   [TestCase("/test1", "test2", "/test1/test2")]
   [TestCase("/test1", "", "/test1")]
   [TestCase("/test1", ".", "/test1")]
   [TestCase("/test1", "..", "/")]
   [TestCase("/test1", "../..", "/")]
   public void GetFullPath_works_as_expected(
      string? workingFolder,
      string path,
      string expectedPath)
   {
      var fs = new MockFileSystem();
      var repository = Support.CreateRepository(fs);
      if (workingFolder != null)
         repository.SetWorkingFolder(workingFolder);
      var fullPath = repository.GetFullPath(path);
      Assert.That(fullPath, Is.EqualTo(expectedPath));
   }

   [TestCase("/test", "/", "test")]
   [TestCase("/test1/file", "/test1", "file")]
   [TestCase("/test1/file", "/test1/folder1", "../file")]
   [TestCase("/test1/folder1/file", "/test1/folder2", "../folder1/file")]
   public void GetRelativePath_works_as_expected(
      string path,
      string location,
      string expected)
   {
      var fs = new MockFileSystem();
      var repository = Support.CreateRepository(fs);
      var relativePath = repository.GetRelativePath(path, location);
      Assert.That(relativePath, Is.EqualTo(expected));
   }
   
   [Test]
   public void List_root_of_empty_repository_returns_no_items()
   {
      var fs = Support.GetMockFs();
      var repository = Support.CreateRepository(fs);
      Assert.That(!repository.List("/").Any());
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_files()
   {
      var fs = Support.GetMockFs("*test");
      var repository = Support.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(2));
      var location = "/";
      var items = repository.List(location).ToList();
      var file = items.Single();
      Assert.That(
         GetName(file)!,
         Is.EqualTo("/test"));
   }

   [Test]
   public void List_root_of_non_empty_repository_returns_folders()
   {
      var fs = Support.GetMockFs("@test1/*test2");
      var repository = Support.CreateRepository(fs);
      Assert.That(
         fs.Directory.EnumerateDirectories(".").Count(),
         Is.EqualTo(1));
      var folder = repository.List("/", new ListOptions(false, true, false)).Single();
      Assert.That(
         GetName(folder),
         Is.EqualTo("/test1"));
   }

   [TestCase("test", ".", false, false, false, "")]
   [TestCase("@test", ".", false, false, false, "/test")]
   [TestCase("^test", ".", false, false, false, "")]
   [TestCase("*test", ".", false, false, false, "/test")]
   [TestCase("*_test", ".", false, false, false, "")]
   [TestCase("*_test", ".", false, false, true, "/_test")]
   [TestCase("*.test", ".", false, false, false, "")]
   [TestCase("*.test", ".", false, false, true, "/.test")]
   [TestCase("@f/*test", ".", false, false, false, "")]
   [TestCase("@f/*test", ".", true, false, false, "/f/test")]
   [TestCase("@f/*test", ".", false, true, false, "/f")]
   [TestCase("@f/@test", ".", false, true, false, "/f")]
   [TestCase("f/test", ".", false, true, false, "")]
   [TestCase("@f/*_test", ".", true, false, false, "")]
   [TestCase("@f/*_test", ".", true, false, true, "/f/_test")]
   [TestCase("@f/*test", "f", true, false, false, "/f/test")]
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

      var items =
         repository
            .List(
               listPath,
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
               "/",
               new ListOptions(true, false, false))
            .ToList();

      Assert.That(
         ItemsToPaths(items),
         Is.EqualTo("/f/test1;/f/test2;/test1;/test2"));
   }

   [TestCase(true)]
   [TestCase(false)]
   public async Task CreateFile_creates_a_file(
      bool async)
   {
      var fs = Support.GetMockFs();
      var repository = Support.CreateRepository(fs);

      if (async)
         repository.Write(TestName, TestContent);
      else
         await repository.WriteAsync(TestName, TestContent);

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(2));

      Assert.That(
         repository.List("/").Count(),
         Is.EqualTo(1));
   }

   [Test]
   public void Delete_deletes_a_file()
   {
      var fs = Support.GetMockFs("*test");
      var repository = Support.CreateRepository(fs);

      Assert.That(repository.FileExist("test"));

      Assert.That(
         repository.List("/").Count(),
         Is.EqualTo(1));

      // deleting a file removes it
      repository.Delete("test");

      Assert.That(
         fs.Directory.EnumerateFiles(".").Count(),
         Is.EqualTo(1));

      Assert.That(
         repository.List("/").Count(),
         Is.EqualTo(0));
   }

   private static string ItemsToPaths(
      IEnumerable<string> items)
   {
      var paths =
         items
            .OrderBy(item => item)
            .ToList();
      return string.Join(";", paths);
   }

   private static string? GetName(
      string path)
   {
      return path == "/" ? null : path[path.LastIndexOf('/')..];
   }
}