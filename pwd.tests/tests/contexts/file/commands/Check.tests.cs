using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.tests.contexts.file.commands;

public class Check_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".ch", false)]
   [TestCase("check", false)]
   [TestCase(".check", true)]
   [TestCase(".check ", true)]
   [TestCase(".check test", true)]
   public void Create_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new Check(
            Mock.Of<IView>(),
            Mock.Of<IRepository>(),
            "/");

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [TestCase("content", 0)]
   [TestCase("\"test", 1)]
   public async Task Execute_checks_the_content(
      string fileContent,
      int errors)
   {
      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadAsync("/test"))
         .Returns(Task.FromResult(fileContent));

      var mockView = new Mock<IView>();

      using var factory =
         new Check(
            mockView.Object,
            repository.Object,
            "/test");

      var command = factory.Create(".check");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

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
      using var factory =
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