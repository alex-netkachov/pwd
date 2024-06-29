using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Check(
      IView view,
      Location location)
   : CommandServicesBase
{
   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "check", _) => new DelegateCommand(async _ =>
         {
            var repository = location.Repository;
            var content = await repository.ReadAsync(location);
            if (Shared.CheckYaml(content) is { Message: var msg })
               view.WriteLine(msg);
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
         ? [key]
         : [];
   }
}