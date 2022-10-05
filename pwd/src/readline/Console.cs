using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.readline;

public interface IConsole
{
   int BufferWidth { get; }

   ChannelReader<ConsoleKeyInfo> Subscribe(
      CancellationToken token);

   void Write(
      object? value = null);

   void WriteLine(
      object? value = null);

   (int Left, int Top) GetCursorPosition();

   void SetCursorPosition(
      int left,
      int top);
}

public sealed class StandardConsole
   : IConsole
{
   public int BufferWidth => Console.BufferWidth;

   public ChannelReader<ConsoleKeyInfo> Subscribe(
      CancellationToken token)
   {
      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();

      Task.Run(() =>
      {
         var writer = channel.Writer;

         while (!token.IsCancellationRequested)
         {
            if (!Console.KeyAvailable)
            {
               // Delay between user pressing the key and processing this key by the app.
               // Should be small enough so the user does not notice an input lag. 
               Thread.Sleep(10);
               continue;
            }

            var key = Console.ReadKey(true);
            while (!writer.TryWrite(key) && !token.IsCancellationRequested) ;
         }

         writer.Complete();
      }, token);

      return channel.Reader;
   }

   public void Write(
      object? value)
   {
      Console.Write(value);
   }

   public void WriteLine(
      object? value)
   {
      Console.WriteLine(value);
   }

   public (int Left, int Top) GetCursorPosition()
   {
      return Console.GetCursorPosition();
   }

   public void SetCursorPosition(
      int left,
      int top)
   {
      Console.SetCursorPosition(left, top);
   }
}