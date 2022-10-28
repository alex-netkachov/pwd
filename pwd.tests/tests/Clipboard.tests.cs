using System.Threading.Channels;
using Moq;
using pwd.mocks;

namespace pwd.tests;

public sealed class Clipboard_Tests
{
   [Test]
   [Timeout(1000)]
   public async Task Clipboard_clears_its_content_after_timeout()
   {
      var channel = Channel.CreateUnbounded<string>();
      var (writer, reader) = (channel.Writer, channel.Reader);

      var timers = new TestTimers();
      var mockRunner = new Mock<IRunner>();
      mockRunner
         .Setup(m => m.Run(It.IsAny<string>(), It.IsAny<string>()))
         .Callback<string, string>((cmd, input) => writer.WriteAsync(input));
      var clipboard = new Clipboard(Mock.Of<ILogger>(), mockRunner.Object, timers);
      
      clipboard.Put("test", TimeSpan.FromSeconds(1));
      
      var first = await reader.ReadAsync();
      Assert.That(first, Is.EqualTo("test"));
      
      timers.Forward();
      
      var second = await reader.ReadAsync();
      Assert.That(second, Is.EqualTo(""));
   }
}