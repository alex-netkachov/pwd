using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.mocks;
using pwd.repository.implementation;

namespace pwd.tests.contexts.file;

public class File_Tests
{
   [Test]
   public async Task empty_input_prints_the_content_with_obscured_passwords()
   {
      var view = new Mock<IView>();
      var file = Shared.CreateFileContext(view: view.Object, content: "password: secret");
      await file.ProcessAsync("");
      view.Verify(m => m.WriteLine("password: ************"), Times.Once);
   }
   
   [Test]
   public async Task close_changes_the_context()
   {
      var state = new Mock<IState>();
      var file = Shared.CreateFileContext(state: state.Object);
      await file.ProcessAsync("..");
      state.Verify(m => m.BackAsync(), Times.Once);
   }

   [Test]
   public async Task help_prints_the_content_of_the_help_file()
   {
      var view = new Mock<IView>();
      var file = Shared.CreateFileContext(view: view.Object, content: "password: secret");
      await file.ProcessAsync(".help");
      view.Verify(m => m.WriteLine(It.IsRegex(@"\.help")), Times.Once);
   }
}