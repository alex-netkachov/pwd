using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using pwd.console.abstractions;

namespace pwd.console.mocks;

public sealed class TestConsole
   : IConsole,
     IDisposable
{
   private readonly Lock _lock = new();
   private readonly CancellationTokenSource _cts = new();
   private readonly List<string> _content = [""];
   private bool _disposed;
   private readonly List<Action<TestConsole, IReadOnlyList<string>>> _contentObservers = [];
   private readonly List<Action<ConsoleKeyInfo>> _consoleKeyObservers = [];
   private Action<ConsoleKeyInfo>? _consoleKeyInterceptor;

   public int BufferWidth => -1;

   public int BufferHeight => -1;

   public void SendKeys(
      string text)
   {
      lock (_lock)
      {
         var keys = TestInputToKeyCodeInfos(text);

         foreach (var key in keys)
         {
            foreach (var action in _consoleKeyObservers)
               action(key);

            _consoleKeyInterceptor?.Invoke(key);
         }
      }
   }

   public IDisposable Subscribe(
      Action<TestConsole, IReadOnlyList<string>> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _contentObservers.Add(action);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _contentObservers.Remove(action);
         }
      });
   }

   public IDisposable Observe(
      Action<ConsoleKeyInfo> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _consoleKeyObservers.Add(action);
      }

      return new Disposable(() =>
      {
         lock (_lock)
         {
            _consoleKeyObservers.Remove(action);
         }
      });
   }
   
   public IDisposable Intercept(
      Action<ConsoleKeyInfo> action)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);

         if (_consoleKeyInterceptor != null)
            throw new InvalidOperationException("Interceptor already set.");

         _consoleKeyInterceptor = action;
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
      var text =
         Convert.ToString(value)
         ?? "";

      if (string.IsNullOrEmpty(text))
         return;

      lock (_lock)
      {
         _content[^1] += Convert.ToString(value) ?? "";

         ContentChanged();
      }
   }

   public void WriteLine(
      object? value = null)
   {
      lock (_lock)
      {
         _content[^1] += Convert.ToString(value) ?? "";
         _content.Add("");

         ContentChanged();
      }
   }

   public Point GetCursorPosition()
   {
      return new(0, 0);
   }

   public void SetCursorPosition(
      Point point)
   {
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
         action(this, _content);
   }

   private static IReadOnlyList<ConsoleKeyInfo> TestInputToKeyCodeInfos(
      string text)
   {
      var list = new List<(char Symbol, bool Shift, ConsoleKey ConsoleKey)>();
      var index = 0;
      while (true)
      {
         if (index == text.Length)
            return list
               .Select(item => new ConsoleKeyInfo(item.Symbol, item.ConsoleKey, item.Shift, false, false))
               .ToList();

         var ch = text[index];
         if (ch == '{')
         {
            var close = text.IndexOf('}', index + 1);
            if (close == -1)
               throw new FormatException();

            var token = text.Substring(index + 1, close - index - 1);

            index = close;

            list.Add((' ', false, token switch
            {
               "<" => ConsoleKey.LeftArrow,
               "LA" => ConsoleKey.LeftArrow,
               ">" => ConsoleKey.RightArrow,
               "RA" => ConsoleKey.RightArrow,
               "<x" => ConsoleKey.Backspace,
               "BS" => ConsoleKey.Backspace,
               "x" => ConsoleKey.Delete,
               "DEL" => ConsoleKey.Delete,
               "|<" => ConsoleKey.Home,
               "HOME" => ConsoleKey.Home,
               ">|" => ConsoleKey.End,
               "END" => ConsoleKey.End,
               "TAB" => ConsoleKey.Tab,
               _ => throw new FormatException()
            }));
         }
         else if (ch == ':')
         {
            list.Add((ch, true, ConsoleKey.Oem1));
         }
         else
         {
            list.Add((ch, false, ch switch
            {
               '0' => ConsoleKey.D0,
               '1' => ConsoleKey.D1,
               '2' => ConsoleKey.D2,
               '3' => ConsoleKey.D3,
               '4' => ConsoleKey.D4,
               '5' => ConsoleKey.D5,
               '6' => ConsoleKey.D6,
               '7' => ConsoleKey.D7,
               '8' => ConsoleKey.D8,
               '9' => ConsoleKey.D9,
               'a' => ConsoleKey.A,
               'b' => ConsoleKey.B,
               'c' => ConsoleKey.C,
               'd' => ConsoleKey.D,
               'e' => ConsoleKey.E,
               'f' => ConsoleKey.F,
               'g' => ConsoleKey.G,
               'h' => ConsoleKey.H,
               'i' => ConsoleKey.I,
               'j' => ConsoleKey.J,
               'k' => ConsoleKey.K,
               'l' => ConsoleKey.L,
               'm' => ConsoleKey.M,
               'n' => ConsoleKey.N,
               'o' => ConsoleKey.O,
               'p' => ConsoleKey.P,
               'q' => ConsoleKey.Q,
               'r' => ConsoleKey.R,
               's' => ConsoleKey.S,
               't' => ConsoleKey.T,
               'u' => ConsoleKey.U,
               'v' => ConsoleKey.V,
               'w' => ConsoleKey.W,
               'x' => ConsoleKey.X,
               'y' => ConsoleKey.Y,
               'z' => ConsoleKey.Z,
               '.' => ConsoleKey.Decimal,
               '*' => ConsoleKey.Multiply,
               '\n' => ConsoleKey.Enter,
               ' ' => ConsoleKey.Spacebar,
               _ => throw new FormatException()
            }));
         }

         index++;
      }
   }
}