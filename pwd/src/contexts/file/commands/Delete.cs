using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Delete(
      IState state,
      IView view,
      IRepository repository,
      IItem item)
   : CommandServicesBase
{
   private readonly IState _state = state;
   private readonly IView _view = view;
   private readonly IRepository _repository = repository;
   private readonly IItem _item = item;

    public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rm", _) => new DelegateCommand(async cancellationToken =>
         {
            /*
            if (!await _view.ConfirmAsync($"Delete '{_item.Name}'?", Answer.No, cancellationToken))
               return;

            _repository.Delete(_item.Name.ToPath());
            _view.WriteLine($"'{_item.Name}' has been deleted.");
            var _ = _state.BackAsync();
            */
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