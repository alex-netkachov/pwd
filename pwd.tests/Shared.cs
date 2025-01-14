using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pwd.console;
using pwd.console.abstractions;
using pwd.contexts;
using pwd.contexts.file;
using pwd.contexts.session;
using pwd.mocks;
using pwd.core;
using pwd.core.abstractions;
using pwd.library.interfaced;
using pwd.ui.abstractions;
using Console = pwd.console.Console;

namespace pwd;

public static class Shared
{
   public static File CreateFileContext(
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
               .AddSingleton(fs ?? Mock.Of<IFileSystem>())
               .AddSingleton(repository ?? Mock.Of<IRepository>())
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(Mock.Of<IPresenter>())
               .AddSingleton<Func<IView>>(_ => () => view ?? Mock.Of<IView>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton(@lock ?? Mock.Of<ILock>())
               .AddSingleton<IFileFactory, FileFactory>());

      using var host = builder.Build();

      return CreateFileContext(path, name, content, host);
   }

   private static File CreateFileContext(
      string path,
      string name,
      string content,
      IHost host)
   {
      path =
         string.IsNullOrEmpty(path)
            ? "filename"
            : path;

      var repository = host.Services.GetRequiredService<IRepository>();
      var @lock = host.Services.GetRequiredService<ILock>();

      return (File)host.Services
         .GetRequiredService<IFileFactory>()
         .Create(repository, @lock, path);
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
               .AddSingleton(Mock.Of<IPresenter>())
               .AddSingleton<Func<IView>>(_ => () => view ?? Mock.Of<IView>())
               .AddSingleton(@lock ?? Mock.Of<ILock>())
               .AddSingleton<ISessionFactory, SessionFactory>());

      using var host = builder.Build();

      return (Session)host.Services
         .GetRequiredService<ISessionFactory>()
         .Create(
            repository ?? Mock.Of<IRepository>(),
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

   private static void Test_AutoCompletionHandler()
   {
      var fs = GetMockFs().FileLayout1();
      var console = new Console();
      var view = new View();
      view.Activate(console);

      var repository =
         new FolderRepository(
            Mock.Of<ILogger<FolderRepository>>(),
            fs,
            (_, _) => GetTestCipher(),
            Base64Url.Instance,
            ".",
            "");

      var session = CreateSessionContext(Mock.Of<ILogger>(), repository, view: view);
      Assert.That(string.Join(";", session.Get("../", -1)), Is.EqualTo("../test"));
      Assert.That(string.Join(";", session.Get("", -1)), Is.EqualTo("encrypted;regular_dir"));
      Assert.That(string.Join(";", session.Get("enc", -1)), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Get("encrypted", -1)), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir", -1)), Is.EqualTo("regular_dir"));
      Assert.That(string.Join(";", session.Get("regular_dir/", -1)), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir/enc", -1)), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir/encrypted", -1)), Is.EqualTo("regular_dir/encrypted"));
   }
   
   public static void Run(
      Func<Task> action)
   {
      action();
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
      IFileSystem? fs = null)
   {
      var provider =
         new ServiceCollection()
            .AddLogging()
            .AddSingleton(fs ?? Mock.Of<IFileSystem>())
            .AddSingleton<CipherFactory>((_, _) => GetTestCipher())
            .AddSingleton<IStringEncoder>(Base64Url.Instance)
            .AddSingleton<RepositoryFactory>(
               provider => (path, password) => new FolderRepository(
                  provider.GetRequiredService<ILogger<FolderRepository>>(),
                  provider.GetRequiredService<IFileSystem>(),
                  provider.GetRequiredService<CipherFactory>(),
                  provider.GetRequiredService<IStringEncoder>(),
                  path,
                  password))
            .BuildServiceProvider();

      var repositoryFactory =
         provider.GetRequiredService<RepositoryFactory>();

      var repository =
         repositoryFactory(
            ".",
            "");

      return repository;
   }

   public static ICipher GetTestCipher()
   {
      return new AesCipher(
         new byte[32],
         AesInitialisationData.Zero.ToArray());
   }
}
