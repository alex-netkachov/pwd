using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using pwd.console.abstractions;
using pwd.library;

namespace pwd.console.mocks;

public sealed class VirtualConsoleContentUpdate(
   VirtualConsole console,
   IReadOnlyList<string> content)
{
   public VirtualConsole Console => console;
   public IReadOnlyList<string> Content => content;
}

public sealed class VirtualConsole(
      int width = -1,
      int height = -1)
   : IConsole,
     IObservable<VirtualConsoleContentUpdate>,
     IDisposable
{
   private readonly Lock _lock = new();
   private readonly CancellationTokenSource _cts = new();
   private readonly List<string> _content = [""];
   private readonly List<IObserver<VirtualConsoleContentUpdate>> _contentObservers = [];
   private readonly List<IObserver<ConsoleKeyInfo>> _consoleKeyObservers = [];
   private IObserver<ConsoleKeyInfo>? _consoleKeyInterceptor;
   private bool _disposed;
   private Point _cursorPosition;

   public int Width => width;

   public int Height => height;

   public void SendKeys(
      IReadOnlyCollection<ConsoleKeyInfo> keys)
   {
      lock (_lock)
      {
         foreach (var key in keys)
         {
            foreach (var action in _consoleKeyObservers)
               action.OnNext(key);

            _consoleKeyInterceptor?.OnNext(key);
         }
      }
   }

   public IDisposable Subscribe(
      IObserver<VirtualConsoleContentUpdate> observer)
   {
      lock (_lock)
      {
         if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

         _contentObservers.Add(observer);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _contentObservers.Remove(observer);
         }
      });
   }

   public IDisposable Subscribe(
      IObserver<ConsoleKeyInfo> observer)
   {
      lock (_lock)
      {
         if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

         _consoleKeyObservers.Add(observer);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _consoleKeyObservers.Remove(observer);
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

         if (_consoleKeyInterceptor != null)
            throw new InvalidOperationException("Interceptor already set.");

         _consoleKeyInterceptor = interceptor;
      }
      
      return new Disposable(() =>
      {
         lock (_lock)
         {
            _consoleKeyInterceptor = null;
         }
      });
   }

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

   public void Write(
      object? value = null)
   {
      var text = ValueToString(value);

      if (string.IsNullOrEmpty(text))
         return;

      lock (_lock)
      {
         foreach (var item in text)
            WriteChar(item);

         ContentChanged();
      }
   }

   public void WriteLine(
      object? value = null)
   {
      var text = ValueToString(value);

      lock (_lock)
      {
         foreach (var item in text)
            WriteChar(item);

         WriteChar('\n');

         ContentChanged();
      }
   }

   private void WriteChar(
      char value)
   {
      var x = _cursorPosition.X;
      var y = _cursorPosition.Y;

      if (value == '\n')
      {
         var line = _content[y];
         _content[y] = line[..x];
         _content.Insert(y + 1, line[x..]);
         _cursorPosition =
            _cursorPosition with { X = 0, Y = y + 1 };
      }
      else
      {
         var line = _content[y];
         _content[y] = $"{line[..x]}{value}{line[x..]}";
         _cursorPosition =
            _cursorPosition with { X = x + 1 };
      }
   }

   private static string ValueToString(
      object? value)
   {
      return Convert.ToString(
                value,
                CultureInfo.InvariantCulture)
             ?? "";
   }

   public Point GetCursorPosition()
   {
      return _cursorPosition;
   }

   public void SetCursorPosition(
      Point position)
   {
      _cursorPosition = position;
   }

   public void Clear()
   {
      lock (_lock)
      {
         _content.Clear();
         _content.Add("");
         
         ContentChanged();
      }
   }

   public string GetScreen()
   {
      lock (_lock)
      {
         return string.Join(
            "\n",
            _content);
      }
   }
   
   private void ContentChanged()
   {
      foreach (var action in _contentObservers)
         action.OnNext(new(this, _content));
   }
}