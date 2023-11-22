using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using pwd.contexts;
using pwd.contexts.file;
using pwd.contexts.session;
using pwd.mocks;
using pwd.readline;
using pwd.repository;
using pwd.repository.implementation;

namespace pwd;

public static class Shared
{
   public static contexts.file.File CreateFileContext(
      string path = "",
      string name = "",
      string content = "",
      IFileSystem? fs = null,
      IRunner? runner = null,
      IRepository? repository = null,
      IClipboard? clipboard = null,
      IView? view = null,
      IState? state = null,
      ILock? @lock = null,
      ILogger? logger = null)
   {
      var builder = Host.CreateDefaultBuilder();
      
      builder.ConfigureServices(
         services =>
            services
               .AddSingleton(logger ?? Mock.Of<ILogger>())
               .AddSingleton(Mock.Of<IEnvironmentVariables>())
               .AddSingleton(runner ?? Mock.Of<IRunner>())
               .AddSingleton(clipboard ?? Mock.Of<IClipboard>())
               .AddSingleton(fs ?? new MockFileSystem())
               .AddSingleton(repository ?? Mock.Of<IRepository>())
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton(@lock ?? Mock.Of<ILock>())
               .AddSingleton<IFileFactory, FileFactory>());

      using var host = builder.Build();

      return CreateFileContext(path, name, content, host);

   }

   private static contexts.file.File CreateFileContext(
      string path,
      string name,
      string content,
      IHost host)
   {
      path =
         string.IsNullOrEmpty(path)
            ? System.IO.Path.GetFileName(System.IO.Path.GetTempFileName())
            : path;

      var fs = host.Services.GetRequiredService<IFileSystem>();
      var repository = host.Services.GetRequiredService<IRepository>();
      var @lock = host.Services.GetRequiredService<ILock>();

      var fileName =
         Name.Parse(fs, string.IsNullOrEmpty(path)
         ? System.IO.Path.GetFileName(path)
         : name);

      var file = (repository.IFile)repository.Root.Get(fileName)!;

      return (contexts.file.File)host.Services
         .GetRequiredService<IFileFactory>()
         .Create(repository, @lock, file);
   }

   public static Session CreateSessionContext(
      ILogger? logger = null,
      IRepository? repository = null,
      IExporter? exporter = null,
      IView? view = null,
      IState? state = null,
      IFileFactory? fileFactory = null,
      INewFileFactory? newFileFactory = null,
      ILock? @lock = null)
   {
      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
            services
               .AddSingleton(logger ?? Mock.Of<ILogger>())
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton(fileFactory ?? Mock.Of<IFileFactory>())
               .AddSingleton(newFileFactory ?? Mock.Of<INewFileFactory>())
               .AddSingleton(@lock ?? Mock.Of<ILock>())
               .AddSingleton<ISessionFactory, SessionFactory>());

      using var host = builder.Build();

      return (Session)host.Services
         .GetRequiredService<ISessionFactory>()
         .Create(
            repository ?? Mock.Of<IRepository>(),
            exporter ?? Mock.Of<IExporter>(),
            @lock ?? Mock.Of<ILock>());
   }

   public static IFileSystem GetMockFs(
      string entries = "")
   {
      var fs = new MockFileSystem();
      fs.Directory.CreateDirectory("container/test");
      var dir = fs.DirectoryInfo.New("container/test").FullName;
      fs.Directory.SetCurrentDirectory(dir);

      // entries := item [; item] *
      // item := [ @ | ^ | * ] name [ / item ] *
      // @ - encrypt name
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

   public static IFileSystem FileLayout1(
      IFileSystem fs)
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

   public static IFileSystem FileLayout2(
      IFileSystem fs)
   {
      const string text = "test"; 
      var encryptedContent = Encrypt(text);

      var file = Encrypt("file");
      var hidden = Encrypt(".hidden");
      var encrypted = Encrypt("encrypted");
      var hiddenEncrypted = Encrypt(".hidden_encrypted");
      var regularDir = Encrypt("regular_dir");
      var hiddenDir = Encrypt(".hidden_dir");

      fs.File.WriteAllText(file, text);
      fs.File.WriteAllText(hidden, text);
      fs.File.WriteAllText(encrypted, encryptedContent);
      fs.File.WriteAllText(hiddenEncrypted, encryptedContent);
      fs.Directory.CreateDirectory(regularDir);
      fs.File.WriteAllText($"{regularDir}/{file}", text);
      fs.File.WriteAllText($"{regularDir}/{hidden}", text);
      fs.File.WriteAllText($"{regularDir}/{encrypted}", encryptedContent);
      fs.File.WriteAllText($"{regularDir}/{hiddenEncrypted}", encryptedContent);
      fs.Directory.CreateDirectory(hiddenDir);
      fs.File.WriteAllText($"{hiddenDir}/{file}", text);
      fs.File.WriteAllText($"{hiddenDir}/{hidden}", text);
      fs.File.WriteAllText($"{hiddenDir}/{encrypted}", encryptedContent);
      fs.File.WriteAllText($"{hiddenDir}/{hiddenEncrypted}", encryptedContent);

      return fs;
   }

   private static void Test_AutoCompletionHandler()
   {
      var fs = FileLayout1(GetMockFs());
      var console = new StandardConsole();
      var view = new View(console, new Reader(console));
      var repository = new Repository(Mock.Of<ILogger>(), fs, FastTestCipher.Instance, Base64Url.Instance, ".");
      var session = CreateSessionContext(Mock.Of<ILogger>(), repository, view: view);
      Assert.That(string.Join(";", session.Suggestions("../")), Is.EqualTo("../test"));
      Assert.That(string.Join(";", session.Suggestions("")), Is.EqualTo("encrypted;regular_dir"));
      Assert.That(string.Join(";", session.Suggestions("enc")), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Suggestions("encrypted")), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Suggestions("regular_dir")), Is.EqualTo("regular_dir"));
      Assert.That(string.Join(";", session.Suggestions("regular_dir/")), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Suggestions("regular_dir/enc")), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Suggestions("regular_dir/encrypted")), Is.EqualTo("regular_dir/encrypted"));
   }
   
   public static void Run(
      Func<Task> action)
   {
      action();
   }

   public static string Encrypt(
      string input)
   {
      var encrypted = FastTestCipher.Instance.Encrypt(input);
      var encoded = Base64Url.Instance.Encode(encrypted);
      return encoded;
   }

   public static IRepository CreateRepository(
      IFileSystem? fs = null,
      ILogger? logger = null)
   {
      return new Repository(
         logger ?? Mock.Of<ILogger>(),
         fs ?? Mock.Of<IFileSystem>(),
         FastTestCipher.Instance,
         Base64Url.Instance,
         ".");
   }
}
