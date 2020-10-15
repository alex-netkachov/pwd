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
    }.FirstOrDefault(System.IO.File.Exists) ?? "openssl";

void Test_EncryptDecryptRoundup() {
   var (password, text) = EncryptionTestData();
   var encrypted = Encrypt(password, text);
   var decrypted = Decrypt(password, encrypted);
   Assert(text == decrypted);
}

void Test_DecryptingOpensslEncryptedData() {
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
      using var stream = System.IO.File.OpenWrite(path);
      process.StandardOutput.BaseStream.CopyTo(stream);
   }

   var (password, text) = EncryptionTestData();

   var path = Path.GetTempFileName();
   OpensslEncrypt(path, password, text);
   var decrypted = Decrypt(password, System.IO.File.ReadAllBytes(path));
   System.IO.File.Delete(path);
   Assert(text == decrypted);
}

void Test_OpensslDecryptingEncryptedData() {
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
      var encrypted = System.IO.File.ReadAllBytes(path);
      writer.Write(encrypted, 0, encrypted.Length);
      writer.Close();
      return process.StandardOutput.ReadToEnd();
   }

   var (password, text) = EncryptionTestData();

   var path = Path.GetTempFileName();
   System.IO.File.WriteAllBytes(path, Encrypt(password, text));
   var decrypted = OpensslDecrypt(path, password);
   System.IO.File.Delete(path);
   Assert(text == decrypted);
}

void Test_GetFilesRecursively() {
   var files = GetFiles(new FileSystem(), ".", recursively: true).ToList();
   foreach (var file in new[] { "LICENSE", "README.md" })
      Assert(files.Contains(file));
}

void Test_ParseRegexCommand() {
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

void Test_Session_Ctor() {
   var fs = new MockFileSystem();
   var session = new Session("pwd", fs);
   Assert(session.File == null);
}

void Test_Session_WriteFile() {
   var fs = new MockFileSystem();
   var (pwd, content) = EncryptionTestData();
   var session = new Session(pwd, fs);
   session.Write("resource", content);
   var encrypted = fs.File.ReadAllBytes("resource");
   Assert(content == Decrypt(pwd, encrypted));
}

void Test_Session_GetItems1() {
   var (pwd, _) = EncryptionTestData();
   var session = new Session(pwd, GetMockFs());
   Assert(session.GetItems().Count() == 0);
   Assert(session.GetItems(null).Count() == 0);
   Assert(session.GetItems(".").Count() == 0);
}

void Test_Session_GetItems2() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   Assert(string.Join(";", session.GetItems()) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems(".")) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems(null)) == "encrypted;regular_dir");
   Assert(string.Join(";", session.GetItems("regular_dir")) == "regular_dir/encrypted");
   Assert(string.Join(";", session.GetItems(".hidden_dir")) == ".hidden_dir/encrypted");
}

void Test_Session_GetEncryptedFilesRecursively1() {
   var (pwd, _) = EncryptionTestData();
   var session = new Session(pwd, GetMockFs());
   Assert(session.GetEncryptedFilesRecursively().Count() == 0);
   Assert(session.GetEncryptedFilesRecursively(null).Count() == 0);
   Assert(session.GetEncryptedFilesRecursively(".").Count() == 0);
}

void Test_Session_GetEncryptedFilesRecursively2() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);

   Assert(string.Join(";", session.GetEncryptedFilesRecursively()) == "encrypted;regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(".")) == "encrypted;regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(null)) == "encrypted;regular_dir/encrypted");

   Assert(string.Join(";", session.GetEncryptedFilesRecursively(includeHidden: true)) ==
      ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted");

   Assert(string.Join(";", session.GetEncryptedFilesRecursively("regular_dir")) ==
      "regular_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively("regular_dir", includeHidden: true)) ==
      "regular_dir/.hidden_encrypted;regular_dir/encrypted");

   Assert(string.Join(";", session.GetEncryptedFilesRecursively(".hidden_dir")) ==
      ".hidden_dir/encrypted");
   Assert(string.Join(";", session.GetEncryptedFilesRecursively(".hidden_dir", includeHidden: true)) ==
      ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted");
}

void Test_Session_Read() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);

   var files = new [] {
      "encrypted",
      ".hidden_encrypted",
      "regular_dir/encrypted",
      "regular_dir/.hidden_encrypted",
      ".hidden_dir/encrypted",
      ".hidden_dir/.hidden_encrypted"
   };

   foreach (var file in files)
      Assert(session.Read(file) == text);
}

void Test_Session_Write() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   session.Write("test", text);
   Assert(fs.File.Exists("test"));
   Assert(Decrypt(pwd, fs.File.ReadAllBytes("test")) == text);
}

void Test_Session_Write_Clears_midification_flag_for_open_file() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   session.Open("encrypted").Update("test");
   Assert(session.File.Modified);
   session.Write("encrypted", text);
   Assert(!session.File.Modified);
}

void Test_File_ExportContentToTempFile() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", text);
   var path = file.ExportContentToTempFile();
   Assert(fs.File.Exists(path));
   Assert(fs.File.ReadAllText(path) == text);
}

void Test_File_ReadFromFile1() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", "");
   file.ReadFromFile("file");
   Assert(file.Content == text);
   Assert(file.Modified);
}

void Test_File_ReadFromFile2() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", text);
   file.ReadFromFile("file");
   Assert(file.Content == text);
   Assert(!file.Modified);
}

void Test_File_Save() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", "test");
   file.Update(text);
   Assert(file.Modified);
   file.Save();
   Assert(!file.Modified);
   Assert(fs.File.Exists("test"));
   Assert(Decrypt(pwd, fs.File.ReadAllBytes(file.Path)) == text);
}

void Test_File_Rename() {
   var (pwd, text) = EncryptionTestData();
   var fs = FileLayout1(GetMockFs());
   var session = new Session(pwd, fs);
   var file = session.Open("encrypted");
   file.Rename("encrypted.test");
   Assert(fs.File.Exists("encrypted.test"));
   file.Rename("regular_dir/encrypted.test");
   Assert(fs.File.Exists("regular_dir/encrypted.test"));
}

void Test_File_Replace() {
   var (pwd, text) = EncryptionTestData();
   var fs = GetMockFs();
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", "text");
   Assert(!file.Modified);
   file.Replace($"/1111/{text}/i");
   Assert(!file.Modified);
   file.Replace($"/TEXT/{text}/i");
   Assert(file.Modified);
   Assert(file.Content == text);
}

void Test_File_Update() {
   var (pwd, text) = EncryptionTestData();
   var fs = GetMockFs();
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", "text");
   Assert(!file.Modified);
   file.Update("text");
   Assert(!file.Modified);
   file.Update(text);
   Assert(file.Modified);
   Assert(file.Content == text);
}

void Test_File_Field() {
   var (pwd, text) = EncryptionTestData();
   var fs = GetMockFs();
   var session = new Session(pwd, fs);
   var file = new File(fs, session, "test", "a: 1\nab: 2");
   Assert(file.Field("a") == "1");
   Assert(file.Field("ab") == "2");
}

void Tests() {
   Test(Test_ParseRegexCommand, nameof(Test_ParseRegexCommand));
   Test(Test_EncryptDecryptRoundup, nameof(Test_EncryptDecryptRoundup));
   Test(Test_OpensslDecryptingEncryptedData, nameof(Test_OpensslDecryptingEncryptedData));
   Test(Test_DecryptingOpensslEncryptedData, nameof(Test_DecryptingOpensslEncryptedData));
   Test(Test_GetFilesRecursively, nameof(Test_GetFilesRecursively));
   Test(Test_Session_Ctor, nameof(Test_Session_Ctor));
   Test(Test_Session_WriteFile, nameof(Test_Session_WriteFile));
   Test(Test_Session_GetItems1, nameof(Test_Session_GetItems1));
   Test(Test_Session_GetItems2, nameof(Test_Session_GetItems2));
   Test(Test_Session_GetEncryptedFilesRecursively1, nameof(Test_Session_GetEncryptedFilesRecursively1));
   Test(Test_Session_GetEncryptedFilesRecursively2, nameof(Test_Session_GetEncryptedFilesRecursively2));
   Test(Test_Session_Read, nameof(Test_Session_Read));
   Test(Test_Session_Write, nameof(Test_Session_Write));
   Test(Test_Session_Write_Clears_midification_flag_for_open_file, nameof(Test_Session_Write_Clears_midification_flag_for_open_file));
   Test(Test_File_ExportContentToTempFile, nameof(Test_File_ExportContentToTempFile));
   Test(Test_File_ReadFromFile1, nameof(Test_File_ReadFromFile1));
   Test(Test_File_ReadFromFile2, nameof(Test_File_ReadFromFile2));
   Test(Test_File_Save, nameof(Test_File_Save));
   Test(Test_File_Rename, nameof(Test_File_Rename));
   Test(Test_File_Replace, nameof(Test_File_Replace));
   Test(Test_File_Update, nameof(Test_File_Update));
   Test(Test_File_Field, nameof(Test_File_Field));
}

Tests();