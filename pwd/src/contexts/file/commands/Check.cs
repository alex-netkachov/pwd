﻿using System;
using System.Collections.Generic;
using pwd.contexts.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Check(
      IView view,
      IRepository repository,
      string path)
   : CommandServicesBase
{
   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "check", _) => new DelegateCommand(async _ =>
         {
            var content = await repository.ReadAsync(path);
            if (Shared.CheckYaml(content) is { Message: var msg })
               view.WriteLine(msg);
         }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".check";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}