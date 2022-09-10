using System.Text;
using pwd.ciphers;

namespace pwd.tests;

public sealed class Repository_Tests
{
   [Test]
   public async Task listing_an_empty_repository_returns_no_items()
   {
      var fs = Shared.GetMockFs();
      var repository = new Repository(fs, ZeroCipher.Instance, ZeroCipher.Instance, ".");
      await repository.Initialise();
      Assert.That(!repository.List("").Any());
      Assert.That(!repository.List(".").Any());
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
   public async Task listing_repository(
      string path,
      string listPath,
      bool recursive,
      bool includeFolders,
      bool includeDottedFilesAndFolders,
      string expected)
   {
      // @ - encrypt name, ^ - encrypt content, * - encrypt name and content
      var fs = Shared.GetMockFs();

      var nameCipher = new NameCipher("pwd$");
      var contentCipher = new ContentCipher("pwd$");

      var filePath =
         string.Join(
            '/',
            path.Split('/')
               .Select(item => item[0] switch
               {
                  '@' or '*' => Encoding.UTF8.GetString(nameCipher.Encrypt(item[1..])),
                  '^' => item[1..],
                  _ => item
               }));

      var fileContent =
         path.Split('/')[^1][0] is '^' or '*'
            ? await contentCipher.EncryptAsync("test")
            : Encoding.UTF8.GetBytes("test");

      var completeFilePath = fs.Path.Combine("folder1/folder2", filePath);
      fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(completeFilePath));
      await fs.File.WriteAllBytesAsync(completeFilePath, fileContent);

      var repository = new Repository(fs, nameCipher, contentCipher, "folder1/folder2");
      await repository.Initialise();

      var items = repository.List(listPath, (recursive, includeFolders, includeDottedFilesAndFolders)).ToList();
      Assert.That(string.Join(";", items.Select(item => item.Path)), Is.EqualTo(expected));
   }

   [Test]
   public async Task listing_repository_special_case1()
   {
      var fs = Shared.GetMockFs();
      var nameCipher = new NameCipher("pwd$");
      var contentCipher = new ContentCipher("pwd$");

      var f = Encoding.UTF8.GetString(await nameCipher.EncryptAsync("f"));
      var test11 = Encoding.UTF8.GetString(await nameCipher.EncryptAsync("test1"));
      var test21 = Encoding.UTF8.GetString(await nameCipher.EncryptAsync("test2"));
      var test12 = Encoding.UTF8.GetString(await nameCipher.EncryptAsync("test1"));
      var test22 = Encoding.UTF8.GetString(await nameCipher.EncryptAsync("test2"));

      fs.Directory.CreateDirectory(f);
      await fs.File.WriteAllBytesAsync($"{f}/{test12}", await contentCipher.EncryptAsync("test"));
      await fs.File.WriteAllBytesAsync($"{f}/{test22}", await contentCipher.EncryptAsync("test"));
      await fs.File.WriteAllBytesAsync($"{test11}", await contentCipher.EncryptAsync("test"));
      await fs.File.WriteAllBytesAsync($"{test21}", await contentCipher.EncryptAsync("test"));

      var repository = new Repository(fs, nameCipher, contentCipher, ".");
      await repository.Initialise();

      var items = repository.List(".", (true, false, false)).ToList();
      Assert.That(string.Join(";", items.Select(item => item.Path)), Is.EqualTo("test1;test2;f/test1;f/test2"));
   }

   [Test]
   public async Task writing_creates_a_file()
   {
      var fs = Shared.GetMockFs();
      var repository = new Repository(fs, ZeroCipher.Instance, ZeroCipher.Instance, ".");
      await repository.Initialise();

      // writing a file creates a new one
      await repository.WriteAsync("test", "test");
      Assert.That(fs.Directory.EnumerateFiles(".").Count(), Is.EqualTo(1));
      Assert.That(repository.List(".").Count(), Is.EqualTo(1));

      // overwriting a file does not create a new one
      await repository.WriteAsync("test", "test");
      Assert.That(fs.Directory.EnumerateFiles(".").Count(), Is.EqualTo(1));
      Assert.That(repository.List(".").Count(), Is.EqualTo(1));
   }

   [Test]
   public async Task deleting_deletes_a_file()
   {
      var fs = Shared.GetMockFs();
      var repository = new Repository(fs, ZeroCipher.Instance, ZeroCipher.Instance, ".");
      await repository.Initialise();

      // writing a file creates a new one
      await repository.WriteAsync("test", "test");
      Assert.That(fs.Directory.EnumerateFiles(".").Count(), Is.EqualTo(1));
      Assert.That(repository.List(".").Count(), Is.EqualTo(1));
      
      // deleting a file removes it
      repository.Delete("test");
      Assert.That(fs.Directory.EnumerateFiles(".").Count(), Is.EqualTo(0));
      Assert.That(repository.List(".").Count(), Is.EqualTo(0));
   }
}