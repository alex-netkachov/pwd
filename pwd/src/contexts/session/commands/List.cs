using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;
using pwd.repository.implementation;

namespace pwd.contexts.session.commands;

public sealed class List
   : CommandServicesBase
{
    private readonly ILogger _logger;
    private readonly IRepository _repository;
   private readonly IFileFactory _fileFactory;
   private readonly ILock _lock;
   private readonly IState _state;
   private readonly IView _view;

   public List(
      ILogger logger,
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state,
      IView view)
   {
      _logger = logger;
      _repository = repository;
      _fileFactory = fileFactory;
      _lock = @lock;
      _state = state;
      _view = view;
   }

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
      CancellationToken cancellationToken)
   {
      if (input == "")
      {
         // show all files if there is no user input
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