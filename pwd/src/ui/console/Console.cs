using System;
using System.Threading.Channels;

namespace pwd.ui.console;

public interface IConsole
{
   int BufferWidth { get; }

   void Subscribe(
      ChannelWriter<ConsoleKeyInfo> writer);
   
   void Unsubscribe(
      ChannelWriter<ConsoleKeyInfo> writer);

   void Write(
      object? value = null);

   void WriteLine(
      object? value = null);

   (int Left, int Top) GetCursorPosition();

   void SetCursorPosition(
      int left,
      int top);

   void Clear();
}