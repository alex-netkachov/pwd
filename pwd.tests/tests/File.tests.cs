using System.IO.Abstractions;
using Moq;
using File = pwd.contexts.File;

namespace pwd.tests;

public class File_Tests
{
    private static (
        File File,
        IContext Context,
        IFileSystem FileSystem,
        ICipher Cipher,
        IClipboard Clipboard,
        IView View,
        string Path)
        CreateFileContext(
            string path = "",
            string content = "",
            IContext? context = null,
            ICipher? cipher = null,
            IFileSystem? fs = null,
            IClipboard? clipboard = null,
            IView? view = null)
    {
        path =
            string.IsNullOrEmpty(path)
                ? Path.GetFileName(Path.GetTempFileName())
                : path;

        context ??= Mock.Of<IContext>();
        cipher ??= Mock.Of<ICipher>();
        fs ??= Mock.Of<IFileSystem>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new File(context, fs, cipher, clipboard, view, path, content),
            context,
            fs,
            cipher,
            clipboard,
            view,
            path);
    }

    [Test]
    public async Task saving_creates_a_file()
    {
        var fs = Shared.GetMockFs();
        var sut = CreateFileContext(fs: fs);
        await sut.File.Process(Mock.Of<IState>(), ".save");
        Assert.That(fs.File.Exists(sut.Path));
    }
    
    [Test]
    public async Task closing_changes_the_context()
    {
        var sut = CreateFileContext();
        var context = sut.Context;
        var state = new Mock<IState>();
        await sut.File.Process(state.Object, "..");
        state.VerifySet(m => m.Context = context, Times.Once);
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
        var sut = CreateFileContext(fs: fs, path: "test1");
        await sut.File.Process(Mock.Of<IState>(), ".rename test2");
        Assert.That(fs.File.Exists("test2"));
        Assert.That(!fs.File.Exists(sut.Path));
    }
}