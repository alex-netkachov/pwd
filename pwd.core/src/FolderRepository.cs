using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using pwd.core.abstractions;

namespace pwd.core;

public class FolderRepository : IRepository
{
   private readonly ILogger<FolderRepository> _logger;
   private readonly IFileSystem _fs;
   private readonly IStringEncoder _encoder;
   private readonly ICipher _cipher;
   private readonly string _path;

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

   public Location Root => new(this, []);

   public void Write(
      Location location,
      string value)
   {
      var (container, _) = location.Up();
      CreateFolder(container);

      _fs.File.WriteAllBytes(
         ToFilesystemPath(location),
         _cipher.Encrypt(value));
   }

   public async Task WriteAsync(
      Location location,
      string value)
   {
      var (container, _) = location.Up();
      CreateFolder(container);

      var localPath = ToFilesystemPath(location);
      var encrypted = await _cipher.EncryptAsync(value);

      await _fs.File.WriteAllBytesAsync(
         localPath,
         encrypted);
   }
   
   public string Read(
      Location location)
   {
      var localPath = ToFilesystemPath(location);
      var content = _fs.File.ReadAllBytes(localPath);
      using var input = new MemoryStream(content);
      using var output = new MemoryStream();
      _cipher.Decrypt(input, output);
      return Encoding.UTF8.GetString(output.ToArray());
   }

   public async Task<string> ReadAsync(
      Location location)
   {
      var localPath = ToFilesystemPath(location);
      var content = await _fs.File.ReadAllBytesAsync(localPath);
      using var input = new MemoryStream(content);
      using var output = new MemoryStream();
      await _cipher.DecryptAsync(input, output, CancellationToken.None);
      return Encoding.UTF8.GetString(output.ToArray());
   }

   public void CreateFolder(
      Location location)
   {
      if (location.Items.Count == 0)
         return;

      var localPath = ToFilesystemPath(location);
      _fs.Directory.CreateDirectory(localPath);
   }

   public void Delete(
      Location location)
   {
      var localPath = ToFilesystemPath(location);
      _fs.File.Delete(localPath);
   }

   public void Move(
      Location location,
      Location newLocation)
   {
      throw new System.NotImplementedException();
   }

   public IEnumerable<Location> List(
      Location location,
      ListOptions? options = null)
   {
      Log($"start with '{ToString(location)}'");
      
      var listOptions = options ?? ListOptions.Default;

      var localPath = ToFilesystemPath(location);

      if (!_fs.Directory.Exists(localPath))
         throw new InvalidOperationException($"The location '{ToString(location)}' is not a folder.");

      var fsItems =
         _fs.Directory
            .EnumerateFileSystemEntries(localPath);

      foreach (var fsItem in fsItems)
      {
         var fsFileName = _fs.Path.GetFileName(fsItem);

         if (!TryDecryptName(fsFileName, out var decryptedName))
            continue;
         
         if (_fs.File.Exists(fsItem))
         {
            if (!decryptedName!.IsDotted()
                || listOptions.IncludeDottedFilesAndFolders)
            {
               yield return location.Down(decryptedName!);
            }
         }
         else if (_fs.Directory.Exists(fsItem))
         {
            var folderLocation = location.Down(decryptedName!);

            if (listOptions.IncludeFolders
                && (!decryptedName!.IsDotted()
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

      Log($"done");
   }

   public bool FileExist(
      Location location)
   {
      var localPath = ToFilesystemPath(location);
      return _fs.File.Exists(localPath);
   }

   public bool FolderExist(
      Location location)
   {
      var localPath = ToFilesystemPath(location);
      return _fs.Directory.Exists(localPath);
   }

   public bool TryParseName(
      string value,
      out Name? name)
   {
      name = null;

      if (value is "")
         return false;

      var invalidNameChars = _fs.Path.GetInvalidFileNameChars();

      if (value.IndexOfAny(invalidNameChars) != -1)
         return false;

      name = new(this, value);
      return true;
   }

   public bool TryParseLocation(
      string value,
      out Location? location)
   {
      location = null;

      if (value == "")
      {
         location = new(this, []);
         return true;
      }

      var pathItems =
         value.Split(
            _fs.Path.DirectorySeparatorChar,
            _fs.Path.AltDirectorySeparatorChar);

      var items = new List<Name>(pathItems.Length);

      foreach (var item in pathItems)
      {
         if (!TryParseName(item, out var name)
             || name is null)
         {
            return false;
         }

         items.Add(name);
      }

      location = new(this, items);

      return true;
   }

   public string ToString(
      Location location)
   {
      var items = location.Items;

      if (items.Count == 0)
         return ".";

      return _fs.Path.Join(
         items
            .Select(item => item.Value)
            .ToArray());
   }

   public string ToString(
      Name name)
   {
      return name.Value;
   }

   public string ToFilesystemPath(
      Location location)
   {
      var fullPath = _fs.Path.GetFullPath(_path);

      if (location.Items.Count == 0)
         return fullPath;

      var relativePath = ToFilesystemRelativePath(location);

      return _fs.Path.Combine(
         fullPath,
         relativePath);
   }
   
   private string ToFilesystemRelativePath(
      Location location)
   {
      var items = location.Items;

      if (items.Count == 0)
         return ".";

      return Path.Join(
         items
            .Select(item => _encoder.Encode(_cipher.Encrypt(item.Value)))
            .ToArray());
   }

   private Location? FromPath(
      string relativePath)
   {
      if (!TryParseLocation(relativePath, out var location)
          || location == null)
      {
         return null;
      }

      var decryptedPathNames = new List<Name>();
      foreach (var item in location.Items)
      {
         Name decryptedPathName;
         try
         {
            if (!TryDecryptName(item.Value, out var value)
                || value == null)
            {
               return null;
            }

            decryptedPathName = value;
         }
         catch
         {
            return null;
         }

         decryptedPathNames.Add(decryptedPathName);
      }

      return new Location(this, decryptedPathNames);
   }

   private bool TryDecryptName(
      string value,
      out Name? name)
   {
      try
      {
         var decoded = _encoder.Decode(value);
         var decrypted = _cipher.DecryptString(decoded);
         return TryParseName(decrypted, out name);
      }
      catch
      {
         name = null;
         return false;
      }
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
   
   public static IRepository Open(
      string password,
      string path)
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