using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;

namespace pwd.contexts.session.commands;

public sealed class List
   : ICommandFactory
{
   private readonly IRepository _repository;
   private readonly IFileFactory _fileFactory;
   private readonly ILock _lock;
   private readonly IState _state;
   private readonly IView _view;

   public List(
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state,
      IView view)
   {
      _repository = repository;
      _fileFactory = fileFactory;
      _lock = @lock;
      _state = state;
      _view = view;
   }

   public ICommand? Parse(
      string input)
   {
      return new DelegateCommand(cancellationToken => Exec(input, cancellationToken));
   }

   public ICommand Command()
   {
      return new DelegateCommand(cancellationToken => Exec("", cancellationToken));
   }
   
   private async Task Exec(
      string input,
      CancellationToken cancellationToken)
   {
      if (input == "")
      {
         // show all files if there is no user input
         var items =
            _repository
               .List(".")
               .Select(item => item.Path)
               .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
               .ToList();

         _view.WriteLine(string.Join("\n", items));
      }
      else
      {
         // show files and folders
         var items =
            _repository.List(".", (false, true, false))
               .Where(item => item.Path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .ToList();

         var match =
            items.FirstOrDefault(
               item => string.Equals(item.Path, input, StringComparison.OrdinalIgnoreCase));

         var chosen =
            match == default
               ? items.Count == 1 && input != "" ? items[0].Path : default
               : match.Path;

         if (chosen == null)
            _view.WriteLine(string.Join("\n", items.Select(item => item.Path).OrderBy(item => item)));
         else
            await Open(chosen, cancellationToken);
      }
   }
   
   private async Task Open(
      string name,
      CancellationToken cancellationToken)
   {
      var item = _repository.Get(name);
      if (item == null)
         return;

      var file = _fileFactory.Create(_repository, _lock, item);
      await _state.OpenAsync(file).WaitAsync(cancellationToken);
   }
}