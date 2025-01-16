using pwd.console.mocks;

namespace pwd.console.tests;

public class VirtualConsole_Tests
{
   [TestCase("a", "{X=1,Y=0}")]
   [TestCase("1234", "{X=4,Y=0}")]
   public void Writing_moves_position(
      string text,
      string expectedPosition)
   {
      var console = new VirtualConsole();
      console.Write(text);
      Assert.That(
         console.GetCursorPosition().ToString(),
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