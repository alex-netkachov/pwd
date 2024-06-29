using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.tests.contexts.file.commands;

public class Delete_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".r", false)]
   [TestCase("rm", false)]
   [TestCase(".rm", true)]
   [TestCase(".rm ", true)]
   [TestCase(".rm test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var repository = Shared.CreateRepository();

      using var factory =
         new Delete(
            Mock.Of<IState>(),
            Mock.Of<IView>(),
            repository,
            repository.Root);

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_delete()
   {
      var fs = Shared.GetMockFs();

      var mockState = new Mock<IState>();

      var mockView = new Mock<IView>();
      mockView
         .Setup(m =>
            m.ConfirmAsync(
               It.IsAny<string>(),
               It.IsAny<Answer>(),
               It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(true));

      var repository = Shared.CreateRepository();

      using var factory =
         new Delete(
            mockState.Object,
            mockView.Object,
            repository,
            repository.Root);

      var command = factory.Create(".rm");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

      //mockRepository
      //   .Verify(m => m.Delete("test"));
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
      var repository = Shared.CreateRepository();

      using var factory =
         new Delete(
            Mock.Of<IState>(),
            Mock.Of<IView>(),
            repository,
            repository.Root);

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}