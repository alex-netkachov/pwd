using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Rename
   : ICommandFactory
{
   private readonly IFile _file;

   public Rename(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rename", var name) => new DelegateCommand(_ => _file.Rename(name)),
         _ => null
      };
   }
}