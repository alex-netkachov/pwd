﻿using System.Threading.Channels;
using Moq;
using pwd.console.abstractions;

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
      var channel = Channel.CreateUnbounded<string>();

      _ =
         new Func<Task>(async () =>
         {
            await Task.Delay(100);
            await channel.Writer.WriteAsync(keys);
         }).Invoke();

      using var console = new TestConsole(channel.Reader);
      var reader = new Reader(console);
      var input = await reader.ReadAsync();
      Assert.That(input, Is.EqualTo(expected));
   }

   [Test]
   public void Disposing_reader_cancels_reading()
   {
      var channel = Channel.CreateUnbounded<string>();
      var reader = new Reader(new TestConsole(channel.Reader));
      var task1 = reader.ReadAsync();
      var task2 = reader.ReadAsync();
      reader.Dispose();
      Assert.That(task1.IsCanceled);
      Assert.That(task2.IsCanceled);
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
      var channel = Channel.CreateUnbounded<string>();
      var reader = new Reader(new TestConsole(channel.Reader));
      var mockSuggestionsProvider = new Mock<ISuggestionsProvider>();
      mockSuggestionsProvider
         .Setup(m => m!.Suggestions(It.IsAny<string>()))
         .Returns<string>(input =>
         {
            return new[] { "test1", "test2", "ok" }
               .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
               .ToList();
         });
      var inputTask = reader.ReadAsync(suggestionsProvider: mockSuggestionsProvider.Object);

      await Task.Delay(100);
      await channel.Writer.WriteAsync(keys);

      var input = await inputTask;
      
      Assert.That(input, Is.EqualTo(expected));
   }
}