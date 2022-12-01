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
      using var factory =
         new Print(
            Mock.Of<IView>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Create(input);

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
      
      using var factory =
         new Print(
            mockView.Object,
            mockItem.Object);

      var command = factory.Create(".print");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockItem.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
      mockView.Verify(m => m.WriteLine(output), Times.Once);
   }

   [TestCase("", ".print")]
   [TestCase(".", ".print")]
   [TestCase(".PR", ".print")]
   [TestCase(".Pr", ".print")]
   [TestCase(".print", "")]
   [TestCase(".print ", "")]
   [TestCase(".print test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Print(
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