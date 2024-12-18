﻿using System.Collections.Immutable;
using System.Drawing;
using pwd.console.delegated;
using pwd.console.abstractions;

namespace pwd.console;

public sealed class Console
   : IConsole,
     IDisposable
{
   private record State(
      bool Disposed,
      ImmutableList<IObserver<ConsoleKeyInfo>> Observers);

   private State _state;
   private readonly CancellationTokenSource _cts;

   public Console()
   {
      _state = new(false, ImmutableList<IObserver<ConsoleKeyInfo>>.Empty);

      _cts = new();

      var token = _cts.Token;

      Task.Run(() =>
      {
         while (!token.IsCancellationRequested)
         {
            if (!System.Console.KeyAvailable)
            {
               // Delay between user pressing the key and processing this key by the app.
               // Should be small enough so the user does not notice an input lag and big
               // enough to not fall back to spin wait. 
               Thread.Sleep(10);
               continue;
            }

            if (token.IsCancellationRequested)
               break;

            var key = System.Console.ReadKey(true);
            var state = _state;
            foreach (var item in state.Observers)
               item.OnNext(key);
         }
      }, token);
   }

   public int BufferWidth => System.Console.BufferWidth;
   
   public void Dispose()
   {
      State initial, updated;
      do
      {
         initial = _state;
         if (initial.Disposed)
            return;
         updated = new State(true, ImmutableList<IObserver<ConsoleKeyInfo>>.Empty);
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));

      _cts.Cancel();
      _cts.Dispose();
      
      foreach (var item in initial.Observers)
         item.OnCompleted();
   }
   
   public IDisposable Subscribe(
      IObserver<ConsoleKeyInfo> observer)
   {
      {
         State initial, updated;
         do
         {
            initial = _state;
            ObjectDisposedException.ThrowIf(_state.Disposed, this);
            updated = _state with { Observers = initial.Observers.Add(observer) };
         } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
      }

      return new Disposable(() =>
      {
         State initial, updated;
         do
         {
            initial = _state;
            if (initial.Disposed)
               return;
            updated = _state with { Observers = initial.Observers.Remove(observer) };
         } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
      });
   }

   public void Write(
      object? value)
   {
      System.Console.Write(value);
   }

   public void WriteLine(
      object? value)
   {
      System.Console.WriteLine(value);
   }

   public Point GetCursorPosition()
   {
      var (left, top) = System.Console.GetCursorPosition();
      return new(left, top);
   }

   public void SetCursorPosition(
      Point point)
   {
      System.Console.SetCursorPosition(
         point.X,
         point.Y);
   }

   public void Clear()
   {
      // clears the console and its buffer
      System.Console.Write("\x1b[2J\x1b[3J");

      // followed by the standard clear
      System.Console.Clear();
   }
}