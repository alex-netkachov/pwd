using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.library;

namespace pwd.console;

public sealed class Console
   : IConsole,
     IDisposable
{
   private readonly object _lock = new();

   private bool _disposed;
   private readonly List<IObserver<ConsoleKeyInfo>> _observers = [];
   private IObserver<ConsoleKeyInfo>? _interceptor;
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
                  item.OnNext(key);
               _interceptor?.OnNext(key);
            }
         }
      }, token);
   }

   public int Width => System.Console.BufferWidth;
   
   public int Height => System.Console.BufferHeight;

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
   
   public IDisposable Subscribe(
      IObserver<ConsoleKeyInfo> observer)
   {
      lock (_lock)
      {
         if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

         _observers.Add(observer);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _observers.Remove(observer);
         }
      });
   }
   
   public IDisposable Intercept(
      IObserver<ConsoleKeyInfo> interceptor)
   {
      lock (_lock)
      {
         if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

         if (_interceptor != null)
            throw new InvalidOperationException("Interceptor already set.");

         _interceptor = interceptor;
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
            value,
            CultureInfo.InvariantCulture)
         ?? "";

      System.Console.WriteLine(text);
   }

   public Point GetCursorPosition()
   {
      var left = System.Console.CursorLeft;
      var top = System.Console.CursorTop;
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
      var width = console.Width;
      var cursorPosition = console.GetCursorPosition();
      var (left, top) = (cursorPosition.X, cursorPosition.Y);
      for (var i = 0; i < steps; i++)
      {
         left--;
         if (left != -1)
            continue;
         left = width - 1;
         top--;
      }

      console.SetCursorPosition(new(left, top));
   }
   
   public static void MoveRight(
      this IConsole console,
      int steps)
   {
      var width = console.Width;
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