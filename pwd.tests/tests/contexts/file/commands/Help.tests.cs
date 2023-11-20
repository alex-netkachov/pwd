using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
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
      using var factory =
         new Help(
            Mock.Of<IView>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_writes_to_view()
   {
      var mockView = new Mock<IView>();

      using var factory =
         new Help(
            mockView.Object);

      var command = factory.Create(".help");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

      mockView.Verify(m => m.WriteLine(It.IsAny<string>()), Times.Once);
   }

   [TestCase("", ".help")]
   [TestCase(".", ".help")]
   [TestCase(".HE", ".help")]
   [TestCase(".He", ".help")]
   [TestCase(".help", "")]
   [TestCase(".help ", "")]
   [TestCase(".help test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      using var factory =
         new Help(
            Mock.Of<IView>());

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}