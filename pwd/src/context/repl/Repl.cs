using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.context.repl;

public interface ICommand
{
   Task DoAsync(
      CancellationToken cancellationToken = default);
}

public interface ICommandFactory
{
   ICommand? Parse(
      string input);
}

public sealed class DelegateCommand
   : ICommand
{
   private readonly Func<CancellationToken, Task>  _action;

   public DelegateCommand(
      Func<CancellationToken, Task> action)
   {
      _action = action;
   }
   
   public DelegateCommand(
      Action action)
   {
      _action = _ =>
      {
         action();
         return Task.CompletedTask;
      };
   }


   public async Task DoAsync(
      CancellationToken cancellationToken = default)
   {
      if (cancellationToken.IsCancellationRequested)
         return;

      await _action(cancellationToken);
   }
}

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
   private readonly IReadOnlyCollection<ICommandFactory> _commandFactories;

   protected Repl(
      ILogger logger,
      IView view,
      IReadOnlyCollection<ICommandFactory> commandFactories)
   {
      _logger = logger;
      _view = view;
      _state = null;
      _commandFactories = commandFactories;
   }

   public virtual async Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      var command =
         _commandFactories
            .Select(item => item.Parse(input))
            .FirstOrDefault(item => item != null);

      if (command == null)
         return;

      await command.DoAsync(cancellationToken);
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

   public virtual (int offset, IReadOnlyList<string>) Get(
      string input)
   {
      return (0, Array.Empty<string>());
   }
}