using System.Threading.Tasks;
using Moq;
using pwd.console.abstractions;
using pwd.cli.contexts.file.commands;
using pwd.cli.ui.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Up_Tests
{
   [Test]
   public async Task DoAsync_calls_back()
   {
      var mockState = new Mock<IState>();

      var command =
         new Up(
            mockState.Object);

      await command.ExecuteAsync(
         Mock.Of<IView>(),
         "..");

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
      var command =
         new Up(
            Mock.Of<IState>());

      Assert.That(
         command.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}