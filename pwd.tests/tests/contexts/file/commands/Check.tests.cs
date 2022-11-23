using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Check_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".ch", false)]
   [TestCase("check", false)]
   [TestCase(".check", true)]
   [TestCase(".check ", true)]
   [TestCase(".check test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Check(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_archive()
   {
      var mockView = new Mock<IView>();

      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(""));
      
      var factory =
         new Check(
            mockView.Object,
            mockItem.Object);

      var command = factory.Parse(".check");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();
   }
}