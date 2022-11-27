using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Delete
   : ICommandFactory
{
   private readonly IState _state;
   private readonly IView _view;
   private readonly IRepository _repository;
   private readonly IRepositoryItem _item;

   public Delete(
      IState state,
      IView view,
      IRepository repository,
      IRepositoryItem item)
   {
      _state = state;
      _view = view;
      _repository = repository;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rm", _) => new DelegateCommand(async cancellationToken =>
         {
            if (!await _view.ConfirmAsync($"Delete '{_item.Name}'?", Answer.No, cancellationToken))
               return;

            _repository.Delete(_item.Name);
            _view.WriteLine($"'{_item.Name}' has been deleted.");
            await _state.BackAsync().WaitAsync(cancellationToken);
         }),
         _ => null
      };
   }
}