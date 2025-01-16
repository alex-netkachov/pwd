using System;
using System.Collections.Generic;
using System.Linq;

namespace pwd.console.mocks;

public static class ConsoleKeys
{
   public static IReadOnlyList<ConsoleKeyInfo> Parse(
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