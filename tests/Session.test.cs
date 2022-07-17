using System.Linq;
using System.Threading.Tasks;
using pwd.tests;

namespace pwd;

// ReSharper disable UnusedMember.Local because the tests are called through reflection

public static partial class Program
{
    private static Task Test_Session_Ctor()
    {
        CreateSessionWithMocks();
        return Task.CompletedTask;
    }

    private static async Task Test_Session_GetItems1()
    {
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: GetMockFs());
        Assert(!(await session.GetItems()).Any());
        Assert(!(await session.GetItems()).Any());
        Assert(!(await session.GetItems(".")).Any());
    }

    private static async Task Test_Session_GetItems2()
    {
        var fs = await FileLayout1(GetMockFs());
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: fs);
        Assert(string.Join(";", await session.GetItems()) == "encrypted;regular_dir");
        Assert(string.Join(";", await session.GetItems(".")) == "encrypted;regular_dir");
        Assert(string.Join(";", await session.GetItems()) == "encrypted;regular_dir");
        Assert(string.Join(";", await session.GetItems("regular_dir")) == "regular_dir/encrypted");
        Assert(string.Join(";", await session.GetItems(".hidden_dir")) == ".hidden_dir/encrypted");
    }

    private static async Task Test_Session_GetEncryptedFilesRecursively1()
    {
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: GetMockFs());
        Assert(!(await session.GetEncryptedFilesRecursively()).ToList().Any());
        Assert(!(await session.GetEncryptedFilesRecursively(".")).Any());
    }

    private static async Task Test_Session_GetEncryptedFilesRecursively2()
    {
        var (session, _, _, _, _) = CreateSessionWithMocks(fs: await FileLayout1(GetMockFs()));

        Assert(string.Join(";", await session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");
        Assert(string.Join(";", await session.GetEncryptedFilesRecursively(".")) == "encrypted;regular_dir/encrypted");
        Assert(string.Join(";", await session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");

        Assert(string.Join(";", await session.GetEncryptedFilesRecursively(includeHidden: true)) ==
               ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted");

        Assert(string.Join(";", await session.GetEncryptedFilesRecursively("regular_dir")) ==
               "regular_dir/encrypted");
        Assert(string.Join(";", await session.GetEncryptedFilesRecursively("regular_dir", true)) ==
               "regular_dir/.hidden_encrypted;regular_dir/encrypted");

        Assert(string.Join(";", await session.GetEncryptedFilesRecursively(".hidden_dir")) ==
               ".hidden_dir/encrypted");
        Assert(string.Join(";", await session.GetEncryptedFilesRecursively(".hidden_dir", true)) ==
               ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted");
    }

    private static async Task Test_Session_Read()
    {
        var (pwd, text) = EncryptionTestData();
        var cipher = new Cipher(pwd);
        var (session, _, _, _, view) = CreateSessionWithMocks(cipher: cipher, fs: await FileLayout1(GetMockFs()));
        var mockView = (MockView) view;

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
            mockView.Clear();
            var state = new State(session);
            await session.Process(state, $".open {file}");
            Assert(mockView.ToString().Trim() == text, $"Expect: {text} Actual: {view}");
        }
    }
}