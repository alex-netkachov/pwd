﻿using System;
using System.Drawing;

namespace pwd.console.abstractions;

public interface IConsole
   : IObservable<ConsoleKeyInfo>
{
   int BufferWidth { get; }

   void Write(
      object? value = null);

   void WriteLine(
      object? value = null);

   Point GetCursorPosition();

   void SetCursorPosition(
      Point point);

   void Clear();
}