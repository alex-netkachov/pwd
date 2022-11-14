using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Archive
   : ICommandFactory
{
   private readonly IFile _file;

   public Archive(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".archive" => new DelegateCommand(_file.Archive),
         _ => null
      };
   }
}