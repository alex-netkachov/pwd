using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.mocks;

public sealed class ZeroEncoder
   : IEncoder
{
   public static readonly ZeroEncoder Instance = new();

   public void Decode(
      Stream input,
      Stream output)
   {
      input.CopyTo(output);
   }

   public Task DecodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default)
   {
      return input.CopyToAsync(output, token);
   }

   public void Encode(
      Stream input,
      Stream output)
   {
      input.CopyTo(output);
   }

   public Task EncodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default)
   {
      return input.CopyToAsync(output, token);
   }
}
