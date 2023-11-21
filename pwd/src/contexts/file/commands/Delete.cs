using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Delete(
      IState state,
      IView view,
      IRepository repository,
      repository.IFile file)
   : CommandServicesBase
{
   private readonly IState _state = state;
   private readonly IView _view = view;
   private readonly IRepository _repository = repository;
   private readonly repository.IFile _file = file;

    public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rm", _) => new DelegateCommand(async cancellationToken =>
         {
            if (!await _view.ConfirmAsync($"Delete '{_file.Name}'?", Answer.No, cancellationToken))
               return;

            _repository.Delete(_file);

            _view.WriteLine($"'{_file.Name}' has been deleted.");

            _ = _state.BackAsync();
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
         ? [ key ]
         : Array.Empty<string>();
   }
}