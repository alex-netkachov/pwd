using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using pwd.context.repl;
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
   : CommandServicesBase
{
   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "edit", var editor) => new DelegateCommand(async cancellationToken =>
         {
            var chosenEditor =
               string.IsNullOrEmpty(editor)
                  ? environmentVariables.Get("EDITOR")
                  : editor;

            if (string.IsNullOrEmpty(chosenEditor))
            {
               view.WriteLine("The editor is not specified and the environment variable EDITOR is not set.");
               return;
            }

            var content = await repository.ReadAsync(path);

            var tmpFileName = fs.Path.GetTempFileName();

            try
            {
               await fs.File.WriteAllTextAsync(tmpFileName, content, cancellationToken);

               var exception =
                  await runner.RunAsync(
                     chosenEditor,
                     arguments: tmpFileName,
                     cancellationToken: cancellationToken);

               if (exception != null)
                  view.Write($"Cannot start the editor. Reason: {exception.Message}");

               var updated = await fs.File.ReadAllTextAsync(tmpFileName, cancellationToken);
               if (updated == content ||
                   !await view.ConfirmAsync("Update the content?", Answer.Yes, cancellationToken))
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
         }),
         _ => null
      };
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