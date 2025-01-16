using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using pwd.cli;
using pwd.mocks;

namespace pwd.tests;

public sealed class Clipboard_Tests
{
   [Test]
   [CancelAfter(1000)]
   public async Task Clipboard_clears_its_content_after_timeout()
   {
      var channel = Channel.CreateUnbounded<string>();
      var (writer, reader) = (channel.Writer, channel.Reader);

      var timers = new TestTimers();

      var mockRunner = new Mock<IRunner>();
      mockRunner
         .Setup(m => m.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
         .Callback<string, string, string>((cmd, arguments, input) => writer.WriteAsync(input));

      var clipboard = new Clipboard(Mock.Of<ILogger<Clipboard>>(), mockRunner.Object, timers.Create);

      clipboard.Put("test", TimeSpan.FromSeconds(1));

      var first = await reader.ReadAsync();
      Assert.That(first, Is.EqualTo("test"));

      timers.Run();

      var second = await reader.ReadAsync();
      Assert.That(second, Is.EqualTo(""));
   }

   [Test]
   public async Task Disposing_clipboard_does_not_raise_timer()
   {
      var channel = Channel.CreateUnbounded<string>();
      var (writer, reader) = (channel.Writer, channel.Reader);

      var timers = new TestTimers();

      var mockRunner = new Mock<IRunner>();

      var clipboard = new Clipboard(Mock.Of<ILogger<Clipboard>>(), mockRunner.Object, timers.Create);

      mockRunner
         .Setup(m => m.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
         .Callback<string, string, string>((cmd, arguments, input) => writer.WriteAsync(input));

      clipboard.Put("test", TimeSpan.FromMinutes(1));
      
      var first = await reader.ReadAsync();
      Assert.That(first, Is.EqualTo("test"));

      clipboard.Dispose();

      timers.Run();

      // removing test timer and forwarding it move the timer to the max value 
      Assert.That(timers.Time, Is.EqualTo(TimeSpan.MaxValue));
   }
}