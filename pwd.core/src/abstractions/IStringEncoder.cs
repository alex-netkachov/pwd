namespace pwd.core.abstractions;

/// <summary>
///   Provides methods for encoding and decoding data into strings.
/// </summary>
/// <remarks>
///   Implementers are responsible for ensuring that methods are thread-safe if instances
///   of implementations are expected to be called from multiple threads.
/// </remarks>
public interface IStringEncoder
{
   /// <summary>
   ///   Encodes the data from the byte array into a string. 
   /// </summary>
   string Encode(
      byte[] input);

   /// <summary>
   ///   Decodes the data from the input string into a byte array.
   /// </summary>
   byte[] Decode(
      string input);
   
   /// <summary>
   ///   Decodes the data from the input string into a byte array.
   /// </summary>
   bool TryDecode(
      string input,
      out byte[]? output);
}
