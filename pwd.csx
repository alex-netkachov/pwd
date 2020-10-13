#!/usr/bin/env dotnet-script

#r "nuget: ReadLine, 2.0.1"
#r "nuget: YamlDotNet, 8.1.2"
#r "nuget: PasswordGenerator, 2.0.5"
#r "nuget: System.IO.Abstractions, 12.2.5"

using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

static Exception Try(Action action) {
   try { action(); return null; } catch (Exception e) { return e; }
}

static Aes CreateAes(byte[] salt, string password) {
   var aes = Aes.Create();
   (aes.Mode, aes.Padding) = (CipherMode.CBC, PaddingMode.PKCS7);
   // 10000 and SHA256 are defaults for pbkdf2 in openssl
   using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
   (aes.Key, aes.IV) = (rfc2898.GetBytes(32), rfc2898.GetBytes(16));
   return aes;
}

static byte[] ReadBytes(Stream stream, int length) {
   var chunk = new byte[length];
   stream.Read(chunk, 0, length);
   return chunk;
}

static byte[] Encrypt(string password, string text) {
   var salt = new byte[8];
   using var rng = new RNGCryptoServiceProvider();
   rng.GetBytes(salt);
   using var aes = CreateAes(salt, password);
   using var stream = new MemoryStream();
   var preambule = Encoding.ASCII.GetBytes("Salted__").Concat(salt).ToArray();
   stream.Write(preambule, 0, preambule.Length);
   using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
   using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);
   var data = Encoding.UTF8.GetBytes(text);
   cryptoStream.Write(data, 0, data.Length);
   cryptoStream.Close();
   return stream.ToArray();
}

static string Decrypt(string password, byte[] data) {
   using var stream = new MemoryStream(data);
   if ("Salted__" != Encoding.ASCII.GetString(ReadBytes(stream, 8)))
      throw new FormatException("Expecting the data stream to begin with Salted__.");
   var salt = ReadBytes(stream, 8);
   using var aes = CreateAes(salt, password);
   using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
   using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
   using var reader = new StreamReader(cryptoStream);
   return reader.ReadToEnd();
}

static bool IsFileEncrypted(IFileSystem fs, string path) {
   using var stream = fs.File.OpenRead(path);
   // openssl adds Salted__ at the beginning of a file, let's use to to check whether it is enrypted or not
   return "Salted__" == Encoding.ASCII.GetString(ReadBytes(stream, 8));
}

static string JoinPath(string path1, string path2) =>
    (path1 == "." ? "" : $"{path1}/") + path2;

static IEnumerable<string> GetFiles(
      IFileSystem fs,
      string path,
      bool recursively = false,
      bool includeFolders = false,
      bool includeDottedFilesAndFolders = false) =>
    fs.Directory.Exists(path) ? fs.DirectoryInfo.FromDirectoryName(path)
        .EnumerateFileSystemInfos()
        .OrderBy(info => info.Name)
        .SelectMany(info => info switch {
           IFileInfo file when !file.Name.StartsWith(".") || includeDottedFilesAndFolders =>
               new[] { JoinPath(path, file.Name) },
           IDirectoryInfo dir when !dir.Name.StartsWith(".") || includeDottedFilesAndFolders =>
               (recursively
                  ? GetFiles(fs, JoinPath(path, dir.Name), recursively, includeFolders, includeDottedFilesAndFolders)
                  : new string[0]).Concat(includeFolders ? new[] { JoinPath(path, dir.Name) } : new string[0]),
           _ => new string[0]
        }) : Enumerable.Empty<string>();

static (string, string, string) ParseRegexCommand(string text, int idx = 0) {
   string Read() {
      var (begin, escape) = (++idx, false);
      for (; idx < text.Length; idx++) {
         var ch = text[idx];
         if (!escape && ch == '\\') { escape = true; continue; }
         else if (!escape && ch == '/')
            return text.Substring(begin, idx - begin);
         escape = false;
      }
      return text.Substring(begin);
   }

   var (pattern, replacement, options) = (Read(), Read(), Read());
   replacement = Regex.Replace(replacement, @"\\.", m =>
        m.Groups[0].Value[1] switch { 'n' => "\n", 't' => "\t", 'r' => "\r", var n => $"{n}" });
   return (pattern, replacement, options);
}

static Exception CheckYaml(string text) {
   using var input = new StringReader(text);
   return Try(() => new YamlStream().Load(input));
}

public class Session {
   private string _password;
   private IFileSystem _fs;

   public Session(string password, IFileSystem fs) =>
      (_password, _fs) = (password, fs);

   public string Path { get; private set; }
   public string Content { get; private set; }
   public bool Modified { get; private set; }

   public IEnumerable<string> GetItems(string path = null) =>
      GetFiles(_fs, path ?? ".", includeFolders: true)
         .Where(item => !_fs.File.Exists(item) || IsFileEncrypted(_fs, item));

   public IEnumerable<string> GetEncryptedFilesRecursively(string path = null, bool includeHidden = false) =>
       GetFiles(_fs, path ?? ".", recursively: true, includeDottedFilesAndFolders: includeHidden)
         .Where(file => IsFileEncrypted(_fs, file));

   public string Read(string path) =>
      Decrypt(_password, _fs.File.ReadAllBytes(path));

   public void Write(string path, string content) {
      _fs.File.WriteAllBytes(path, Encrypt(_password, content));
      if (Path == path) (Content, Modified) = (content, false);
   }

   public string ExportContentToTempFile() {
      var path = System.IO.Path.GetTempFileName() + ".yaml";
      _fs.File.WriteAllText(path, Content);
      return path;
   }

   public void ReadContentFromFile(string path) =>
       (Content, Modified) = (_fs.File.ReadAllText(path), path != Path);

   public void Open(string path) =>
      (Path, Content, Modified) = (path, Read(path), false);

   public void Close() =>
      (Path, Content, Modified) = ("", "", false);

   public void Save() {
      if (Path == "") return;
      Write(Path, Content);
      Modified = false;
   }

   public void Replace(string command) {
      var (pattern, replacement, options) = ParseRegexCommand(command);
      var re = new Regex(
          pattern,
          options.Contains('i') ? RegexOptions.IgnoreCase : RegexOptions.None);
      Content = re.Replace(Content, replacement, options.Contains('g') ? -1 : 1);
      Modified = true;
   }

   public void Check() {
      var names = GetEncryptedFilesRecursively(includeHidden: true).ToList();
      if (names.Count == 0)
         return;
      var wrongPassword = new List<string>();
      var notYaml = new List<string>();
      foreach (var name in names) {
         var content = default(string);
         try {
            content = Decrypt(_password, _fs.File.ReadAllBytes(name));
            if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
               content = default;
         } catch { }

         if (content == default) {
            wrongPassword.Add(name);
            Console.Write('*');
         } else if (CheckYaml(content) != null) {
            notYaml.Add(name);
            Console.Write('+');
         } else
            Console.Write('.');
      }

      Console.WriteLine();

      if (wrongPassword.Count > 0) {
         var more = wrongPassword.Count > 3 ? ", ..." : "";
         var failuresText = string.Join(", ", wrongPassword.Take(Math.Min(3, wrongPassword.Count)));
         throw new Exception($"Integrity check failed for: {failuresText}{more}");
      }

      if (notYaml.Count > 0)
         Console.Error.WriteLine($"YAML check failed for: {(string.Join(", ", notYaml))}");
   }

   public void CheckContent() {
      if (CheckYaml(Content) is { Message: var msg })
         Console.Error.WriteLine(msg);
   }
}

class AutoCompletionHandler : IAutoCompleteHandler {
   private Session _session;

   public AutoCompletionHandler(Session session) {
      _session = session;
   }

   public char[] Separators { get; set; } = new char[] { '/' };

   public string[] GetSuggestions(string text, int index) {
      if (text.StartsWith("."))
         return null;
      var path = text.Split('/');
      var folder = string.Join("/", path.Take(path.Length - 1));
      var query = path.Last();
      return _session.GetItems(folder.Length == 0 ? "." : folder)
          .Where(item => item.StartsWith(query))
          .ToArray();
   }
}

(string, string, string) ParseCommand(string input) {
   var match = Regex.Match(input, @"^\.(\w+)(?: +(.+))?$");
   return match.Success ? ("", match.Groups[1].Value, match.Groups[2].Value) : (input, "", "");
}

Action<Session> Route(string input, IFileSystem fs) =>
    ParseCommand(input) switch {
       (_, "save", _) => session => session.Save(),
       ("..", _, _) => session => session.Close(),
       _ when input.StartsWith("/") => session => {
          if (session.Path == "") return;
          session.Replace(input);
          Console.WriteLine(session.Content);
       },
       (_, "check", _) => session => {
          if (string.IsNullOrEmpty(session.Path)) session.Check();
          else session.CheckContent();
       },
       (_, "open", var path) => session => {
          if (!fs.File.Exists(path) || !IsFileEncrypted(fs, path)) return;
          session.Open(path);
          Console.WriteLine(session.Content);
       },
       (_, "archive", _) => session => {
          if (session.Path == "") return;
          if (!fs.Directory.Exists(".archive")) fs.Directory.CreateDirectory(".archive");
          fs.File.Move(session.Path, ".archive/" + session.Path);
          session.Close();
       },
       (_, "rm", _) => session => {
          if (session.Path == "") return;
          Console.Write("Delete '" + session.Path + "'? (y/n)");
          if (Console.ReadLine().Trim().ToUpperInvariant() != "Y") return;
          fs.File.Delete(session.Path);
          session.Close();
       },
       (_, "rename", var name) => session => {
          if (session.Path == "") return;
          fs.File.Move(session.Path, name);
          session.Close();
          session.Open(name);
       },
       (_, "edit", var editor) => session => {
          editor = string.IsNullOrEmpty(editor) ? Environment.GetEnvironmentVariable("EDITOR") : editor;
          if (string.IsNullOrEmpty(editor))
             throw new Exception("The editor is not specified and the environment variable EDITOR is not set.");
          var originalContent = session.Content;
          var path = session.ExportContentToTempFile();
          try {
             var process = Process.Start(new ProcessStartInfo(editor, path));
             process.WaitForExit();
             session.ReadContentFromFile(path);
             Console.WriteLine(session.Content);
             Console.Write("Save the content (y/n)? ");
             var choice = Console.ReadLine();
             if (choice.ToLowerInvariant() == "y") session.Save();
             else session.Write(session.Path, originalContent);
          } finally {
             fs.File.Delete(path);
          }
       },
       (_, "pwd", _) => session => Console.WriteLine(new PasswordGenerator.Password().Next()),
       (_, "add", var path) => session => {
          var folder = Path.GetDirectoryName(path);
          if (folder != "" && !fs.Directory.Exists(folder))
             fs.Directory.CreateDirectory(folder);
          var line = "";
          var content = new StringBuilder();
          while ("" != (line = Console.ReadLine()))
             content.AppendLine(line.Replace("***", new PasswordGenerator.Password().Next()));
          session.Write(path, content.ToString());
          session.Open(path);
          Console.WriteLine(session.Content);
       },
       (_, "cc", var name) => session => {
          var match = Regex.Match(session.Content, @$"{name}: *([^\n]+)");
          if (match.Success) {
             var process = default(Process);
             if (Try(() => process = Process.Start(new ProcessStartInfo("clip.exe") { RedirectStandardInput = true })) == null ||
                  Try(() => process = Process.Start(new ProcessStartInfo("pbcopy") { RedirectStandardInput = true })) == null ||
                  Try(() => process = Process.Start(new ProcessStartInfo("xclip -sel clip") { RedirectStandardInput = true })) == null) {
               process.StandardInput.Write(match.Groups[1].Value);
               process.StandardInput.Close();
             }
          }
       },
       (_, "ccu", _) => Route(".cc user", fs),
       (_, "ccp", _) => Route(".cc password", fs),
       _ => session => {
          if (!string.IsNullOrEmpty(session.Path)) {
             Console.WriteLine(session.Content);
             return;
          }

          var names = session.GetEncryptedFilesRecursively()
              .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
              .ToList();

          var name = names.Count == 1 && input != ""
              ? names[0]
              : names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase));

          if (name != default) {
             session.Open(name);
             Console.WriteLine(session.Content);
             return;
          }

          Console.Write(string.Join("", names.Select(name => $"{name}\n")));
       }
    };

if (!Args.Contains("-t")) {
   var fs = new FileSystem();

   var password = ReadLine.ReadPassword("Password: ");

   var session = new Session(password, fs);
   var e1 = Try(() => session.Check());
   if (e1 != null) {
      Console.Error.WriteLine(e1.Message);
      return;
   }
   if (!session.GetEncryptedFilesRecursively(".", true).Any()) {
      var confirmPassword = ReadLine.ReadPassword("It seems that you are creating a new repository. Please confirm password:");
      if (confirmPassword != password) {
         Console.WriteLine("passwords do not match");
         return;
      }
      session.Write("template", "site: xxx\nuser: xxx\npassword: xxx\n");
   }

   ReadLine.HistoryEnabled = true;
   ReadLine.AutoCompletionHandler = new AutoCompletionHandler(session);

   while (true) {
      var input = ReadLine.Read((session.Modified ? "*" : "") + session.Path + "> ").Trim();
      if (input == ".quit") break;
      var e2 = Try(() => Route(input, fs)?.Invoke(session));
      if (e2 != null) Console.Error.WriteLine(e2.Message);
   }
   Console.Clear();
}
