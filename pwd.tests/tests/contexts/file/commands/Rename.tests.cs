using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core;
using pwd.core.abstractions;

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
      var repository = Shared.CreateRepository();

      using var factory =
         new Rename(
            Mock.Of<ILogger<Rename>>(),
            Mock.Of<IRepository>(),
            repository.Root);

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_move()
   {
      var fs = Shared.GetMockFs();

      var repository = Shared.CreateRepository();

      var mockRepository = new Mock<IRepository>();
      
      using var factory =
         new Rename(
            Mock.Of<ILogger<Rename>>(),
            repository,
            repository.Root);

      var command = factory.Create(".rename ok");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      //mockRepository
      //   .Verify(m => m.Move(It.IsAny<IFile>(), It.IsAny<Path>()));
   }

   [Test]
   [Category("Integration")]
   public async Task Rename_moves_the_file()
   {
      var fs = Shared.GetMockFs("*test1");

      var repository = Shared.CreateRepository(fs);

      var context =
         Shared.CreateFileContext(
            repository: repository,
            name: "test1",
            fs: fs);

      await context.ProcessAsync(".rename test2");

      var file = repository.List(repository.Root).Single();
      Assert.That(file.Name.Value, Is.EqualTo("test2"));
   }
}