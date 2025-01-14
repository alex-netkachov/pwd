using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console;
using pwd.console.abstractions;
using pwd.ui.abstractions;

namespace pwd.contexts.repl;

/// <summary>
///   Repl is a Read-Eval-Print-Loop (REPL) context. It serves as a foundational
///   context for other command-line user interface contexts.
/// </summary>
/// <remarks>
///   A REPL context reads the user's input, evaluates it, and then prints
///   the result.
///
///   The basic elements of this REPL interface include a prompt, a command,
///   command parameters, and a result.
///
///   The prompt is a string that is displayed before the user's input to
///   indicate readiness for input.
///
///   The command is a string entered by the user that typically starts with
///   a period ('.').
///
///   The default command is assumed when the user's input does not start
///   with a period. This default command takes a single argument: the user's
///   input.
///
///   Command parameters are space-separated values. Spaces within parameters
///   can be escaped with quotes. To include quotes within a parameter, they
///   must be doubled up. Both single ('') and double ("") quotes are supported.
///
///   The result of a command is a string that is displayed after the command
///   has been processed and executed.
///
///   The REPL context can be stopped any time. It restarts with the current
///   user input and the view.
///
///   Stopping the context does not stop the command, it continues to update
///   the view.
///
///   The command can be stopped by pressing Ctrl+C.
/// </remarks>
public abstract class Repl
   : Views,
     IContext,
     ISuggestions
{
   private readonly object _lock = new { };

   private readonly ILogger<Repl> _logger;
   private readonly IReadOnlyDictionary<string, ICommand> _commands;
   private readonly string _defaultCommand;

   private readonly CancellationTokenSource _cts = new();
   private bool _disposed;

   protected Repl(
      ILogger<Repl> logger,
      Func<Task<IView?>> initialise,
      Func<IView> viewFactory,
      IReadOnlyDictionary<string, ICommand> commands,
      string defaultCommand)
   {
      _logger = logger;
      _commands = commands;
      _defaultCommand = defaultCommand;

      var token = _cts.Token;

      Task.Run(async () =>
      {
         if (token.IsCancellationRequested)
            return;

         if (await initialise() is { } startView)
         {
            Publish(startView);
         }

         while (!token.IsCancellationRequested)
         {
            var view = viewFactory();

            Publish(view);

            string input;
            try
            {
               var prompt = Prompt();

               _logger.LogInformation("ReplContext.Loop(): _view.ReadAsync(...)");

               input = (await view.ReadAsync(new($"{prompt}> "), this, null, token)).Trim();

               _logger.LogInformation($"ReplContext.Loop(): _view.ReadAsync(...) has been completed with '{input}'");
            }
            catch (Exception e)
            {
               _logger.LogError($"waiting for the user's input ended with the following exception: {e}");
               continue;
            }

            try
            {
               await ProcessAsync(view, input, token);
            }
            catch (Exception e)
            {
               _logger.LogError($"Executing the command '{input}' ended with the following exception: {e}");
            }
         }
      }, token);
   }

   public async Task ProcessAsync(
      IView view,
      string input,
      CancellationToken token = default)
   {
      _logger.LogInformation($"{nameof(ProcessAsync)}: processing '{input}'");

      var parts = Shared.ParseCommand(input);
      if (string.IsNullOrEmpty(parts.Name))
      {
         parts =
            input == ".."
               ? new("..", "up", [])
               : (input, defaultCommand: _defaultCommand, input == "" ? [] : input.Split(' '));
      }

      var command =
         _commands
            .FirstOrDefault(
               item => item.Key.Equals(parts.Name, StringComparison.OrdinalIgnoreCase))
            .Value;
      if (command == null)
      {
         _logger.LogInformation($"no commands for input '{input}'");
         return;
      }

      _logger.LogInformation($"executing command {command}");
      await command.ExecuteAsync(view, parts.Name, parts.Parameters, token);
   }

   protected virtual string Prompt()
   {
      return "";
   }

   public virtual IReadOnlyList<string> Get(
      string input,
      int position)
   {
      return _commands
         .SelectMany(item => item.Value.Suggestions(input))
         .ToList();
   }

   public void Dispose()
   {
      if (_disposed)
         return;

      lock (_lock)
      {
         if (_disposed)
            return;

         _disposed = true;
         _cts.Cancel();
         _cts.Dispose();
      }
   }
}