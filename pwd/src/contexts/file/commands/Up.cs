using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up
   : ICommandFactory
{
   private readonly IState _state;

   public Up(
      IState state)
   {
      _state = state;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".." => new DelegateCommand(async cancellationToken => await _state.BackAsync().WaitAsync(cancellationToken)),
         _ => null
      };
   }
}