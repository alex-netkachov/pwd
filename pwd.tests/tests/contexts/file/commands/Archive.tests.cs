using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Archive_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".arch", false)]
   [TestCase("archive", false)]
   [TestCase(".archive", true)]
   [TestCase(".archive ", true)]
   [TestCase(".archive test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Archive(
            Mock.Of<IState>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_archive()
   {
      var mockItem = new Mock<IRepositoryItem>();
      var mockState = new Mock<IState>();
      
      var factory =
         new Archive(
            mockState.Object,
            mockItem.Object);

      var command = factory.Parse(".archive");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();

      mockItem.Verify(m => m.Archive(), Times.Once);
      mockState.Verify(m => m.BackAsync(), Times.Once);
   }
}