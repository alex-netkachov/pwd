using pwd.console.mocks;

namespace pwd.console.tests;

public class VirtualConsole_Tests
{
   [TestCase(-1, -1, "", "{X=0,Y=0}")]
   [TestCase(-1, -1, "a", "{X=1,Y=0}")]
   [TestCase(-1, -1, "1234", "{X=4,Y=0}")]
   [TestCase(-1, -1, "a\nb", "{X=1,Y=1}")]
   [TestCase(2, 2, "a", "{X=1,Y=0}")]
   [TestCase(2, 2, "ab", "{X=1,Y=0}")]
   [TestCase(2, 2, "abc", "{X=1,Y=1}")]
   [TestCase(2, 2, "test", "{X=1,Y=1}")]
   public void Writes_text_as_expected(
      int width,
      int height,
      string text,
      string expectedPosition)
   {
      var console =
         new VirtualConsole(
            width,
            height);

      console.Write(text);

      var position = console.GetCursorPosition();

      Assert.That(
         position.ToString(),
         Is.EqualTo(expectedPosition));
   }
   
   [Test]
   public void Writing_line_moves_position()
   {
      var console = new VirtualConsole();
      console.WriteLine("a");
      Assert.That(
         console.GetCursorPosition().ToString(),
         Is.EqualTo("{X=0,Y=1}"));
   }
}