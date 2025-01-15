using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.console;

public sealed class Console
   : IConsole,
     IDisposable
{
   private readonly Lock _lock = new();

   private bool _disposed;
   private readonly List<Action<ConsoleKeyInfo>> _observers = [];
   private Action<ConsoleKeyInfo>? _interceptor;
   private readonly CancellationTokenSource _cts;

   public Console()
   {
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

            lock (_lock)
            {
               foreach (var item in _observers)
                  item.Invoke(key);
               _interceptor?.Invoke(key);
            }
         }
      }, token);
   }

   public int BufferWidth => System.Console.BufferWidth;
   
   public int BufferHeight => System.Console.BufferHeight;

   public void Dispose()
   {
      lock (_lock)
      {
         if (_disposed)
            return;

         _disposed = true;
      }

      _cts.Cancel();
      _cts.Dispose();
   }
   
   public IDisposable Observe(
      Action<ConsoleKeyInfo> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _observers.Add(action);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _observers.Remove(action);
         }
      });
   }
   
   public IDisposable Intercept(
      Action<ConsoleKeyInfo> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_interceptor != null)
            throw new InvalidOperationException("Interceptor already set.");

         _interceptor = action;
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _interceptor = null;
         }
      });
   }

   public void Write(
      object? value)
   {
      if (value == null)
         return;

      var text =
         Convert.ToString(
            value,
            CultureInfo.InvariantCulture);

      if (string.IsNullOrEmpty(text))
         return;
      
      System.Console.Write(text);
   }

   public void WriteLine(
      object? value)
   {
      var text =
         Convert.ToString(
            value
            ?? "",
            CultureInfo.InvariantCulture);

      System.Console.WriteLine(text);
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

public static class ConsoleExtensions
{
   public static void WriteAndMoveBack(
      this IConsole console,
      string text)
   {
      var position = console.GetCursorPosition();
      console.Write(text);
      console.SetCursorPosition(position);
   }
   
   public static void MoveLeft(
      this IConsole console,
      int steps)
   {
      var cursorPosition = console.GetCursorPosition();
      var (left, top) = (cursorPosition.X, cursorPosition.Y);
      for (var i = 0; i < steps; i++)
      {
         left--;
         if (left != -1)
            continue;
         left = console.BufferWidth - 1;
         top--;
      }

      console.SetCursorPosition(new(left, top));
   }
   
   public static void MoveRight(
      this IConsole console,
      int steps)
   {
      var width = console.BufferWidth;
      var cursorPosition = console.GetCursorPosition();
      var (left, top) = (cursorPosition.X, cursorPosition.Y);
      for (var i = 0; i < steps; i++)
      {
         left++;
         if (left != width)
            continue;
         left = 0;
         top++;
      }
      console.SetCursorPosition(new(left, top));
   }
}