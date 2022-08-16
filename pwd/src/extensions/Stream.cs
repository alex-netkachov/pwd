using System;
using System.IO;
using System.Threading.Tasks;

namespace pwd.extensions;

public static class StreamExtensions
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
      int length)
   {
      var buffer = new byte[length];
      var offset = 0;
      while (offset != length)
      {
         var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset));
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
      this Stream stream)
   {
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);
      return memoryStream.ToArray();
   }
}
