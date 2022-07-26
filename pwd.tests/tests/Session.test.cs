using System.Text;
using Moq;
using pwd.contexts;

namespace pwd.tests;

public sealed class Session_Tests
{
    private static (
        Session Session,
        IRepository repository,
        IClipboard Clipboard,
        IView View)
        CreateSessionContext(
            IRepository? repository = null,
            IClipboard? clipboard = null,
            IView? view = null)
    {
        repository ??= Mock.Of<IRepository>();
        clipboard ??= Mock.Of<IClipboard>();
        view ??= Mock.Of<IView>();

        return (new Session(repository, clipboard, view),
            repository,
            clipboard,
            view);
    }

    [Test]
    public void Constructs_session_well()
    {
        CreateSessionContext();
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
                new Repository(await Shared.FileLayout1(Shared.GetMockFs()), new ZeroCipher(), cipher, "."),
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