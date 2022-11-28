using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.session.commands;

public sealed class Add
   : ICommandFactory
{
   private readonly IState _state;
   private readonly INewFileFactory _newFileFactory;
   private readonly IRepository _repository;

   public Add(
      IState state,
      INewFileFactory newFileFactory,
      IRepository repository)
   {
      _state = state;
      _newFileFactory = newFileFactory;
      _repository = repository;
   }

   public ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "add", var name) =>
            new DelegateCommand(async cancellationToken =>
            {
               await _state.OpenAsync(_newFileFactory.Create(_repository, name)).WaitAsync(cancellationToken);
            }),
         _ => null
      };
   }
}