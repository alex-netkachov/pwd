using System.Threading;
using System.Threading.Tasks;
using Moq;
using pwd.console.abstractions;
using pwd.cli.contexts.file.commands;
using pwd.core.abstractions;
using pwd.cli.ui.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Delete_Tests
{
   [Test]
   public async Task Execute_calls_repository_delete()
   {
      var mockState = new Mock<IState>();

      var mockView = new Mock<IView>();
      mockView
         .Setup(m =>
            m.ConfirmAsync(
               It.IsAny<string>(),
               It.IsAny<Answer>(),
               It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(true));

      var repository = new Mock<IRepository>();

      var command =
         new Delete(
            mockState.Object,
            repository.Object,
            "/test");

      await command.ExecuteAsync(
         mockView.Object,
         "rm",
         []);
      
      repository.Verify(m => m.Delete("/test"), Times.Once);
   }

   [TestCase("", ".rm")]
   [TestCase(".", ".rm")]
   [TestCase(".R", ".rm")]
   [TestCase(".r", ".rm")]
   [TestCase(".rm", "")]
   [TestCase(".rm ", "")]
   [TestCase(".rm test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      var repository = new Mock<IRepository>();

      var factory =
         new Delete(
            Mock.Of<IState>(),
            repository.Object,
            "/test");

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}