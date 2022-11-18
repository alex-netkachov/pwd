using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Print
   : ICommandFactory
{
   private readonly IFile _file;

   public Print(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".print" => Command(),
         "" => Command(),
         _ => null
      };
   }

   public ICommand Command()
   {
      return new DelegateCommand(_ => _file.Print());
   }
}