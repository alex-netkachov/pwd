using System.IO.Abstractions;
using System.Text;
using Moq;
using pwd.contexts;

namespace pwd.tests.contexts;

public sealed class Session_Tests
{
    private static (
        Session Session,
        IFileSystem FileSystem,
        IRepository Repository,
        IClipboard Clipboard,
        IView View)
        CreateSessionContext(
            IFileSystem? fs = null,
            IRepository? repository = null,
            IClipboard? clipboard = null,
            IView? view = null)
    {
        fs ??= Mock.Of<IFileSystem>();
        repository ??= Mock.Of<IRepository>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new Session(fs, repository, clipboard, view),
            fs,
            repository,
            clipboard,
            view);
    }

    [Test]
    public void construct_session()
    {
        CreateSessionContext();
    }

    [TestCase("encrypted")]
    [TestCase(".hidden_encrypted")]
    [TestCase("regular_dir/encrypted")]
    [TestCase("regular_dir/.hidden_encrypted")]
    [TestCase(".hidden_dir/encrypted")]
    [TestCase(".hidden_dir/.hidden_encrypted")]
    public async Task open_file(
        string file)
    {
        var (pwd, text, _) = Shared.ContentEncryptionTestData();

        var cipher = new ContentCipher(pwd);

        var view = new BufferedView();

        var repository = new Repository(await Shared.FileLayout1(Shared.GetMockFs()), new ZeroCipher(), cipher, ".");
        await repository.Initialise();

        var sut = CreateSessionContext(repository: repository, view: view);

        var session = sut.Session;

        var state = new State(session);
        await session.Process(state, $".open {file}");
        Assert.That(view.ToString().Trim(), Is.EqualTo(text));
    }
}