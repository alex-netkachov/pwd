using System;
using System.Drawing;

namespace pwd.console.abstractions;

public interface IConsole
   : IObservable<ConsoleKeyInfo>
{
   /// <summary>
   ///   Width of the console.
   /// </summary>
   /// <remarks>
   ///   Value less than 1 indicates that the console does not wrap long lines.
   /// </remarks>
   int Width { get; }

   /// <summary>
   ///   Height of the console.
   /// </summary>
   /// <remarks>
   ///   Value less than 1 indicates that the console does not move lines up.
   /// </remarks>
   int Height { get; }

   /// <summary>
   ///    Writes string representation of the value to the console
   ///    in the current position.
   /// </summary>
   /// <remarks>
   ///    The value will be converted to string according to
   ///    the rules of the invariant culture.
   /// </remarks>
   void Write(
      object? value);

   /// <summary>
   ///    Writes string representation of the value to the console
   ///    in the current position and creates a new line.
   /// </summary>
   /// <remarks>
   ///    The value will be converted to string according to
   ///    the rules of the invariant culture.
   /// </remarks>
   void WriteLine(
      object? value);

   /// <summary>
   ///    Reads the current cursor position.
   /// </summary>
   Point GetCursorPosition();

   /// <summary>
   ///    Changes position of the cursor.
   /// </summary>
   void SetCursorPosition(
      Point position);

   /// <summary>
   ///    Clears the console.
   /// </summary>
   void Clear();

   /// <summary>
   ///    Starts intercepting console key input.
   /// </summary>
   /// <remarks>
   ///    Only one interceptor can be active at a time.
   /// </remarks>
   IDisposable Intercept(
      IObserver<ConsoleKeyInfo> interceptor);
}