using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.readline;

/// <summary>Provides reading access to the buffered stream of console input-related events.</summary>
/// <remarks>The reader should be disposed when the client is no longer interested in the events.</remarks>
public interface IConsoleReader
   : IDisposable
{
   ValueTask<ConsoleKeyInfo> ReadAsync(
      CancellationToken cancellationToken = default);
}

public sealed class ConsoleReader
   : IConsoleReader
{
   private readonly ChannelReader<ConsoleKeyInfo> _reader;
   private readonly Action _disposing;
   private int _disposed;

   public ConsoleReader(
      ChannelReader<ConsoleKeyInfo> reader,
      Action? disposing = null)
   {
      _reader = reader;
      _disposing = disposing ?? new Action(() => { });
   }

   public ValueTask<ConsoleKeyInfo> ReadAsync(
      CancellationToken cancellationToken = default)
   {
      return _reader.ReadAsync(cancellationToken);
   }

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) != 1)
         return;
      _disposing();
   }
}

public interface IConsole
{
   int BufferWidth { get; }

   IConsoleReader Subscribe();

   void Write(
      object? value = null);

   void WriteLine(
      object? value = null);

   (int Left, int Top) GetCursorPosition();

   void SetCursorPosition(
      int left,
      int top);

   void Clear();
}

public sealed class StandardConsole
   : IConsole,
      IDisposable
{
   private record State(
      bool Disposed,
      ImmutableList<Channel<ConsoleKeyInfo>> Subscribers);

   private State _state;
   private readonly CancellationTokenSource _cts;

   public StandardConsole()
   {
      _state = new(false, ImmutableList<Channel<ConsoleKeyInfo>>.Empty);

      _cts = new();

      var token = _cts.Token;

      Task.Run(() =>
      {
         while (!token.IsCancellationRequested)
         {
            if (!Console.KeyAvailable)
            {
               // Delay between user pressing the key and processing this key by the app.
               // Should be small enough so the user does not notice an input lag and big
               // enough to not fall back to spin wait. 
               Thread.Sleep(10);
               continue;
            }

            if (token.IsCancellationRequested)
               break;

            var key = Console.ReadKey(true);
            var state = _state;
            foreach (var subscriber in state.Subscribers)
               while (!subscriber.Writer.TryWrite(key))
               {
               }
         }
      }, token);
   }

   public int BufferWidth => Console.BufferWidth;
   
   public void Dispose()
   {
      State initial;
      while (true)
      {
         initial = _state;
         if (initial.Disposed)
            return;
         var updated = new State(true, ImmutableList<Channel<ConsoleKeyInfo>>.Empty);
         if (initial == Interlocked.CompareExchange(ref _state, updated, initial))
            break;
      }

      _cts.Cancel();
      _cts.Dispose();
      
      foreach (var channel in initial.Subscribers)
         channel.Writer.Complete();
   }

   public IConsoleReader Subscribe()
   {
      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();

      var reader = new ConsoleReader(channel.Reader, () =>
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
            throw new ObjectDisposedException(nameof(StandardConsole));
         var updated = _state with { Subscribers = initial.Subscribers.Add(channel) };
         if (initial == Interlocked.CompareExchange(ref _state, updated, initial))
            break;
      }

      return reader;
   }

   public void Write(
      object? value)
   {
      Console.Write(value);
   }

   public void WriteLine(
      object? value)
   {
      Console.WriteLine(value);
   }

   public (int Left, int Top) GetCursorPosition()
   {
      return Console.GetCursorPosition();
   }

   public void SetCursorPosition(
      int left,
      int top)
   {
      Console.SetCursorPosition(left, top);
   }

   public void Clear()
   {
      Console.Clear();

      // clears the console and its buffer
      Console.Write("\x1b[2J\x1b[3J");
   }
}