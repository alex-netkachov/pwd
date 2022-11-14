using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using pwd.ciphers;
using pwd.contexts;
using pwd.contexts.file;
using pwd.readline;
using pwd.mocks;

namespace pwd;

public static class Shared
{
   public static contexts.file.File CreateFileContext(
      string path = "",
      string name = "",
      string content = "",
      IFileSystem? fs = null,
      IRepository? repository = null,
      IClipboard? clipboard = null,
      IView? view = null,
      IState? state = null,
      ILock? @lock = null)
   {
      path = string.IsNullOrEmpty(path) ? Path.GetFileName(Path.GetTempFileName()) : path;
      name = string.IsNullOrEmpty(path) ? Path.GetFileName(path) : name;
      repository ??= Mock.Of<IRepository>();
      
      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
            services
               .AddSingleton(Mock.Of<ILogger>())
               .AddSingleton(clipboard ?? Mock.Of<IClipboard>())
               .AddSingleton(fs ?? Mock.Of<IFileSystem>())
               .AddSingleton(repository)
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton<IFileFactory, FileFactory>());

      using var host = builder.Build();

      return (contexts.file.File)host.Services
         .GetRequiredService<IFileFactory>()
         .Create(
            repository,
            @lock ?? Mock.Of<ILock>(),
            name,
            content);
   }

   public static Session CreateSessionContext(
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
               .AddSingleton(Mock.Of<ILogger>())
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

   public static IFileSystem GetMockFs()
   {
      var fs = new MockFileSystem();
      fs.Directory.CreateDirectory("container/test");
      var dir = fs.DirectoryInfo.FromDirectoryName("container/test").FullName;
      fs.Directory.SetCurrentDirectory(dir);
      return fs;
   }

   public static IFileSystem FileLayout1(IFileSystem fs)
   {
      const string text = "test"; 
      var cipher = new ContentCipher("secret");
      var encrypted = cipher.Encrypt(text);

      fs.File.WriteAllText("file", text);
      fs.File.WriteAllText(".hidden", text);
      fs.Directory.CreateDirectory("regular_dir");
      fs.File.WriteAllText("regular_dir/file", text);
      fs.File.WriteAllText("regular_dir/.hidden", text);
      fs.Directory.CreateDirectory(".hidden_dir");
      fs.File.WriteAllText(".hidden_dir/file", text);
      fs.File.WriteAllText(".hidden_dir/.hidden", text);
      fs.File.WriteAllBytes("encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_encrypted", encrypted);
      fs.File.WriteAllBytes("regular_dir/encrypted", encrypted);
      fs.File.WriteAllBytes("regular_dir/.hidden_encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_dir/encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_dir/.hidden_encrypted", encrypted);
      return fs;
   }

   private static async Task Test_AutoCompletionHandler()
   {
      var fs = FileLayout1(GetMockFs());
      var console = new StandardConsole();
      var view = new View(console, new Reader(console));
      var repository = new Repository(fs, new ZeroCipher(), new ContentCipher("secret"), ".");
      await repository.Initialise();
      var session = CreateSessionContext(repository, view: view);
      Assert.That(string.Join(";", session.Get("../")), Is.EqualTo("../test"));
      Assert.That(string.Join(";", session.Get("")), Is.EqualTo("encrypted;regular_dir"));
      Assert.That(string.Join(";", session.Get("enc")), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Get("encrypted")), Is.EqualTo("encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir")), Is.EqualTo("regular_dir"));
      Assert.That(string.Join(";", session.Get("regular_dir/")), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir/enc")), Is.EqualTo("regular_dir/encrypted"));
      Assert.That(string.Join(";", session.Get("regular_dir/encrypted")), Is.EqualTo("regular_dir/encrypted"));
   }
   
   public static void Run(
      Func<Task> action)
   {
      action();
   }
}