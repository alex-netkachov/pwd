﻿using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up
   : ICommandFactory
{
   private readonly IFile _file;

   public Up(
      IFile file)
   {
      _file = file;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".." => new DelegateCommand(_file.Up),
         _ => null
      };
   }
}