using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;

namespace pwd.tests.contexts.file.commands;

public class Up_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase("...", false)]
   [TestCase("..", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new Up(
            Mock.Of<IState>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_back()
   {
      var mockState = new Mock<IState>();

      using var factory =
         new Up(
            mockState.Object);

      var command = factory.Create("..");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

      mockState.Verify(m => m.BackAsync(), Times.Once);
   }

   [TestCase("", "..")]
   [TestCase(".", "..")]
   [TestCase("..", "")]
   [TestCase(".. ", "")]
   [TestCase("...", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Up(
            Mock.Of<IState>());

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}