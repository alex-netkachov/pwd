﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using pwd.ui.console;

namespace pwd.mocks;

public sealed class TestConsole
   : IConsole,
     IDisposable
{
   private record State(
      bool Disposed,
      ImmutableList<ChannelWriter<ConsoleKeyInfo>> Writers);

   private State _state;

   private readonly CancellationTokenSource _cts;
   private readonly List<StringBuilder> _writes;

   public event EventHandler<EventArgs>? OnSubscriber;

   public TestConsole(
      ChannelReader<string> reader)
   {
      _writes = new() { new() };

      _state = new(false, ImmutableList<ChannelWriter<ConsoleKeyInfo>>.Empty);

      _cts = new();

      var token = _cts.Token;

      Task.Run(async () =>
      {
         while (!token.IsCancellationRequested)
         {
            var input = await reader.ReadAsync(token);
            var infos = TestInputToKeyCodeInfos(input);

            var state = _state;
            foreach (var info in infos)
            foreach (var writer in state.Writers)
               await writer.WriteAsync(info, token);
         }
      }, token);
   }

   public int BufferWidth => 80;

   public void Subscribe(
      ChannelWriter<ConsoleKeyInfo> writer)
   {
      State initial, updated;
      do
      {
         initial = _state;
         if (_state.Disposed)
            throw new ObjectDisposedException(nameof(StandardConsole));
         updated = _state with { Writers = initial.Writers.Add(writer) };
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
      
      OnSubscriber?.Invoke(this, EventArgs.Empty);
   }

   public void Unsubscribe(
      ChannelWriter<ConsoleKeyInfo> writer)
   {
      State initial, updated;
      do
      {
         initial = _state;
         if (_state.Disposed)
            break;
         updated = _state with { Writers = initial.Writers.Remove(writer) };
      } while (initial != Interlocked.CompareExchange(ref _state, updated, initial));
   }

   public void Dispose()
   {
      State initial;
      while (true)
      {
         initial = _state;
         if (initial.Disposed)
            return;
         var updated = new State(true, ImmutableList<ChannelWriter<ConsoleKeyInfo>>.Empty);
         if (initial == Interlocked.CompareExchange(ref _state, updated, initial))
            break;
      }

      _cts.Cancel();
      _cts.Dispose();

      foreach (var writer in initial.Writers)
         writer.Complete();
   }

   public void Write(
      object? value = null)
   {
      _writes.Last().Append(Convert.ToString(value) ?? "");
   }

   public void WriteLine(
      object? value = null)
   {
      _writes.Last().Append(Convert.ToString(value) ?? "");
      _writes.Add(new());
   }

   public (int Left, int Top) GetCursorPosition()
   {
      return (0, 0);
   }

   public void SetCursorPosition(int left, int top)
   {
   }

   public void Clear()
   {
      _writes.Clear();
      _writes.Add(new());
   }

   public string GetScreen()
   {
      return string.Join("\n", _writes.Select(item => item.ToString()));
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