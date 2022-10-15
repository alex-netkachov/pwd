using System.Text;
using pwd.readline;

namespace pwd.mocks;

public static class TestConsoleReaderText
{
   public static IReadOnlyList<ConsoleKeyInfo> Parse(
      string text)
   {
      var list = new List<(char, ConsoleKey)>();
      var index = 0;
      while (true)
      {
         if (index == text.Length)
            return list
               .Select(item => new ConsoleKeyInfo(item.Item1, item.Item2, false, false, false))
               .ToList();

         var ch = text[index];
         if (ch == '{')
         {
            var close = text.IndexOf('}', index + 1);
            if (close == -1)
               throw new FormatException();

            var token = text.Substring(index + 1, close - index - 1);

            index = close;

            list.Add((' ', token switch
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
         else
         {
            list.Add((ch, ch switch
            {
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

public sealed class TestConsoleReader
   : IConsoleReader
{
   private readonly IReadOnlyList<ConsoleKeyInfo> _text;
   private int _index;

   public TestConsoleReader(
      string text = "")
   {
      _text = TestConsoleReaderText.Parse(text);
   }

   public bool Disposed { get; private set; }

   public void Dispose()
   {
      Disposed = true;
   }

   public async ValueTask<ConsoleKeyInfo> ReadAsync(
      CancellationToken cancellationToken = default)
   {
      if (_index == _text.Count)
      {
         await Task.Delay(Timeout.Infinite, cancellationToken);
         return default;
      }

      var key = _text[_index];
      _index++;
      if (key.Key == ConsoleKey.Multiply)
         await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
      return key;

   }
}

public sealed class TestConsole
   : IConsole
{
   private readonly Func<IConsoleReader> _readerFactory;
   private readonly List<StringBuilder> _writes;

   public TestConsole(
      Func<IConsoleReader> readerFactory)
   {
      _readerFactory = readerFactory;

      _writes = new() { new()};
   }

   public int BufferWidth => 80;

   public IConsoleReader Subscribe()
   {
      return _readerFactory();
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

   public static ConsoleKeyInfo CharToKeyInfo(
      char character)
   {
      var key = character switch
      {
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
         '\n' => ConsoleKey.Enter,
         '→' => ConsoleKey.RightArrow,
         '←' => ConsoleKey.LeftArrow,
         '^' => ConsoleKey.Home,
         '$' => ConsoleKey.End,
         '⌫' => ConsoleKey.Backspace,
         '`' => ConsoleKey.Delete,
         '*' => ConsoleKey.Multiply,
         _ => ConsoleKey.Spacebar
      };

      return new(character, key, false, false, false);
   }
}