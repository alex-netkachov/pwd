using System.Threading.Channels;
using Moq;
using pwd.ciphers;
using pwd.readline;
using pwd.mocks;

namespace pwd.tests;

[TestFixture]
public class Integration_Tests
{
   private static readonly Settings DefaultSettings = new(Timeout.InfiniteTimeSpan);

   [Test]
   [Timeout(2000)]
   public async Task Initialise_from_empty_repository()
   {
      var fs = Shared.GetMockFs();

      var channel = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync(".quit\n");
      });

      using var console = new TestConsole(channel.Reader);
      using var reader = new Reader(console);
      var view = new View(console, reader);

      await Program.Run(Mock.Of<ILogger>(), console, fs, view, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         "",
         "repository contains 0 files",
         "It seems that you are creating a new repository. Please confirm password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [Timeout(3000)]
   public async Task Initialise_with_repository_with_files()
   {
      var fs = Shared.GetMockFs();
      var nameCipher = new NameCipher("secret");
      var contentCipher = new ContentCipher("secret");
      var repository = new Repository(fs, nameCipher, contentCipher, ".");
      await repository.WriteAsync("file1", "content1");

      var channel = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("file1\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("..\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync(".quit\n");
      });

      var console = new TestConsole(channel.Reader);

      var view = new View(console, new Reader(console));
      
      await Program.Run(Mock.Of<ILogger>(), console, fs, view, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         ".",
         "repository contains 1 file",
         "> file1",
         "content1",
         "file1> ..",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [Timeout(4000)]
   public async Task Initialise_from_empty_repository_plus_locking()
   {
      var fs = Shared.GetMockFs();

      var channel = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync(".lock\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync(".quit\n");
      });

      var console = new TestConsole(channel.Reader);

      var view = new View(console, new Reader(console));
      
      await Program.Run(Mock.Of<ILogger>(), console, fs, view, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }

   [Test]
   [Timeout(20000)]
   public async Task Initialise_from_empty_repository_plus_timeout_lock()
   {
      var fs = Shared.GetMockFs();

      var channel = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(500);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(4000);
         await channel.Writer.WriteAsync("secret\n");
         await Task.Delay(1000);
         await channel.Writer.WriteAsync(".quit\n");
      });

      var console = new TestConsole(channel.Reader);

      var view = new View(console, new Reader(console));
      
      await Program.Run(Mock.Of<ILogger>(), console, fs, view, new(TimeSpan.FromMilliseconds(2000)));
      var expected = string.Join("\n",
         "Password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
}