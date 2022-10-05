using Moq;

namespace pwd.tests.contexts;

public class File_Tests
{
   [Test]
   public async Task saving_writes_a_file()
   {
      var repository = new Mock<IRepository>();
      var file = Shared.CreateFileContext(repository: repository.Object);
      await file.ProcessAsync(".save");
      repository.Verify(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
   }

   [Test]
   public async Task closing_changes_the_context()
   {
      var state = new Mock<IState>();
      var file = Shared.CreateFileContext(state: state.Object);
      await file.ProcessAsync("..");
      state.Verify(m => m.Back(), Times.Once);
   }

   [Test]
   public async Task printing_outputs_the_content()
   {
      var view = new Mock<IView>();
      var file = (pwd.contexts.File) Shared.CreateFileContext(view: view.Object, content: "test");
      await file.ProcessAsync("");
      view.Verify(m => m.WriteLine("test"), Times.Once);
   }

   [Test]
   public async Task printing_hides_passwords()
   {
      var view = new Mock<IView>();
      var file = (pwd.contexts.File) Shared.CreateFileContext(view: view.Object, content: "password: secret");
      await file.ProcessAsync("");
      view.Verify(m => m.WriteLine("password: ************"), Times.Once);
   }
   
   [Test]
   public async Task prints_help()
   {
      var view = new Mock<IView>();
      var file = (pwd.contexts.File) Shared.CreateFileContext(view: view.Object, content: "password: secret");
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