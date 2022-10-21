using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using pwd.ciphers;
using pwd.contexts;
using pwd.readline;

[assembly: InternalsVisibleTo("pwd.tests")]

namespace pwd;

public record Settings(
   TimeSpan LockTimeout);

public static class Program
{
   internal static async Task Run(
      ILogger logger,
      IConsole console,
      IFileSystem fs,
      IView view,
      Settings settings)
   {
      // read the password and initialise ciphers 
      var password = await view.ReadPasswordAsync("Password: ");

      // open the repository from the current working folder
      var path = fs.Path.GetFullPath(".");

      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
         {
            services
               .AddSingleton(logger)
               .AddSingleton(fs)
               .AddSingleton(view)
               .AddSingleton(console)
               .AddSingleton<IClipboard, Clipboard>()
               .AddSingleton<IState, State>()
               .AddSingleton<IRepositoryFactory, RepositoryFactory>()
               .AddSingleton<IExporterFactory, ExporterFactory>()
               .AddSingleton<INameCipherFactory, NameCipherFactory>()
               .AddSingleton<IContentCipherFactory, ContentCipherFactory>()
               .AddSingleton<ISessionFactory, SessionFactory>()
               .AddSingleton<IFileFactory, FileFactory>()
               .AddSingleton<INewFileFactory, NewFileFactory>()
               .AddSingleton<ILockFactory, LockFactory>();
         });

      using var host = builder.Build();

      var services = host.Services;

      var @lock = services.GetRequiredService<ILockFactory>().Create(password, settings.LockTimeout);
      @lock.Password();

      var contentCipher = services.GetRequiredService<IContentCipherFactory>().Create(password);
      var nameCipher = services.GetRequiredService<INameCipherFactory>().Create(password);
      var repository = services.GetRequiredService<IRepositoryFactory>().Create(nameCipher, contentCipher, path);

      try
      {
         var decryptErrors = new List<string>();
         var yamlErrors = new List<string>();
         await ((Repository) repository).Initialise((file, name, decryptError, yamlError) =>
         {
            if (decryptError != null)
            {
               view.Write("*");
               decryptErrors.Add(file);
            }
            else if (yamlError != null)
            {
               view.Write("+");
               yamlErrors.Add(name ?? file);
            }
            else
            {
               view.Write(".");
            }
         });

         view.WriteLine("");

         if (decryptErrors.Count > 0)
         {
            var more = decryptErrors.Count > 3 ? ", ..." : "";
            var failuresText = string.Join(", ", decryptErrors.Take(Math.Min(3, decryptErrors.Count)));
            view.WriteLine($"Integrity check failed for: {failuresText}{more}");
         }

         if (yamlErrors.Count > 0)
            view.WriteLine($"YAML check failed for: {string.Join(", ", yamlErrors)}");
      }
      catch (Exception e)
      {
         await Console.Error.WriteLineAsync(e.ToString());
         return;
      }

      var files = repository.List(".", (true, false, true)).ToList();
      view.WriteLine($"repository contains {files.Count} file{files.Count switch {1 => "", _ => "s"}}");

      if (files.Count == 0)
      {
         var confirmPassword =
            await view.ReadPasswordAsync("It seems that you are creating a new repository. Please confirm password: ");
         if (confirmPassword != password)
         {
            view.WriteLine("passwords do not match");
            return;
         }
      }

      var exporter = services.GetRequiredService<IExporterFactory>().Create(contentCipher, repository);
      var session = services.GetRequiredService<ISessionFactory>().Create(repository, exporter, @lock);

      var state = services.GetRequiredService<IState>();
      var subscription = state.Subscribe();
      await state.OpenAsync(session);
      await subscription.ReadAsync();
   }

   public static async Task Main(
      string[] args)
   {
      var logger = new ConsoleLogger();
      var console = new StandardConsole();
      var view = new View(console, new Reader(console));
      var fs = new FileSystem();

      var isGitRepository =
         fs.Directory.Exists(".git") ||
         fs.Directory.Exists("../.git") ||
         fs.Directory.Exists("../../.git");

      if (isGitRepository)
      {
         await Exec(logger, "git", "remote update");
         var (status, _, e) = await Exec(logger, "git", "status");
         if (e == null &&
             status.Contains("Your branch is behind") &&
             await view.ConfirmAsync("Pull changes from the remote?", Answer.Yes))
         {
            await Exec(logger, "git", "pull");
         }
      }

      await Run(
         logger,
         console,
         fs,
         view,
         new(TimeSpan.FromMinutes(5)));

      view.Clear();

      if (isGitRepository && await view.ConfirmAsync("Update the repository?", Answer.Yes))
      {
         await ExecChain(
            () => Exec(logger, "git", "add ."),
            () => Exec(logger, "git", "commit -m update"),
            () => Exec(logger, "git", "push"));
      }
   }
   
   private static async Task ExecChain(
      params Func<Task<(string, string, Exception?)>>[] execs)
   {
      foreach (var item in execs)
      {
         var (_, _, exception) = await item();
         if (exception != null)
            break;
      }
   }

   private static async Task<(string StdOut, string StdErr, Exception? Exception)> Exec(
      ILogger logger,
      string exe,
      string arguments)
   {
      var processStartInfo =
         new ProcessStartInfo(exe, arguments)
         {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
         };

      logger.Info($"> {exe} {arguments}");

      Process? process;

      try
      {
         process = Process.Start(processStartInfo);

         if (process == null)
            throw new($"Cannot run `{exe} {arguments}`");
      }
      catch (Exception e)
      {
         logger.Error(e.Message);
         return ("", "", e);
      }

      var stdout = await process.StandardOutput.ReadToEndAsync();
      var stderr = await process.StandardError.ReadToEndAsync();

      if (stdout != "")
         logger.Info(stdout.TrimEnd());
      if (stderr != "")
         logger.Info(stderr.TrimEnd());

      return (stdout, stderr, null);
   }
}