using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using pwd.contexts;
using pwd.contexts.file;
using pwd.contexts.session;
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
      ILock? @lock = null)
   {
      var builder = Host.CreateDefaultBuilder();
      
      builder.ConfigureServices(
         services =>
            services
               .AddSingleton(Mock.Of<ILogger>())
               .AddSingleton(Mock.Of<IEnvironmentVariables>())
               .AddSingleton(runner ?? Mock.Of<IRunner>())
               .AddSingleton(clipboard ?? Mock.Of<IClipboard>())
               .AddSingleton(fs ?? Mock.Of<IFileSystem>())
               .AddSingleton(repository ?? Mock.Of<IRepository>())
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
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

      var mockRepositoryItem = new Mock<repository.IFile>();
      mockRepositoryItem
         .SetupGet(m => m.Name)
         .Returns(fileName);
      mockRepositoryItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(() => Task.FromResult(content));

      return (contexts.file.File)host.Services
         .GetRequiredService<IFileFactory>()
         .Create(repository, @lock, mockRepositoryItem.Object);
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
      var dir = fs.DirectoryInfo.New("container/test").FullName;
      fs.Directory.SetCurrentDirectory(dir);
      return fs;
   }

   public static IFileSystem FileLayout1(IFileSystem fs)
   {
      const string text = "test"; 
      var cipher = new Cipher("secret");
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
      var repository = new Repository(fs, new Cipher("secret"), Base64Url.Instance, ".");
      var session = CreateSessionContext(repository, view: view);
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
}
