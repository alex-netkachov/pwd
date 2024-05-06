using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.ui;
using pwd.ui.readline;

namespace pwd.context.repl;

public interface ICommandServices
   : IDisposable
{
   ICommand? Create(
      string input);

   IReadOnlyList<string> Suggestions(
      string input);
}

public abstract class CommandServicesBase
   : ICommandServices
{
   public abstract ICommand? Create(string input);

   public virtual IReadOnlyList<string> Suggestions(string input)
   {
      return Array.Empty<string>();
   }

   public virtual void Dispose()
   {
   }
}

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
   : IContext,
     ISuggestionsProvider
{
   private record State(
      TaskCompletionSource Starting,
      TaskCompletionSource Stopped,
      CancellationTokenSource Cancel);

   private State? _state;

   private readonly ILogger _logger;
   private readonly IView _view;
   private readonly IReadOnlyCollection<ICommandServices> _commandFactories;

   protected Repl(
      ILogger logger,
      IView view,
      IReadOnlyCollection<ICommandServices> commandFactories)
   {
      _logger = logger;
      _view = view;
      _state = null;
      _commandFactories = commandFactories;
   }

   public async Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      _logger.Info($"{nameof(Repl)}.{nameof(ProcessAsync)}: processing '{input}'");

      var command =
         _commandFactories
            .Select(item => item.Create(input))
            .FirstOrDefault(item => item != null);

      if (command == null)
      {
         _logger.Info($"no commands for input '{input}'");
         return;
      }

      _logger.Info($"executing command {command}");
      await command.ExecuteAsync(cancellationToken);
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

               _logger.Info("ReplContext.Loop(): _view.ReadAsync(...)");

               input = (await _view.ReadAsync(new($"{prompt}> "), this, token)).Trim();

               _logger.Info($"ReplContext.Loop(): _view.ReadAsync(...) has been completed with '{input}'");
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token)
            {
               // StopAsync() is called
               _logger.Info("ReplContext.Loop(): _view.ReadAsync(...) has been cancelled");
               break;
            }
            catch (Exception e)
            {
               _logger.Error($"waiting for the user's input ended with the following exception: {e}");
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
               _logger.Error($"Executing the command '{input}' ended with the following exception: {e}");
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
      return _commandFactories
         .SelectMany(item => item.Suggestions(input))
         .ToList();
   }

   public void Dispose()
   {
      foreach (var factory in _commandFactories)
         factory.Dispose();
   }
}