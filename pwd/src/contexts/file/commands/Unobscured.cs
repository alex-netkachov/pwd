using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.core;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Unobscured(
      IView view,
      IRepository repository,
      Location location)
   : CommandServicesBase
{
   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "unobscured", _) => new DelegateCommand(async cancellationToken =>
         {
            var content = await repository.ReadAsync(location);
            view.WriteLine(content);
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