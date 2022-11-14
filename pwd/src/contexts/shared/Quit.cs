using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Quit
   : ICommandFactory
{
   private readonly IState _state;

   public Quit(
      IState state)
   {
      _state = state;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".quit" => new DelegateCommand(
            cancellationToken => _state.DisposeAsync().AsTask().WaitAsync(cancellationToken)),
         _ => null
      };
   }
}