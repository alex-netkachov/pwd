using System;
using System.Drawing;

namespace pwd.console.abstractions;

public interface IConsole
{
   int BufferWidth { get; }
   
   int BufferHeight { get; }

   void Write(
      object? value);

   void WriteLine(
      object? value);

   Point GetCursorPosition();

   void SetCursorPosition(
      Point point);

   void Clear();
   
   IDisposable Observe(
      Action<ConsoleKeyInfo> action);
   
   IDisposable Intercept(
      Action<ConsoleKeyInfo> action);
}