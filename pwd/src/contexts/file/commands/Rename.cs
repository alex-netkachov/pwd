using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Rename(
      IRepository repository,
      INamedItem item)
   : CommandServicesBase
{
   private readonly IRepository _repository = repository;
   private readonly IItem _item = item;

    public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rename", var name) when !string.IsNullOrEmpty(name) =>
            new DelegateCommand(() => /*
               _repository.Rename(
                  _item.Name.ToPath(),
                  Name.Parse(_item.Name.FileSystem, name).ToPath())*/ {}),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".rename";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [ key ]
         : Array.Empty<string>();
   }
}