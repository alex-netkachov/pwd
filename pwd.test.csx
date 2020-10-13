#load "pwd.csx"

#r "nuget: System.IO.Abstractions.TestingHelpers, 12.2.5"

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

bool Assert(bool value, string message = "") =>
    value ? true : throw new Exception(message);

void Test(Action test, string name) {
   var e = Try(test);
   Console.WriteLine($"{name}: {(e == null ? "OK" : $"FAIL - {e.Message}")}");
}

(string pwd, string text) EncryptionTestData() =>
    ("secret", "lorem ipsum ...");

IFileSystem GetMockFs() {
   var fs = new MockFileSystem();
   fs.Directory.CreateDirectory("test");
   var dir = fs.DirectoryInfo.FromDirectoryName("test").FullName;
   fs.Directory.SetCurrentDirectory(dir);
   return fs;
}

IFileSystem FileLayout1(IFileSystem fs) {
   var (pwd, text) = EncryptionTestData();
   fs.File.WriteAllText("file", text);
   fs.File.WriteAllText(".hidden", text);
   fs.Directory.CreateDirectory("regular_dir");
   fs.File.WriteAllText("regular_dir/file", text);
   fs.File.WriteAllText("regular_dir/.hidden", text);
   fs.Directory.CreateDirectory(".hidden_dir");
   fs.File.WriteAllText(".hidden_dir/file", text);
   fs.File.WriteAllText(".hidden_dir/.hidden", text);
   fs.File.WriteAllBytes("encrypted", Encrypt(pwd, text));
   fs.File.WriteAllBytes(".hidden_encrypted", Encrypt(pwd, text));
   fs.File.WriteAllBytes("regular_dir/encrypted", Encrypt(pwd, text));
   fs.File.WriteAllBytes("regular_dir/.hidden_encrypted", Encrypt(pwd, text));
   fs.File.WriteAllBytes(".hidden_dir/encrypted", Encrypt(pwd, text));
   fs.File.WriteAllBytes(".hidden_dir/.hidden_encrypted", Encrypt(pwd, text));
   return fs;
}

string LocateOpenssl() =>
    new[] {
        Environment.GetEnvironmentVariable("ProgramFiles") + @"\Git\usr\bin\openssl.exe",
        Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\Programs\Git\usr\bin\openssl.exe"
    }.FirstOrDefault(File.Exists) ?? "openssl";

void TestEncryptDecryptRoundup() {
   var (password, text) = EncryptionTestData();
   var encrypted = Encrypt(password, text);
   var decrypted = Decrypt(password, encrypted);
   Assert(text == decrypted);
}

void TestDecryptingOpensslEncryptedData() {
   void OpensslEncrypt(string path, string password, string text) {
      var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -e -salt -pbkdf2 -pass stdin") {
         RedirectStandardInput = true,
         RedirectStandardOutput = true,
         RedirectStandardError = true
      };
      var process = Process.Start(info);
      using var writer = new BinaryWriter(process.StandardInput.BaseStream);
      var passswordData = Encoding.ASCII.GetBytes(password + "\n");
      writer.Write(passswordData, 0, passswordData.Length);
      var data = Encoding.UTF8.GetBytes(text);
      writer.Write(data, 0, data.Length);
      writer.Close();
      using var stream = File.OpenWrite(path);
      process.StandardOutput.BaseStream.CopyTo(stream);
   }

   var (password, text) = EncryptionTestData();

   var path = Path.GetTempFileName();
   OpensslEncrypt(path, password, text);
   var decrypted = Decrypt(password, File.ReadAllBytes(path));
   File.Delete(path);
   Assert(text == decrypted);
}

void TestOpensslDecryptingEncryptedData() {
   string OpensslDecrypt(string path, string password) {
      var info = new ProcessStartInfo(LocateOpenssl(), "aes-256-cbc -d -salt -pbkdf2 -pass stdin") {
         RedirectStandardInput = true,
         RedirectStandardOutput = true,
         RedirectStandardError = true
      };
      var process = Process.Start(info);
      using var writer = new BinaryWriter(process.StandardInput.BaseStream);
      var passswordData = Encoding.ASCII.GetBytes(password + "\n");
      writer.Write(passswordData, 0, passswordData.Length);
      var encrypted = File.ReadAllBytes(path);
      writer.Write(encrypted, 0, encrypted.Length);
      writer.Close();
      return process.StandardOutput.ReadToEnd();
   }

   var (password, text) = EncryptionTestData();

   var path = Path.GetTempFileName();
   File.WriteAllBytes(path, Encrypt(password, text));
   var decrypted = OpensslDecrypt(path, password);
   File.Delete(path);
   Assert(text == decrypted);
}

void TestGetFilesRecursively() {
   var files = GetFiles(new FileSystem(), ".", recursively: true).ToList();
   foreach (var file in new[] { "LICENSE", "README.md" })
      Assert(files.Contains(file));
}

void TestParseRegexCommand() {
   void Test(string text, string pattern, string replacement, string options) {
      var (p, r, o) = ParseRegexCommand(text);
      Assert(
         p == pattern && r == replacement && o == options,
         $"/{pattern}/{replacement}/{options} != /{p}/{r}/{o}");
   }

   Test(@"///", "", "", "");
   Test(@"/:-[/:-|/:-)", ":-[", ":-|", ":-)");
   Test("/\\n/\\n/", "\\n", "\n", "");
}

void TestSessionInit() {
   var fs = new MockFileSystem();
   var session = new Session("pwd", fs);
   Assert(session.File is { Path : "", Content : "", Modified : false });
}

void TestSessionWriteFile() {
   var fs = new MockFileSystem();
   var (pwd, content) = EncryptionTestData();
   var session = new Session(pwd, fs);
   session.Write("resource", content);
   var encrypted = fs.File.ReadAllBytes("resource");
   Assert(content == Decrypt(pwd, encrypted));
}

void TestSessionGetItems1() {
   var (pwd, _) = EncryptionTestData();
   var session = new Session(pwd, GetMockFs());
   Assert(session.GetItems().Count() == 0);
   Assert(session.GetItems(null).Count() == 0);
   Assert(session.GetItems(".").Count() == 0);
}

void TestSessionGetItems2() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   Assert(string.Join(";", session.GetItems()) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems(".")) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems(null)) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems("regular_dir")) == "regular_dir/encrypted");
   Assert(string.Join(";", session.GetItems(".hidden_dir")) == ".hidden_dir/encrypted");
}

void TestSessionGetEncryptedFilesRecursively1() {
   var (pwd, _) = EncryptionTestData();
   var session = new Session(pwd, GetMockFs());
   Assert(session.GetEncryptedFilesRecursively().Count() == 0);
   Assert(session.GetEncryptedFilesRecursively(null).Count() == 0);
   Assert(session.GetEncryptedFilesRecursively(".").Count() == 0);
}

void TestSessionGetEncryptedFilesRecursively2() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);

   Assert(string.Join(";", session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(".")) == "encrypted;regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(null)) == "encrypted;regular_dir/encrypted");

   Assert(string.Join(";", session.GetEncryptedFilesRecursively(includeHidden: true)) ==
      ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted");

   Assert(string.Join(";", session.GetEncryptedFilesRecursively("regular_dir")) == "regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(".hidden_dir")) == ".hidden_dir/encrypted");
}


void Tests() {
   Test(TestParseRegexCommand, nameof(TestParseRegexCommand));
   Test(TestEncryptDecryptRoundup, nameof(TestEncryptDecryptRoundup));
   Test(TestOpensslDecryptingEncryptedData, nameof(TestOpensslDecryptingEncryptedData));
   Test(TestDecryptingOpensslEncryptedData, nameof(TestDecryptingOpensslEncryptedData));
   Test(TestGetFilesRecursively, nameof(TestGetFilesRecursively));
   Test(TestParseRegexCommand, nameof(TestParseRegexCommand));
   Test(TestSessionInit, nameof(TestSessionInit));
   Test(TestSessionWriteFile, nameof(TestSessionWriteFile));
   Test(TestSessionGetItems1, nameof(TestSessionGetItems1));
   Test(TestSessionGetItems2, nameof(TestSessionGetItems2));
   Test(TestSessionGetEncryptedFilesRecursively1, nameof(TestSessionGetEncryptedFilesRecursively1));
   Test(TestSessionGetEncryptedFilesRecursively2, nameof(TestSessionGetEncryptedFilesRecursively2));
}

Tests();