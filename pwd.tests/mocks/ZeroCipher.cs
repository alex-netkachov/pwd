using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.mocks;

public sealed class ZeroCipher
   : ICipher
{
   public static readonly ZeroCipher Instance = new();

   public void Decrypt(
      Stream input,
      Stream output)
   {
      input.CopyToAsync(output);
   }

   public Task DecryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default)
   {
      return input.CopyToAsync(output, cancellationToken);
   }

   public void Encrypt(
      Stream input,
      Stream output)
   {
      input.CopyToAsync(output);
   }

   public Task EncryptAsync(
      Stream input,
      Stream output,
      CancellationToken cancellationToken = default)
   {
      return input.CopyToAsync(output, cancellationToken);
   }
}
