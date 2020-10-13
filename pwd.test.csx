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
   Assert(session is { Path : null, Content : null, Modified : false });
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
   var session = new Session(pwd, new MockFileSystem());
   Assert(session.GetItems().Count() == 0);
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
}

Tests();