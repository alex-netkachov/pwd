using System;

namespace pwd.ui.console;

public interface IConsole
   : IObservable<ConsoleKeyInfo>
{
   int BufferWidth { get; }

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