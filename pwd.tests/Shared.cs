using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Text;
using Moq;
using File = pwd.contexts.File;
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

    private static async Task Test(
        Func<Task> test,
        string name)
    {
        var e = await test.Try();
        Console.WriteLine($"{name}: {(e == null ? "OK" : $"FAIL - {e}")}");
    }

    public static (string pwd, string text) EncryptionTestData()
    {
        return ("secret", "lorem ipsum ...");
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
        var (pwd, text) = EncryptionTestData();
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

    private static async Task Test_File_Rename()
    {
        var (pwd, _) = EncryptionTestData();
        var fs = await FileLayout1(GetMockFs());
        var view = new Mock<View>();
        var session = new Session(new Cipher(pwd), fs, Mock.Of<Clipboard>(), view.Object);
        var state = new State(session);
        await session.Process(state, ".open encrypted");
        var file = state.Context as File;
        file?.Process(state, ".rename encrypted.test");
        Assert(fs.File.Exists("encrypted.test"));
        file?.Process(state, ".rename regular_dir/encrypted.test");
        Assert(fs.File.Exists("regular_dir/encrypted.test"));
    }

    private static async Task Test_AutoCompletionHandler()
    {
        var (pwd, _) = EncryptionTestData();
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
        var (pwd, _) = EncryptionTestData();
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
        var stdout = Console.Out;
        Console.SetOut(new StringWriter(stdoutBuilder));
        await Program.Run(fs, read, read, s => session = s, _ => { });
        Console.SetOut(stdout);
        var expected = string.Join("\n", "Password: secret",
            "It seems that you are creating a new repository. Please confirm password: secret", ">", "> test",
            "user: user", "password: password", "test> ..", "> .quit");
        var actual = string.Join("\n", messages.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)));
        Assert(expected == actual);
    }

    private static void Test_Try()
    {
        var msg = new Action(() => throw new()).Try() switch {{ } e => e.Message, _ => default};
        Assert(msg != null);
    }
}