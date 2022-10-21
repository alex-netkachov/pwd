using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.contexts;

public interface IContext
{
   /// <summary>Runs the context. The returned task completes when the context is started.</summary>
   /// <remarks>The context can be started and stopped multiple times. If the context is started, this method
   /// does nothing.</remarks>
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
      var state = new State(new(), new());
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
               input = (await _view.ReadAsync(new($"{Prompt()}> "), this, token)).Trim();
            }
            catch (OperationCanceledException e)
            {
               if (e.CancellationToken == token)
                  // StopAsync() is called, exit gracefully
                  break;

               // no need to cancel CTS, need to dispose it
               Interlocked.CompareExchange(ref _state, null, state);
               state.Cancel.Dispose();
               state.Starting.TrySetCanceled();
               return;
            }

            try
            {
               await ProcessAsync(input, token);
            }
            catch (TaskCanceledException e) when (e.CancellationToken == token)
            {
               // graceful cancellation, e.g. state asked this context to stop
            }
            catch (Exception e)
            {
               _logger.Error($"Executing the command '{input}' caused the following exception: {e}");
            }
         }

         // StopAsync() is called, exit gracefully, CTS should be disposed already
         Interlocked.CompareExchange(ref _state, null, state);
         state.Starting.SetResult();
      });

      state.Starting.SetResult();
      return state.Starting.Task;
   }

   public virtual Task StopAsync()
   {
      var state = _state;
      if (state == null)
         return Task.CompletedTask;
      state.Cancel.Cancel();
      state.Cancel.Dispose();
      return state.Starting.Task;
   }

   public virtual (int offset, IReadOnlyList<string>) Get(
      string input)
   {
      return (0, Array.Empty<string>());
   }
}