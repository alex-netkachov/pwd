using System.IO.Abstractions;
using Moq;
using File = pwd.contexts.File;

namespace pwd.tests.contexts;

public class File_Tests
{
    private static (
        File File,
        IContext Context,
        IFileSystem FileSystem,
        IRepository Repository,
        IClipboard Clipboard,
        IView View,
        string Name)
        CreateFileContext(
            string path = "",
            string name = "",
            string content = "",
            IContext? context = null,
            IFileSystem? fs = null,
            IRepository? repository = null,
            IClipboard? clipboard = null,
            IView? view = null)
    {
        path =
            string.IsNullOrEmpty(path)
                ? Path.GetFileName(Path.GetTempFileName())
                : path;

        name =
            string.IsNullOrEmpty(path)
                ? Path.GetFileName(path)
                : name;

        context ??= Mock.Of<IContext>();
        fs ??= Mock.Of<IFileSystem>();
        repository ??= Mock.Of<IRepository>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new File(fs, repository, clipboard, view, name, content),
            context,
            fs,
            repository,
            clipboard,
            view,
            name);
    }

    [Test]
    public async Task saving_writes_a_file()
    {
        var repository = new Mock<IRepository>();
        var sut = CreateFileContext(repository: repository.Object);
        await sut.File.Process(Mock.Of<IState>(), ".save");
        repository.Verify(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    
    [Test]
    public async Task closing_changes_the_context()
    {
        var sut = CreateFileContext();
        var state = new Mock<IState>();
        await sut.File.Process(state.Object, "..");
        state.Verify(m => m.Up(), Times.Once);
    }
    
    [Test]
    public void printing_outputs_the_content()
    {
        var view = new Mock<IView>();
        var sut = CreateFileContext(view: view.Object, content: "test");
        sut.File.Print();
        view.Verify(m => m.WriteLine("test"), Times.Once);
    }
    
    [Test]
    public void printing_hides_passwords()
    {
        var view = new Mock<IView>();
        var sut = CreateFileContext(view: view.Object, content: "password: secret");
        sut.File.Print();
        view.Verify(m => m.WriteLine("password: ************"), Times.Once);
    }

    [Test]
    public async Task renaming_moves_the_file()
    {
        var fs = Shared.GetMockFs();
        await fs.File.WriteAllBytesAsync("test1", Array.Empty<byte>());
        var repository = new Repository(fs, new ZeroCipher(), new ZeroCipher(), ".");
        await repository.Initialise();
        var sut = CreateFileContext(repository: repository, name: "test1");
        await sut.File.Process(Mock.Of<IState>(), ".rename test2");
        Assert.That(fs.File.Exists("test2"));
        Assert.That(!fs.File.Exists(sut.Name));
    }
}