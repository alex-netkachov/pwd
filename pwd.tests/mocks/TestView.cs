using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.mocks;

public class TestView(
      IReadOnlyList<string>? input = null)
   : IView,
     IDisposable
{
   private int _disposed;
   private readonly CancellationTokenSource _cts = new();
   private readonly StringBuilder _builder = new();
   private int _inputIndex = -1;

   public string Id { get; } = Guid.NewGuid().ToString("N");

   public async Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      _builder.Append(question + " (y/N) ");

      if (NextInput() is { } value)
      {
         _builder.Append(value + "\n");
         return value == "y";
      }

      using var linkedToken =
         CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            token);

      await Task.Delay(
         int.MaxValue,
         linkedToken.Token);

      return false;
   }

   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestions? suggestions = null,
      IHistory? history = null,
      CancellationToken token = default)
   {
      _builder.Append(prompt);
      
      if (NextInput() is { } value)
      {
         _builder.Append(value + "\n");
         return value;
      }

      using var linkedToken =
         CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            token);

      await Task.Delay(
         int.MaxValue,
         linkedToken.Token);

      return "";

   }

   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      _builder.Append(prompt);
      if (NextInput() is { } value)
      {
         _builder.Append(new string('*', value.Length) + "\n");
         return value;
      }
      
      using var linkedToken =
         CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            token);

      await Task.Delay(
         int.MaxValue,
         linkedToken.Token);

      return "";
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

   private string? NextInput()
   {
      return input?.ElementAtOrDefault(++_inputIndex);
   }

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) > 1)
         return;

      Dispose(true);
      GC.SuppressFinalize(this);
   }

   private void Dispose(
      bool disposing)
   {
      if (!disposing)
         return;

      _cts.Cancel();
      _cts.Dispose();
   }
   
   ~TestView()
   {
      Dispose(false);
   }
}