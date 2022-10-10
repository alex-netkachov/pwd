using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using pwd.ciphers;
using pwd.contexts;
using pwd.readline;

namespace pwd;

public static class Shared
{
   private static void Assert(
      bool value,
      string message = "")
   {
      if (!value) throw new(message);
   }

   public static (string pwd, string text, byte[] encrypted) ContentEncryptionTestData()
   {
      return (
         "secret",
         "only you can protect what is yours",
         Convert.FromHexString(
            "53616C7465645F5FD2586E38D8F094E37022709B84AAD604AB513AA251223B2F49E2222A67C81DF3A2A772B33D8EEC32C83AB0FE7C46860575E695E2F7858D3A"));
   }

   public static (string pwd, string text, byte[] encrypted) NameEncryptionTestData()
   {
      return (
         "secret",
         "only you can protect what is yours",
         Convert.FromHexString(
            "475349596B69396453506F675378444A525F73396D6D636E616D6A746A3734616E4D43793255324B6A464B48345F335234477859675452326C446E726778352B694E654A573375474F63737E"));
   }

   public static contexts.File CreateFileContext(
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
            services
               .AddSingleton(Mock.Of<ILogger>())
               .AddSingleton(clipboard ?? Mock.Of<IClipboard>())
               .AddSingleton(fs ?? Mock.Of<IFileSystem>())
               .AddSingleton(repository)
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton<IFileFactory, FileFactory>());

      using var host = builder.Build();

      return (contexts.File)host.Services
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
      var builder = Host.CreateDefaultBuilder();
      builder.ConfigureServices(
         services =>
            services
               .AddSingleton(Mock.Of<ILogger>())
               .AddSingleton(state ?? Mock.Of<IState>())
               .AddSingleton(view ?? Mock.Of<IView>())
               .AddSingleton(fileFactory ?? Mock.Of<IFileFactory>())
               .AddSingleton(newFileFactory ?? Mock.Of<INewFileFactory>())
               .AddSingleton(@lock ?? Mock.Of<ILock>())
               .AddSingleton<ISessionFactory, SessionFactory>());

      using var host = builder.Build();

      return (Session)host.Services
         .GetRequiredService<ISessionFactory>()
         .Create(
            repository ?? Mock.Of<IRepository>(),
            exporter ?? Mock.Of<IExporter>(),
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
      var console = new StandardConsole();
      var view = new View(console, new Reader(console), Timeout.InfiniteTimeSpan);
      var repository = new Repository(fs, new ZeroCipher(), new ContentCipher(pwd), ".");
      await repository.Initialise();
      var session = CreateSessionContext(repository, view: view);
      Assert(string.Join(";", session.Get("../")) == "../test");
      Assert(string.Join(";", session.Get("")) == "encrypted;regular_dir");
      Assert(string.Join(";", session.Get("enc")) == "encrypted");
      Assert(string.Join(";", session.Get("encrypted")) == "encrypted");
      Assert(string.Join(";", session.Get("regular_dir")) == "regular_dir");
      Assert(string.Join(";", session.Get("regular_dir/")) == "regular_dir/encrypted");
      Assert(string.Join(";", session.Get("regular_dir/enc")) == "regular_dir/encrypted");
      Assert(string.Join(";", session.Get("regular_dir/encrypted")) == "regular_dir/encrypted");
   }

   private static async Task Test_Main1()
   {
      var (pwd, _, _) = ContentEncryptionTestData();
      var fs = GetMockFs();

      var testData = new MemoryStream();
      await new ContentCipher(pwd).EncryptAsync("user: user\npassword: password\n", testData);

      var view = new BufferedView(
         pwd,
         pwd,
         "",
         "test",
         "..",
         ".quit");
      var state = new State();
      await Program.Run(Mock.Of<ILogger>(), Mock.Of<IConsole>(), fs, view, state);
      var expected = string.Join("\n",
         "Password: secret",
         "It seems that you are creating a new repository. Please confirm password: secret",
         ">",
         "> test",
         "user: user",
         "password: password",
         "test> ..",
         "> .quit");
      var actual = view.ToString();
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
   private readonly string[] _input;
   private int _index;

   public event EventHandler? Idle;

   public BufferedView(
      params string[] input)
   {
      _input = input;
   }

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

   public Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      return Task.FromResult(true);
   }

   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      if (_index <= _input.Length)
      {
         var value = _input[_index];
         _index++;
         return Task.FromResult(value);   
      }
      return Task.FromResult("");
   }

   public Task<string> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default)
   {
      if (_index <= _input.Length)
      {
         var value = _input[_index];
         _index++;
         return Task.FromResult(value);   
      }
      return Task.FromResult("");
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