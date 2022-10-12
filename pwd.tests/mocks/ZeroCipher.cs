using System.Text;
using pwd.ciphers;

namespace pwd.mocks;

public sealed class ZeroCipher
   : INameCipher,
      IContentCipher
{
   public static readonly ZeroCipher Instance = new();

   public int Encrypt(
      string text,
      Stream stream)
   {
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return data.Length;
   }

   public Task<int> EncryptAsync(
      string text,
      Stream stream)
   {
      var data = Encoding.UTF8.GetBytes(text);
      stream.Write(data);
      return Task.FromResult(data.Length);
   }

   public (bool Success, string Text) DecryptString(
      Stream stream)
   {
      using var reader = new StreamReader(stream);
      return (true, reader.ReadToEnd());
   }

   public async Task<(bool Success, string Text)> DecryptStringAsync(
      Stream stream)
   {
      using var reader = new StreamReader(stream);
      return (true, await reader.ReadToEndAsync());
   }
}