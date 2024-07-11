using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.ui;
using pwd.ui.readline;

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
public abstract class Repl(
      ILogger<Repl> logger,
      IView view,
      IReadOnlyDictionary<string, ICommand> commands,
      string defaultCommand)
   : IContext,
     ISuggestionsProvider
{
   private record State(
      TaskCompletionSource Starting,
      TaskCompletionSource Stopped,
      CancellationTokenSource Cancel);

   private State? _state;

   public async Task ProcessAsync(
      string input,
      CancellationToken token = default)
   {
      logger.LogInformation($"{nameof(ProcessAsync)}: processing '{input}'");

      var parts = Shared.ParseCommand(input);
      if (string.IsNullOrEmpty(parts.Name))
      {
         parts =
            input == ".."
               ? new("..", "up", [])
               : (input, defaultCommand, input == "" ? [] : input.Split(' '));
      }

      var command =
         commands
            .FirstOrDefault(
               item => item.Key.Equals(parts.Name, StringComparison.OrdinalIgnoreCase))
            .Value;
      if (command == null)
      {
         logger.LogInformation($"no commands for input '{input}'");
         return;
      }

      logger.LogInformation($"executing command {command}");
      await command.ExecuteAsync(parts.Name, parts.Parameters, token);
   }

   protected virtual string Prompt()
   {
      return "";
   }

   public virtual Task StartAsync()
   {
      var initial = _state;
      if (initial != null)
         return initial.Starting.Task;
      var state = new State(new(), new(), new());
      var current = Interlocked.CompareExchange(ref _state, state, initial);
      if (initial != current)
      {
         state.Cancel.Dispose();
         return state.Starting.Task;
      }

      var token = state.Cancel.Token;

      Task.Run(async () =>
      {
         while (!token.IsCancellationRequested)
         {
            string input;
            try
            {
               var prompt = Prompt();

               logger.LogInformation("ReplContext.Loop(): _view.ReadAsync(...)");

               input = (await view.ReadAsync(new($"{prompt}> "), this, token)).Trim();

               logger.LogInformation($"ReplContext.Loop(): _view.ReadAsync(...) has been completed with '{input}'");
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token)
            {
               // StopAsync() is called
               logger.LogInformation("ReplContext.Loop(): _view.ReadAsync(...) has been cancelled");
               break;
            }
            catch (Exception e)
            {
               logger.LogError($"waiting for the user's input ended with the following exception: {e}");
               continue;
            }

            try
            {
               await ProcessAsync(input, token);
            }
            catch (TaskCanceledException e) when (e.CancellationToken == token)
            {
               // StopAsync() is called
               break;
            }
            catch (Exception e)
            {
               logger.LogError($"Executing the command '{input}' ended with the following exception: {e}");
            }
         }

         state.Stopped.TrySetResult();
         state.Cancel.Dispose();
      }, token);

      state.Starting.SetResult();

      return state.Starting.Task;
   }

   public virtual Task StopAsync()
   {
      State? state;
      while (true)
      {
         state = _state;

         if (state == null)
            // this method is called for the already stopped context
            return Task.CompletedTask;

         if (state == Interlocked.CompareExchange(ref _state, null, state))
            break;
      }

      state.Cancel.Cancel();
      state.Cancel.Dispose();
      return state.Stopped.Task;
   }

   public virtual IReadOnlyList<string> Suggestions(
      string input)
   {
      return commands
         .SelectMany(item => item.Value.Suggestions(input))
         .ToList();
   }

   public void Dispose()
   {
      // empty
   }
}