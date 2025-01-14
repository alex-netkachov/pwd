using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.console.abstractions;
using pwd.core.abstractions;
using pwd.mocks;
using pwd.ui.abstractions;

namespace pwd.tests.contexts.file;

public class File_Tests
{
   [Test]
   public async Task empty_input_prints_the_content_with_obscured_passwords()
   {
      var firstView = new TestView([]);

      var repository = new Mock<IRepository>();
      repository
         .Setup(m => m.ReadTextAsync(It.IsAny<string>()))
         .Returns(Task.FromResult("password: secret"));

      var file =
         Shared.CreateFileContext(
            view: firstView,
            repository: repository.Object);

      var view = new TestView([]);

      await file.ProcessAsync(view, "");

      Assert.That(
         view.GetOutput(),
         Is.EqualTo("password: ************\n"));
   }
   
   [Test]
   public async Task close_changes_the_context()
   {
      var state = new Mock<IState>();
      var file = Shared.CreateFileContext(state: state.Object);
      await file.ProcessAsync(Mock.Of<IView>(), "..");
      state.Verify(m => m.BackAsync(), Times.Once);
   }

   [Test]
   public async Task help_prints_the_content_of_the_help_file()
   {
      var firstView = new TestView([]);
      var file = Shared.CreateFileContext(view: firstView, content: "password: secret");

      var view = new TestView([]);
      await file.ProcessAsync(view, ".help");
      Assert.That(
         view
            .GetOutput()
            .StartsWith("Commands:", StringComparison.Ordinal),
         Is.True);
   }
}