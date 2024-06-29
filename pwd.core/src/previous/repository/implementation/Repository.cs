using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using pwd.core.previous.repository.interfaces;
using IFile = pwd.core.previous.repository.interfaces.IFile;

namespace pwd.core.previous.repository.implementation;

public sealed partial class Repository
   : IRepository,
      IDisposable
{
   private readonly ICipher _cipher;
   private readonly IEncoder _encoder;
   private readonly ILogger _logger;
   private readonly IFileSystem _fs;
   private readonly string _path;

   private readonly IRootFolder _root;

   private readonly ContentCache _cache;

   public Repository(
      ILogger logger,
      IFileSystem fs,
      ICipher cipher,
      IEncoder encoder,
      string path)
   {
      _logger = logger;
      _fs = fs;
      _cipher = cipher;
      _path = path;
      _encoder = encoder;

      _root = new RootFolder(this);

      _cache =
         new ContentCache(
            fs,
            cipher,
            encoder,
            path);
   }

   public void Dispose()
   {
      // do nothing
   }

   public IRootFolder Root => _root;

   public IFile CreateFile(
      Path path)
   {
      Log($"start with '{path}'");

      var (containerPath, name) = path.Tail();

      if (name == null)
         throw new InvalidOperationException("Cannot create a file at the specified path.");

      var container =
         containerPath.Items.Count == 0
            ? Root
            : CreateFolder(containerPath) as IContainer;

      var encryptedName = Name.Parse(_fs, _cache.Encrypt(name.Value));

      var file = new File(this, name, encryptedName, container);

      var fsPath = Resolve(file.GetEncryptedPath());
      _fs.File.WriteAllText(fsPath, "");

      Log($"done");

      return file;
   }

   public async Task<IFile> CreateFileAsync(
      Path path,
      CancellationToken token = default)
   {
      Log($"start with '{path}'");

      var (containerPath, name) = path.Tail();

      if (name == null)
         throw new InvalidOperationException("Cannot create a file at the specified path.");

      var container =
         containerPath.Items.Count == 0
            ? Root
            : CreateFolder(containerPath) as IContainer;

      var encryptedName = Name.Parse(_fs, await _cache.EncryptAsync(name.Value, token));

      var file = new File(this, name, encryptedName, container);

      var fsPath = Resolve(file.GetEncryptedPath());
      await _fs.File.WriteAllTextAsync(fsPath, "", token);

      Log($"done");

      return file;
   }

   public IFolder CreateFolder(
      Path path)
   {
      Log($"start with '{path}'");

      var (containerPath, name) = path.Tail();

      if (name == null)
         throw new InvalidOperationException("Cannot create a folder at the specified path.");

      var container =
         containerPath.Items.Count == 0
            ? Root
            : CreateFolder(containerPath) as IContainer;

      var encrypted = _cache.Encrypt(name.Value);
      var encryptedName = Name.Parse(_fs, encrypted);

      var folder = new Folder(this, name, encryptedName, container);

      var fsPath = Resolve(folder.GetEncryptedPath());
      _fs.Directory.CreateDirectory(fsPath);

      Log($"done");

      return folder;
   }

   public async Task<IFolder> CreateFolderAsync(
      Path path,
      CancellationToken token = default)
   {
      Log($"start with '{path}'");

      var (containerPath, name) = path.Tail();

      if (name == null)
         throw new InvalidOperationException("Cannot create a folder at the specified path.");

      var container =
         containerPath.Items.Count == 0
            ? Root
            : await CreateFolderAsync(containerPath, token) as IContainer;

      var encrypted = await _cache.EncryptAsync(name.Value, token);
      var encryptedName = Name.Parse(_fs, encrypted);

      var folder = new Folder(this, name, encryptedName, container);

      var fsPath = Resolve(folder.GetEncryptedPath());
      _fs.Directory.CreateDirectory(fsPath);

      Log($"done");

      return folder;
   }

   public void Delete(
      INamedItem item)
   {
      Log($"start with '{item.GetPath()}'");

      if (item is not File file)
         throw new InvalidOperationException("Cannot delete a folder.");

      var fsPath = Resolve(file.GetEncryptedPath());

      _fs.File.Delete(fsPath);

      Log($"done");
   }

   public IItem? Get(
      Path path)
   {
      Log($"start with '{path}'");

      if (path.Items.Count == 0)
         return Root;

      var container = (IContainer)Root;
      var (containerNames, itemName) = path.Tail();

      foreach (var containerName in containerNames.Items)
      {
         var items = container.List(new ListOptions(false, true, true));
         var item = items.FirstOrDefault(item => item.Name.Equals(containerName));

         // specified path is not created
         if (item is not IContainer newContainer)
            return null;

         container = newContainer;
      }

      return container
         .List(new ListOptions(false, true, true))
         .FirstOrDefault(item => item.Name.Equals(itemName));
   }

   public async Task<IItem?> GetAsync(
      Path path,
      CancellationToken token = default)
   {
      Log($"start with '{path}'");

      if (path.Items.Count == 0)
         return Root;

      var container = (IContainer)Root;
      var (containerNames, itemName) = path.Tail();

      foreach (var containerName in containerNames.Items)
      {
         IContainer? newContainer = null;
         await foreach (var containerItem in container.ListAsync(token: token))
         {
            if (containerItem.Name.Equals(containerName))
            {
               newContainer = containerItem as IContainer;
               break;
            }
         }
         if (newContainer == null)
            return null;

         container = newContainer;
      }

      await foreach (var item in container.ListAsync(token: token))
      {
         if (item.Name.Equals(itemName))
            return item;
      }

      return null;
   }

   public async void Move(
      IFile file,
      Path newPath)
   {
      if (file == null)
         throw new ArgumentNullException(nameof(file));
      if (newPath == null)
         throw new ArgumentNullException(nameof(newPath));

      Log($"start with '{file.GetPath()}' and '{newPath}'");

      var sourcePath = ((File)file).GetEncryptedPath();

      var (pathItems, pathItem) = newPath.Tail();

      if (newPath.Items.Count == 0
          || pathItem == null)
      {
         // move the file to the root
         if (sourcePath.Items.Count == 1)
         {
            // it is already in the root folder
            return;
         }

         _fs.File.Move(
            Resolve(sourcePath),
            Resolve(Path.From(sourcePath.Items[^1])));

         return;
      }

      var item = Get(newPath);

      if (item is File existingFile)
      {
         await existingFile.WriteAsync(await file.ReadAsync());
         return;
      }

      var fileName = Name.Parse(_fs, _encoder.Encode(_cipher.Encrypt(pathItem.Value)));

      if (item is Folder existingFolder)
      {
         _fs.File.Move(
            Resolve(sourcePath),
            Resolve(existingFolder.GetEncryptedPath().Down(fileName)));

         return;
      }

      if (item == null)
      {
         if (pathItems.Items.Count == 0)
         {
            _fs.File.Move(
               Resolve(sourcePath),
               Resolve(Path.From(fileName)));

            return;
         }

         var newFolder = (Folder)CreateFolder(pathItems);

         _fs.File.Move(
            Resolve(sourcePath),
            Resolve(newFolder.GetEncryptedPath().Down(fileName)));

         return;
      }

      throw new InvalidOperationException("Cannot move the file.");
   }

   public bool TryParseName(
      string value,
      out Name? name)
   {
      return Name.TryParse(_fs, value, out name);
   }

   public bool TryParsePath(
      string value,
      out Path? path)
   {
      return Path.TryParse(_fs, value, out path);
   }

   public IEnumerable<INamedItem> List(
      IItem item,
      ListOptions? options = default)
   {
      Log($"start with '{item.GetPath()}'");

      var fsPath =
         item switch
         {
            RootFolder _ => _path,
            Folder folder => Resolve(folder.GetEncryptedPath()),
            _ => throw new InvalidOperationException("The item is not a folder.")
         };

      var fsItems =
         _fs.Directory
            .EnumerateFileSystemEntries(fsPath);

      var entries = new HashSet<string>();

      foreach (var fsItem in fsItems)
      {
         var fsFileName = _fs.Path.GetFileName(fsItem);

         var name = Name.Parse(_fs, fsFileName);
         if (!_cache.TryDecrypt(name.Value, out var decrypted))
            continue;

         var decryptedName = Name.Parse(_fs, decrypted);

         if (entries.Contains(decryptedName.Value))
            throw new Exception($"Duplicate entry '{decryptedName}'.");

         entries.Add(decryptedName.Value);

         if (_fs.File.Exists(fsItem))
         {
            var content = _fs.File.ReadAllText(fsItem);
            if (!_cache.TryDecrypt(content, out _))
               continue;

            var file = new File(this, decryptedName, name, (IContainer)item);
            if (!file.Name.IsDotted()
                || (options?.IncludeDottedFilesAndFolders == true))
            {
               yield return file;
            }
         }
         else if (_fs.Directory.Exists(fsItem))
         {
            var folder = new Folder(this, decryptedName, name, (IContainer)item);

            if (options?.IncludeFolders == true
               && (!folder.Name.IsDotted()
                   || (options?.IncludeDottedFilesAndFolders == true)))
            {
               yield return folder;
            }

            if (options?.Recursively == true)
            {
               foreach (var namedItem in List(folder, options))
                  yield return namedItem;
            }
         }
      }

      Log($"done");
   }

   public async IAsyncEnumerable<INamedItem> ListAsync(
      IItem item,
      ListOptions? options = default,
      [EnumeratorCancellation]
      CancellationToken token = default)
   {
      Log($"start with '{item.GetPath()}'");

      var fsPath =
         item switch
         {
            RootFolder _ => _path,
            Folder folder => Resolve(folder.GetEncryptedPath()),
            _ => throw new InvalidOperationException("The item is not a folder.")
         };

      var fsItems = _fs.Directory.EnumerateFileSystemEntries(fsPath);
      foreach (var fsItem in fsItems)
      {
         var name = Name.Parse(_fs, _fs.Path.GetFileName(fsItem));
         var (decrypted, decryptedValue) =
            await _cache.TryDecryptAsync(name.Value, token);
         if (!decrypted
             || decryptedValue == null)
         {
            continue;
         }

         var decryptedName = Name.Parse(_fs, decryptedValue);

         if (_fs.File.Exists(fsItem))
         {
            var file = new File(this, decryptedName, name, (IContainer)item);
            if (!file.Name.IsDotted()
                || (options?.IncludeDottedFilesAndFolders == true))
            {
               yield return file;
            }
         }
         else if (_fs.Directory.Exists(fsItem))
         {
            var folder = new Folder(this, decryptedName, name, (IContainer)item);

            if (options?.IncludeFolders == true
               && !folder.Name.IsDotted()
               || (options?.IncludeDottedFilesAndFolders == true))
            {
               yield return folder;
            }

            if (options?.Recursively == true)
            {
               foreach (var namedItem in List(folder, options))
                  yield return namedItem;
            }
         }
      }

      Log($"done");
   }

   public async Task<string> ReadAsync(
      IFile file,
      CancellationToken token = default)
   {
      Log($"start with '{file.GetPath()}'");

      if (file is not File repositoryfile)
         throw new InvalidOperationException("Cannot delete a folder.");

      var fsPath = Resolve(repositoryfile.GetEncryptedPath());

      var content = await _fs.File.ReadAllTextAsync(fsPath, token);
      return await _cache.DecryptAsync(content, token);
   }

   public async Task WriteAsync(
      IFile file,
      string text,
      CancellationToken token = default)
   {
      Log($"start with '{file.GetPath()}'");

      if (file is not File repositoryfile)
         throw new InvalidOperationException("Cannot write into a file.");

      var fsPath = Resolve(repositoryfile.GetEncryptedPath());

      var encrypted = await _cache.EncryptAsync(text, token);
      await _fs.File.WriteAllTextAsync(fsPath, encrypted, token);
   }

   public void Archive(
      IItem item)
   {
      throw new NotImplementedException();
   }

   private string Resolve(
      Path path)
   {
      return path.Resolve(_path);
   }

   private void Log(
      string message,
      [CallerMemberName] string memberName = "")
   {
      _logger.Info($"{nameof(Repository)}.{memberName}: {message}");
   }

   /*
      /// <summary>Subscribes for the events related to this repository file.</summary>
      IRepositoryUpdatesReader Subscribe();
      private ImmutableList<Channel<IRepositoryUpdate>> _subscribers =
         ImmutableList<Channel<IRepositoryUpdate>>.Empty;
      public IRepositoryUpdatesReader Subscribe()
      {
         var channel = Channel.CreateUnbounded<IRepositoryUpdate>();

         var reader = new RepositoryUpdatesReader(channel.Reader, () =>
         {
            while (true)
            {
               var initial = _subscribers;
               var updated = initial.Remove(channel);
               if (initial != Interlocked.CompareExchange(ref _subscribers, updated, initial))
                  continue;
               channel.Writer.Complete();
               break;
            }
         });

         while (true)
         {
            var initial = _subscribers;
            var updated = _subscribers.Add(channel);
            if (initial == Interlocked.CompareExchange(ref _subscribers, updated, initial))
               break;
         }

         return reader;
      }

      private void NotifySubscribers(
         IRepositoryUpdate update)
      {
         var subscribers = _subscribers;

         foreach (var subscriber in subscribers)
         {
            while (!subscriber.Writer.TryWrite(update))
            {
            }
         }
      }

      public void Modified()
      {
         NotifySubscribers(new Modified());
      }

      public void Deleted()
      {
         NotifySubscribers(new Deleted());
      }

      public void Dispose()
      {
      }
   */
}