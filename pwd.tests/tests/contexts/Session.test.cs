using System.Threading.Channels;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using pwd.contexts.file;
using pwd.mocks;
using pwd.readline;

namespace pwd.tests.contexts;

public sealed class Session_Tests
{
   [Test]
   public void construct_session()
   {
      Shared.CreateSessionContext();
   }

   [TestCase("encrypted")]
   [TestCase(".hidden_encrypted")]
   [TestCase("regular_dir/encrypted")]
   [TestCase("regular_dir/.hidden_encrypted")]
   [TestCase(".hidden_dir/encrypted")]
   [TestCase(".hidden_dir/.hidden_encrypted")]
   [Category("Integration")]
   public async Task open_file(
      string file)
   {
      const string text = "test";

      var logger = Mock.Of<ILogger>();

      var fs = Shared.FileLayout2(Shared.GetMockFs());

      var state = new State(logger);

      var repository = Shared.CreateRepository(fs, logger);

      var channel = Channel.CreateUnbounded<string>();
      var console = new TestConsole(channel.Reader);
      var reader = new Reader(console);
      var view = new View(console, reader);

      var fileFactory =
         new FileFactory(
            logger,
            Mock.Of<IEnvironmentVariables>(),
            Mock.Of<IRunner>(),
            Mock.Of<IClipboard>(),
            fs,
            state,
            view);

      var session =
         Shared.CreateSessionContext(
            logger,
            repository,
            view: view,
            state: state,
            fileFactory: fileFactory);

      logger.Info($"{nameof(Session_Tests)}.{nameof(open_file)}: processing input");
      await session.ProcessAsync($".open {file}");

      Assert.That(console.GetScreen(), Is.EqualTo(text + "\n"));
   }
   
   [Test]
   public async Task prints_help()
   {
      var view = new Mock<IView>();
      var session = Shared.CreateSessionContext(view: view.Object);
      await session.ProcessAsync(".help");
      view.Verify(m => m.WriteLine(It.IsRegex(@"\.help")), Times.Once);
   }
}