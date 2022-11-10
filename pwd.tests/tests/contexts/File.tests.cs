using Moq;
using pwd.mocks;

namespace pwd.tests.contexts;

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

   [Test]
   [Category("Integration")]
   public async Task renaming_moves_the_file()
   {
      var fs = Shared.GetMockFs();
      await fs.File.WriteAllBytesAsync("test1", Array.Empty<byte>());
      var repository = new Repository(fs, new ZeroCipher(), new ZeroCipher(), ".");
      await repository.Initialise();
      var file = Shared.CreateFileContext(repository: repository, name: "test1");
      await file.ProcessAsync(".rename test2");
      Assert.That(fs.File.Exists("test2"));
      Assert.That(!fs.File.Exists("test1"));
   }
}