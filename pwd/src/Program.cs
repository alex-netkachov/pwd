using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using pwd.contexts;
using pwd.contexts.file;
using pwd.contexts.session;
using pwd.readline;
using pwd.repository;
using pwd.repository.implementation;

[assembly: InternalsVisibleTo("pwd.tests")]

namespace pwd;

public record Settings(
   TimeSpan LockTimeout);

public static class Program
{
   internal static IHost SetupHost(
         ILogger logger,
         IConsole console,
         IFileSystem fs,
         ICipherFactory cipherFactory,
         IView view)
   {
      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
         {
            services
               .AddSingleton(logger)
               .AddSingleton(fs)
               .AddSingleton(view)
               .AddSingleton(console)
               .AddSingleton(cipherFactory)
               .AddSingleton<IRunner, Runner>()
               .AddSingleton<ITimers, Timers>()
               .AddSingleton<IClipboard, Clipboard>()
               .AddSingleton<IState, State>()
               .AddSingleton<IEnvironmentVariables, EnvironmentVariables>()
               .AddSingleton<IFactory, RepositoryFactory>()
               .AddSingleton<IExporterFactory, ExporterFactory>()
               .AddSingleton<IEncoder, Base64Url>()
               .AddSingleton<ISessionFactory, SessionFactory>()
               .AddSingleton<IFileFactory, FileFactory>()
               .AddSingleton<INewFileFactory, NewFileFactory>()
               .AddSingleton<ILockFactory, LockFactory>();
         });

      return builder.Build();
   }

   internal static async Task Run(
      IHost host,
      Settings settings)
   {
      var services = host.Services;

      var fs = services.GetRequiredService<IFileSystem>();
      var view = services.GetRequiredService<IView>();

      // read the password and initialise ciphers 
      var password = await view.ReadPasswordAsync("Password: ");

      // open the repository from the current working folder
      var path = fs.Path.GetFullPath(".");

      var @lock = services.GetRequiredService<ILockFactory>().Create(password, settings.LockTimeout);
      @lock.Password();

      var cipher = services.GetRequiredService<ICipherFactory>().Create(password);
      var repository = services.GetRequiredService<IFactory>().Create(password, path);

      try
      {
         var decryptErrors = new List<string>();
         var yamlErrors = new List<string>();

         await foreach (var item in repository.Root.ListAsync())
         {
            view.Write(".");
         }

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

      /*
      var files = repository.Root.List(new ListOptions(true, false, true)).ToList();
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
      */

      var exporter = services.GetRequiredService<IExporterFactory>().Create(cipher, repository);
      var session = services.GetRequiredService<ISessionFactory>().Create(repository, exporter, @lock);

      var state = services.GetRequiredService<IState>();
      var subscription = state.Subscribe();
      await state.OpenAsync(session);
      await subscription.ReadAsync();
   }

   public static async Task Main(
      string[] args)
   {
      var logger = new NullLogger();
      var console = new StandardConsole();
      var view = new View(console, new Reader(console));
      var fs = new FileSystem();
      var cipherFactory = new CipherFactory();

      var isGitRepository =
         fs.Directory.Exists(".git") ||
         fs.Directory.Exists("../.git") ||
         fs.Directory.Exists("../../.git");

      if (isGitRepository)
      {
         await Exec(view, "git", "remote update");
         var (status, _, e) = await Exec(view, "git", "status");
         if (e == null &&
             status.Contains("Your branch is behind") &&
             await view.ConfirmAsync("Pull changes from the remote?", Answer.Yes))
         {
            await Exec(view, "git", "pull");
         }
      }

      using var host = SetupHost(logger, console, fs, cipherFactory, view);

      await Run(host, new(TimeSpan.FromMinutes(5)));

      view.Clear();

      if (isGitRepository && await view.ConfirmAsync("Update the repository?", Answer.Yes))
      {
         await ExecChain(
            () => Exec(view, "git", "add ."),
            () => Exec(view, "git", "commit -m update"),
            () => Exec(view, "git", "push"));
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
      IView view,
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

      view.WriteLine($"> {exe} {arguments}");

      Process? process;

      try
      {
         process = Process.Start(processStartInfo);

         if (process == null)
            throw new($"Cannot run `{exe} {arguments}`");
      }
      catch (Exception e)
      {
         view.WriteLine(e.Message);
         return ("", "", e);
      }

      var stdout = await process.StandardOutput.ReadToEndAsync();
      var stderr = await process.StandardError.ReadToEndAsync();

      if (stdout != "")
         view.WriteLine(stdout.TrimEnd());
      if (stderr != "")
         view.WriteLine(stderr.TrimEnd());

      return (stdout, stderr, null);
   }
}