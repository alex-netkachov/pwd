using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.tests.contexts.file.commands;

public class Print_Tests
{
   [Test]
   public async Task Execute_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";
      const string output = "password: ************";

      var mockView = new Mock<IView>();

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadAsync("/test"))
         .Returns(Task.FromResult(content));

      var command =
         new Print(
            mockView.Object,
            repository.Object,
            "/test");

      await command.ExecuteAsync("print", []);
      
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
      var command =
         new Print(
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            "/");

      Assert.That(
         command.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}