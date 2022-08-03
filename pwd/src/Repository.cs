using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts;

namespace pwd;

public interface IRepositoryItem
{
   /// <summary>Plaintext name.</summary>
   public string Name { get; }

   /// <summary>Plaintext path.</summary>
   public string Path { get; }

   /// <summary>Encrypted name.</summary>
   public string EncryptedName { get; }

   /// <summary>Encrypted filenames path.</summary>
   public string EncryptedPath { get; }

   /// <summary>Whether the item is a folder or a file. Null when the item does not exist.</summary>
   public bool? IsFolder { get; }

   /// <summary>Whether the corresponding file exists.</summary>
   public bool Exists { get; }
}

public interface IRepository
{
   /// <summary>Moves the specified file into the ".archive" folder.</summary>
   /// <remarks>".archive" is a dotted folder so it will not be suggested or listed.</remarks>
   void Archive(
      string path);

   /// <summary>Deletes the file in the repository.</summary>
   void Delete(
      string path);

   /// <summary>Enumerates all encrypted files in a folder.</summary>
   IEnumerable<IRepositoryItem> List(
      string path,
      (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default);

   /// <summary>Reads from the encrypted file.</summary>
   Task<string> ReadAsync(
      string path);

   /// <summary>Renames (moves) the encrypted file.</summary>
   void Rename(
      string path,
      string newPath);

   /// <summary>Encrypts and writes the text into the file.</summary>
   Task WriteAsync(
      string path,
      string text);
}

public sealed class RepositoryItem
   : IRepositoryItem
{
   private RepositoryItem(
      string name,
      string encryptedName,
      IRepositoryItem? item = null)
   {
      Name = name;
      Path = Repository.PathCombine(item?.Path, name);
      EncryptedName = encryptedName;
      EncryptedPath = Repository.PathCombine(item?.EncryptedPath, encryptedName);
   }

   public List<RepositoryItem> Items { get; } = new();

   public string Name { get; set; }
   public string Path { get; private set; }
   public string EncryptedName { get; set; }
   public string EncryptedPath { get; private set; }
   public bool? IsFolder { get; set; }
   public bool Exists { get; set; }

   public RepositoryItem? Get(
      string name)
   {
      return Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
   }

   public RepositoryItem Create(
      string name,
      string encryptedName)
   {
      return new RepositoryItem(name, encryptedName, this);
   }

   public static RepositoryItem Root()
   {
      return new("", "")
      {
         IsFolder = true,
         Exists = true
      };
   }

   public void UpdatePaths()
   {
      foreach (var item in Items)
      {
         item.Path = Repository.PathCombine(Path, item.Name);
         item.EncryptedPath = Repository.PathCombine(EncryptedPath, item.EncryptedName);
         item.UpdatePaths();
      }
   }
}

public sealed class Repository
   : IRepository,
      IDisposable
{
   private readonly ICipher _contentCipher;
   private readonly IFileSystem _fs;
   private readonly ICipher _nameCipher;
   private readonly string _path;

   private readonly RepositoryItem _root;

   private TaskCompletionSource? _initialising;

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

      _root = RepositoryItem.Root();
   }

   public void Dispose()
   {
   }

   public void Archive(
      string path)
   {
      Rename(path, PathCombine(".archive", path));
   }

   public void Delete(
      string path)
   {
      var (items, item) = Tail(PathItems(path));
      var (_, container) = Tail(items);
      if (!item.Exists || item.IsFolder == false)
         return;
      container.Items.Remove(item);
      _fs.File.Delete(_fs.Path.Combine(_path, item.EncryptedPath));
   }

   public IEnumerable<IRepositoryItem> List(
      string path,
      (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
   {
      var names =
         string.IsNullOrEmpty(path) || path == "."
            ? Enumerable.Empty<string>()
            : path.Split('/');

      var item = _root;
      foreach (var name in names)
      {
         item = item.Get(name);
         if (item == null)
            return Enumerable.Empty<IRepositoryItem>();
      }

      return List(item, options);
   }

   public async Task<string> ReadAsync(
      string path)
   {
      var (_, file) = Tail(PathItems(path));
      if (!file.Exists)
         throw new("The file does not exist.");
      await using var stream = _fs.File.OpenRead(file.EncryptedPath);
      return await _contentCipher.DecryptStringAsync(stream);
   }

   public void Rename(
      string path,
      string newPath)
   {
      var (pathItems, pathItem) = Tail(PathItems(path));
      if (!pathItem.Exists)
         throw new("The item does not exist.");
      var pathItemContainer = pathItems[^1];

      var (newPathItems, newPathItem) = Tail(PathItems(newPath));
      if (newPathItem.Exists)
         throw new("The item already exists.");

      var newPathItemContainer = CreateFolders(newPathItems);

      if (pathItem.IsFolder == true)
         _fs.Directory.Move(
            _fs.Path.Combine(_path, pathItem.EncryptedPath),
            _fs.Path.Combine(_path, newPathItem.EncryptedPath));
      else
         _fs.File.Move(
            _fs.Path.Combine(_path, pathItem.EncryptedPath),
            _fs.Path.Combine(_path, newPathItem.EncryptedPath));

      pathItemContainer.Items.Remove(pathItem);
      if (pathItem.Name != newPathItem.Name)
      {
         pathItem.Name = newPathItem.Name;
         pathItem.EncryptedName = newPathItem.EncryptedName;
      }

      newPathItemContainer.Items.Add(pathItem);
      newPathItemContainer.UpdatePaths();
   }

   public async Task WriteAsync(
      string path,
      string text)
   {
      var (folders, file) = Tail(PathItems(path, false));

      if (folders.Count > 1)
         _fs.Directory.CreateDirectory(_fs.Path.GetDirectoryName(file.EncryptedPath));

      var folder = CreateFolders(folders);

      using var stream = new MemoryStream();
      await _contentCipher.EncryptAsync(text, stream);

      await _fs.File.WriteAllBytesAsync(file.EncryptedPath, stream.ToArray());

      if (!file.Exists)
      {
         file.Exists = true;
         folder.Items.Add(file);
      }
   }

   /// <summary>
   ///    Initialises the internal cache of the repository by iterating over the filesystem items in
   ///    the repository's folder.
   /// </summary>
   public Task Initialise(
      Action<string, string?, string?, string?>? loading = null)
   {
      if (_initialising != null)
         return _initialising.Task;

      var initialising = new TaskCompletionSource();

      if (null != Interlocked.CompareExchange(ref _initialising, initialising, null))
         return _initialising.Task;

      Task.Run(async () =>
      {
         var invalidFileNameChars = _fs.Path.GetInvalidFileNameChars();

         // items is a list of relative paths of all the files in folders in the repository's folder
         var items =
            EnumerateFilesystemItems(
                  _path,
                  (Recursively: true,
                     IncludeFolders: true,
                     IncludeDottedFilesAndFolders: true))
               .ToList();

         foreach (var item in items)
         {
            var itemPath = _fs.Path.Combine(_path, item);

            var isFolder = _fs.Directory.Exists(itemPath);

            var names = item.Split('/');

            var folders = isFolder ? names : names.Take(names.Length - 1).ToArray();
            var name = isFolder ? "" : names[^1];

            // create folders in the internal cache
            var repositoryItem = _root;
            foreach (var folder in folders)
            {
               var next =
                  repositoryItem.Items
                     .FirstOrDefault(value => value.EncryptedName.Equals(folder, StringComparison.Ordinal));

               // the folder is already in the cache
               if (next != null)
               {
                  repositoryItem = next;
                  continue;
               }

               // the folder is not in the cache, create it if it is encrypted
               var folderNameBytes = Encoding.UTF8.GetBytes(folder);
               if (!await _nameCipher.IsEncryptedAsync(folderNameBytes))
               {
                  repositoryItem = null;
                  break;
               }

               var folderName = await _nameCipher.DecryptStringAsync(folderNameBytes);
               if (folderName.IndexOfAny(invalidFileNameChars) != -1)
               {
                  repositoryItem = null;
                  break;
               }

               next = repositoryItem.Create(folderName, folder);
               next.IsFolder = true;
               next.Exists = true;
               repositoryItem.Items.Add(next);
               repositoryItem = next;
            }

            if (repositoryItem == null)
            {
               loading?.Invoke(item, null, "Invalid password.", null);
               continue;
            }

            // if the last part of the path is a file
            if (!isFolder)
            {
               if (!await _nameCipher.IsEncryptedAsync(Encoding.UTF8.GetBytes(name)))
                  continue;

               try
               {
                  await using var stream = _fs.File.OpenRead(itemPath);
                  if (!await _contentCipher.IsEncryptedAsync(stream))
                  {
                     loading?.Invoke(item, null, "Cannot decrypt a file.", null);
                     continue;
                  }
               }
               catch
               {
                  loading?.Invoke(item, null, "Cannot decrypt a file.", null);
                  continue;
               }

               string content;
               try
               {
                  await using var stream = _fs.File.OpenRead(itemPath);
                  content = await _contentCipher.DecryptStringAsync(stream);
               }
               catch
               {
                  loading?.Invoke(item, null, "Cannot decrypt a file.", null);
                  continue;
               }

               if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
               {
                  loading?.Invoke(item, null, "Invalid password.", null);
                  continue;
               }

               var fileName = await _nameCipher.DecryptStringAsync(Encoding.UTF8.GetBytes(name));
               var fileItem = repositoryItem.Create(fileName, name);
               fileItem.IsFolder = false;
               fileItem.Exists = true;
               repositoryItem.Items.Add(fileItem);

               if (Shared.CheckYaml(content) != null)
               {
                  loading?.Invoke(item, fileName, null, "Yaml formatting error.");
                  continue;
               }

               loading?.Invoke(item, fileName, null, null);
            }
         }

         _initialising.SetResult();
      });

      return _initialising.Task;
   }

   private static IEnumerable<IRepositoryItem> List(
      RepositoryItem item,
      (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
   {
      return item.Items
         .OrderBy(value => value.IsFolder)
         .ThenBy(value => value.Name)
         .SelectMany(value => value switch
         {
            {IsFolder: false} when value.Name[0] is not ('.' or '_') || options.IncludeDottedFilesAndFolders =>
               new[] {value},
            {IsFolder: true} when value.Name[0] is not ('.' or '_') || options.IncludeDottedFilesAndFolders =>
               (options.Recursively
                  ? List(value, options)
                  : Array.Empty<IRepositoryItem>())
               .Concat(
                  options.IncludeFolders
                     ? new[] {value}
                     : Array.Empty<IRepositoryItem>()),
            _ => Array.Empty<IRepositoryItem>()
         });
   }

   private IEnumerable<string> EnumerateFilesystemItems(
      string path,
      (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
   {
      bool IsDotted(IFileSystemInfo info)
      {
         return info.Name[0] is '.' or '_';
      }

      return _fs.Directory.Exists(path)
         ? _fs.DirectoryInfo.FromDirectoryName(path)
            .EnumerateFileSystemInfos()
            .OrderBy(info => info.Name)
            .SelectMany(info => info switch
            {
               IFileInfo file when !IsDotted(file) || options.IncludeDottedFilesAndFolders =>
                  new[] {file.Name},
               IDirectoryInfo dir when !IsDotted(dir) || options.IncludeDottedFilesAndFolders =>
                  (options.Recursively
                     ? EnumerateFilesystemItems(PathCombine(path, dir.Name), options)
                        .Select(item => PathCombine(dir.Name, item))
                     : Array.Empty<string>())
                  .Concat(
                     options.IncludeFolders
                        ? new[] {dir.Name}
                        : Array.Empty<string>()),
               _ => Array.Empty<string>()
            })
         : Enumerable.Empty<string>();
   }

   private RepositoryItem CreateFolders(
      IReadOnlyList<RepositoryItem> items)
   {
      var container = items[0];
      foreach (var item in items.Skip(0))
      {
         if (item.Exists)
            continue;
         if (item.IsFolder == true)
         {
            _fs.Directory.CreateDirectory(_fs.Path.Combine(_path, item.EncryptedPath));
            item.Exists = true;
         }

         container.Items.Add(item);
         container = item;
      }

      return container;
   }

   private IReadOnlyList<RepositoryItem> PathItems(
      string path,
      bool? isFolder = null)
   {
      var names = ParsePath(path);

      var list = new List<RepositoryItem>(names.Count);

      var item = _root;

      list.Add(item);

      foreach (var name in names)
      {
         if (!item.Exists)
            item.IsFolder = true;

         var next = item.Get(name);
         if (next == null)
         {
            var encryptedName =
               Encoding.UTF8.GetString(_nameCipher.Encrypt(name));

            next = item.Create(name, encryptedName);
         }

         item = next;
         list.Add(item);
      }

      if (isFolder != null)
         item.IsFolder = isFolder;

      return list;
   }

   private IReadOnlyList<string> ParsePath(
      string path)
   {
      if (string.IsNullOrEmpty(path))
         throw new ArgumentException("Invalid path.", nameof(path));

      var items =
         path.Split(
            _fs.Path.DirectorySeparatorChar,
            _fs.Path.AltDirectorySeparatorChar);

      var invalidNameChars = _fs.Path.GetInvalidFileNameChars();

      foreach (var item in items)
         if (string.IsNullOrEmpty(item) || item.IndexOfAny(invalidNameChars) != -1)
            throw new ArgumentException("Invalid path.", nameof(path));

      return items;
   }

   public static string PathCombine(
      string? path1,
      string? path2)
   {
      return (string.IsNullOrEmpty(path1) || path1 == "."
         ? path2
         : string.IsNullOrEmpty(path2) || path2 == "."
            ? path1
            : $"{path1}/{path2}") ?? "";
   }

   private static (IReadOnlyList<T> Body, T Tail) Tail<T>(
      IReadOnlyList<T> list)
   {
      return (list.Take(list.Count - 1).ToList(), list[^1]);
   }
}