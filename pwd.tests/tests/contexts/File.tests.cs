using System.IO.Abstractions;
using Moq;
using File = pwd.contexts.File;

namespace pwd.tests.contexts;

public class File_Tests
{
    private static File CreateFileContext(
        string path = "",
        string name = "",
        string content = "",
        IFileSystem? fs = null,
        IRepository? repository = null,
        IClipboard? clipboard = null,
        IView? view = null,
        IState? state = null)
    {
        path = string.IsNullOrEmpty(path) ? Path.GetFileName(Path.GetTempFileName()) : path;
        name = string.IsNullOrEmpty(path) ? Path.GetFileName(path) : name;

        return new File(
            clipboard ?? Mock.Of<IClipboard>(),
            fs ?? Mock.Of<IFileSystem>(),
            repository ?? Mock.Of<IRepository>(),
            state ?? Mock.Of<IState>(),
            view ?? Mock.Of<IView>(),
            name,
            content);
    }

    [Test]
    public async Task saving_writes_a_file()
    {
        var repository = new Mock<IRepository>();
        var file = CreateFileContext(repository: repository.Object);
        await file.Process(".save");
        repository.Verify(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    
    [Test]
    public async Task closing_changes_the_context()
    {
        var state = new Mock<IState>();
        var file = CreateFileContext(state: state.Object);
        await file.Process("..");
        state.Verify(m => m.Back(), Times.Once);
    }
    
    [Test]
    public void printing_outputs_the_content()
    {
        var view = new Mock<IView>();
        var file = CreateFileContext(view: view.Object, content: "test");
        file.Print();
        view.Verify(m => m.WriteLine("test"), Times.Once);
    }
    
    [Test]
    public void printing_hides_passwords()
    {
        var view = new Mock<IView>();
        var file = CreateFileContext(view: view.Object, content: "password: secret");
        file.Print();
        view.Verify(m => m.WriteLine("password: ************"), Times.Once);
    }

    [Test]
    [Category("Integration")]
    public async Task renaming_moves_the_file()
    {
        var fs = Shared.GetMockFs();
        await fs.File.WriteAllBytesAsync("test1", Array.Empty<byte>());
        var repository = new Repository(fs, new ZeroCipher(), new ZeroCipher(), ".");
        await repository.Initialise();
        var file = CreateFileContext(repository: repository, name: "test1");
        await file.Process(".rename test2");
        Assert.That(fs.File.Exists("test2"));
        Assert.That(!fs.File.Exists("test1"));
    }
}