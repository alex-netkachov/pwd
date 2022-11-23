using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Unobscured
   : ICommandFactory
{
   private readonly IView _view;
   private readonly IRepositoryItem _item;

   public Unobscured(
      IView view,
      IRepositoryItem item)
   {
      _view = view;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".unobscured" => new DelegateCommand(async cancellationToken =>
         {
            var content = await _item.ReadAsync(cancellationToken);
            _view.WriteLine(content);
         }),
         _ => null
      };
   }
}