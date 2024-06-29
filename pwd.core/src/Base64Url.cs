using pwd.core.abstractions;
using pwd.core.extensions;

namespace pwd.core;

/// <summary>
///   Base64url encoder and decoder.
/// </summary>
public sealed class Base64Url
   : IStringEncoder
{
   public static readonly Base64Url Instance = new();

   private static string Encode(
      byte[] input)
   {
      return Convert.ToBase64String(input)
         .Replace('+', '-')
         .Replace('/', '_');
   }

   private static byte[] Decode(
      string input)
   {
      return Convert.FromBase64String(
         input.Replace('-', '+')
            .Replace('_', '/'));
   }
   
   public string Encode(
      Stream input)
   {
      var inputData = input.ReadAllBytes();
      return Encode(inputData);
   }

   public async Task<string> EncodeAsync(
      Stream input,
      CancellationToken token = default)
   {
      var inputData = await input.ReadAllBytesAsync(token);
      return Encode(inputData);
   }

   public void Decode(
      string input,
      Stream output)
   {
      var decodedData = Decode(input);
      output.Write(decodedData);
   }

   public async Task DecodeAsync(
      string input,
      Stream output,
      CancellationToken token = default)
   {
      var decodedData = Decode(input);
      await output.WriteAsync(decodedData, token);
   }
}