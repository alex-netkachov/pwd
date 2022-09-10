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

[assembly: InternalsVisibleTo("pwd.tests")]

namespace pwd;

public static class Program
{
   internal static async Task Run(
      ILogger logger,
      IFileSystem fs,
      IView view,
      IState state)
   {
      // read the password and initialise ciphers 
      var password = view.ReadPassword("Password: ");

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
               .AddSingleton<IClipboard, Clipboard>()
               .AddSingleton(state)
               .AddSingleton<IRepositoryFactory, RepositoryFactory>()
               .AddSingleton<IExporterFactory, ExporterFactory>()
               .AddSingleton<INameCipherFactory, NameCipherFactory>()
               .AddSingleton<IContentCipherFactory, ContentCipherFactory>()
               .AddSingleton<ISessionFactory, SessionFactory>()
               .AddSingleton<IFileFactory, FileFactory>()
               .AddSingleton<INewFileFactory, NewFileFactory>();
         });

      using var host = builder.Build();

      var services = host.Services;

      var contentCipher = services.GetRequiredService<IContentCipherFactory>().Create(password);
      var nameCipher = services.GetRequiredService<INameCipherFactory>().Create(password);
      var repository = services.GetRequiredService<IRepositoryFactory>().Create(nameCipher, contentCipher, path);
      var exporter = services.GetRequiredService<IExporterFactory>().Create(contentCipher, repository);
      var session = services.GetRequiredService<ISessionFactory>().Create(repository, exporter);
      state.Open(session);

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
            view.ReadPassword("It seems that you are creating a new repository. Please confirm password: ");
         if (confirmPassword != password)
         {
            await Console.Error.WriteLineAsync("passwords do not match");
            return;
         }
      }

      while (true)
      {
         var input = view.Read($"{state.Context.Prompt()}> ").Trim();

         if (input == ".quit")
            break;

         try
         {
            await state.Context.Process(input);
         }
         catch (Exception e)
         {
            await Console.Error.WriteLineAsync(e.Message);
         }
      }
   }

   public static async Task Main(
      string[] args)
   {
      var logger = new ConsoleLogger();
      var fs = new FileSystem();
      var state = new State(NullContext.Instance);
      var view = new View(state);

      var isGitRepository =
         fs.Directory.Exists(".git") ||
         fs.Directory.Exists("../.git") ||
         fs.Directory.Exists("../../.git");

      async Task<(string, Exception?)> Exec(string exe, string arguments)
      {
         var processStartInfo =
            new ProcessStartInfo(exe, arguments)
            {
               RedirectStandardOutput = true
            };

         Process? process;

         try
         {
            process = Process.Start(processStartInfo);

            if (process == null)
               return ("", new($"Cannot run `{exe} {arguments}`"));
         }
         catch (Exception e)
         {
            return ("", e);
         }

         return (await process.StandardOutput.ReadToEndAsync(), null);
      }

      async Task ExecChain(params Func<Task<(string, Exception?)>>[] execs)
      {
         foreach (var item in execs)
         {
            var (output, exception) = await item();
            view.WriteLine(exception != null ? exception.Message : output);
            if (exception != null)
               break;
         }
      }

      if (isGitRepository)
      {
         var (output, exception) = await Exec("git", "remote update");
         if (exception == null && output.Trim() != "Fetching origin") 
            view.Write(output);
      }

      await Run(
         logger,
         fs,
         view,
         state);

      view.Clear();

      if (isGitRepository && view.Confirm("Update the repository?", Choice.Accept))
      {
         await ExecChain(
            () => Exec("git", "add *"),
            () => Exec("git", "commit -m update"),
            () => Exec("git", "push"));
      }
   }
}

public class AutoCompletionHandler
   : IAutoCompleteHandler
{
   private readonly IState _state;

   public AutoCompletionHandler(
      IState state)
   {
      _state = state;
   }

   public char[] Separators { get; set; } = Array.Empty<char>();

   public string[] GetSuggestions(
      string text,
      int index)
   {
      return _state.Context.GetInputSuggestions(text, index);
   }
}