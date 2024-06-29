using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using pwd.context.repl;
using pwd.core;
using pwd.core.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Rename(
      ILogger<Rename> logger,
      IRepository repository,
      Location location)
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

                  var (folder, _) = location.Up();
                  var newLocation = folder.Down(repository.ParseName(name));

                  repository.Move(
                     location,
                     newLocation);
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