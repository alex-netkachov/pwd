using pwd.core.extensions;

namespace pwd.core.abstractions;

/// <summary>
///   Provides methods for encoding and decoding data streams into strings.
/// </summary>
/// <remarks>
///   Implementers are responsible for ensuring that methods are thread-safe if instances
///   of implementations are expected to be called from multiple threads.
///
///   The methods do not close or flush the output streams.
/// </remarks>
public interface IStringEncoder
{
   /// <summary>
   ///   Encodes the data from the binary input stream into a string. 
   /// </summary>
   /// <remarks>Does not close the input stream.</remarks>
   string Encode(
      Stream input);

   /// <summary>
   ///   Encodes the data from the binary input stream into a string. 
   /// </summary>
   /// <remarks>
   ///   Does not close the input stream. Canceling the task may
   ///   result in only a portion of the data being read.
   /// </remarks>
   Task<string> EncodeAsync(
      Stream input,
      CancellationToken token = default);

   /// <summary>
   ///   Decodes the data from the input string and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   void Decode(
      string input,
      Stream output);

   /// <summary>
   ///   Decode the data from the input string and writes
   ///   it to the output stream.
   /// </summary>
   /// <remarks>Does not close or flush the output stream.</remarks>
   Task DecodeAsync(
      string input,
      Stream output,
      CancellationToken token = default);
}

public static class StringEncoderExtensions
{
   /// <summary>
   ///   Encodes the specified data.
   /// </summary>
   /// <returns>A string containing the encoded data.</returns>
   public static string Encode(
      this IStringEncoder encoder,
      byte[] input)
   {
      using var inputStream = input.AsStream();
      return encoder.Encode(inputStream);
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
      this IStringEncoder encoder,
      byte[] input,
      CancellationToken token = default)
   {
      using var inputStream = input.AsStream();
      return await encoder.EncodeAsync(inputStream, token);
   }

   /// <summary>
   ///   Decodes a string array into a byte array.
   /// </summary>
   /// <returns>A string containing the decoded data.</returns>
   public static byte[] Decode(
      this IStringEncoder encoder,
      string input)
   {
      using var outputStream = new MemoryStream();
      encoder.Decode(input, outputStream);
      return outputStream.ToArray();
   }

   /// <summary>
   ///   Decodes a string array into a byte array.
   /// </summary>
   /// <returns>A string containing the decoded data.</returns>
   public static async Task<byte[]> DecodeAsync(
      this IStringEncoder encoder,
      string input,
      CancellationToken token = default)
   {
      using var inputStream = input.AsStream();
      using var outputStream = new MemoryStream();
      await encoder.DecodeAsync(input, outputStream, token);
      return outputStream.ToArray();
   }

   public static bool TryDecode(
      this IStringEncoder encoder,
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
         output = [];
         return false;
      }
   }
}
