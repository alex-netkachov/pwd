using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using pwd.core.abstractions;

namespace pwd.core;

public class FolderRepository
   : IRepository
{
   private readonly ILogger<FolderRepository> _logger;
   private readonly IFileSystem _fs;
   private readonly IStringEncoder _encoder;
   private readonly ICipher _cipher;
   private readonly string _path;

   private string _currentFolder = "/";

   public FolderRepository(
      ILogger<FolderRepository> logger,
      IFileSystem fs,
      CipherFactory cipherFactory,
      IStringEncoder encoder,
      string path,
      string password)
   {
      _logger = logger;
      _fs = fs;
      _encoder = encoder;
      _path = path;
      
      var fullPath = fs.Path.GetFullPath(path);
      var pwdJsonFilePath = fs.Path.Combine(fullPath, "pwd.json");

      var initialised =
         ConfigFile.TryGetCipherInitialisationData(
            fs,
            pwdJsonFilePath,
            out var initialisationData);

      if (!initialised)
         initialisationData = null;

      _cipher = cipherFactory(password, initialisationData);

      if (!initialised)
      {
         ConfigFile.WriteCipherInitialisationData(
            fs,
            pwdJsonFilePath,
            _cipher.GetInitialisationData());
      }
   }
   
   public string GetWorkingFolder()
   {
      return _currentFolder;
   }

   public void SetWorkingFolder(
      string path)
   {
      _currentFolder = GetFullPathInt(
         path,
         _currentFolder);
   }

   public string GetFolder(
      string path)
   {
      return PathUp(path).Folder;
   }

   public string? GetName(
      string path)
   {
      return PathUp(path).Name;
   }
   
   public string GetFullPath(
      string path)
   {
      return GetFullPathInt(path);
   }
   
   public string GetRelativePath(
      string path,
      string relativeToPath)
   {
      var fullPath = GetFullPathInt(path);
      var fullRelativeToPath = GetFullPathInt(relativeToPath);
      
      var fullPathParts =
         fullPath == "/"
            ? []
            : fullPath.Trim('/').Split('/');

      var fullRelativeToParts =
         fullRelativeToPath == "/"
            ? []
            : fullRelativeToPath.Trim('/').Split('/');

      var index = 0;
      while (index < fullPathParts.Length
             && index < fullRelativeToParts.Length
             && fullPathParts[index] == fullRelativeToParts[index])
      {
         index++;
      }
      
      var relativePathParts = new List<string>();
      for (var i = index; i < fullRelativeToParts.Length; i++)
         relativePathParts.Add("..");
      for (var i = index; i < fullPathParts.Length; i++)
         relativePathParts.Add(fullPathParts[i]);
      return string.Join("/", relativePathParts);
   }

   public void Write(
      string path,
      string value)
   {
      var (folder, name) = PathUp(path);
      if (name == null)
         throw new IOException("The specified path is a root folder path.");

      CreateFolder(folder);

      var localPath = ToFilesystemPath(path);
      var encrypted = _cipher.Encrypt(value);

      _fs.File.WriteAllBytes(
         localPath,
         encrypted);
   }

   public async Task WriteAsync(
      string path,
      string value)
   {
      _logger.LogInformation(
         "{Name}: writing {Path}",
         nameof(WriteAsync),
         path);
      
      var (folder, name) = PathUp(path);
      if (name == null)
         throw new IOException("The specified path is a root folder path.");

      CreateFolder(folder);

      var localPath = ToFilesystemPath(path);
      var encrypted = await _cipher.EncryptAsync(value);

      await _fs.File.WriteAllBytesAsync(
         localPath,
         encrypted);
   }
   
   public string ReadText(
      string path)
   {
      var localPath = ToFilesystemPath(path);
      var content = _fs.File.ReadAllBytes(localPath);
      using var input = new MemoryStream(content);
      using var output = new MemoryStream();
      _cipher.Decrypt(input, output);
      return Encoding.UTF8.GetString(output.ToArray());
   }

   public async Task<string> ReadTextAsync(
      string path)
   {
      var localPath = ToFilesystemPath(path);
      var content = await _fs.File.ReadAllBytesAsync(localPath);
      using var input = new MemoryStream(content);
      using var output = new MemoryStream();
      await _cipher.DecryptAsync(input, output, CancellationToken.None);
      return Encoding.UTF8.GetString(output.ToArray());
   }

   public void CreateFolder(
      string path)
   {
      if (path == "/")
         return;

      var localPath = ToFilesystemPath(path);
      _fs.Directory.CreateDirectory(localPath);
   }

   public void Delete(
      string path)
   {
      var localPath = ToFilesystemPath(path);
      _fs.File.Delete(localPath);
   }

   public void Move(
      string path,
      string newPath)
   {
      Log($"start with '{path}' and '{newPath}'");

      if (!FileExist(path))
      {
         throw new IOException(
            "The location does not correspond to a file.");
      }

      var (_, locationName) = PathUp(path);
      if (locationName == null)
      {
         throw new IOException(
            "The location does not correspond to a file.");
      }

      if (FolderExist(newPath))
      {
         var newPathUpdated = PathDown(newPath, locationName);
         if (FolderExist(newPathUpdated))
         {
            throw new IOException(
               "Cannot overwrite a folder.");
         }

         _fs.File.Move(
            ToFilesystemPath(path),
            ToFilesystemPath(newPathUpdated));

         return;
      }

      if (FileExist(newPath))
      {
         _fs.File.Move(
            ToFilesystemPath(path),
            ToFilesystemPath(newPath));
         return;
      }

      var (newPathFolder, _) = PathUp(newPath);

      try
      {
         _fs.Directory.CreateDirectory(
            ToFilesystemPath(newPathFolder));
      }
      catch
      {
         throw new IOException(
            "Cannot create a folder.");
      }
      
      _fs.File.Move(
         ToFilesystemPath(path),
         ToFilesystemPath(newPath));
   }

   public IEnumerable<string> List(
      string path,
      ListOptions? options = null)
   {
      Log($"start with '{path}'");
      
      var listOptions = options ?? ListOptions.Default;

      var localPath = ToFilesystemPath(path);

      if (!_fs.Directory.Exists(localPath))
         throw new InvalidOperationException($"'{path}' is not a folder.");

      var fsItems =
         _fs.Directory
            .EnumerateFileSystemEntries(localPath);

      foreach (var fsItem in fsItems)
      {
         var fsFileName = _fs.Path.GetFileName(fsItem);

         if (!TryDecryptName(fsFileName, out var value)
             || value == null)
         {
            continue;
         }

         var decryptedName = value;

         if (_fs.File.Exists(fsItem))
         {
            if (!IsDotted(decryptedName)
                || listOptions.IncludeDottedFilesAndFolders)
            {
               yield return PathDown(path, decryptedName);
            }
         }
         else if (_fs.Directory.Exists(fsItem))
         {
            var folderLocation = PathDown(path, decryptedName);

            if (listOptions.IncludeFolders
                && (!IsDotted(decryptedName)
                    || listOptions.IncludeDottedFilesAndFolders))
            {
               yield return folderLocation;
            }

            if (listOptions.Recursively)
            {
               foreach (var namedItem in List(folderLocation, listOptions))
                  yield return namedItem;
            }
         }
      }

      Log("done");
   }

   public bool FileExist(
      string path)
   {
      var localPath = ToFilesystemPath(path);
      return _fs.File.Exists(localPath);
   }

   public bool FolderExist(
      string path)
   {
      var localPath = ToFilesystemPath(path);
      return _fs.Directory.Exists(localPath);
   }

   public string ToFilesystemPath(
      string path)
   {
      var fullFilesystemPath = _fs.Path.GetFullPath(_path);

      var fullPath = GetFullPathInt(path);

      if (fullPath == "/")
         return fullFilesystemPath;

      var relativePath = ToFilesystemRelativePath(fullPath);

      return _fs.Path.Combine(
         fullFilesystemPath,
         relativePath);
   }
   
   private string ToFilesystemRelativePath(
      string path)
   {
      var items = GetFullPathInt(path).Trim('/').Split('/');

      if (items.Length == 0)
         return ".";

      return Path.Join(
         items
            .Select(item => _encoder.Encode(_cipher.Encrypt(item)))
            .ToArray());
   }

   private bool TryDecryptName(
      string value,
      out string? name)
   {
      var ok = _encoder.TryDecode(value, out var decoded);
      if (!ok || decoded == null)
      {
         name = null;
         return false;
      }

      ok = _cipher.TryDecryptString(decoded, out var content);
      if (!ok || content == null)
      {
         name = null;
         return false;
      }

      name = content;
      return true;
   }
   
   private void Log(
      string message,
      [CallerMemberName] string memberName = "")
   {
      _logger.LogInformation(
         "{Member}: {Message}",
         memberName,
         message);
   }
   
   private string GetFullPathInt(
      string path,
      string? location = null)
   {
      // - removes leading and trailing slashes
      // - collapses multiple slashes
      // - removes dots as points to itself
      // - collapses double dots as points to parent
      // - does not fail when the path goes out of the root
      // - will be resolved from the location if the path does not start with "/"

      location =
         location switch
         {
            null => _currentFolder,
            "/" => "/",
            _ => GetFullPathInt(location, "/")
         };

      var absolute = path.Length > 0 && path[0] == '/';

      var parts = path.Trim('/').Split('/');

      if (!absolute)
         parts = location.Trim('/').Split('/').Concat(parts).ToArray();

      var stack = new Stack<string>();

      foreach (var item in parts)
      {
         switch (item)
         {
            case "." or "":
               continue;
            case "..":
               stack.TryPop(out _);
               continue;
            default:
               stack.Push(item);
               break;
         }
      }

      return "/" + string.Join("/", stack.Reverse());
   }

   private (string Folder, string? Name) PathUp(
      string path,
      string? location = null)
   {
      var fullPath = GetFullPathInt(path, location);
      if (fullPath == "/")
         return ("/", null);
      var separatorPosition = fullPath.LastIndexOf('/');
      var name = fullPath[(separatorPosition + 1)..];

      return separatorPosition == 0
         ? ("/", name)
         : (fullPath[..separatorPosition], name);
   }

   private string PathDown(
      string path,
      string name,
      string? location = null)
   {
      var fullPath = GetFullPathInt(path, location);
      return fullPath == "/"
         ? "/" + name
         : fullPath + "/" + name;
   }

   private static bool IsDotted(
      string name)
   {
      return name.Length > 0 && name[0] is '.' or '_';
   }
   
   public static IRepository Open(
      string path,
      string password)
   {
      return new FolderRepository(
         NullLogger<FolderRepository>.Instance,
         new FileSystem(),
         (pwd, data) => new AesCipher(pwd, data),
         Base64Url.Instance,
         path,
         password);
   }
}