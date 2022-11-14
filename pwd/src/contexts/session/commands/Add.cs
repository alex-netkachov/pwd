using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Add
   : ICommandFactory
{
   private readonly ISession _session;

   public Add(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "add", var name) =>
            new DelegateCommand(cancellationToken => _session.Add(name, cancellationToken)),
         _ => null
      };
   }
}