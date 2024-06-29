using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core;
using pwd.core.abstractions;
using pwd.ui;

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
      var repository = Shared.CreateRepository();

      using var factory =
         new Print(
            Mock.Of<IView>(),
            repository,
            repository.Root);

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_read_and_writes_to_view()
   {
      const string content = "password: 123";
      const string output = "password: ************";

      var mockView = new Mock<IView>();

      var repository = Shared.CreateRepository();

      using var factory =
         new Print(
            mockView.Object,
            repository,
            repository.Root);

      var command = factory.Create(".print");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      //mockItem.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
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
      var repository = Shared.CreateRepository();

      using var factory =
         new Print(
            Mock.Of<IView>(),
            repository,
            repository.Root);

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? []
               : suggestions.Split(';')));
   }
}