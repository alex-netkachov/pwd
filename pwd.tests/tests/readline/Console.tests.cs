using System.Threading.Channels;
using pwd.readline;

namespace pwd.tests.readline;

public class Console_Tests
{
   [Test]
   public void ConsoleReader_ReadAsync_does_not_read_when_cancelled()
   {
      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
      using var reader = new ConsoleReader(channel.Reader);
      var token = new CancellationToken(true);
      var task = reader.ReadAsync(token);
      Assert.That(task.IsCanceled);
   }
   
   [Test]
   public void ConsoleReader_ReadAsync_can_be_cancelled()
   {
      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
      using var reader = new ConsoleReader(channel.Reader);
      using var cts = new CancellationTokenSource();
      cts.CancelAfter(TimeSpan.FromMilliseconds(250));
      var token = cts.Token;
      Assert.CatchAsync<OperationCanceledException>(async () => await reader.ReadAsync(token));
   }

   [Test]
   public void StandardConsole_Subscribe_throws_when_disposed()
   {
      var console = new StandardConsole();
      console.Dispose();
      Assert.That(() => console.Subscribe(), Throws.InstanceOf<ObjectDisposedException>());
   }

   [Test]
   public void StandardConsole_disposing_subscriber_works_well_when_console_is_disposed()
   {
      var console = new StandardConsole();
      var reader = console.Subscribe();
      console.Dispose();
      reader.Dispose();
      Assert.Pass();
   }
}