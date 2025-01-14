using System;
using pwd.core.abstractions;

namespace pwd.core;

/// <summary>
///   Base64url encoder and decoder.
/// </summary>
public sealed class Base64Url
   : IStringEncoder
{
   public static readonly Base64Url Instance = new();

   public string Encode(
      byte[] input)
   {
      return PostEncodeString(
         Convert.ToBase64String(input));
   }

   public byte[] Decode(
      string input)
   {
      return Convert.FromBase64String(
         PreDecodeString(input));
   }

   public bool TryDecode(
      string input,
      out byte[]? output)
   {
      var buffer = new byte[input.Length];
      var converted =
         Convert.TryFromBase64String(
            PreDecodeString(input),
            buffer,
            out var length);
      if (!converted)
      {
         output = null;
         return false;
      }

      output = new byte[length];
      Array.Copy(buffer, output, length);
      return true;
   }

   private static string PreDecodeString(
      string input)
   {
      return input
         .Replace('-', '+')
         .Replace('_', '/');
   }
   
   private static string PostEncodeString(
      string input)
   {
      return input
         .Replace('+', '-')
         .Replace('/', '_');
   }
}