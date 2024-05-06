using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using pwd.context.repl;
using pwd.repository;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Print
   : CommandServicesBase
{
   private readonly IView _view;
   private readonly repository.IFile _file;

   public Print(
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
         (_, "print", _) => Command(),
         _ when string.IsNullOrWhiteSpace(input) => Command(),
         _ => null
      };
   }

   private ICommand Command()
   {
      return new DelegateCommand(async cancellationToken =>
      {
         var content = await _file.ReadAsync(cancellationToken);

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