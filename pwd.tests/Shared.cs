using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Moq;
using Session = pwd.contexts.Session;

// ReSharper disable UnusedMember.Local because the tests are called through reflection

namespace pwd;

public static class Shared
{
    private static void Assert(
        bool value,
        string message = "")
    {
        if (!value) throw new(message);
    }

    public static (string pwd, string text, byte[] encrypted) EncryptionTestData()
    {
        return (
            "secret",
            "only you can protect what is yours",
            Convert.FromHexString(
                "53616C7465645F5FD2586E38D8F094E37022709B84AAD604AB513AA251223B2F49E2222A67C81DF3A2A772B33D8EEC32C83AB0FE7C46860575E695E2F7858D3A"));
    }

    public static IFileSystem GetMockFs()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("container/test");
        var dir = fs.DirectoryInfo.FromDirectoryName("container/test").FullName;
        fs.Directory.SetCurrentDirectory(dir);
        return fs;
    }

    public static async Task<IFileSystem> FileLayout1(IFileSystem fs)
    {
        var (pwd, text, _) = EncryptionTestData();
        var cipher = new Cipher(pwd);

        async Task EncryptWrite(
            string path,
            ICipher cipher1,
            string text1)
        {
            using var stream = new MemoryStream();
            await cipher1.Encrypt(text1, stream);
            await fs.File.WriteAllBytesAsync(path, stream.ToArray());
        }

        fs.File.WriteAllText("file", text);
        fs.File.WriteAllText(".hidden", text);
        fs.Directory.CreateDirectory("regular_dir");
        fs.File.WriteAllText("regular_dir/file", text);
        fs.File.WriteAllText("regular_dir/.hidden", text);
        fs.Directory.CreateDirectory(".hidden_dir");
        fs.File.WriteAllText(".hidden_dir/file", text);
        fs.File.WriteAllText(".hidden_dir/.hidden", text);
        await EncryptWrite("encrypted", cipher, text);
        await EncryptWrite(".hidden_encrypted", cipher, text);
        await EncryptWrite("regular_dir/encrypted", cipher, text);
        await EncryptWrite("regular_dir/.hidden_encrypted", cipher, text);
        await EncryptWrite(".hidden_dir/encrypted", cipher, text);
        await EncryptWrite(".hidden_dir/.hidden_encrypted", cipher, text);
        return fs;
    }

    private static async Task Test_AutoCompletionHandler()
    {
        var (pwd, _, _) = EncryptionTestData();
        var fs = await FileLayout1(GetMockFs());
        var view = new View();
        var session = new Session(new Cipher(pwd), fs, Mock.Of<Clipboard>(), view);
        var handler = new AutoCompletionHandler(session);
        Assert(string.Join(";", handler.GetSuggestions("../", 0)) == "../test");
        Assert(string.Join(";", handler.GetSuggestions("", 0)) == "encrypted;regular_dir");
        Assert(string.Join(";", handler.GetSuggestions("enc", 0)) == "encrypted");
        Assert(string.Join(";", handler.GetSuggestions("encrypted", 0)) == "encrypted");
        Assert(string.Join(";", handler.GetSuggestions("regular_dir", 0)) == "regular_dir");
        Assert(string.Join(";", handler.GetSuggestions("regular_dir/", 0)) == "regular_dir/encrypted");
        Assert(string.Join(";", handler.GetSuggestions("regular_dir/enc", 0)) == "regular_dir/encrypted");
        Assert(string.Join(";", handler.GetSuggestions("regular_dir/encrypted", 0)) == "regular_dir/encrypted");
    }

    private static async Task Test_Main1()
    {
        var (pwd, _, _) = EncryptionTestData();
        var fs = GetMockFs();
        var session = default(Session);

        var testData = new MemoryStream();
        await new Cipher(pwd).Encrypt("user: user\npassword: password\n", testData);

        IEnumerable<string> Input()
        {
            yield return pwd;
            yield return pwd;
            yield return "";
            fs.File.WriteAllBytes("test", testData.ToArray());
            yield return "test";
            yield return "..";
            yield return ".quit";
        }

        var messages = new List<string>();
        var stdoutBuilder = new StringBuilder();
        using var e = Input().GetEnumerator();
        var read = (Func<string, string>) (text =>
        {
            e.MoveNext();
            var output = stdoutBuilder.ToString();
            if (output.Trim().Length > 0)
                messages.Add(output);
            stdoutBuilder.Clear();
            messages.Add($"{text}{e.Current}");
            return e.Current;
        });
        var view = new Mock<IView>();
        view.Setup(m => m.Read(It.IsAny<string>())).Returns(read);
        view.Setup(m => m.ReadPassword(It.IsAny<string>())).Returns(read);
        var stdout = Console.Out;
        Console.SetOut(new StringWriter(stdoutBuilder));
        await Program.Run(fs, view.Object, s => session = s, (_, _) => { });
        Console.SetOut(stdout);
        var expected = string.Join("\n", "Password: secret",
            "It seems that you are creating a new repository. Please confirm password: secret", ">", "> test",
            "user: user", "password: password", "test> ..", "> .quit");
        var actual = string.Join("\n", messages.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)));
        Assert(expected == actual);
    }
}