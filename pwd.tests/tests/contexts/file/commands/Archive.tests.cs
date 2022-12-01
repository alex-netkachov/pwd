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
      using var factory =
         new Archive(
            Mock.Of<IState>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_archive()
   {
      var mockItem = new Mock<IRepositoryItem>();
      var mockState = new Mock<IState>();
      
      using var factory =
         new Archive(
            mockState.Object,
            mockItem.Object);

      var command = factory.Create(".archive");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

      mockItem.Verify(m => m.Archive(), Times.Once);
      mockState.Verify(m => m.BackAsync(), Times.Once);
   }

   [TestCase("", ".archive")]
   [TestCase(".", ".archive")]
   [TestCase(".ARCH", ".archive")]
   [TestCase(".Arch", ".archive")]
   [TestCase(".arch", ".archive")]
   [TestCase(".archive", "")]
   [TestCase(".archive ", "")]
   [TestCase(".archive test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Archive(
            Mock.Of<IState>(),
            Mock.Of<IRepositoryItem>());

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? Array.Empty<string>()
               : suggestions.Split(';')));
   }
}