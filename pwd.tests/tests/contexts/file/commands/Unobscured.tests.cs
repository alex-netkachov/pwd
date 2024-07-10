using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.tests.contexts.file.commands;

public class Unobscured_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".un", false)]
   [TestCase("unobscured", false)]
   [TestCase(".unobscured", true)]
   [TestCase(".unobscured ", true)]
   [TestCase(".unobscured test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new Unobscured(
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            "/");

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task Execute_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadAsync("/test"))
         .Returns(Task.FromResult(content));

      var mockView = new Mock<IView>();

      using var factory =
         new Unobscured(
            mockView.Object,
            repository.Object,
            "/test");

      var command = factory.Create(".unobscured");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockView.Verify(m => m.WriteLine(content), Times.Once);
   }

   [TestCase("", ".unobscured")]
   [TestCase(".", ".unobscured")]
   [TestCase(".UNO", ".unobscured")]
   [TestCase(".Uno", ".unobscured")]
   [TestCase(".unobscured", "")]
   [TestCase(".unobscured ", "")]
   [TestCase(".unobscured test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Unobscured(
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            "/test");

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}