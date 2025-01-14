using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.mocks;

public class TestView(
      IReadOnlyList<string>? input = null)
   : IView
{
   private readonly StringBuilder _builder = new();
   private int _inputIndex = -1;

   public Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      _builder.Append(question);
      var value = NextInput();
      _builder.Append(value + "\n");
      return Task.FromResult(value == "y");
   }

   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestions? suggestions = null,
      IHistory? history = null,
      CancellationToken token = default)
   {
      _builder.Append(prompt);
      var value = NextInput();
      _builder.Append(value + "\n");
      return Task.FromResult(value);
   }

   public Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      _builder.Append(prompt);
      var value = NextInput();
      _builder.Append(new string('*', value.Length) + "\n");
      return Task.FromResult(value);
   }

   public int BufferWidth => -1;

   public int BufferHeight => -1;

   public void Write(
      object? value)
   {
      _builder.Append(
         Convert.ToString(
            value
            ?? ""));
   }

   public void WriteLine(
      object? value)
   {
      _builder.Append(
         Convert.ToString(
            value
            ?? "")
         + "\n");
   }

   public Point GetCursorPosition()
   {
      throw new NotImplementedException();
   }

   public void SetCursorPosition(Point point)
   {
   }

   public void Clear()
   {
      _builder.Append("\n<CLEAR>\n");
   }

   public IDisposable Observe(Action<ConsoleKeyInfo> action)
   {
      throw new NotImplementedException();
   }

   public IDisposable Intercept(Action<ConsoleKeyInfo> action)
   {
      throw new NotImplementedException();
   }

   public void Activate(
      IConsole console)
   {
   }

   public void Deactivate()
   {
   }
   
   public string GetOutput()
   {
      return _builder.ToString();
   }

   private string NextInput()
   {
      if (input is null)
         return string.Empty;
      _inputIndex++;
      return input[_inputIndex];
   }
}