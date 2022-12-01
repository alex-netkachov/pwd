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
      using var factory =
         new Unobscured(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Create(input);

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
      
      using var factory =
         new Unobscured(
            mockView.Object,
            mockItem.Object);

      var command = factory.Create(".unobscured");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockItem.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
      mockView.Verify(m => m.WriteLine(content), Times.Once);
   }

   [TestCase("", ".unobscured")]
   [TestCase(".", ".unobscured")]
   [TestCase(".UNO", ".unobscured")]
   [TestCase(".Uno", ".unobscured")]
   [TestCase(".unobscured", "")]
   [TestCase(".unobscured ", "")]
   [TestCase(".unobscured test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Unobscured(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? Array.Empty<string>()
               : suggestions.Split(';')));
   }
}