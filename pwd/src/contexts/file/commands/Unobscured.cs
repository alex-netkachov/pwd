using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Unobscured
   : CommandServicesBase
{
   private readonly IView _view;
   private readonly repository.IFile _file;

   public Unobscured(
      IView view,
      repository.IFile file)
   {
      _view = view;
      _file = file;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "unobscured", _) => new DelegateCommand(async cancellationToken =>
         {
            var content = await _file.ReadAsync(cancellationToken);
            _view.WriteLine(content);
         }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".unobscured";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}