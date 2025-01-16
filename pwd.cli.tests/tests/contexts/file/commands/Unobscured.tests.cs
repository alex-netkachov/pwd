using System.Threading.Tasks;
using Moq;
using pwd.console.abstractions;
using pwd.cli.contexts.file.commands;
using pwd.core.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Unobscured_Tests
{
   [Test]
   public async Task Execute_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadTextAsync("/test"))
         .Returns(Task.FromResult(content));

      var mockView = new Mock<IView>();

      var command =
         new Unobscured(
            repository.Object,
            "/test");

      await command.ExecuteAsync(
         mockView.Object,
         "unobscured");
      
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
      var command =
         new Unobscured(
            Mock.Of<IRepository>(),
            "/test");

      Assert.That(
         command.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}