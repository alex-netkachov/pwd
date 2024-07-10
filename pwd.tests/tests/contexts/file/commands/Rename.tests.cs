using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
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
      using var factory =
         new Rename(
            Mock.Of<ILogger<Rename>>(),
            Mock.Of<IRepository>(),
            "/");

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task Execute_calls_repository_move()
   {
      var repository = new Mock<IRepository>();
      
      using var factory =
         new Rename(
            Mock.Of<ILogger<Rename>>(),
            repository.Object,
            "/test");

      var command = factory.Create(".rename ok");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      repository.Verify(
         m => m.Move("/test", "ok"),
         Times.Once);
   }
}