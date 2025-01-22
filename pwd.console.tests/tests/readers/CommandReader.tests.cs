using System.Threading;
using System.Threading.Tasks;
using pwd.console.mocks;
using pwd.console.readers;
using pwd.library;

namespace pwd.console.tests.readers;

public class CommandReader_Tests
{
   [TestCase("a", "a", ">a")]
   [TestCase("a{<x}", "", "> ")]
   [TestCase("ab", "ab", ">ab")]
   [TestCase("abc", "abc", ">abc")]
   [TestCase("test", "test", ">tes\nt")]
   [TestCase("{<x}a", "a", ">a")]
   [TestCase("ab{<x}", "a", ">a ")]
   [TestCase("ab{<}{<x}", "b", ">b ")]
   public async Task Navigates_as_expected(
      string keys,
      string expectedInput,
      string expectedScreen)
   {
      var console = new VirtualConsole(4, 3);

      var reader = new CommandReader(console);

      using var contentSubscription =
         console.Subscribe(
            new Observer<VirtualConsoleContentUpdated>(
               update =>
               {
                  if (update.Console.GetCurrentLine() != ">")
                     return;
                 
                  var input =
                     ConsoleKeys.Parse($"{keys}\n");
                  update.Console.SendKeys(input);
               }));
      
      var input = await reader.ReadAsync(">", CancellationToken.None);

      Assert.That(
         input,
         Is.EqualTo(expectedInput));

      Assert.That(
         console.GetText(),
         Is.EqualTo(expectedScreen));
   }
}