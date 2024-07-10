using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pwd.contexts;
using pwd.contexts.file;
using pwd.contexts.session;
using pwd.core;
using pwd.core.abstractions;
using pwd.core.previous.repository;
using pwd.ui;
using pwd.ui.console;
using pwd.ui.readline;
using ListOptions = pwd.core.previous.repository.interfaces.ListOptions;
using PreviousRepositoryFactory = pwd.core.previous.repository.implementation.RepositoryFactory;
using PreviousCipherFactory = pwd.core.previous.CipherFactory;

namespace pwd;

public static class Migration
{
   public static async Task Migrate()
   {
      var fs = new FileSystem();
      var console = new StandardConsole();
      var view = new ConsoleView(console, new ConsoleReader(console));

      var services = new ServiceCollection();

      services
         .AddLogging(builder => builder.AddConsole())
         .AddSingleton(fs)
         .AddSingleton(view)
         .AddSingleton(console)
         .AddSingleton<IRunner, Runner>()
         .AddSingleton<ITimers, Timers>()
         .AddSingleton<IClipboard, Clipboard>()
         .AddSingleton<IState, State>()
         .AddSingleton<IEnvironmentVariables, EnvironmentVariables>()
         .AddTransient<RepositoryFactory>(
            provider =>
               (path, password) =>
               {
                  var repository =
                     new FolderRepository(
                        provider.GetRequiredService<ILogger<FolderRepository>>(),
                        fs,
                        (pwd, data) => new AesCipher(pwd, data),
                        provider.GetRequiredService<IStringEncoder>(),
                        path,
                        password);

                  return repository;
               })
         .AddSingleton<IExporterFactory, ExporterFactory>()
         .AddSingleton<IStringEncoder, Base64Url>()
         .AddSingleton<ISessionFactory, SessionFactory>()
         .AddSingleton<IFileFactory, FileFactory>()
         .AddSingleton<INewFileFactory, NewFileFactory>()
         .AddSingleton<ILockFactory, LockFactory>();

      var provider = services.BuildServiceProvider();

      var repositoryFactory = provider.GetRequiredService<RepositoryFactory>();
      
      var password = await view.ReadPasswordAsync("Password: ");

      var repository = repositoryFactory(@"C:\Projects\accounts1", password);

      var previousCipherFactory =
         new PreviousCipherFactory();

      var previousRepositoryFactory =
         new PreviousRepositoryFactory(
            new pwd.core.previous.ConsoleLogger(),
            fs,
            previousCipherFactory,
            pwd.core.previous.Base64Url.Instance);
      
      var previousRepository = previousRepositoryFactory.Create(password, @"C:\Projects\accounts");

      var files =
         previousRepository
            .Root
            .ListAsync(new ListOptions(true, false, true));

      await foreach (var item in files)
      {
         if (item is not pwd.core.previous.repository.interfaces.IFile file)
            continue;

         var path = file.GetPath().ToString();
         Console.WriteLine(path);

         await repository.WriteAsync(path, await file.ReadAsync());
      }
   }
}