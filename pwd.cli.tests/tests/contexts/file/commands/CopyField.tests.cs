using System;
using System.Threading.Tasks;
using Moq;
using pwd.cli;
using pwd.console.abstractions;
using pwd.cli.contexts.file.commands;
using pwd.core.abstractions;

namespace pwd.tests.contexts.file.commands;

public class CopyField_Tests
{
   [TestCase("ccp", "", "secret", "user: joe\npassword: secret")]
   [TestCase("ccp", "", "top secret", "user: joe\npassword: \"top secret\"")]
   [TestCase("ccu", "", "joe", "user: joe\npassword: secret")]
   [TestCase("cc", "user", "joe","user: joe\npassword: secret")]
   [TestCase("cc", "password", "secret", "user: joe\npassword: secret")]
   [TestCase("cc", "site", "https://example.com", "site: https://example.com\nuser: joe")]
   public async Task Execute_copies_the_expected_content(
      string commandName,
      string commandArgs,
      string expected,
      string content)
   {
      var mockClipboard = new Mock<IClipboard>();

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadTextAsync("/test"))
         .Returns(Task.FromResult(content));

      var command =
         new CopyField(
            mockClipboard.Object,
            repository.Object,
            "/test");

      await command.ExecuteAsync(
         Mock.Of<IView>(),
         commandName,
         commandArgs == "" ? [] : commandArgs.Split(" "));
      
      mockClipboard
         .Verify(m => m.Put(expected, It.IsAny<TimeSpan>()));
   }
}