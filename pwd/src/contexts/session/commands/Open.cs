using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.contexts.file;

namespace pwd.contexts.session.commands;

public sealed class Open
   : ICommandFactory
{
   private readonly IRepository _repository;
   private readonly IFileFactory _fileFactory;
   private readonly ILock _lock;
   private readonly IState _state;

   public Open(
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state)
   {
      _repository = repository;
      _fileFactory = fileFactory;
      _lock = @lock;
      _state = state;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "open", var name) =>
            new DelegateCommand(async cancellationToken =>
               await Exec(name, cancellationToken)),
         _ => null
      };
   }

   private async Task Exec(
      string name,
      CancellationToken cancellationToken)
   {
      var content = await _repository.ReadAsync(name);
      var file = _fileFactory.Create(_repository, _lock, name, content);
      await _state.OpenAsync(file).WaitAsync(cancellationToken);
   }
}