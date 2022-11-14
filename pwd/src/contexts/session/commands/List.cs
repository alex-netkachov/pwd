using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class List
   : ICommandFactory
{
   private readonly ISession _session;

   public List(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return new DelegateCommand(cancellationToken => _session.List(input, cancellationToken));
   }

   public ICommand Command()
   {
      return new DelegateCommand(cancellationToken => _session.List("", cancellationToken));
   }
}