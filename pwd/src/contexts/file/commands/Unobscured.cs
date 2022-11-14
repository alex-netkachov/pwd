using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Unobscured
   : ICommandFactory
{
   private readonly IFile _file;

   public Unobscured(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".unobscured" => new DelegateCommand(_file.Unobscured),
         _ => null
      };
   }
}