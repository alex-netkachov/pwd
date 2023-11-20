using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.repository.implementation;

public sealed partial class Repository
   : IRepository,
      IDisposable
{
   private readonly ICipher _cipher;
   private readonly IEncoder _encoder;
   private readonly IFileSystem _fs;
   private readonly string _path;

   private readonly IRootFolder _root;

   private readonly ContentCache _cache;

   public Repository(
     IFileSystem fs,
     ICipher cipher,
     IEncoder encoder,
     string path)
   {
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

      return file;
   }

   public async Task<IFile> CreateFileAsync(
      Path path,
      CancellationToken token = default)
   {
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

      return file;
   }

   public IFolder CreateFolder(
      Path path)
   {
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

      return folder;
   }

   public async Task<IFolder> CreateFolderAsync(
      Path path,
      CancellationToken token = default)
   {
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

      return folder;
   }

   public void Delete(
      INamedItem item)
   {
      if (item is not File file)
         throw new InvalidOperationException("Cannot delete a folder.");

      var fsPath = Resolve(file.GetEncryptedPath());

      _fs.File.Delete(fsPath);
   }

   public IItem? Get(
      Path path)
   {
      if (path.Items.Count == 0)
         return Root;

      var container = (IContainer)Root;
      var (containerNames, itemName) = path.Tail();

      foreach (var containerName in containerNames.Items)
      {
         var items = container.List();
         var item = items.FirstOrDefault(item => item.Name.Equals(containerName));

         // specified path is not created
         if (item is not IContainer newContainer)
            return null;

         container = newContainer;
      }

      return container
         .List()
         .FirstOrDefault(item => item.Name.Equals(itemName));
   }

   public async Task<IItem?> GetAsync(
      Path path,
      CancellationToken token = default)
   {
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

   public void Move(
      IFile file,
      Path newPath)
   {
      /*
      var (pathItems, pathItem) = Tail(GetItems(path));
      if (!pathItem.Exists)
         throw new("The item does not exist.");

      var pathItemContainer = pathItems[^1];

      var (newPathItems, newPathItem) = Tail(GetItems(newPath));
      if (newPathItem.Exists)
         throw new("The item already exists.");

      var newPathItemContainer = CreateFolders(newPathItems);

      if (pathItem.IsFolder == true)
         _fs.Directory.Move(
            Resolve(pathItem.GetEncryptedPath()),
            Resolve(newPathItem.GetEncryptedPath()));
      else
         _fs.File.Move(
            Resolve(pathItem.GetEncryptedPath()),
            Resolve(newPathItem.GetEncryptedPath()));

      pathItemContainer.Items.Remove(pathItem);
      if (pathItem.Name != newPathItem.Name)
      {
         pathItem.Name = newPathItem.Name;
         pathItem.EncryptedName = newPathItem.EncryptedName;
      }

      newPathItemContainer.Items.Add(pathItem);
      */
      throw new NotImplementedException();
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
         if (!_cache.TryDecrypt(name.Value, out var decrypted))
            continue;

         var decryptedName = Name.Parse(_fs, decrypted);

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
   }

   public async IAsyncEnumerable<INamedItem> ListAsync(
      IItem item,
      ListOptions? options = default,
      [EnumeratorCancellation]
      CancellationToken token = default)
   {
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
   }

   public async Task<string> ReadAsync(
      IFile file,
      CancellationToken token = default)
   {
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