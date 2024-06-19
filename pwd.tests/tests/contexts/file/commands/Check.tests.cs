using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.repository;
using pwd.repository.interfaces;
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
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      using var factory =
         new Check(
            Mock.Of<IView>(),
            Mock.Of<IFile>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_item_archive()
   {
      var mockView = new Mock<IView>();

      var mockItem = new Mock<IFile>();
      mockItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(""));
      
      using var factory =
         new Check(
            mockView.Object,
            mockItem.Object);

      var command = factory.Create(".check");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
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
            Mock.Of<IFile>());

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? Array.Empty<string>()
               : suggestions.Split(';')));
   }
}