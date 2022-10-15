using Moq;
using pwd.readline;
using pwd.mocks;

namespace pwd.tests.readline;

public sealed class Reader_Tests
{
   [TestCase("b{<}a\n", "ab")]
   [TestCase("b{<}{<}{<}a\n", "ab")]
   [TestCase("b{<}{<}{<}{>}{>}{>}{<}{<}{<}{<}a\n", "ab")]
   [TestCase("b{<}a{>}c\n", "abc")]
   [TestCase("{<x}\n", "")]
   [Timeout(1000)]
   public async Task Read(
      string instruction,
      string expected)
   {
      var reader = new Reader(new TestConsole(() => new TestConsoleReader(instruction)));
      var input = await reader.ReadAsync();
      Assert.That(input, Is.EqualTo(expected));
   }

   [Test]
   public void Disposing_reader_cancels_reading()
   {
      var reader = new Reader(new TestConsole(() => new TestConsoleReader()));
      var task1 = reader.ReadAsync();
      var task2 = reader.ReadAsync();
      reader.Dispose();
      Assert.That(task1.IsCanceled);
      Assert.That(task2.IsCanceled);
   }

   [Test]
   public async Task Reader_reads_sequentially()
   {
      TestConsoleReader? consoleReader = null;
      using var reader = new Reader(new TestConsole(() =>
      {
         Assert.That(consoleReader == null || consoleReader.Disposed);
         return consoleReader = new("*\n");
      }));
      await Task.WhenAll(reader.ReadAsync(), reader.ReadAsync(), reader.ReadAsync());
      reader.Dispose();
      Assert.That(consoleReader is { Disposed: true });
   }

   [TestCase("{TAB}\n", "test1")]
   [TestCase("{TAB}{TAB}\n", "test2")]
   [TestCase("t{TAB}\n", "test1")]
   [TestCase("t{TAB}{TAB}\n", "test2")]
   [TestCase("t{TAB}{TAB}{TAB}\n", "test1")]
   [TestCase("o{TAB}\n", "ok")]
   [TestCase("t{TAB}{BS}{TAB}\n", "test1")]
   public async Task Reader_shows_suggestions_on_tab(
      string instruction,
      string expected)
   {
      var reader = new Reader(new TestConsole(() => new TestConsoleReader(instruction)));
      var mockSuggestionsProvider = new Mock<ISuggestionsProvider?>();
      mockSuggestionsProvider
         .Setup(m => m.Get(It.IsAny<string>()))
         .Returns<string>(input =>
         {
            return (
               input.Length,
               new[] { "test1", "test2", "ok" }.Where(item => item.StartsWith(input)).ToList());
         });
      var input = await reader.ReadAsync(suggestionsProvider: mockSuggestionsProvider.Object);
      Assert.That(input, Is.EqualTo(expected));

   }
}