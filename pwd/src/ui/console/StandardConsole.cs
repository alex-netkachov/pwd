using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.ui.console;

public sealed class StandardConsole
   : IConsole,
     IDisposable
{
   private record State(
      bool Disposed,
      ImmutableList<ChannelWriter<ConsoleKeyInfo>> Writers);

   private State _state;
   private readonly CancellationTokenSource _cts;

   public StandardConsole()
   {
      _state = new(false, ImmutableList<ChannelWriter<ConsoleKeyInfo>>.Empty);

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
            foreach (var writer in state.Writers)
               while (!writer.TryWrite(key))
               {
               }
         }
      }, token);
   }

   public int BufferWidth => Console.BufferWidth;
   
   public void Dispose()
   {
      State initial, updated;
      do
      {
         initial = _state;
         if (initial.Disposed)
            return;
         updated = new State(true, ImmutableList<ChannelWriter<ConsoleKeyInfo>>.Empty);
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));

      _cts.Cancel();
      _cts.Dispose();
      
      foreach (var writer in initial.Writers)
         writer.Complete();
   }

   public void Subscribe(
      ChannelWriter<ConsoleKeyInfo> writer)
   {
      State initial, updated;
      do
      {
         initial = _state;
         ObjectDisposedException.ThrowIf(_state.Disposed, this);
         updated = _state with { Writers = initial.Writers.Add(writer) };
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
   }

   public void Unsubscribe(
      ChannelWriter<ConsoleKeyInfo> writer)
   {
      State initial, updated;
      do
      {
         initial = _state;
         if (initial.Disposed)
            return;
         updated = _state with { Writers = initial.Writers.Remove(writer) };
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
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
      // clears the console and its buffer
      Console.Write("\x1b[2J\x1b[3J");

      // followed by the standard clear
      Console.Clear();
   }
}