using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Rename(
      ILogger logger,
      IRepository repository,
      repository.IFile file)
   : CommandServicesBase
{
   private readonly ILogger _logger = logger;
   private readonly IRepository _repository = repository;
   private readonly repository.IFile _file = file;

    public override ICommand? Create(
      string input)
   {
      _logger.Info($"{nameof(Rename)}.{nameof(Create)}: start with '{input}'");

      return Shared.ParseCommand(input) switch
      {
         (_, "rename", var name) when !string.IsNullOrEmpty(name) =>
            new DelegateCommand(
               () =>
               {
                  _logger.Info($"{nameof(Rename)}.{nameof(DelegateCommand)}: start");

                  _repository.Move(
                     _file,
                     Path.From(Name.Parse(_file.Name.FileSystem, name)));
               }),
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