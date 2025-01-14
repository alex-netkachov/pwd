using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.core.extensions;

internal static class StreamExtensions
{
   public static byte[] ReadBytes(
      this Stream stream,
      int length)
   {
      var buffer = new byte[length];
      var offset = 0;
      while (offset != length)
      {
         var read = stream.Read(buffer.AsSpan(offset, length - offset));
         offset += read;
         if (read == 0)
            return buffer[..offset];
      }

      return buffer;
   }

   public static async Task<byte[]> ReadBytesAsync(
      this Stream stream,
      int length,
      CancellationToken cancellationToken = default)
   {
      var buffer = new byte[length];
      var offset = 0;
      while (offset != length)
      {
         var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
         offset += read;
         if (read == 0)
            return buffer[..offset];
      }

      return buffer;
   }

   public static byte[] ReadAllBytes(
      this Stream stream)
   {
      using var memoryStream = new MemoryStream();
      stream.CopyTo(memoryStream);
      return memoryStream.ToArray();
   }

   public static async Task<byte[]> ReadAllBytesAsync(
      this Stream stream,
      CancellationToken cancellationToken = default)
   {
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream, cancellationToken);
      return memoryStream.ToArray();
   }

   public static MemoryStream AsStream(
      this string text)
   {
      return new MemoryStream(Encoding.UTF8.GetBytes(text));
   }

   public static MemoryStream AsStream(
      this byte[] bytes)
   {
      return new MemoryStream(bytes);
   }

   public static string AsString(
      this MemoryStream stream)
   {
      return Encoding.UTF8.GetString(stream.ToArray());
   }
}