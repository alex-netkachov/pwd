using System.IO.Abstractions;
using System.Text;
using Moq;
using pwd.contexts;

namespace pwd.tests;

public sealed class Session_Tests
{
    private static (
        Session Session,
        ICipher ContentCipher,
        ICipher NameCipher,
        IFileSystem FileSystem,
        IClipboard Clipboard,
        IView View)
        CreateSessionContext(
            ICipher? contentCipher = null,
            ICipher? nameCipher = null,
            IFileSystem? fs = null,
            IClipboard? clipboard = null,
            IView? view = null)
    {
        contentCipher ??= Mock.Of<ICipher>();
        nameCipher ??= Mock.Of<ICipher>();
        fs ??= Mock.Of<IFileSystem>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new Session(contentCipher, nameCipher, fs, clipboard, view),
            contentCipher,
            nameCipher,
            fs,
            clipboard,
            view);
    }

    [Test]
    public void Constructs_session_well()
    {
        CreateSessionContext();
    }

    [Test]
    public async Task GetItems1()
    {
        var session = CreateSessionContext(fs: Shared.GetMockFs()).Session;
        Assert.That(!(await session.GetItems()).Any());
        Assert.That(!(await session.GetItems()).Any());
        Assert.That(!(await session.GetItems(".")).Any());
    }

    [Test]
    public async Task GetItems2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());
        var sut = CreateSessionContext(fs: fs, contentCipher: cipher);
        var items1 = await sut.Session.GetItems();
        Assert.That(string.Join(";", items1) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await sut.Session.GetItems(".")) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await sut.Session.GetItems()) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await sut.Session.GetItems("regular_dir")) == "regular_dir/encrypted");
        Assert.That(string.Join(";", await sut.Session.GetItems(".hidden_dir")) == ".hidden_dir/encrypted");
    }

    [Test]
    public async Task GetEncryptedFilesRecursively1()
    {
        var sut = CreateSessionContext(fs: Shared.GetMockFs());
        Assert.That(!(await sut.Session.GetEncryptedFilesRecursively()).ToList().Any());
        Assert.That(!(await sut.Session.GetEncryptedFilesRecursively(".")).Any());
    }

    [Test]
    public async Task GetEncryptedFilesRecursively2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());

        var sut = CreateSessionContext(fs: fs, contentCipher: cipher);

        var session = sut.Session;

        Assert.That(
            string.Join(";", (await session.GetEncryptedFilesRecursively()).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await session.GetEncryptedFilesRecursively(".")).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await session.GetEncryptedFilesRecursively()).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await session.GetEncryptedFilesRecursively(includeHidden: true)).Select(item => item.Path)),
            Is.EqualTo(
                ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await session.GetEncryptedFilesRecursively("regular_dir")).Select(item => item.Path)),
            Is.EqualTo("regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await session.GetEncryptedFilesRecursively("regular_dir", true)).Select(item => item.Path)),
            Is.EqualTo("regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await session.GetEncryptedFilesRecursively(".hidden_dir")).Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await session.GetEncryptedFilesRecursively(".hidden_dir", true)).Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted"));
    }

    [Test]
    public async Task Read()
    {
        var (pwd, text, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);

        var sb = new StringBuilder();
        
        var view = new Mock<IView>();
        view.Setup(m => m.Write(It.IsAny<string>())).Callback<string>(value => sb.Append(value));
        view.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(value => sb.AppendLine(value));

        var sut =
            CreateSessionContext(
                contentCipher: cipher,
                fs: await Shared.FileLayout1(Shared.GetMockFs()),
                view: view.Object);

        var session = sut.Session;

        var files = new[]
        {
            "encrypted",
            ".hidden_encrypted",
            "regular_dir/encrypted",
            "regular_dir/.hidden_encrypted",
            ".hidden_dir/encrypted",
            ".hidden_dir/.hidden_encrypted"
        };

        foreach (var file in files)
        {
            sb.Clear();
            var state = new State(session);
            await session.Process(state, $".open {file}");
            Assert.That(sb.ToString().Trim(), Is.EqualTo(text));
        }
    }
}