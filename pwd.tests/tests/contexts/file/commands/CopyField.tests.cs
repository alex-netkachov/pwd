using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;

namespace pwd.tests.contexts.file.commands;

public class CopyField_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".c", false)]
   [TestCase("cc", false)]
   [TestCase(".cc", false)]
   [TestCase(".cc ", false)]
   [TestCase(".cc test", true)]
   [TestCase(".ccu", true)]
   [TestCase(".ccu ", true)]
   [TestCase(".ccu test", true)]
   [TestCase(".ccp", true)]
   [TestCase(".ccp ", true)]
   [TestCase(".ccp test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new CopyField(
            Mock.Of<IClipboard>(),
            Mock.Of<IRepository>(),
            "/");

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [TestCase(".ccp", "secret", "user: joe\npassword: secret")]
   [TestCase(".ccp", "top secret", "user: joe\npassword: \"top secret\"")]
   [TestCase(".ccu", "joe", "user: joe\npassword: secret")]
   [TestCase(".cc user", "joe","user: joe\npassword: secret")]
   [TestCase(".cc password", "secret", "user: joe\npassword: secret")]
   [TestCase(".cc site", "https://example.com", "site: https://example.com\nuser: joe")]
   public async Task Execute_copies_the_expected_content(
      string input,
      string expected,
      string content)
   {
      var mockClipboard = new Mock<IClipboard>();

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadAsync("/test"))
         .Returns(Task.FromResult(content));

      using var factory =
         new CopyField(
            mockClipboard.Object,
            repository.Object,
            "/test");

      var command = factory.Create(input);
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockClipboard
         .Verify(m => m.Put(expected, It.IsAny<TimeSpan>()));
   }
}