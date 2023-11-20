﻿using System.IO.Abstractions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
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
      using var factory =
         new Rename(
            Mock.Of<IRepository>(),
            Mock.Of<INamedItem>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task DoAsync_calls_repository_delete()
   {
      var mockItem = new Mock<INamedItem>();
      mockItem
         .SetupGet(m => m.Name)
         .Returns(Name.Parse(Mock.Of<IFileSystem>(), "test"));

      var mockRepository = new Mock<IRepository>();
      
      using var factory =
         new Rename(
            mockRepository.Object,
            mockItem.Object);

      var command = factory.Create(".rename ok");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
      
      mockRepository
         .Verify(m => m.Move(It.IsAny<pwd.repository.IFile>(), It.IsAny<Path>()));
   }
}