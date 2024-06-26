﻿using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.core;
using pwd.core.abstractions;

namespace pwd.contexts.session.commands;

public sealed class Add
   : CommandServicesBase
{
   private readonly IState _state;
   private readonly INewFileFactory _newFileFactory;
   private readonly IRepository _repository;

   public Add(
      IState state,
      INewFileFactory newFileFactory,
      IRepository repository)
   {
      _state = state;
      _newFileFactory = newFileFactory;
      _repository = repository;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "add", var name) =>
            new DelegateCommand(async cancellationToken =>
            {
               await _state.OpenAsync(_newFileFactory.Create(_repository, name)).WaitAsync(cancellationToken);
            }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}