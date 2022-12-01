using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Archive
   : CommandServicesBase
{
   private readonly IState _state;
   private readonly IRepositoryItem _item;

   public Archive(
      IState state,
      IRepositoryItem item)
   {
      _state = state;
      _item = item;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "archive", _) => new DelegateCommand(
            () =>
            {
               _item.Archive();
               _state.BackAsync();
            }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".archive";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}