using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using pwd.core;
using pwd.core.abstractions;

namespace pwd;

public interface IExporter
{
   Task Export(
      string path);
}

public interface IExporterFactory
{
   IExporter Create(
      ICipher cipher,
      IRepository repository);
}

public sealed class Exporter
   : IExporter
{
   private readonly ICipher _cipher;
   private readonly IFileSystem _fs;
   private readonly IRepository _repository;

   public Exporter(
      ICipher cipher,
      IRepository repository,
      IFileSystem fs)
   {
      _cipher = cipher;
      _repository = repository;
      _fs = fs;
   }

   public async Task Export(
      string path)
   {
      await using var stream =
         Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("pwd.res.template.html");

      if (stream == null)
         return;

      using var reader = new StreamReader(stream);

      var template = await reader.ReadToEndAsync();

      // for now, only the files in the main folder
      var files = _repository.List("/").ToList();

      var script = "{ " + string.Join(",\n  ",
         files
            .OrderBy(item => _repository.GetName(item))
            .Select(item =>
            {
               var content = _fs.File.ReadAllBytes(((FolderRepository)_repository).ToFilesystemPath(item));
               return (Name: _repository.GetName(item), Content: string.Join("", Convert.ToHexString(content)));
            })
            .Select(item => $"\"{item.Name}\" : \"{item.Content}\"")) + " }";
      var encrypted = Convert.ToHexString(await _cipher.EncryptAsync(script));
      var content = template.Replace("const data = await testData();", $"const data = '{encrypted}';");
      await _fs.File.WriteAllTextAsync(path, content);
   }
}

public sealed class ExporterFactory
   : IExporterFactory
{
   private readonly IFileSystem _fs;

   public ExporterFactory(
      IFileSystem fs)
   {
      _fs = fs;
   }

   public IExporter Create(
      ICipher cipher,
      IRepository repository)
   {
      return new Exporter(
         cipher,
         repository,
         _fs);
   }
}