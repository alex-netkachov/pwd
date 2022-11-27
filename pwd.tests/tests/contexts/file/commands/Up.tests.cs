using Moq;
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
      var factory =
         new Up(
            Mock.Of<IState>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_back()
   {
      var mockState = new Mock<IState>();

      var factory =
         new Up(
            mockState.Object);

      var command = factory.Parse("..");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();

      mockState.Verify(m => m.BackAsync(), Times.Once);
   }
}