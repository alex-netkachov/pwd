using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Unobscured
   : ICommandServices
{
   private readonly IView _view;
   private readonly IRepositoryItem _item;

   public Unobscured(
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
         (_, "unobscured", _) => new DelegateCommand(async cancellationToken =>
         {
            var content = await _item.ReadAsync(cancellationToken);
            _view.WriteLine(content);
         }),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}