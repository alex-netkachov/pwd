using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Print_Tests
{
   [TestCase("", true)]
   [TestCase(".", false)]
   [TestCase(".p", false)]
   [TestCase("print", false)]
   [TestCase(".print", true)]
   [TestCase(".print ", true)]
   [TestCase(".print test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Print(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";
      const string output = "password: ************";

      var mockView = new Mock<IView>();

      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(content));
      
      var factory =
         new Print(
            mockView.Object,
            mockItem.Object);

      var command = factory.Parse(".print");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();
      
      mockItem.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
      mockView.Verify(m => m.WriteLine(output), Times.Once);
   }
}