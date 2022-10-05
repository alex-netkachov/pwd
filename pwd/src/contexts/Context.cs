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

   private TaskCompletionSource? _tcs;
   private CancellationTokenSource? _cts;

   protected ReplContext(
      ILogger logger,
      IView view)
   {
      _logger = logger;
      _view = view;
   }

   public virtual Task ProcessAsync(
      string input)
   {
      return Task.CompletedTask;
   }

   protected virtual string Prompt()
   {
      return "";
   }

   public virtual Task RunAsync()
   {
      var @new = new TaskCompletionSource();
      var updated = Interlocked.CompareExchange(ref _tcs, @new, null);
      if (updated != null)
         return _tcs.Task;

      _cts = new();

      Task.Run(async () =>
      {
         while (true)
         {
            string input;
            try
            {
               input = (await _view.ReadAsync(new($"{Prompt()}> "), this, _cts.Token)).Trim();
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cts.Token)
            {
               // StopAsync() is called, exit gracefully. 
               break;
            }

            try
            {
               await ProcessAsync(input);
            }
            catch (Exception e)
            {
               _logger.Error($"Executing the command '{input}' caused the following exception: {e}");
            }
         }
         
         @new.SetResult();
      });

      return @new.Task;
   }

   public virtual Task StopAsync()
   {
      var value = _tcs;
      var updated = Interlocked.CompareExchange(ref _tcs, null, value);
      if (updated == value)
         _cts?.Cancel();
      return Task.CompletedTask;
   }

   public virtual (int offset, IReadOnlyList<string>) Get(
      string input)
   {
      return (0, Array.Empty<string>());
   }
}