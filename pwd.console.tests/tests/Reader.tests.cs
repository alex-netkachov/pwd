using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Moq;
using pwd.console.abstractions;
using pwd.console.readers;
using pwd.console.mocks;

namespace pwd.console.tests;

public sealed class Reader_Tests
{
   [TestCase("b{<}a\n", "ab")]
   [TestCase("b{<}{<}{<}a\n", "ab")]
   [TestCase("b{<}{<}{<}{>}{>}{>}{<}{<}{<}{<}a\n", "ab")]
   [TestCase("b{<}a{>}c\n", "abc")]
   [TestCase("{<x}\n", "")]
   [CancelAfter(1000)]
   public async Task Read(
      string keys,
      string expected)
   {
      using var console = new TestConsole();

      var reader = new CommandReader(console);

      using var contentSubscription =
         console.Subscribe(
            (sender, content) =>
            {
               if (content[^1] == "> ")
                  sender.SendKeys(keys);
            });

      var input =
         await reader.ReadAsync(
            "> ",
            CancellationToken.None);
      Assert.That(input, Is.EqualTo(expected));
   }

   [Test]
   public void Disposing_reader_cancels_reading()
   {
      var reader =
         new CommandReader(
            new TestConsole());

      var input =
         reader.ReadAsync(
            "",
            CancellationToken.None);

      reader.Dispose();

      Assert.That(input.IsCanceled);
   }

   [TestCase("{TAB}\n", "test1")]
   [TestCase("{TAB}{TAB}\n", "test2")]
   [TestCase("t{TAB}\n", "test1")]
   [TestCase("t{TAB}{TAB}\n", "test2")]
   [TestCase("t{TAB}{TAB}{TAB}\n", "test1")]
   [TestCase("o{TAB}\n", "ok")]
   [TestCase("t{TAB}{BS}{TAB}\n", "test1")]
   [CancelAfter(1000)]
   public async Task Reader_shows_suggestions_on_tab(
      string keys,
      string expected)
   {
      var suggestions = new Mock<ISuggestions>();
      suggestions
         .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<int>()))
         .Returns<string, int>((input, position) =>
         {
            return new[] { "test1", "test2", "ok" }
               .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .ToList();
         });

      using var console = new TestConsole();
      
      using var contentSubscription =
         console.Subscribe(
            (sender, content) =>
            {
               if (content[^1] == "> ")
                  sender.SendKeys(keys);
            });

      var reader =
         new CommandReader(
            console,
            suggestions.Object);

      var input =
         await reader.ReadAsync(
            "> ",
            CancellationToken.None);

      Assert.That(
         input,
         Is.EqualTo(expected));
   }
}