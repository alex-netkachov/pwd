using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using pwd.contexts;
using IFile=pwd.contexts.IFile;

[assembly: InternalsVisibleTo("pwd.tests")]

namespace pwd;

public static class Program
{
   internal static async Task Run(
      IFileSystem fs,
      IView view,
      IClipboard clipboard,
      IState state)
   {
      // read the password and initialise ciphers 
      var password = view.ReadPassword("Password: ");
      var nameCipher = new NameCipher(password);
      var contentCipher = new ContentCipher(password);

      // open the repository from the current working folder
      var path = fs.Path.GetFullPath(".");
      view.WriteLine($"path = {path}");
      using var repository = new Repository(fs, nameCipher, contentCipher, path);

      var exporter = new Exporter(contentCipher, repository, fs);

      IFile FileFactory(string name, string content)
      {
         return new File(clipboard, fs, repository, state, view, name, content);
      }

      INewFile NewFileFactory(string name)
      {
         return new NewFile(repository, state, view, name);
      }

      var session = new Session(exporter, repository, state, view, FileFactory, NewFileFactory);

      state.Open(session);

      try
      {
         var decryptErrors = new List<string>();
         var yamlErrors = new List<string>();
         await repository.Initialise((file, name, decryptError, yamlError) =>
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
      var fs = new FileSystem();
      using var clipboard = new Clipboard();
      var state = new State(NullContext.Instance);
      var view = new View(state);

      await Run(
         fs,
         view,
         clipboard,
         state);

      view.Clear();
      if ((fs.Directory.Exists(".git") || fs.Directory.Exists("../.git") ||
           fs.Directory.Exists("../../.git")) &&
          view.Confirm("Update the repository?"))
      {
         var tempQualifier = new[] {"add *", "commit -m update", "push"}
            .Select(_ =>
            {
               try
               {
                  Process.Start(new ProcessStartInfo("git", _))?.WaitForExit();
                  return default;
               }
               catch (Exception e)
               {
                  return e;
               }
            })
            .FirstOrDefault(e => e != null);

         if (tempQualifier != null)
            Console.Error.WriteLine(tempQualifier);
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