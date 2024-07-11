using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Rename_Tests
{
   [Test]
   public async Task Execute_calls_repository_move()
   {
      var repository = new Mock<IRepository>();
      
      var command =
         new Rename(
            Mock.Of<ILogger<Rename>>(),
            repository.Object,
            "/test");

      await command.ExecuteAsync("rename", ["ok"]);
      
      repository.Verify(
         m => m.Move("/test", "ok"),
         Times.Once);
   }
}