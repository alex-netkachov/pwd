using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using pwd.console.abstractions;
using pwd.library;

namespace pwd.console.mocks;

public sealed class VirtualConsoleContentUpdated(
   VirtualConsole console)
{
   public VirtualConsole Console => console;
}

public sealed class VirtualConsole(
      int width = -1,
      int height = -1)
   : IConsole,
     IObservable<VirtualConsoleContentUpdated>,
     IDisposable
{
   private readonly Lock _lock = new();
   private readonly CancellationTokenSource _cts = new();
   private readonly List<List<char>> _buffer = [];
   private readonly List<IObserver<VirtualConsoleContentUpdated>> _contentObservers = [];
   private readonly List<IObserver<ConsoleKeyInfo>> _consoleKeyObservers = [];
   private IObserver<ConsoleKeyInfo>? _consoleKeyInterceptor;
   private Point _cursorPosition;
   private bool _cursorPositionLineEndFlag;
   private bool _disposed;

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
      IObserver<VirtualConsoleContentUpdated> observer)
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
      }
   }

   private void WriteChar(
      char value)
   {
      var y = _cursorPosition.Y;
      var x = _cursorPosition.X;

      if (value == '\n')
      {
         _cursorPositionLineEndFlag = false;

         if (Height > 0
             && x == Height - 1)
         {
            for (var i = 1; i < Height; i++)
               _buffer[i - 1] = _buffer[i];
            _buffer[^1] = [];
            _cursorPosition = _cursorPosition with { X = 0 };
            _cursorPositionLineEndFlag = false;
         }
         else
         {
            _cursorPosition = _cursorPosition with { X = 0, Y = y + 1 };
            _buffer.Add(new());
         }
      }
      else
      {
         if (_cursorPositionLineEndFlag)
         {
            _cursorPositionLineEndFlag = false;

            if (Height > 0
                && y == Height - 1)
            {
               for (var i = 1; i < Height; i++)
                  _buffer[i - 1] = _buffer[i];
               _buffer[^1] = [];
               _cursorPosition = _cursorPosition with { X = 0 };
            }
            else
            {
               _cursorPosition = _cursorPosition with { X = 0, Y = y + 1 };
               _buffer.Add(new());
            }

            SetBufferAtCursorPosition(value);
            _cursorPosition = _cursorPosition with { X = 1 };
         }
         else
         {
            SetBufferAtCursorPosition(value);
            if (Width > 0
                && x == Width - 1)
            {
               _cursorPositionLineEndFlag = true;
            }
            else
               _cursorPosition = _cursorPosition with { X = x + 1 };
         }
      }
      
      ContentChanged();
   }

   private void SetBufferAtCursorPosition(
      char value)
   {
      while (_buffer.Count < _cursorPosition.Y + 1)
         _buffer.Add(new());
      var row = _buffer[_cursorPosition.Y];
      while (row.Count < _cursorPosition.X + 1)
         row.Add(' ');
      row[_cursorPosition.X] = value;
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
      _cursorPositionLineEndFlag = false;
   }

   public void Clear()
   {
      lock (_lock)
      {
         _buffer.Clear();
         _cursorPosition = new(0, 0);
         _cursorPositionLineEndFlag = false;

         ContentChanged();
      }
   }

   public string GetCurrentLine()
   {
      lock (_lock)
      {
         return GetString(_cursorPosition.Y);
      }
   }

   public string GetText()
   {
      lock (_lock)
      {
         var result = new List<string>();

         for (var i = 0; i < _buffer.Count; i++)
            result.Add(GetString(i));

         while (result.Count > 0
                && string.IsNullOrWhiteSpace(result[^1]))
         {
            result.RemoveAt(result.Count - 1);
         }

         return string.Join("\n", result);
      }
   }

   private string GetString(
      int index)
   {
      return new(_buffer[index].ToArray());
   }
   
   private void ContentChanged()
   {
      foreach (var action in _contentObservers)
         action.OnNext(new(this));
   }
}