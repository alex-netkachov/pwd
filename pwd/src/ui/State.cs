using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.ui.abstractions;

namespace pwd.ui;

public class State
   : IState
{
   private record StateInt(
      bool Disposed,
      IImmutableStack<IContext> Stack,
      ImmutableList<Channel<IStateChange>> Subscribers);

   private readonly ILogger<State> _logger;

   private StateInt _state;

   public State(
      ILogger<State> logger)
   {
      _logger = logger;
      _state = new(
         false,
         ImmutableStack<IContext>.Empty,
         ImmutableList<Channel<IStateChange>>.Empty);
   }

   public IStateChangeReader Subscribe()
   {
      var channel = Channel.CreateUnbounded<IStateChange>();

      var reader = new StateChangeReader(channel.Reader, () =>
      {
         while (true)
         {
            var initial = _state;
            if (_state.Disposed)
               break;
            var updated = _state with { Subscribers = initial.Subscribers.Remove(channel) };
            if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
               continue;
            channel.Writer.Complete();
            break;
         }
      });

      while (true)
      {
         var initial = _state;
         if (_state.Disposed)
            throw new ObjectDisposedException(nameof(State));
         var updated = _state with { Subscribers = initial.Subscribers.Add(channel) };
         if (initial == Interlocked.CompareExchange(ref _state, updated, initial))
            break;
      }

      return reader;
   }

   public async Task BackAsync()
   {
      _logger.LogInformation("{0}: start", nameof(BackAsync));

      IContext? removed;
      IContext? active;
      while (true)
      {
         var initial = _state;
         if (initial.Disposed)
            throw new ObjectDisposedException(nameof(State));
         if (initial.Stack.IsEmpty)
            return;
         var updated = initial with { Stack = initial.Stack.Pop(out var item) };
         if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
            continue;
         removed = item;
         active = updated.Stack.IsEmpty ? null : updated.Stack.Peek();
         break;
      }

      await removed.StopAsync();
      removed.Dispose();
      if (active != null)
         await active.StartAsync();
   }

   public async Task OpenAsync(
      IContext context)
   {
      _logger.LogInformation(
         "{0}: start with {1}",
         nameof(OpenAsync),
         context.GetType().Name);

      IContext? previous;
      while (true)
      {
         var initial = _state;
         if (initial.Disposed)
            throw new ObjectDisposedException(nameof(State));
         var updated = initial with { Stack = initial.Stack.Push(context) };
         if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
            continue;
         previous = initial.Stack.IsEmpty ? null : initial.Stack.Peek();
         break;
      }

      if (previous != null)
      {
         _logger.LogInformation("stopping the context");

         await previous.StopAsync();
      }

      _logger.LogInformation("starting the context");

      await context.StartAsync();
   }

   public async ValueTask DisposeAsync()
   {
      List<IContext> contexts;
      IContext? active;
      ImmutableList<Channel<IStateChange>> channels;

      while (true)
      {
         var initial = _state;
         if (initial.Disposed)
            return;
         var updated = new StateInt(
            Disposed: true,
            Stack: initial.Stack.Clear(),
            Subscribers: ImmutableList<Channel<IStateChange>>.Empty);
         if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
            continue;
         contexts = new(initial.Stack);
         active = initial.Stack.IsEmpty ? null : initial.Stack.Peek();
         channels = initial.Subscribers;
         break;
      }

      if (active != null)
         await active.StopAsync();

      foreach (var context in contexts)
         context.Dispose();

      var stateDisposed = new StateDisposed();
      foreach (var channel in channels)
      {
         await channel.Writer.WriteAsync(stateDisposed);
         channel.Writer.Complete();
      }
   }
}