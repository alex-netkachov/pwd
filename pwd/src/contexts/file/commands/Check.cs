using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Check
   : ICommandFactory
{
   private readonly IView _view;
   private readonly IRepositoryItem _item;

   public Check(
      IView view,
      IRepositoryItem item)
   {
      _view = view;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "check", _) => new DelegateCommand(async cancellationToken =>
         {
            var content = await _item.ReadAsync(cancellationToken);
            if (Shared.CheckYaml(content) is { Message: var msg })
               _view.WriteLine(msg);
         }),
         _ => null
      };
   }
}