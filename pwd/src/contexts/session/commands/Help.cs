using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Help
   : ICommandFactory
{
   private readonly ISession _session;

   public Help(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".help" => new DelegateCommand(_ => _session.Help()),
         _ => null
      };
   }
}