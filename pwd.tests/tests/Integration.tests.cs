using Moq;
using pwd.readline;
using pwd.mocks;

namespace pwd.tests;

[TestFixture]
public class Integration_Tests
{
   [Test]
   public async Task Test_Main1()
   {
      var fs = Shared.GetMockFs();

      var index = -1;
      var console = new TestConsole(() =>
      {
         return new TestConsoleReader(++index switch
         {
            0 => "",
            1 => "secret\n",
            2 => "secret\n",
            _ => ".quit\n"
         });
      });

      var view = new View(console, new Reader(console), Timeout.InfiniteTimeSpan);
      
      var state = new State();
      await Program.Run(Mock.Of<ILogger>(), console, fs, view, state);
      var expected = string.Join("\n",
         "Password: ******\n",
         "repository contains 0 files",
         "It seems that you are creating a new repository. Please confirm password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
}