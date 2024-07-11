using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.contexts.repl;

public interface ICommand
{
   Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default);

   IReadOnlyList<string> Suggestions(
      string input);
}

public abstract class CommandBase
   : ICommand
{
   public abstract Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default);

   public virtual IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}
