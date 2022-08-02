using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace pwd;

public interface IExporter
{
    Task Export(
        string path);
}

public sealed class Exporter
    : IExporter
{
    private readonly ICipher _contentCipher;
    private readonly IRepository _repository;
    private readonly IFileSystem _fs;

    public Exporter(
        ICipher contentCipher,
        IRepository repository,
        IFileSystem fs)
    {
        _contentCipher = contentCipher;
        _repository = repository;
        _fs = fs;
    }

    public async Task Export(
        string path)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.template.html");
        if (stream == null)
            return;
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();

        // for now, only the files in the main folder
        var files =
            _repository
                .List(".")
                .ToList();

        var script = "{ " + string.Join(",\n  ",
            files
                .OrderBy(item => item.Name)
                .Select(item =>
                {
                    var content = _fs.File.ReadAllBytes(item.EncryptedPath);
                    return (item.Name, Content: string.Join("", Convert.ToHexString(content)));
                })
                .Select(item => $"\"{item.Name}\" : \"{item.Content}\"")) + " }";
        var encrypted = Convert.ToHexString(await _contentCipher.EncryptAsync(script));
        var content = template.Replace("const data = await testData();", $"const data = '{encrypted}';");
        await _fs.File.WriteAllTextAsync(path, content);
    }
}