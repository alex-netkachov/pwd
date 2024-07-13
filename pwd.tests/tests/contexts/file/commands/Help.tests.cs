using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Help_Tests
{
   [Test]
   public async Task Execute_writes_to_view()
   {
      var mockView = new Mock<IView>();

      var command =
         new Help(
            mockView.Object);

      await command.ExecuteAsync("help", []);

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
      var factory =
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