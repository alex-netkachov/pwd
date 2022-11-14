using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class CopyField
   : ICommandFactory
{
   private readonly IFile _file;

   public CopyField(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "ccp", _) => new DelegateCommand(() => _file.CopyField("password")),
         (_, "ccu", _) => new (() => _file.CopyField("user")),
         (_, "cc", var name) => new (() => _file.CopyField(name)),
         _ => null
      };
   }
}