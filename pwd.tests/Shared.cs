using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using pwd.ciphers;
using pwd.contexts;
using IFile = pwd.contexts.IFile;


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

   public static (byte[] pwd, string text, byte[] encrypted) ContentEncryptionTestData()
   {
      return (
         Encoding.UTF8.GetBytes("secret"),
         "only you can protect what is yours",
         Convert.FromHexString(
            "53616C7465645F5FD2586E38D8F094E37022709B84AAD604AB513AA251223B2F49E2222A67C81DF3A2A772B33D8EEC32C83AB0FE7C46860575E695E2F7858D3A"));
   }

   public static (byte[] pwd, string text, byte[] encrypted) NameEncryptionTestData()
   {
      return (
         Encoding.UTF8.GetBytes("secret"),
         "only you can protect what is yours",
         Convert.FromHexString(
            "475349596B69396453506F675378444A525F73396D6D636E616D6A746A3734616E4D43793255324B6A464B48345F335234477859675452326C446E726778352B694E654A573375474F63737E"));
   }

   public static IFile CreateFileContext(
      string path = "",
      string name = "",
      string content = "",
      IFileSystem? fs = null,
      IRepository? repository = null,
      IClipboard? clipboard = null,
      IView? view = null,
      IState? state = null,
      ILock? @lock = null)
   {
      path = string.IsNullOrEmpty(path) ? Path.GetFileName(Path.GetTempFileName()) : path;
      name = string.IsNullOrEmpty(path) ? Path.GetFileName(path) : name;
      repository ??= Mock.Of<IRepository>();
      
      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
            services.AddSingleton(clipboard ?? Mock.Of<IClipboard>())
               .AddSingleton(fs ?? Mock.Of<IFileSystem>())
               .AddSingleton(repository)
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton<IFileFactory, FileFactory>());

      using var host = builder.Build();

      return host.Services
         .GetRequiredService<IFileFactory>()
         .Create(
            repository,
            @lock ?? Mock.Of<ILock>(),
            name,
            content);
   }

   public static Session CreateSessionContext(
      IRepository? repository = null,
      IExporter? exporter = null,
      IView? view = null,
      IState? state = null,
      IFileFactory? fileFactory = null,
      INewFileFactory? newFileFactory = null,
      ILock? @lock = null)
   {
      return new Session(
         exporter ?? Mock.Of<IExporter>(),
         repository ?? Mock.Of<IRepository>(),
         state ?? Mock.Of<IState>(),
         view ?? Mock.Of<IView>(),
         fileFactory ?? Mock.Of<IFileFactory>(),
         newFileFactory ?? Mock.Of<INewFileFactory>(),
         @lock ?? Mock.Of<ILock>());
   }

   public static IFileSystem GetMockFs()
   {
      var fs = new MockFileSystem();
      fs.Directory.CreateDirectory("container/test");
      var dir = fs.DirectoryInfo.FromDirectoryName("container/test").FullName;
      fs.Directory.SetCurrentDirectory(dir);
      return fs;
   }

   public static IFileSystem FileLayout1(IFileSystem fs)
   {
      var (_, text, encrypted) = ContentEncryptionTestData();

      fs.File.WriteAllText("file", text);
      fs.File.WriteAllText(".hidden", text);
      fs.Directory.CreateDirectory("regular_dir");
      fs.File.WriteAllText("regular_dir/file", text);
      fs.File.WriteAllText("regular_dir/.hidden", text);
      fs.Directory.CreateDirectory(".hidden_dir");
      fs.File.WriteAllText(".hidden_dir/file", text);
      fs.File.WriteAllText(".hidden_dir/.hidden", text);
      fs.File.WriteAllBytes("encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_encrypted", encrypted);
      fs.File.WriteAllBytes("regular_dir/encrypted", encrypted);
      fs.File.WriteAllBytes("regular_dir/.hidden_encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_dir/encrypted", encrypted);
      fs.File.WriteAllBytes(".hidden_dir/.hidden_encrypted", encrypted);
      return fs;
   }

   private static async Task Test_AutoCompletionHandler()
   {
      var (pwd, _, _) = ContentEncryptionTestData();
      var fs = FileLayout1(GetMockFs());
      var view = new View(Mock.Of<IState>());
      var repository = new Repository(fs, new ZeroCipher(), new ContentCipher(pwd), ".");
      await repository.Initialise();
      var session = CreateSessionContext(repository, view: view);
      var state = new State(session);
      var handler = new AutoCompletionHandler(state);
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
      var (pwd, _, _) = ContentEncryptionTestData();
      var fs = GetMockFs();

      var testData = new MemoryStream();
      await new ContentCipher(pwd).EncryptAsync("user: user\npassword: password\n", testData);

      IEnumerable<string> Input()
      {
         yield return Encoding.UTF8.GetString(pwd);
         yield return Encoding.UTF8.GetString(pwd);
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
      var state = new State(NullContext.Instance);
      await Program.Run(Mock.Of<ILogger>(), fs, view.Object, state);
      Console.SetOut(stdout);
      var expected = string.Join("\n", "Password: secret",
         "It seems that you are creating a new repository. Please confirm password: secret", ">", "> test",
         "user: user", "password: password", "test> ..", "> .quit");
      var actual = string.Join("\n", messages.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)));
      Assert(expected == actual);
   }
}

public sealed class ZeroCipher
   : INameCipher,
      IContentCipher
{
   public static readonly ZeroCipher Instance = new();

   public int Encrypt(
      string text,
      Stream stream)
   {
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return data.Length;
   }

   public Task<int> EncryptAsync(
      string text,
      Stream stream)
   {
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return Task.FromResult(data.Length);
   }

   public (bool Success, string Text) DecryptString(
      Stream stream)
   {
      using var reader = new StreamReader(stream);
      return (true, reader.ReadToEnd());
   }

   public async Task<(bool Success, string Text)> DecryptStringAsync(
      Stream stream)
   {
      using var reader = new StreamReader(stream);
      return (true, await reader.ReadToEndAsync());
   }
}

public sealed class BufferedView
   : IView
{
   private readonly StringBuilder _output = new();

   public event EventHandler? Interaction;

   public void WriteLine(
      string text)
   {
      _output.AppendLine(text);
   }

   public void Write(
      string text)
   {
      _output.Append(text);
   }

   public bool Confirm(
      string question,
      Answer @default = Answer.No)
   {
      return true;
   }

   public string Read(
      string prompt)
   {
      return "";
   }

   public byte[] ReadPassword(
      string prompt)
   {
      return Array.Empty<byte>();
   }

   public void Clear()
   {
      _output.Clear();
   }

   public override string ToString()
   {
      return _output.ToString();
   }
}