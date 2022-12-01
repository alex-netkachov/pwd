using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Delete
   : CommandServicesBase
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

   public override ICommand? Create(
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
            var _ = _state.BackAsync();
         }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".rm";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}