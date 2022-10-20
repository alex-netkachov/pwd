using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.contexts;

public interface IContext
{
   /// <summary>Runs the context. The returned task completes when the context
   /// gracefully stops, either by StopAsync() or by its own.</summary>
   /// <remarks>The context can be started and stopped multiple times. If
   /// the context is started, this method does nothing.</remarks>
   Task RunAsync();

   /// <summary>Stops the context. Completes when the context is stopped.</summary>
   Task StopAsync();
}

public sealed class NullContext
   : IContext
{
   public static IContext Instance { get; } = new NullContext();

   private TaskCompletionSource? _tcs;

   public Task RunAsync()
   {
      var @new = new TaskCompletionSource();
      var updated = Interlocked.CompareExchange(ref _tcs, @new, null);
      return updated == null ? @new.Task : updated.Task;
   }

   public Task StopAsync()
   {
      var value = _tcs;
      var updated = Interlocked.CompareExchange(ref _tcs, null, value);
      if (updated == value)
         value?.SetResult();
      return Task.CompletedTask;
   }
}

public abstract class ReplContext
   : IContext,
      ISuggestionsProvider
{
   private readonly ILogger _logger;
   private readonly IView _view;

   private record State(
      TaskCompletionSource Complete,
      CancellationTokenSource Cancel);

   private State? _state;

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

   public virtual Task RunAsync()
   {
      var initial = _state;
      if (initial != null)
         return initial.Complete.Task;
      var state = new State(new(), new());
      var current = Interlocked.CompareExchange(ref _state, state, initial);
      if (initial != current)
      {
         state.Cancel.Dispose();
         return current!.Complete.Task;
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
               state.Complete.TrySetCanceled();
               return;
            }

            try
            {
               await ProcessAsync(input, token);
            }
            catch (Exception e)
            {
               _logger.Error($"Executing the command '{input}' caused the following exception: {e}");
            }
         }

         // StopAsync() is called, exit gracefully, CTS should be disposed already
         Interlocked.CompareExchange(ref _state, null, state);
         state.Complete.SetResult();
      });

      return state.Complete.Task;
   }

   public virtual Task StopAsync()
   {
      var state = _state;
      if (state == null)
         return Task.CompletedTask;
      state.Cancel.Cancel();
      state.Cancel.Dispose();
      return state.Complete.Task;
   }

   public virtual (int offset, IReadOnlyList<string>) Get(
      string input)
   {
      return (0, Array.Empty<string>());
   }
}