using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Check
   : ICommandFactory
{
   private readonly IFile _file;

   public Check(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".check" => new DelegateCommand(_file.Check),
         _ => null
      };
   }
}