using Moq;
using pwd.ciphers;
using pwd.contexts;

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
      var (pwd, text, _) = Shared.ContentEncryptionTestData();

      var cipher = new ContentCipher(pwd);
      var view = new BufferedView();
      var fs = Shared.FileLayout1(Shared.GetMockFs());
      var state = new State();
      var repository = new Repository(fs, new ZeroCipher(), cipher, ".");
      await repository.Initialise();

      var fileFactory =
         new FileFactory(
            Mock.Of<ILogger>(),
            Mock.Of<IClipboard>(),
            fs,
            state,
            view);

      var session =
         Shared.CreateSessionContext(
            repository,
            view: view,
            state: state,
            fileFactory: fileFactory);

      await session.ProcessAsync($".open {file}");
      Assert.That(view.ToString().Trim(), Is.EqualTo(text));
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