using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Print
   : CommandServicesBase
{
   private readonly IView _view;
   private readonly IRepositoryItem _item;

   public Print(
      IView view,
      IRepositoryItem item)
   {
      _view = view;
      _item = item;
   }

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
      return new DelegateCommand(async cancellationToken =>
      {
         var content = await _item.ReadAsync(cancellationToken);

         var obscured =
            Regex.Replace(
               content,
               "password:\\s*[^\n\\s]+",
               "password: ************");

         _view.WriteLine(obscured);
      });
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".print";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}