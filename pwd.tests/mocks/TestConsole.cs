using System.Text;
using pwd.readline;

namespace pwd.mocks;

public sealed class TestConsoleReader
   : IConsoleReader
{
   private readonly string _text;
   private int _index;

   public TestConsoleReader(
      string text = "")
   {
      _text = text;
   }

   public bool Disposed { get; private set; }

   public void Dispose()
   {
      Disposed = true;
   }

   public async ValueTask<ConsoleKeyInfo> ReadAsync(
      CancellationToken cancellationToken = default)
   {
      if (_index == _text.Length)
      {
         await Task.Delay(Timeout.Infinite, cancellationToken);
         return default;
      }

      var key = _text[_index];
      _index++;
      if (key == '*')
         await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
      return TestConsole.CharToKeyInfo(key);
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
         '>' => ConsoleKey.RightArrow,
         '<' => ConsoleKey.LeftArrow,
         '^' => ConsoleKey.Home,
         '$' => ConsoleKey.End,
         '~' => ConsoleKey.Backspace,
         '`' => ConsoleKey.Delete,
         '*' => ConsoleKey.Multiply,
         _ => ConsoleKey.Spacebar
      };

      return new(character, key, false, false, false);
   }
}