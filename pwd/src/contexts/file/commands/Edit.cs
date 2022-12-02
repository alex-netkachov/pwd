using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Edit
   : CommandServicesBase
{
   private readonly IEnvironmentVariables _environmentVariables;
   private readonly IRunner _runner;
   private readonly IView _view;
   private readonly IFileSystem _fs;
   private readonly IRepositoryItem _item;

   public Edit(
      IEnvironmentVariables environmentVariables,
      IRunner runner,
      IView view,
      IFileSystem fs,
      IRepositoryItem item)
   {
      _environmentVariables = environmentVariables;
      _runner = runner;
      _view = view;
      _fs = fs;
      _item = item;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "edit", var editor) => new DelegateCommand(async cancellationToken =>
         {
            var chosenEditor =
               string.IsNullOrEmpty(editor)
                  ? _environmentVariables.Get("EDITOR")
                  : editor;

            if (string.IsNullOrEmpty(chosenEditor))
            {
               _view.WriteLine("The editor is not specified and the environment variable EDITOR is not set.");
               return;
            }

            var content = await _item.ReadAsync(cancellationToken);

            var path = _fs.Path.GetTempFileName();

            try
            {
               await _fs.File.WriteAllTextAsync(path, content, cancellationToken);

               var exception =
                  await _runner.RunAsync(
                     chosenEditor,
                     arguments: path,
                     cancellationToken: cancellationToken);

               if (exception != null)
                  _view.Write($"Cannot start the editor. Reason: {exception.Message}");

               var updated = await _fs.File.ReadAllTextAsync(path, cancellationToken);
               if (updated == content ||
                   !await _view.ConfirmAsync("Update the content?", Answer.Yes, cancellationToken))
               {
               }
               else
               {
                  await _item.WriteAsync(updated);
               }
            }
            finally
            {
               _fs.File.Delete(path);
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
         ? new[] { key }
         : Array.Empty<string>();
   }
}