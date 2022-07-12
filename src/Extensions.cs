using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace pwd;

public static class Extensions
{
    public static Exception? Try(
        this Action action)
    {
        try
        {
            action();
            return default;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public static T? Apply<T>(
        this T? value,
        Action<T> action)
    {
        if (!EqualityComparer<T>.Default.Equals(value, default))
            action(value!);
        return value;
    }

    public static TResult? Map<TValue, TResult>(
        this TValue? value,
        Func<TValue, TResult?> func)
    {
        return EqualityComparer<TValue>.Default.Equals(value, default)
            ? default
            : func(value!);
    }

    public static byte[] ReadBytes(
        this Stream stream,
        int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset != length)
        {
            var read = stream.Read(buffer, offset, length - offset);
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
            return (path1 == "." ? "" : $"{path1}/") + path2;
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

    public static Exception? CheckYaml(
        this string text)
    {
        return new Action(() =>
        {
            using var input = new StringReader(text);
            new YamlStream().Load(input);
        }).Try();
    }
}