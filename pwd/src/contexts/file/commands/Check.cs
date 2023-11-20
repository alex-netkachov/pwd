using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Check
   : CommandServicesBase
{
   private readonly IView _view;
   private readonly repository.IFile _item;

   public Check(
      IView view,
      repository.IFile item)
   {
      _view = view;
      _item = item;
   }

   public override ICommand? Create(
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

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".check";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}