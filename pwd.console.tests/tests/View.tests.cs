using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using pwd.console.mocks;

namespace pwd.console.tests;

public class View_Tests
{
   [Test]
   public async Task Read_makes_output_as_expected_when_with_console()
   {
      using var console = new TestConsole();
      using var contentSubscription =
         console.Subscribe(
            (sender, content) =>
            { 
               if (content[^1] == "test> ")
                  sender.SendKeys("ok\n");
            });

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

      using var console = new TestConsole();
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

      using var console = new TestConsole();
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

      using var console = new TestConsole();
      view.Activate(console);
      Assert.That(
         console.GetScreen(),
         Is.EqualTo("az"));
   }
   
   private static View CreateView()
   {
      return new View(
         Mock.Of<ILogger<View>>(),
         "test");
   }
}