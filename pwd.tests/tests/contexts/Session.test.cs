using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pwd.console.abstractions;
using pwd.contexts.file;
using pwd.core;
using pwd.core.abstractions;
using pwd.library.interfaced;
using pwd.mocks;
using pwd.ui;

namespace pwd.tests.contexts;

public sealed class Session_Tests
{
   [Test]
   public void construct_session()
   {
      Shared.CreateSessionContext();
   }

   [Test]
   public async Task prints_help()
   {
      var view = new Mock<IView>();
      var session = Shared.CreateSessionContext(view: view.Object);
      await session.ProcessAsync(view.Object, ".help");
      view.Verify(m => m.WriteLine(It.IsRegex(@"\.help")), Times.Once);
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
      
      var state =
         new State(
            Mock.Of<ILogger<State>>(),
            Mock.Of<IPresenter>());

      var fs = Shared.GetMockFs();
      CreateFoldersAndFiles(fs);
      var repository = Shared.CreateRepository(fs);

      using var view = new TestView();

      var fileFactory =
         new FileFactory(
            Mock.Of<ILoggerFactory>(),
            Mock.Of<IEnvironmentVariables>(),
            Mock.Of<IRunner>(),
            Mock.Of<IClipboard>(),
            fs,
            state,
            () => view);

      var session =
         Shared.CreateSessionContext(
            Mock.Of<ILogger>(),
            repository,
            state: state,
            view: view,
            fileFactory: fileFactory);

      //logger.Info($"{nameof(Session_Tests)}.{nameof(open_file)}: processing input");
      await session.ProcessAsync(view, $".open {file}");
      
      var name = fs.Path.GetFileName(file);

      Assert.That(
         view.GetOutput(),
         Is.EqualTo($"{text}\n{name}> "));
   }
   
   private static void CreateFoldersAndFiles(
      IFileSystem fs)
   {
      var encryptedContent = EncryptFileContent("test");

      var encrypted = EncryptFileName("encrypted");
      var hiddenEncrypted = EncryptFileName(".hidden_encrypted");
      var regularDir = EncryptFileName("regular_dir");
      var hiddenDir = EncryptFileName(".hidden_dir");

      fs.File.WriteAllBytes(encrypted, encryptedContent);
      fs.File.WriteAllBytes(hiddenEncrypted, encryptedContent);
      fs.Directory.CreateDirectory(regularDir);
      fs.File.WriteAllBytes($"{regularDir}/{encrypted}", encryptedContent);
      fs.File.WriteAllBytes($"{regularDir}/{hiddenEncrypted}", encryptedContent);
      fs.Directory.CreateDirectory(hiddenDir);
      fs.File.WriteAllBytes($"{hiddenDir}/{encrypted}", encryptedContent);
      fs.File.WriteAllBytes($"{hiddenDir}/{hiddenEncrypted}", encryptedContent);
   }

   private static string EncryptFileName(
      string input)
   {
      var cipher = Shared.GetTestCipher();
      var encrypted = cipher.Encrypt(input);
      var encoded = Base64Url.Instance.Encode(encrypted);
      return encoded;
   }
   
   private static byte[] EncryptFileContent(
      string input)
   {
      var cipher = Shared.GetTestCipher();
      var encrypted = cipher.Encrypt(input);
      return encrypted;
   }

}