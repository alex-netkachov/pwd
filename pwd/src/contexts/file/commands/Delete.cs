using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Delete
   : ICommandFactory
{
   private readonly IFile _file;

   public Delete(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".rm" => new DelegateCommand(_file.Delete),
         _ => null
      };
   }
}