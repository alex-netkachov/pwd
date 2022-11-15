using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Export
   : ICommandFactory
{
   private readonly IView _view;

   public Export(
      IView view)
   {
      _view = view;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "export", var name) =>
            new DelegateCommand(() => _view.WriteLine("Not implemented")),
         _ => null
      };
   }
}