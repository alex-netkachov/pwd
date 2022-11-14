using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Edit
   : ICommandFactory
{
   private readonly IFile _file;

   public Edit(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "edit", var editor) => new DelegateCommand(cancellationToken => _file.Edit(editor, cancellationToken)),
         _ => null
      };
   }
}