using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Print
   : ICommandServices
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

   public ICommand? Create(
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

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}