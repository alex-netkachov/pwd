using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.cli.contexts.repl;

public interface ICommand
{
   Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default);

   IReadOnlyList<string> Suggestions(
      string input);
}

public abstract class CommandBase
   : ICommand
{
   public abstract Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default);

   public virtual IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}
