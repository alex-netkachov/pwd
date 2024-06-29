using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd;
using Timer = System.Threading.Timer;

namespace pwd.core.previous.repository.implementation;

public class ContentCache
{
   private readonly IFileSystem _fs;
   private readonly ICipher _cipher;
   private readonly string _path;
   private readonly IEncoder _encoder;

   private readonly Dictionary<string, string> _items = new();

   private readonly object _lock = new();

   private readonly Timer _saver;

   public ContentCache(
      IFileSystem fs,
      ICipher cipher,
      IEncoder encoder,
      string path)
   {
      _fs = fs;
      _cipher = cipher;
      _path = path;
      _encoder = encoder;

      Read();

      _saver =
         new Timer(
            _ =>
            {
               lock (_lock)
                  Save();
            },
            null,
            5000,
            Timeout.Infinite);

   }

   private void Read()
   {
      _items.Clear();

      var cachePath = _fs.Path.Combine(_path, ".cache");

      if (!_fs.File.Exists(cachePath))
         return;

      var content = _fs.File.ReadAllText(cachePath);
      var decoded = _encoder.Decode(content);
      var decrypted = _cipher.DecryptString(decoded);

      var items =
         decrypted
            .Split('\n')
            .Select(line => line.Trim().Split('\t'))
            .Where(parts => parts.Length == 2)
            .ToList();

      foreach (var pair in items)
      {
         var key = pair[0];
         var value = pair[1];

         _items[key] = Encoding.UTF8.GetString(_encoder.Decode(value));
      }
   }

   private void Save()
   {
      Console.WriteLine("save...");
      var cachePath = _fs.Path.Combine(_path, ".cache");

      var sb = new StringBuilder();
      foreach (var (key, value) in _items)
      {
         var encoded = _encoder.Encode(Encoding.UTF8.GetBytes(value));
         sb.AppendLine($"{key}\t{encoded}");
      }

      var content = sb.ToString();
      var encrypted = _cipher.Encrypt(content);
      _fs.File.WriteAllText(cachePath, _encoder.Encode(encrypted));

      _saver.Change(Timeout.Infinite, Timeout.Infinite);
   }

   public string Encrypt(
      string input)
   {
      var encrypted = _cipher.Encrypt(input);
      var encoded = _encoder.Encode(encrypted);

      lock (_lock)
      {
         _items[encoded] = input;
         _saver.Change(1000, Timeout.Infinite);
      }

      return encoded;
   }

   public async Task<string> EncryptAsync(
      string input,
      CancellationToken token = default)
   {
      var encrypted = await _cipher.EncryptAsync(input, token);
      var encoded = _encoder.Encode(encrypted);

      lock (_lock)
      {
         _items[encoded] = input;
         _saver.Change(5000, Timeout.Infinite);
      }

      return encoded;
   }

   public string Decrypt(
      string input)
   {
      lock (_lock)
      {
         if (_items.TryGetValue(input, out var value))
            return value;
      }

      var decoded = _encoder.Decode(input);
      var decrypted = _cipher.DecryptString(decoded);

      lock (_lock)
      {
         _items[input] = decrypted;
         _saver.Change(5000, Timeout.Infinite);
      }

      return decrypted;
   }

   public async Task<string> DecryptAsync(
      string input,
      CancellationToken token = default)
   {
      lock (_lock)
      {
         if (_items.TryGetValue(input, out var value))
            return value;
      }

      var decoded = _encoder.Decode(input);
      var decrypted = await _cipher.DecryptStringAsync(decoded, token);

      lock (_lock)
      {
         _items[input] = decrypted;
         _saver.Change(5000, Timeout.Infinite);
      }

      return decrypted;
   }

   public bool TryDecrypt(
      string input,
      out string output)
   {
      lock (_lock)
      {
         if (_items.TryGetValue(input, out var value))
         {
            output = value;
            return true;
         }
      }

      if (!_encoder.TryDecode(input, out var decoded))
      {
         output = "";
         return false;
      }

      if (!_cipher.TryDecryptString(decoded, out var decrypted))
      {
         output = "";
         return false;
      }

      lock (_lock)
      {
         _items[input] = decrypted;
         _saver.Change(5000, Timeout.Infinite);
      }

      output = decrypted;
      return true;
   }

   public async Task<(bool, string?)> TryDecryptAsync(
      string input,
      CancellationToken token = default)
   {
      lock (_lock)
      {
         if (_items.TryGetValue(input, out var value))
            return (true, value);
      }

      if (!_encoder.TryDecode(input, out var decoded))
         return (false, null);

      var (decrypted, decryptedValue) =
         await _cipher.TryDecryptStringAsync(decoded, token);

      if (!decrypted
          || decryptedValue == null)
      {
         return (false, null);
      }

      lock (_lock)
      {
         _items[input] = decryptedValue;
         _saver.Change(5000, Timeout.Infinite);
      }

      return (true, decryptedValue);
   }
}