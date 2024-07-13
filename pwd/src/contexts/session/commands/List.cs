using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.contexts.repl;
using pwd.contexts.file;
using pwd.core.abstractions;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.session.commands;

public sealed class List(
      ILogger<List> logger,
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state,
      IView view)
   : CommandBase
{
   private readonly ILogger _logger = logger;

   public override Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var match = parameters.FirstOrDefault() ?? "";
      Exec(match, token);
      return Task.CompletedTask;
   }

   private Task Exec(
      string input,
      CancellationToken token)
   {
      const string context = $"{nameof(List)}.{nameof(Exec)}";

      _logger.LogInformation($"{context}: start with '{input}'");

      if (input == "")
      {
         _logger.LogInformation($"{context}: enumerating all the files as there is no user input");

         var items =
            repository
               .List(".")
               .Select(item => repository.GetRelativePath(item, "."))
               .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
               .ToList();

         view.WriteLine(string.Join("\n", items));
      }
      else
      {
         // show files and folders
         var items =
            repository
               .List(
                  ".",
                  new ListOptions(false, true, false))
               .Select(item => repository.GetRelativePath(item, "."))
               .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .ToList();

         _logger.LogInformation($"found {items.Count} items");

         var match =
            items.FirstOrDefault(
               item => string.Equals(item, input, StringComparison.OrdinalIgnoreCase));

         var chosen = match ?? (items.Count == 1 && input != "" ? items[0] : default);

         _logger.LogInformation($"chosen item: {chosen}");

         if (chosen == null)
            view.WriteLine(string.Join("\n", items.OrderBy(item => item)));
         else
            Open(chosen);
      }

      return Task.CompletedTask;
   }
   
   private void Open(
      string name)
   {
      var file = fileFactory.Create(repository, @lock, name);
      _ = state.OpenAsync(file);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}