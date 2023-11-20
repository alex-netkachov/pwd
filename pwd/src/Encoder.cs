using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.extensions;

namespace pwd;

/// <summary>
///   Provides methods for encoding and decoding data streams.
/// </summary>
/// <remarks>
///   Encoder is different from Cipher is a way that if produces the same output
///   for the same input.
///
///   Implementers are responsible for ensuring that methods are thread-safe if instances
///   of implementations are expected to be called from multiple threads.
///
///   The methods do not close or flush the output streams.
/// </remarks>
public interface IEncoder
{
   /// <summary>
   ///   Encodes the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   void Encode(
      Stream input,
      Stream output);

   /// <summary>
   ///   Encodes the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>
   ///   Does not close or flush the output stream. Canceling the task may
   ///   result in only a portion of the data being encrypted and written
   ///   to the output stream.
   /// </remarks>
   Task EncodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default);

   /// <summary>
   ///   Decodes the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   void Decode(
      Stream input,
      Stream output);

   /// <summary>
   ///   Decode the data from the input stream and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   Task DecodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default);
}

internal static class EncoderExtensions
{
   /// <summary>
   ///   Encodes the specified data.
   /// </summary>
   /// <returns>A string containing the encoded data.</returns>
   public static string Encode(
      this IEncoder encoder,
      byte[] input)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      encoder.Encode(inputStream, outputStream);
      return Encoding.UTF8.GetString(outputStream.ToArray());
   }

   /// <summary>
   ///   Encrypts the specified text.
   /// </summary>
   /// <returns>A byte array containing the encrypted data.</returns>
   /// <remarks>
   ///   Converts the input text to a byte array using UTF8 encoding, then
   ///   encrypts the data using the Encrypt method of the IEncoder interface.
   /// </remarks>
   public static async Task<string> EncodeAsync(
      this IEncoder encoder,
      byte[] input,
      CancellationToken token = default)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      await encoder.EncodeAsync(inputStream, outputStream, token);
      return Encoding.UTF8.GetString(outputStream.ToArray());
   }

   /// <summary>
   ///   Decodes a string array into a byte array.
   /// </summary>
   /// <returns>A string containing the decoded data.</returns>
   public static byte[] Decode(
      this IEncoder encoder,
      string input)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      encoder.Decode(inputStream, outputStream);
      return outputStream.ToArray();
   }

   /// <summary>
   ///   Decodes a string array into a byte array.
   /// </summary>
   /// <returns>A string containing the decoded data.</returns>
   public static async Task<byte[]> DecodeAsync(
      this IEncoder encoder,
      string input,
      CancellationToken token = default)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      await encoder.DecodeAsync(inputStream, outputStream, token);
      return outputStream.ToArray();
   }

   public static bool TryDecode(
      this IEncoder encoder,
      string input,
      out byte[] output)
   {
      try
      {
         output = Decode(encoder, input);
         return true;
      }
      catch
      {
         output = Array.Empty<byte>();
         return false;
      }
   }
}

/// <summary>
///   Base64url encoder and decoder.
/// </summary>
public sealed class Base64Url
   : IEncoder
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

   public void Encode(
     Stream input,
     Stream output)
   {
      var inputData = input.ReadAllBytes();
      var encodedData = Encode(inputData);
      var outputData = Encoding.UTF8.GetBytes(encodedData);
      output.Write(outputData);

   }

   public async Task EncodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default)
   {
      var inputData = await input.ReadAllBytesAsync(token);
      var encodedData = Encode(inputData);
      var outputData = Encoding.UTF8.GetBytes(encodedData);
      await output.WriteAsync(outputData, token);
   }

   public void Decode(
      Stream input,
      Stream output)
   {
      var inputData = input.ReadAllBytes();
      var encodedData = Encoding.UTF8.GetString(inputData);
      var decodedData = Decode(encodedData);
      output.Write(decodedData);
   }

   public async Task DecodeAsync(
      Stream input,
      Stream output,
      CancellationToken token = default)
   {
      var inputData = await input.ReadAllBytesAsync(token);
      var encodedData = Encoding.UTF8.GetString(inputData);
      var decodedData = Decode(encodedData);
      await output.WriteAsync(decodedData, token);
   }
}