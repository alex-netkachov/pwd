using System.Text;
using pwd.ciphers;

namespace pwd.mocks;

public sealed class ZeroCipher
   : INameCipher,
      IContentCipher
{
   public static readonly ZeroCipher Instance = new();

   public int EncryptString(
      string text,
      Stream stream)
   {
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return data.Length;
   }

   public Task<int> EncryptStringAsync(
      string text,
      Stream stream,
      CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return Task.FromResult(data.Length);
   }

   public string DecryptString(
      Stream stream)
   {
      using var reader = new StreamReader(stream);
      return reader.ReadToEnd();
   }

   public async Task<string> DecryptStringAsync(
      Stream stream,
      CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();
      using var reader = new StreamReader(stream);
      return await reader.ReadToEndAsync();
   }
}