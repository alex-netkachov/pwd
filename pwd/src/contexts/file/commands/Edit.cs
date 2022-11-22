using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Edit
   : ICommandFactory
{
   private readonly IRunner _runner;
   private readonly IView _view;
   private readonly IFileSystem _fs;
   private readonly IRepository _repository;
   private readonly IRepositoryItem _item;

   public Edit(
      IRunner runner,
      IView view,
      IFileSystem fs,
      IRepository repository,
      IRepositoryItem item)
   {
      _runner = runner;
      _view = view;
      _fs = fs;
      _repository = repository;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "edit", var editor) => new DelegateCommand(async cancellationToken =>
         {
            var chosenEditor =
               string.IsNullOrEmpty(editor)
                  ? Environment.GetEnvironmentVariable("EDITOR")
                  : editor;

            if (string.IsNullOrEmpty(chosenEditor))
            {
               _view.WriteLine("The editor is not specified and the environment variable EDITOR is not set.");
            }
            else
            {
               var content = await _item.ReadAsync();

               var path = _fs.Path.GetTempFileName();
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
                  await _repository.WriteAsync(_item.Name, updated);
               }

               Process? process = null;
               try
               {
                  var startInfo = new ProcessStartInfo(chosenEditor, path);
                  process = Process.Start(startInfo);
                  if (process == null)
                  {
                     _view.WriteLine($"Starting the process '{startInfo.FileName}' failed.");
                  }
                  else
                  {
                     await process.WaitForExitAsync(cancellationToken);

                  }
               }
               catch (TaskCanceledException)
               {
                  // this catch captures an exception in interrupted process.WaitForExitAsync(...)
                  if (process == null || process.HasExited)
                  {
                  }
                  else
                  {
                     process.Kill();
                  }

                  // kill the process
               }
               finally
               {
                  _fs.File.Delete(path);
               }
            }
         }),
         _ => null
      };
   }
}