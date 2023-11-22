using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Rename_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".re", false)]
   [TestCase("rename", false)]
   [TestCase(".rename", false)]
   [TestCase(".rename ", false)]
   [TestCase(".rename ok", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new Rename(
            Mock.Of<ILogger>(),
            Mock.Of<IRepository>(),
            Mock.Of<IFile>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_move()
   {
      var fs = Shared.GetMockFs();

      var mockFile = new Mock<IFile>();
      mockFile
         .SetupGet(m => m.Name)
         .Returns(Name.Parse(fs, "test"));

      var mockRepository = new Mock<IRepository>();
      
      using var factory =
         new Rename(
            new ConsoleLogger(),
            mockRepository.Object,
            mockFile.Object);

      var command = factory.Create(".rename ok");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockRepository
         .Verify(m => m.Move(It.IsAny<IFile>(), It.IsAny<Path>()));
   }

   [Test]
   [Category("Integration")]
   public async Task Rename_moves_the_file()
   {
      var logger = new NullLogger();

      var fs = Shared.GetMockFs("*test1");

      var repository = Shared.CreateRepository(fs, logger: logger);

      var context =
         Shared.CreateFileContext(
            repository: repository,
            name: "test1",
            logger: logger,
            fs: fs);

      await context.ProcessAsync(".rename test2");

      var file = repository.Root.List().Single();
      Assert.That(file.Name.Value, Is.EqualTo("test2"));
   }
}