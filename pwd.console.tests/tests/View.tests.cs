using System.Threading.Tasks;
using pwd.console.mocks;

namespace pwd.console.tests;

public class View_Tests
{
   [Test]
   public async Task View_reader_makes_output_as_expected()
   {
      var view = new View();
      var console = new TestConsole(["ok\n"]);
      view.Activate(console);
      await view.ReadAsync("test> ");
      Assert.That(
         console.GetScreen(),
         Is.EqualTo("test> ok\n"));
   }
}