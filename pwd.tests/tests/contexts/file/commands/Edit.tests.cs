using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.mocks;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.tests.contexts.file.commands;

public class Edit_Tests
{
   [TestCase("", "", ".edit", "", "EDITOR is not set")]
   [TestCase("notepad", "", ".edit notepad", "", "")]
   public async Task Execute_edits_and_updates_the_content_of_the_item(
      string editor,
      string content,
      string input,
      string updated,
      string outcome)
   {
      var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();
      mockEnvironmentVariables
         .Setup(m => m.Get("EDITOR"))
         .Returns(editor);

      var mockFileSystem = new MockFileSystem();

      var mockRunner = new Mock<IRunner>();
      mockRunner
         .Setup(
            m =>
               m.RunAsync(
                  It.IsAny<string>(),
                  It.IsAny<string?>(),
                  It.IsAny<string?>(),
                  It.IsAny<CancellationToken>()))
         .Callback(() =>
         {
            var tempFile = mockFileSystem.AllFiles.First();
            mockFileSystem.File.WriteAllText(tempFile, updated);
         })
         .Returns(Task.FromResult<Exception?>(null));

      var mockView = new Mock<IView>();

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadAsync("/test"))
         .Returns(Task.FromResult("content"));
      
      var command =
         new Edit(
            mockEnvironmentVariables.Object,
            mockRunner.Object,
            mockView.Object,
            mockFileSystem,
            repository.Object,
            "/test");

      await command.ExecuteAsync(
         "edit",
         editor == "" ? [] : [editor]);

      switch (outcome)
      {
         case "EDITOR is not set":
            mockView
               .Verify(m => m.WriteLine("The editor is not specified and the environment variable EDITOR is not set."));
            break;
         case "":
            break;
         default:
            Assert.Fail("The unsupported outcome");
            break;
      }
   }
}