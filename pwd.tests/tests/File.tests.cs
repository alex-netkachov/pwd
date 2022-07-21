using System.IO.Abstractions;
using Moq;
using pwd.contexts;
using File = pwd.contexts.File;

namespace pwd.tests;

public class File_Tests
{
    private static (File, IContext, IFileSystem, ICipher, IClipboard, IView, string) CreateFileWithMocks(
        string path = "",
        string content = "",
        IContext? context = null,
        ICipher? cipher = null,
        IFileSystem? fs = null,
        IClipboard? clipboard = null,
        IView? view = null)
    {
        path = string.IsNullOrEmpty(path) ? Path.GetTempFileName() : path;
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
    public async Task given_opened_file_when_saved_then_saves_the_file()
    {
        var fs = Shared.GetMockFs();
        var (file, _, _, _, _, _, path) = CreateFileWithMocks(fs: fs);
        await file.Process(Mock.Of<IState>(), ".save");
        Assert.That(fs.File.Exists(path));
    }

    [Test]
    public async Task given_opened_file_when_close_then_replace_the_context()
    {
        var (file, context, _, _, _, _, _) = CreateFileWithMocks();
        var state = new Mock<IState>();
        await file.Process(state.Object, "..");
        state.VerifySet(m => m.Context = context, Times.Once);
    }

    [Test]
    public async Task Test_File_Rename()
    {
        var (pwd, _, _) = Shared.EncryptionTestData();
        var fs = await Shared.FileLayout1(Shared.GetMockFs());
        var view = new Mock<IView>();
        var session = new Session(new Cipher(pwd), fs, Mock.Of<IClipboard>(), view.Object);
        var state = new State(session);
        await session.Process(state, ".open encrypted");
        var file = state.Context as File;
        file?.Process(state, ".rename encrypted.test");
        Assert.That(fs.File.Exists("encrypted.test"));
        file?.Process(state, ".rename regular_dir/encrypted.test");
        Assert.That(fs.File.Exists("regular_dir/encrypted.test"));
    }
}