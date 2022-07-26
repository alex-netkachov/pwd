using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwd;

public record Name(string Value);

public record Path(string Value);

public interface IRepository
{
    Task Archive(
        Name name);

    void Delete(
        Name name);
    
    void Delete(
        Path path);

    Task<Path> ExportToTempFile(
        string content);

    Task<IEnumerable<(Path Path, Name Name)>> GetEncryptedFilesRecursively(
        Path? path = null,
        bool includeHidden = false);

    IEnumerable<Path> GetFiles(
        Path path,
        (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default);

    Task<IEnumerable<Path>> GetItems(
        Path? path = null);

    public Task<Name?> GetName(
        Path path);

    Task<bool> IsFileEncrypted(
        Path path);

    Task<string> ReadAsync(
        Name name);

    Task<string> ReadAsync(
        Path path);

    Task<string> ReadTextAsync(
        Path path);

    Task RenameAsync(
        Name name,
        Name newName);

    Task WriteEncryptedAsync(
        Name name,
        string text);
}

public sealed class Repository
    : IRepository,
        IDisposable
{
    private readonly IFileSystem _fs;
    private readonly ICipher _nameCipher;
    private readonly ICipher _contentCipher;
    private readonly string _path;

    public Repository(
        IFileSystem fs,
        ICipher nameCipher,
        ICipher contentCipher,
        string path)
    {
        _fs = fs;
        _nameCipher = nameCipher;
        _contentCipher = contentCipher;
        _path = path;
    }

    public void Dispose()
    {
    }
    
    public async Task Archive(
        string name)
    {
        throw new NotImplementedException();
    }

    public void Delete(
        string path)
    {
        _fs.File.Delete(path);
    }
    
    public async Task<string> ExportToTempFile(
        string content)
    {
        var path = _fs.Path.GetTempFileName();
        await _fs.File.WriteAllTextAsync(path, content);
        return path;
    }
    
    public IEnumerable<string> GetFiles(
        string path,
        (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
    {
        string JoinPath(
            string path1,
            string path2)
        {
            return path1 == "."
                ? path2
                : $"{path1}/{path2}";
        }

        bool IsDotted(
            IFileSystemInfo info)
        {
            var name = info.Name;
            return name.StartsWith('.') || name.StartsWith('_');
        }

        return _fs.Directory.Exists(path)
            ? _fs.DirectoryInfo.FromDirectoryName(path)
                .EnumerateFileSystemInfos()
                .OrderBy(info => info.Name)
                .SelectMany(info => info switch
                {
                    IFileInfo file when !IsDotted(file) || options.IncludeDottedFilesAndFolders =>
                        new[] {JoinPath(path, file.Name)},
                    IDirectoryInfo dir when !IsDotted(dir) || options.IncludeDottedFilesAndFolders =>
                        (options.Recursively
                            ? GetFiles(JoinPath(path, dir.Name), options)
                            : Array.Empty<string>())
                        .Concat(
                            options.IncludeFolders
                                ? new[] {JoinPath(path, dir.Name)}
                                : Array.Empty<string>()),
                    _ => Array.Empty<string>()
                })
            : Enumerable.Empty<string>();
    }
    
    public Task<string> Read(
        string path)
    {
        return _contentCipher.DecryptStringAsync(_fs.File.OpenRead(path));
    }

    public async Task<string> ReadAllTextAsync(
        string path)
    {
        return await _fs.File.ReadAllTextAsync(path);
    }
    
    public async Task Rename(
        string name,
        string newName)
    {
        var originalPath = _fs.Path.Combine(_path, name);

        var encryptedName = Encoding.UTF8.GetString(await _nameCipher.EncryptAsync(newName));

        var path = _fs.Path.Combine(_fs.Path.GetDirectoryName(originalPath), encryptedName);

        _fs.File.Move(originalPath, path);
    }
    
    public async Task Write(
        string path,
        string text)
    {
        using var stream = new MemoryStream();
        await _contentCipher.EncryptAsync(text, stream);

        var folder = _fs.Path.GetDirectoryName(path);
        if (folder != "")
            _fs.Directory.CreateDirectory(folder);

        await _fs.File.WriteAllBytesAsync(path, stream.ToArray());
    }

    public Task<string> GetName(
        string path)
    {
        return _nameCipher.DecryptStringAsync(Encoding.UTF8.GetBytes(_fs.Path.GetFileName(path)));
    }

    public async Task<bool> IsFileEncrypted(
        string path)
    {
        await using var stream = _fs.File.OpenRead(path);
        return await _contentCipher.IsEncryptedAsync(stream);
    }
    
    public async Task<IEnumerable<(string Path, string Name)>> GetItems(
        string? path = null)
    {
        var items = GetFiles(path ?? ".", (false, true, false));
        var result = new List<(string, string)>();
        foreach (var item in items)
            if (_fs.Directory.Exists(item) || await IsFileEncrypted(item))
            {
                var bytes = Encoding.UTF8.GetBytes(_fs.Path.GetFileName(item));
                var name =
                    await _nameCipher.IsEncryptedAsync(bytes)
                        ? await _nameCipher.DecryptStringAsync(bytes)
                        : item;
                result.Add((item, name));
            }

        return result;
    }

    public async Task<IEnumerable<(string Path, string Name)>> GetEncryptedFilesRecursively(
        string? path = null,
        bool includeHidden = false)
    {
        var files = GetFiles(path ?? ".", (true, false, includeHidden));
        var result = new List<(string, string)>();
        foreach (var file in files)
            if (await IsFileEncrypted(file))
            {
                var fileName = _fs.Path.GetFileName(file);
                var data = Encoding.UTF8.GetBytes(fileName);
                using var testStream = new MemoryStream(data);
                var encrypted = await _nameCipher.IsEncryptedAsync(testStream);
                if (encrypted)
                {
                    using var nameStream = new MemoryStream(data);
                    var name = await _nameCipher.DecryptStringAsync(nameStream);
                    result.Add((file, name));
                }
                else
                    result.Add((file, fileName));
            }
        return result;
    }
}