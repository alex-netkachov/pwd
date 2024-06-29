using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using pwd.context.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Print(
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
         (_, "print", _) => Command(),
         _ when string.IsNullOrWhiteSpace(input) => Command(),
         _ => null
      };
   }

   private ICommand Command()
   {
      return new DelegateCommand(async _ =>
      {
         var content = await repository.ReadAsync(location);

         var obscured =
            Regex.Replace(
               content,
               "password:\\s*[^\n\\s]+",
               "password: ************");

         view.WriteLine(obscured);
      });
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".print";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}