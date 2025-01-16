using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using pwd.console.mocks;
using pwd.library;

namespace pwd.console.tests;

public class View_Tests
{
   [Test]
   public async Task Read_makes_output_as_expected_when_with_console()
   {
      using var console = new VirtualConsole();
      using var contentSubscription =
         console.Subscribe(
            new Observer<VirtualConsoleContentUpdate>(
               update =>
               {
                  if (update.Content[^1] == "test> ")
                     update.Console.SendKeys(
                        ConsoleKeys.Parse("ok\n"));
               }));

      using var view = CreateView();

      view.Activate(console);
      await view.ReadAsync("test> ");

      Assert.That(
         console.GetScreen(),
         Is.EqualTo("test> ok\n"));
   }

   [TestCase("a", "a", "{X=1,Y=0}")]
   [TestCase("ab", "ab", "{X=2,Y=0}")]
   public void Write_updates_content_as_expected(
      string value,
      string expectedContent,
      string expectedPosition)
   {
      var view = CreateView();

      view.Write(value);
      
      Assert.That(
         view.GetCursorPosition().ToString(),
         Is.EqualTo(expectedPosition));

      using var console = new VirtualConsole();
      view.Activate(console);
      Assert.That(
         console.GetScreen(),
         Is.EqualTo(expectedContent));
   }
   
   [TestCase("a", "a\n", "{X=0,Y=1}")]
   [TestCase("ab", "ab\n", "{X=0,Y=1}")]
   public void WriteLine_updates_content_as_expected(
      string value,
      string expectedContent,
      string expectedPosition)
   {
      var view = CreateView();

      view.WriteLine(value);
      
      Assert.That(
         view.GetCursorPosition().ToString(),
         Is.EqualTo(expectedPosition));

      using var console = new VirtualConsole();
      view.Activate(console);
      Assert.That(
         console.GetScreen(),
         Is.EqualTo(expectedContent));
   }
   
   [Test]
   public void Write_after_reposition_updates_content_as_expected()
   {
      var view = CreateView();

      view.Write("z");
      view.SetCursorPosition(new Point(0, 0));
      view.Write("a");

      Assert.That(
         view.GetCursorPosition().ToString(),
         Is.EqualTo("{X=1,Y=0}"));

      using var console = new VirtualConsole();
      view.Activate(console);
      Assert.That(
         console.GetScreen(),
         Is.EqualTo("az"));
   }

   [TestCase(-1, -1, "a", 0, 0, "{X=0,Y=0}")]
   [TestCase(-1, -1, "abc", 0, 0, "{X=0,Y=0}")]
   [TestCase(-1, -1, "abc", 1, 0, "{X=1,Y=0}")]
   [TestCase(-1, -1, "abc", 2, 0, "{X=2,Y=0}")]
   [TestCase(-1, -1, "abc", 3, 0, "{X=3,Y=0}")]
   public void Reposition_works_as_expected(
      int width,
      int height,
      string content,
      int pointX,
      int pointY,
      string expectedPosition)
   {
      var console = new VirtualConsole(width, height);
      var view = CreateView();
      view.Write(content);
      view.Activate(console);

      var point = new Point(pointX, pointY);
      view.SetCursorPosition(point);

      Assert.That(
         console.GetCursorPosition().ToString(),
         Is.EqualTo(expectedPosition));
   }
   
   private static View CreateView()
   {
      return new View(
         Mock.Of<ILogger<View>>(),
         "test");
   }
}