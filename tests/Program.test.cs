using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using File = pwd.contexts.File;
using Session = pwd.contexts.Session;
using pwd.tests;

// ReSharper disable UnusedMember.Local because the tests are called through reflection

namespace pwd;

public static partial class Program
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

    private static (string pwd, string text) EncryptionTestData()
    {
        return ("secret", "lorem ipsum ...");
    }

    private static IFileSystem GetMockFs()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("container/test");
        var dir = fs.DirectoryInfo.FromDirectoryName("container/test").FullName;
        fs.Directory.SetCurrentDirectory(dir);
        return fs;
    }

    private static async Task<IFileSystem> FileLayout1(IFileSystem fs)
    {
        var (pwd, text) = EncryptionTestData();
        var cipher = new Cipher(pwd);

        fs.File.WriteAllText("file", text);
        fs.File.WriteAllText(".hidden", text);
        fs.Directory.CreateDirectory("regular_dir");
        fs.File.WriteAllText("regular_dir/file", text);
        fs.File.WriteAllText("regular_dir/.hidden", text);
        fs.Directory.CreateDirectory(".hidden_dir");
        fs.File.WriteAllText(".hidden_dir/file", text);
        fs.File.WriteAllText(".hidden_dir/.hidden", text);
        fs.File.WriteAllBytes("encrypted", await cipher.Encrypt(text));
        fs.File.WriteAllBytes(".hidden_encrypted", await cipher.Encrypt(text));
        fs.File.WriteAllBytes("regular_dir/encrypted", await cipher.Encrypt(text));
        fs.File.WriteAllBytes("regular_dir/.hidden_encrypted", await cipher.Encrypt(text));
        fs.File.WriteAllBytes(".hidden_dir/encrypted", await cipher.Encrypt(text));
        fs.File.WriteAllBytes(".hidden_dir/.hidden_encrypted", await cipher.Encrypt(text));
        return fs;
    }

    private static (Session, ICipher, IFileSystem, IClipboard, IView) CreateSessionWithMocks(
        ICipher? cipher = null,
        IFileSystem? fs = null,
        IClipboard? clipboard = null,
        IView? view = null)
    {
        cipher ??= new MockCipher();
        fs ??= new MockFileSystem();
        clipboard ??= new MockClipboard();
        view ??= new MockView();
        return (new Session(cipher, fs, clipboard, view), cipher, fs, clipboard, view);
    }

    private static async Task Test_File_Rename()
    {
        var (pwd, _) = EncryptionTestData();
        var fs = await FileLayout1(GetMockFs());
        var view = new MockView();
        var session = new Session(new Cipher(pwd), fs, new MockClipboard(), view);
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
        var session = new Session(new Cipher(pwd), fs, new MockClipboard(), view);
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

        var testData = await new Cipher(pwd).Encrypt("user: user\npassword: password\n");

        IEnumerable<string> Input()
        {
            yield return pwd;
            yield return pwd;
            yield return "";
            fs.File.WriteAllBytes("test", testData);
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
        await Run(fs, read, read, s => session = s, _ => { });
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

    private static async Task Tests(
        string[] tests)
    {
        var allTests =
            typeof(Program)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(item => item.Name.StartsWith("Test_"));

        var selectedTests = allTests
            .Where(item => tests.Length == 0 ||
                           tests.Any(value => item.Name.Contains(value, StringComparison.OrdinalIgnoreCase)));

        foreach (var test in selectedTests.OrderBy(item => item.Name))
        {
            if (test.ReturnType == typeof(void))
                await Test(() =>
                {
                    test.Invoke(null, null);
                    return Task.CompletedTask;
                }, test.Name);
            else
                await Test(() => (Task) test.Invoke(null, null)!, test.Name);
        }
    }
}