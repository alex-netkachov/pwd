using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.contexts;

public interface IContext
{
   /// <summary>Starts the context. The returned task completes when the context is started.</summary>
   /// <remarks>The context can be started and stopped multiple times. Multiple calls to the method returns the same
   /// task. If the context is started, this method does nothing and returns a completed task.</remarks>
   Task StartAsync();

   /// <summary>Stops the context. Completes when the context is stopped.</summary>
   Task StopAsync();
}

public abstract class ReplContext
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

   protected ReplContext(
      ILogger logger,
      IView view)
   {
      _logger = logger;
      _view = view;
      _state = null;
   }

   public virtual Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      return Task.CompletedTask;
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
               input = (await _view.ReadAsync(new($"{prompt}> "), this, token)).Trim();
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token)
            {
               // StopAsync() is called
               break;
            }
            catch (Exception e)
            {
               _logger.Error($"Waiting for the user's input ended with the following exception: {e}");
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
      });

      state.Starting.SetResult();

      return state.Starting.Task;
   }

   public virtual Task StopAsync()
   {
      State? state;
      while (true)
      {
         state = _state;
         if (state == null || state == Interlocked.CompareExchange(ref _state, null, state))
            break;
      }

      if (state == null)
         return Task.CompletedTask;

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