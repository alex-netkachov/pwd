using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.session.commands;

public sealed class List(
      ILogger<List> logger,
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state,
      IView view)
   : CommandServicesBase
{
   private readonly ILogger _logger = logger;
   private readonly IRepository _repository = repository;
   private readonly IFileFactory _fileFactory = fileFactory;
   private readonly ILock _lock = @lock;
   private readonly IState _state = state;
   private readonly IView _view = view;

    public override ICommand? Create(
      string input)
   {
      return new DelegateCommand(cancellationToken => Exec(input, cancellationToken));
   }

   public ICommand Command()
   {
      return new DelegateCommand(cancellationToken => Exec("", cancellationToken));
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
            _repository
               .List(".")
               .Select(item => _repository.GetRelativePath(item, "."))
               .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
               .ToList();

         _view.WriteLine(string.Join("\n", items));
      }
      else
      {
         // show files and folders
         var items =
            _repository
               .List(
                  ".",
                  new ListOptions(false, true, false))
               .Select(item => _repository.GetRelativePath(item, "."))
               .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .ToList();

         _logger.LogInformation($"found {items.Count} items");

         var match =
            items.FirstOrDefault(
               item => string.Equals(item, input, StringComparison.OrdinalIgnoreCase));

         var chosen = match ?? (items.Count == 1 && input != "" ? items[0] : default);

         _logger.LogInformation($"chosen item: {chosen}");

         if (chosen == null)
            _view.WriteLine(string.Join("\n", items.OrderBy(item => item)));
         else
            Open(chosen);
      }

      return Task.CompletedTask;
   }
   
   private void Open(
      string name)
   {
      var file = _fileFactory.Create(_repository, _lock, name);
      _ = _state.OpenAsync(file);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}