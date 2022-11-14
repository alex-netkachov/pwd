using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Export
   : ICommandFactory
{
   private readonly ISession _session;

   public Export(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "export", var name) =>
            new DelegateCommand(_ => _session.Export(name)),
         _ => null
      };
   }
}