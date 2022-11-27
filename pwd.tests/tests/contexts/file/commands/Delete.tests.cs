using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

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
      var factory =
         new Delete(
            Mock.Of<IState>(),
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_delete()
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

      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .SetupGet(m => m.Name)
         .Returns("test");

      var mockRepository = new Mock<IRepository>();
      
      var factory =
         new Delete(
            mockState.Object,
            mockView.Object,
            mockRepository.Object,
            mockItem.Object);

      var command = factory.Parse(".rm");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();
      
      mockRepository
         .Verify(m => m.Delete("test"));
   }
}