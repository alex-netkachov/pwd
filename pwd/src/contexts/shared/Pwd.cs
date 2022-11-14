using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Pwd
   : ICommandFactory
{
   private readonly IView _view;

   public Pwd(
      IView view)
   {
      _view = view;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".pwd" => new DelegateCommand(() => _view.WriteLine(Shared.Password())),
         _ => null
      };
   }
}