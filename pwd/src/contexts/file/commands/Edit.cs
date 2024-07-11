using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Edit(
      IEnvironmentVariables environmentVariables,
      IRunner runner,
      IView view,
      IFileSystem fs,
      IRepository repository,
      string path)
   : CommandBase
{
   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var editor =
         (parameters.ElementAtOrDefault(0) ?? "") switch
         {
            "" => environmentVariables.Get("EDITOR"),
            var value => value
         };

      if (string.IsNullOrEmpty(editor))
      {
         view.WriteLine("The editor is not specified and the environment variable EDITOR is not set.");
         return;
      }

      var content = await repository.ReadAsync(path);

      var tmpFileName = fs.Path.GetTempFileName();

      try
      {
         await fs.File.WriteAllTextAsync(tmpFileName, content, token);

         var exception =
            await runner.RunAsync(
               editor,
               arguments: tmpFileName,
               token: token);

         if (exception != null)
            view.Write($"Cannot start the editor. Reason: {exception.Message}");

         var updated = await fs.File.ReadAllTextAsync(tmpFileName, token);
         if (updated == content ||
             !await view.ConfirmAsync("Update the content?", Answer.Yes, token))
         {
         }
         else
         {
            await repository.WriteAsync(path, updated);
         }
      }
      finally
      {
         fs.File.Delete(tmpFileName);
      }
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".edit";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}