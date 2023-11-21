using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;

namespace pwd.contexts.session.commands;

public sealed class List(
      ILogger logger,
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

      _logger.Info($"{context}: start with '{input}'");

      if (input == "")
      {
         _logger.Info($"{context}: enumerating all the files as there is no user input");

         var items =
            _repository.Root
               .List()
               .Select(item => ((repository.implementation.File)item).GetPath().ToString())
               .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
               .ToList();

         _view.WriteLine(string.Join("\n", items));
      }
      else
      {
         // show files and folders
         var items =
            _repository.Root
               .List(new ListOptions(false, true, false))
               .Where(item => ((repository.implementation.File)item).GetPath().ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .OfType<repository.implementation.File>()
               .ToList();

         _logger.Info($"found {items.Count} items");

         var match =
            items.FirstOrDefault(
               item => string.Equals(item.GetPath().ToString(), input, StringComparison.OrdinalIgnoreCase));

         var chosen =
            match == default
               ? items.Count == 1 && input != "" ? items[0].GetPath() : default
               : match.GetPath();

         _logger.Info($"chosen item: {chosen}");

         if (chosen == null)
            _view.WriteLine(string.Join("\n", items.Select(item => item.GetPath()).OrderBy(item => item)));
         else
            Open(chosen.ToString());
      }

      return Task.CompletedTask;
   }
   
   private void Open(
      string name)
   {
      if (!_repository.TryParsePath(name, out var path)
          || path == null)
      {
         return;
      }

      var item = _repository.Get(path);
      if (item == null)
      {
         _logger.Info($"item '{name}' not found");
         return;
      }

      _logger.Info($"found repository item for path '{name}'");

      var file = _fileFactory.Create(_repository, _lock, (repository.IFile)item);
      var _ = _state.OpenAsync(file);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}