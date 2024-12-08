using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Check_Tests
{
   [TestCase("content", 0)]
   [TestCase("\"test", 1)]
   public async Task Execute_checks_the_content(
      string fileContent,
      int errors)
   {
      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadTextAsync("/test"))
         .Returns(Task.FromResult(fileContent));

      var mockView = new Mock<IView>();

      var command =
         new Check(
            mockView.Object,
            repository.Object,
            "/test");

      await command.ExecuteAsync("check", []);

      mockView.Verify(
         m => m.WriteLine(It.IsAny<string>()),
         Times.Exactly(errors));
   }

   [TestCase("", ".check")]
   [TestCase(".", ".check")]
   [TestCase(".CH", ".check")]
   [TestCase(".Ch", ".check")]
   [TestCase(".ch", ".check")]
   [TestCase(".check", "")]
   [TestCase(".check ", "")]
   [TestCase(".check test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      var factory =
         new Check(
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            "/");

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}