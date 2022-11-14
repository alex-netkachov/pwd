using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Open
   : ICommandFactory
{
   private readonly ISession _session;

   public Open(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "open", var name) =>
            new DelegateCommand(cancellationToken => _session.Open(name, cancellationToken)),
         _ => null
      };
   }
}