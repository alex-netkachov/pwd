using System.Threading;
using System.Threading.Tasks;
using pwd.console.mocks;
using pwd.console.readers;
using pwd.library;

namespace pwd.console.tests.readers;

public class PasswordReader_Tests
{
   [TestCase("a", "a", ">*")]
   [TestCase("a{<x}", "", "> ")]
   [TestCase("ab", "ab", ">**")]
   [TestCase("abc", "abc", ">***")]
   [TestCase("test", "test", ">***\n*")]
   [TestCase("{<x}a", "a", ">*")]
   [TestCase("ab{<x}", "a", ">* ")]
   public async Task Navigates_as_expected(
      string keys,
      string expectedInput,
      string expectedScreen)
   {
      var console = new VirtualConsole(4, 3);

      var reader = new PasswordReader(console);

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