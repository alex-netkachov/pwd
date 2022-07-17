using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace pwd.extensions;

// ReSharper disable once InconsistentNaming

public static class IO
{
    public static async Task<byte[]> ReadBytesAsync(
        this Stream stream,
        int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset != length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset));
            if (read == 0)
                throw new Exception("Reading from the stream failed.");
            offset += read;
        }

        return buffer;
    }

    public static IEnumerable<string> GetFiles(
        this IFileSystem fs,
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

        return fs.Directory.Exists(path)
            ? fs.DirectoryInfo.FromDirectoryName(path)
                .EnumerateFileSystemInfos()
                .OrderBy(info => info.Name)
                .SelectMany(info => info switch
                {
                    IFileInfo file when !IsDotted(file) || options.IncludeDottedFilesAndFolders =>
                        new[] {JoinPath(path, file.Name)},
                    IDirectoryInfo dir when !IsDotted(dir) || options.IncludeDottedFilesAndFolders =>
                        (options.Recursively
                            ? fs.GetFiles(JoinPath(path, dir.Name), options)
                            : Array.Empty<string>())
                        .Concat(
                            options.IncludeFolders
                                ? new[] {JoinPath(path, dir.Name)}
                                : Array.Empty<string>()),
                    _ => Array.Empty<string>()
                })
            : Enumerable.Empty<string>();
    }
    
    public static async Task<string> WriteToTempFile(
        this IFileSystem fs,
        string content)
    {
        var path = fs.Path.GetTempFileName();
        await fs.File.WriteAllTextAsync(path, content);
        return path;
    }
    
    public static async Task Write(
        this IFileSystem fs,
        string path,
        byte[] data)
    {
        var folder = fs.Path.GetDirectoryName(path);
        if (folder != "")
            fs.Directory.CreateDirectory(folder);
        await fs.File.WriteAllBytesAsync(path, data);
    }
    
    public static Task MoveFile(
        this IFileSystem fs,
        string sourceFileName,
        string destFileName)
    {
        var folder = fs.Path.GetDirectoryName(destFileName);
        if (folder != "")
            fs.Directory.CreateDirectory(folder);
        fs.File.Move(sourceFileName, destFileName);
        return Task.CompletedTask;
    }
}