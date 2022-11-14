using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Html
   : ICommandFactory
{
   private readonly ISession _session;

   public Html(
      ISession session)
   {
      _session = session;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "html", var name) =>
            new DelegateCommand(_ => _session.Html(name)),
         _ => null
      };
   }
}