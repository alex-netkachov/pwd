﻿using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Rename_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".re", false)]
   [TestCase("rename", false)]
   [TestCase(".rename", false)]
   [TestCase(".rename ", false)]
   [TestCase(".rename ok", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Rename(
            Mock.Of<IRepository>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Parse(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_delete()
   {
      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .SetupGet(m => m.Name)
         .Returns("test");

      var mockRepository = new Mock<IRepository>();
      
      var factory =
         new Rename(
            mockRepository.Object,
            mockItem.Object);

      var command = factory.Parse(".rename ok");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.DoAsync();
      
      mockRepository
         .Verify(m => m.Rename("test", "ok"));
   }
}