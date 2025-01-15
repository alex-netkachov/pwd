﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;
using pwd.console.readers;

namespace pwd.console;

public sealed class View(
      ILogger<View> logger,
      string id)
   : IView,
     IDisposable
{
   private readonly Lock _lock = new();
   private readonly CancellationTokenSource _cts = new();
   private bool _clear;
   private readonly List<string> _lines = [""];
   private Point _position = new(0, 0);
   private IReader? _reader;
   private IDisposable? _interceptorSubscription;
   private IDisposable? _observersSubscription;
   private IConsole? _console;
   private bool _disposed;
   private readonly List<Action<ConsoleKeyInfo>> _observers = [];
   private Action<ConsoleKeyInfo>? _interceptor;

   public string Id => id;

   public int BufferWidth => -1;

   public int BufferHeight => -1;

   public void Write(
      object? value)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (value == null)
            return;

         var text =
            Convert.ToString(
               value,
               CultureInfo.InvariantCulture);

         if (string.IsNullOrEmpty(text))
            return;

         var (x, y) = (_position.X, _position.Y);
         var line = _lines[y];
         _lines[y] = line[..x] + text + line[x..]; 
         _position = _position with { X = x + text.Length };

         _console?.Write(value);
      }
   }

   public void WriteLine(
      object? value)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         var text =
            Convert.ToString(value)
            ?? "";

         var (x, y) = (_position.X, _position.Y);
         var line = _lines[y];
         _lines[y] = line[..x] + text;
         _lines.Insert(y + 1, line[x..]);
         _position = _position with { Y = _position.Y + 1, X = 0 };

         _console?.WriteLine(text);
      }
   }

   public Point GetCursorPosition()
   {
      return _position;
   }

   public void SetCursorPosition(
      Point point)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_position.Equals(point))
            return;

         if (_position.Y != point.Y)
         {
            throw new NotSupportedException(
               "Changing the cursor position vertically is not supported yet.");
         }

         var delta = point.X - _position.X;

         _position = point;

         if (_console is { } console)
         {
            if (point.X > console.BufferWidth)
            {
               throw new NotSupportedException(
                  "Changing the cursor position in multiline input is not supported yet.");
            }

            var consolePosition = console.GetCursorPosition();
            console.SetCursorPosition(consolePosition with { X = consolePosition.X + delta });
         }
      }
   }

   public async Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      logger.LogDebug(
         "ConfirmAsync: {question}",
         question);

      CommandReader reader;
     
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_reader != null)
            throw new InvalidOperationException("Another operation is in progress.");

         reader = new CommandReader(this);

         _reader = reader;
      }
      
      using var linkedCts =
         CancellationTokenSource.CreateLinkedTokenSource(
            token,
            _cts.Token);
      
      var linkedToken = linkedCts.Token;

      try
      {
         var yes = @default == Answer.Yes ? 'Y' : 'y';
         var no = @default == Answer.No ? 'N' : 'n';

         var input =
            await reader.ReadAsync(
               $"{question} ({yes}/{no}) ",
               linkedToken);

         var answer = input.ToUpperInvariant();

         return @default == Answer.Yes
            ? answer != "N"
            : answer == "Y";
      }
      finally
      {
         _reader = null;
      }
   }

   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestions? suggestions = null,
      IHistory? history = null,
      CancellationToken token = default)
   {
      logger.LogDebug(
         "ReadAsync: {prompt}",
         prompt);

      CommandReader reader;
      
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_reader != null)
         {
            throw new InvalidOperationException(
               "Another operation is in progress.");
         }

         reader =
            new CommandReader(
               this,
               suggestions,
               history);

         _reader = reader;
      }

      using var linkedCts =
         CancellationTokenSource.CreateLinkedTokenSource(
            token,
            _cts.Token);

      var linkedToken = linkedCts.Token;

      try
      {
         return await reader.ReadAsync(
            prompt,
            linkedToken);
      }
      finally
      {
         _reader = null;
      }
   }

   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      logger.LogDebug(
         "ReadPasswordAsync: {prompt}",
         prompt);

      PasswordReader reader;

      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_reader != null)
         {
            throw new InvalidOperationException(
               "Another operation is in progress.");
         }

         reader = new PasswordReader(this);

         _reader = reader;
      }

      using var linkedCts =
         CancellationTokenSource.CreateLinkedTokenSource(
            token,
            _cts.Token);

      var linkedToken = linkedCts.Token;

      try
      {
         return await reader.ReadAsync(
            prompt,
            linkedToken);
      }
      finally
      {
         _reader = null;
      }
   }

   public void Clear()
   {
      _clear = true;
      _lines.Clear();
      _lines.Add("");
      _position = new(0, 0);

      _console?.Clear();
   }

   public IDisposable Observe(
      Action<ConsoleKeyInfo> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _observers.Add(action);
         
         if (_console is { } console
             && _observers.Count == 1)
         {
            _observersSubscription =
               console.Observe(ProcessObservedKey);
         }
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _observers.Remove(action);

            if (_observers.Count == 0)
               _observersSubscription?.Dispose();
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
         
         if (_console is { } console)
         {
            _interceptorSubscription =
               console.Intercept(ProcessInterceptedKey);
         }
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _interceptorSubscription?.Dispose();
            _interceptor = null;
         }
      });
   }

   public void Activate(
      IConsole console)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_console is not null)
         {
            throw new InvalidOperationException(
               "Already activated.");
         }

         _console = console;

         if (_clear)
            console.Clear();
         
         for (var i = 0; i < _lines.Count - 1; i++)
         {
            var item = _lines[i];
            if (!string.IsNullOrEmpty(item))
               console.Write(item);
            console.WriteLine("");
         }

         if (!string.IsNullOrEmpty(_lines[^1]))
            console.Write(_lines[^1]);

         var consolePosition = _console.GetCursorPosition();
         _console.SetCursorPosition(
            consolePosition with { X = _position.X });

         if (_observers.Count > 0)
         {
            _observersSubscription =
               console.Observe(ProcessObservedKey);
         }

         if (_interceptor != null)
         {
            _interceptorSubscription =
               console.Intercept(ProcessInterceptedKey);
         }
      }
   }

   public void Deactivate()
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _interceptorSubscription?.Dispose();
         
         _console = null;
      }
   }

   public void Dispose()
   {
      lock (_lock)
      {
         if (_disposed)
            return;

         _disposed = true;
      }
      
      _interceptorSubscription?.Dispose();

      _cts.Cancel();
      _cts.Dispose();
   }
   
   private void ProcessInterceptedKey(
      ConsoleKeyInfo key)
   {
      lock (_lock)
      {
         if (_disposed)
            return;

         _interceptor?.Invoke(key);
      }
   }
   
   private void ProcessObservedKey(
      ConsoleKeyInfo key)
   {
      lock (_lock)
      {
         if (_disposed)
            return;

         foreach (var observer in _observers)
            observer.Invoke(key);
      }
   }
}