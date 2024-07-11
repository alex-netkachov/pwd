using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using pwd.contexts.repl;
using pwd.core;
using pwd.core.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Rename(
      ILogger<Rename> logger,
      IRepository repository,
      string path)
   : CommandServicesBase
{
    public override ICommand? Create(
      string input)
   {
      logger.LogInformation($"{nameof(Rename)}.{nameof(Create)}: start with '{input}'");

      return Shared.ParseCommand(input) switch
      {
         (_, "rename", var name) when !string.IsNullOrEmpty(name) =>
            new DelegateCommand(
               () =>
               {
                  logger.LogInformation($"{nameof(Rename)}.{nameof(DelegateCommand)}: start");

                  repository.Move(
                     path,
                     name);
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