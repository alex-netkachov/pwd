using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

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
      var factory =
         new Unobscured(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";

      var mockView = new Mock<IView>();

      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(content));
      
      var factory =
         new Unobscured(
            mockView.Object,
            mockItem.Object);

      var command = factory.Parse(".unobscured");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();
      
      mockItem.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
      mockView.Verify(m => m.WriteLine(content), Times.Once);
   }
}