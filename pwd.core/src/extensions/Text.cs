using System;
using System.Text;

namespace pwd.core.extensions;

internal static class TextExtensions
{
   public static bool IsUtf8(
      byte[] data)
   {
      var isUtf8 = true;

      var utf8 =
         Encoding.GetEncoding(
            "UTF-8",
            new EncoderExceptionFallback(),
            new DelegateDecoderFallback(() => isUtf8 = false));

      utf8.GetString(data);

      return isUtf8;
   }

   private sealed class DelegateDecoderFallback(
         Action action)
      : DecoderFallback
   {
      private readonly DelegateDecoderFallbackBuffer _delegateDecoderFallbackBuffer = new(action);

      public override DecoderFallbackBuffer CreateFallbackBuffer() => _delegateDecoderFallbackBuffer;

      public override int MaxCharCount => 0;
   }

   private sealed class DelegateDecoderFallbackBuffer(
         Action action)
      : DecoderFallbackBuffer
   {
      public override bool Fallback(
         byte[] bytesUnknown,
         int index)
      {
         action();
         return false;
      }

      public override char GetNextChar() => (char) 0;

      public override bool MovePrevious() => false;

      public override int Remaining => 0;
   }
}