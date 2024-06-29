using System;
using System.Text;

namespace pwd.extensions;

public static class TextExtensions
{
   public static bool IsUtf8(
      byte[] data)
   {
      var isUtf8 = true;
      var utf8 = Encoding.GetEncoding("UTF-8", new EncoderExceptionFallback(),
         new DelegateDecoderFallback(() => isUtf8 = false));
      utf8.GetString(data);
      return isUtf8;
   }

   private sealed class DelegateDecoderFallback
      : DecoderFallback
   {
      private readonly DelegateDecoderFallbackBuffer _delegateDecoderFallbackBuffer;

      public DelegateDecoderFallback(
         Action action)
      {
         _delegateDecoderFallbackBuffer = new DelegateDecoderFallbackBuffer(action);
      }

      public override DecoderFallbackBuffer CreateFallbackBuffer() => _delegateDecoderFallbackBuffer;

      public override int MaxCharCount => 0;
   }

   private sealed class DelegateDecoderFallbackBuffer
      : DecoderFallbackBuffer
   {
      private readonly Action _action;

      public DelegateDecoderFallbackBuffer(
         Action action)
      {
         _action = action;
      }

      public override bool Fallback(
         byte[] bytesUnknown,
         int index)
      {
         _action();
         return false;
      }

      public override char GetNextChar() => (char) 0;

      public override bool MovePrevious() => false;

      public override int Remaining => 0;
   }
}