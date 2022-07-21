using System.IO.Abstractions;
using System.Text;
using Moq;
using pwd.contexts;

namespace pwd.tests;

public sealed class Session_Tests
{
    private static (Session, ICipher, IFileSystem, IClipboard, IView) CreateSessionWithMocks(
        ICipher? cipher = null,
        IFileSystem? fs = null,
        IClipboard? clipboard = null,
        IView? view = null)
    {
        cipher ??= Mock.Of<ICipher>();
        fs ??= Mock.Of<IFileSystem>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new Session(cipher, fs, clipboard, view),
            cipher,
            fs,
            clipboard,
            view);
    }

    [Test]
    public void Constructs_session_well()
    {
        CreateSessionWithMocks();
    }

    [Test]
    public async Task GetItems1()
    {
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: Shared.GetMockFs());
        Assert.That(!(await session.GetItems()).Any());
        Assert.That(!(await session.GetItems()).Any());
        Assert.That(!(await session.GetItems(".")).Any());
    }

    [Test]
    public async Task GetItems2()
    {
        var (pwd, _, _) = Shared.EncryptionTestData();
        var cipher = new Cipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: fs, cipher: cipher);
        var items1 = await session.GetItems();
        Assert.That(string.Join(";", items1) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await session.GetItems(".")) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await session.GetItems()) == "encrypted;regular_dir");
        Assert.That(string.Join(";", await session.GetItems("regular_dir")) == "regular_dir/encrypted");
        Assert.That(string.Join(";", await session.GetItems(".hidden_dir")) == ".hidden_dir/encrypted");
    }

    [Test]
    public async Task GetEncryptedFilesRecursively1()
    {
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: Shared.GetMockFs());
        Assert.That(!(await session.GetEncryptedFilesRecursively()).ToList().Any());
        Assert.That(!(await session.GetEncryptedFilesRecursively(".")).Any());
    }

    [Test]
    public async Task GetEncryptedFilesRecursively2()
    {
        var (pwd, _, _) = Shared.EncryptionTestData();
        var cipher = new Cipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());

        var (session, _, _, _, _) = CreateSessionWithMocks(fs: fs, cipher: cipher);

        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");
        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively(".")) == "encrypted;regular_dir/encrypted");
        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");

        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively(includeHidden: true)) ==
               ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted");

        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively("regular_dir")) ==
               "regular_dir/encrypted");
        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively("regular_dir", true)) ==
               "regular_dir/.hidden_encrypted;regular_dir/encrypted");

        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively(".hidden_dir")) ==
               ".hidden_dir/encrypted");
        Assert.That(string.Join(";", await session.GetEncryptedFilesRecursively(".hidden_dir", true)) ==
               ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted");
    }

    [Test]
    public async Task Read()
    {
        var (pwd, text, _) = Shared.EncryptionTestData();
        var cipher = new Cipher(pwd);

        var sb = new StringBuilder();
        
        var view = new Mock<IView>();
        view.Setup(m => m.Write(It.IsAny<string>())).Callback<string>(value => sb.Append(value));
        view.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(value => sb.AppendLine(value));

        var (session, _, _, _, _) =
            CreateSessionWithMocks(
                cipher: cipher,
                fs: await Shared.FileLayout1(Shared.GetMockFs()),
                view: view.Object);

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