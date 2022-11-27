using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Help_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".h", false)]
   [TestCase("help", false)]
   [TestCase(".help", true)]
   [TestCase(".help ", true)]
   [TestCase(".help test", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Help(
            Mock.Of<IView>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_writes_to_view()
   {
      var mockView = new Mock<IView>();

      var factory =
         new Help(
            mockView.Object);

      var command = factory.Parse(".help");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();

      mockView.Verify(m => m.WriteLine(It.IsAny<string>()), Times.Once);
   }
}