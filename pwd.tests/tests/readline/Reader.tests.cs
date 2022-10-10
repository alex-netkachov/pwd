using pwd.readline;

namespace pwd.tests.readline;

public sealed class Reader_Tests
{
   [TestCase("b<a\n", "ab")]
   [TestCase("b<<<<<a\n", "ab")]
   [TestCase("b<<<>>><<<<a\n", "ab")]
   [TestCase("b<a>c\n", "abc")]
   [Timeout(1000)]
   public async Task Read(
      string instruction,
      string expected)
   {
      var reader = new Reader(new TestConsole(() => new TestConsoleReader(instruction)));
      var input = await reader.ReadAsync();
      Assert.That(input, Is.EqualTo(expected));
   }
   
   [Test]
   public void Disposing_reader_cancels_reading()
   {
      var reader = new Reader(new TestConsole(() => new TestConsoleReader()));
      var task1 = reader.ReadAsync();
      var task2 = reader.ReadAsync();
      reader.Dispose();
      Assert.That(task1.IsCanceled);
      Assert.That(task2.IsCanceled);
   }

   [Test]
   public async Task Reader_reads_sequentially_20()
   {
      for (var i = 0; i < 20; i++)
         await Reader_reads_sequentially();
   }

   [Test]
   public async Task Reader_reads_sequentially()
   {
      var log = new List<string>();
      var readers = new[]
      {
         new TestConsoleReader("*\n"),
         new TestConsoleReader("-\n"),
         new TestConsoleReader("*\n"),
         new TestConsoleReader("-\n")
      };
      var index = -1;
      using var reader = new Reader(new TestConsole(() =>
      {
         lock (log)
         {
            index++;
            var reader = readers[index];
            log.Add(reader.Text);
            return reader;
         }
      }));
      // ReSharper disable once AccessToDisposedClosure because here it is ok
      await Task.WhenAll(readers.Select(_ => reader.ReadAsync()));
      reader.Dispose();

      Assert.That(string.Join("", log), Is.EqualTo("*\n-\n*\n-\n"));
   }


   private sealed class TestConsoleReader
      : IConsoleReader
   {
      private int _index;

      public TestConsoleReader(
         string text = "")
      {
         Text = text;
      }

      public string Text { get; }

      public void Dispose()
      {
      }

      public async ValueTask<ConsoleKeyInfo> ReadAsync(
         CancellationToken cancellationToken = default)
      {
         if (_index == Text.Length)
         {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return default;
         }

         var key = Text[_index];
         _index++;
         if (key == '*')
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
         return TestConsole.CharToKeyInfo(key);
      }
   }

   private sealed class TestConsole
      : IConsole
   {
      private readonly Func<IConsoleReader> _readerFactory;

      public TestConsole(
         Func<IConsoleReader> readerFactory)
      {
         _readerFactory = readerFactory;
      }

      public int BufferWidth => 80;

      public IConsoleReader Subscribe()
      {
         return _readerFactory();
      }

      public void Write(
         object? value = null)
      {
      }

      public void WriteLine(
         object? value = null)
      {
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
            '.' => ConsoleKey.Delete,
            '*' => ConsoleKey.Multiply,
            _ => ConsoleKey.Spacebar
         };

         return new(character, key, false, false, false);
      }
   }
}