using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using PasswordGenerator;

namespace pwd.contexts;

public sealed class Session
    : IContext
{
    private readonly ICipher _cipher;
    private readonly Timer _cleaner;
    private readonly IFileSystem _fs;
    private readonly IView _view;

    public Session(
        ICipher cipher,
        IFileSystem fs,
        IView view)
    {
        _fs = fs;
        _view = view;
        _cleaner = new(_ => CopyText());
        _cipher = cipher;
    }

    public File? File { get; private set; }

    public void Close()
    {
        File = null;
    }

    public void Default(
        string input)
    {
        var names = GetEncryptedFilesRecursively()
            .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var name =
            names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) ??
            (names.Count == 1 && input != "" ? names[0] : default);

        if (name.Map(value => Open(value)) != null)
            return;

        _view.Write(string.Join("", names.Select(value => $"{value}\n")));
    }

    public IEnumerable<string> GetItems(string? path = null)
    {
        return _fs.GetFiles(path ?? ".", (false, true, false))
            .Where(item => !_fs.File.Exists(item) || IsFileEncrypted(item));
    }

    public IEnumerable<string> GetEncryptedFilesRecursively(string? path = null, bool includeHidden = false)
    {
        return _fs.GetFiles(path ?? ".", (true, false, includeHidden))
            .Where(IsFileEncrypted);
    }

    private bool IsFileEncrypted(string path)
    {
        return null == new Action(() =>
        {
            using var stream = _fs.File.OpenRead(path);
            // openssl adds Salted__ at the beginning of a file, let's use to to check whether it is encrypted or not
            _ = "Salted__" == Encoding.ASCII.GetString(stream.ReadBytes(8))
                ? default(object)
                : throw new Exception();
        }).Try();
    }

    public string Read(string path)
    {
        return _cipher.Decrypt(_fs.File.ReadAllBytes(path));
    }

    public Session Write(
        string path,
        string content)
    {
        var folder = _fs.Path.GetDirectoryName(path);
        if (folder != "") _fs.Directory.CreateDirectory(folder);
        _fs.File.WriteAllBytes(path, _cipher.Encrypt(content));
        if (path == File?.Path)
            Open(path);
        return this;
    }

    public File? Open(
        string path)
    {
        File = _fs.File.Exists(path) && IsFileEncrypted(path)
            ? new File(_fs, _view, this, path, Read(path))
            : null;
        File?.Print();
        return File;
    }

    public Session Check()
    {
        var names = GetEncryptedFilesRecursively(includeHidden: true).ToList();
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        foreach (var name in names)
        {
            string? content;
            try
            {
                content = _cipher.Decrypt(_fs.File.ReadAllBytes(name));
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch
            {
                content = default;
            }

            if (content == default)
            {
                wrongPassword.Add(name);
                Console.Write('*');
            }
            else if (content.CheckYaml() != null)
            {
                notYaml.Add(name);
                Console.Write('+');
            }
            else
            {
                Console.Write('.');
            }
        }

        if (names.Any()) Console.WriteLine();

        if (wrongPassword.Count > 0)
        {
            var more = wrongPassword.Count > 3 ? ", ..." : "";
            var failuresText = string.Join(", ", wrongPassword.Take(Math.Min(3, wrongPassword.Count)));
            throw new Exception($"Integrity check failed for: {failuresText}{more}");
        }

        if (notYaml.Count > 0)
            Console.Error.WriteLine($"YAML check failed for: {string.Join(", ", notYaml)}");

        return this;
    }

    public Session CopyText(string? text = null)
    {
        var process = default(Process);
        text.Apply(_ => _cleaner.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan));
        new Action(() =>
                process = Process.Start(new ProcessStartInfo("clip.exe") {RedirectStandardInput = true}))
            .Try()
            .Map(_ => new Action(() =>
                process = Process.Start(new ProcessStartInfo("pbcopy") {RedirectStandardInput = true})).Try())
            .Map(_ => new Action(() =>
                process = Process.Start(new ProcessStartInfo("xsel") {RedirectStandardInput = true})).Try())
            .Apply(e => Console.WriteLine($"Cannot copy to the clipboard. Reason: {e.Message}"));
        process?.StandardInput.Apply(stdin => stdin.Write(text ?? "")).Apply(stdin => stdin.Close());
        return this;
    }

    public void Export()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.template.html");
        if (stream == null)
            return;
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();
        var script = string.Join(",\n  ",
            Directory.GetFiles(".")
                .Select(file => (Path.GetFileName(file), System.IO.File.ReadAllBytes(file)))
                .Where(item =>
                {
                    if (item.Item2.Length < 16) return false;
                    var prefix = new byte[8];
                    Array.Copy(item.Item2, 0, prefix, 0, prefix.Length);
                    var text = Encoding.ASCII.GetString(prefix);
                    return text == "Salted__";
                })
                .OrderBy(item => item.Item1)
                .Select(item => (item.Item1, string.Join("", item.Item2.Select(value => value.ToString("x2")))))
                .Select(item => $"'{item.Item1}' : '{item.Item2}'"));
        var content = template.Replace("const files = { };", $"const files = {{\n  {script}\n}};");
        System.IO.File.WriteAllText("_index.html", content);
    }

    public void Add(
        string path)
    {
        var content = new StringBuilder();
        for (string? line; "" != (line = Console.ReadLine());)
            content.AppendLine((line ?? "").Replace("***", new Password().Next()));
        Write(path, content.ToString()).Open(path);
    }
}