using System.Threading.Channels;
using pwd.readline;

namespace pwd.tests.readline;

public sealed class Prompt_Tests
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
      var prompt = new Prompt(Timeout.InfiniteTimeSpan, new TestConsole(instruction));
      var input = await prompt.ReadAsync("input: ");
      Assert.That(input, Is.EqualTo(expected));
   }

   private sealed class TestConsole
      : IConsole
   {
      private readonly string _text;

      public TestConsole(
         string text)
      {
         _text = text;
      }

      public int BufferWidth => 80;

      public ChannelReader<ConsoleKeyInfo> Subscribe(
         CancellationToken token)
      {
         var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
         var writer = channel.Writer;

         Task.Run(() =>
         {
            foreach (var character in _text)
            {
               token.ThrowIfCancellationRequested();

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
                  _ => ConsoleKey.Spacebar
               };

               writer.TryWrite(new(character, key, false, false, false));
            }
         }, token);

         return channel.Reader;
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
   }
}