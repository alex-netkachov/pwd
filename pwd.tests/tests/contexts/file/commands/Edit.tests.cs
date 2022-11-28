using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using pwd.contexts.file.commands;
using pwd.repository;

namespace pwd.tests.contexts.file.commands;

public class Edit_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".ed", false)]
   [TestCase("edit", false)]
   [TestCase(".edit", true)]
   [TestCase(".edit ", true)]
   [TestCase(".edit notepad", true)]
   public void Parse_creates_command(
      string input,
      bool creates)
   {
      var factory =
         new Edit(
            Mock.Of<IEnvironmentVariables>(),
            Mock.Of<IRunner>(),
            Mock.Of<IView>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IRepositoryItem>());

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [TestCase("", "", ".edit", "", "EDITOR is not set")]
   [TestCase("notepad", "", ".edit notepad", "", "")]
   public async Task DoAsync_edits_and_updates_the_content_of_the_item(
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

      var mockItem = new Mock<IRepositoryItem>();
      mockItem
         .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.FromResult(content));
      mockItem
         .Setup(m => m.WriteAsync(It.IsAny<string>()))
         .Callback<string>(value => content = value)
         .Returns(Task.CompletedTask);

      var factory =
         new Edit(
            mockEnvironmentVariables.Object,
            mockRunner.Object,
            mockView.Object,
            mockFileSystem,
            mockItem.Object);

      var command = factory.Create(input);
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();

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