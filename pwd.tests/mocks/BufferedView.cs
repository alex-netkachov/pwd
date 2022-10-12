using System.Text;
using pwd.readline;

namespace pwd.mocks;

public sealed class BufferedView
   : IView
{
   private readonly StringBuilder _output = new();
   private readonly string[] _input;
   private int _index;

   public event EventHandler? Idle;

   public BufferedView(
      params string[] input)
   {
      _input = input;
   }

   public void WriteLine(
      string text)
   {
      _output.AppendLine(text);
   }

   public void Write(
      string text)
   {
      _output.Append(text);
   }

   public Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      return Task.FromResult(true);
   }

   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      if (_index < _input.Length)
      {
         var value = _input[_index];
         _index++;
         return Task.FromResult(value);   
      }

      Idle?.Invoke(this, EventArgs.Empty);
 
      return Task.Delay(Timeout.Infinite, token)
         .ContinueWith(_ => "", token);
   }

   public Task<string> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default)
   {
      if (_index < _input.Length)
      {
         var value = _input[_index];
         _index++;
         return Task.FromResult(value);   
      }

      Idle?.Invoke(this, EventArgs.Empty);
 
      return Task.Delay(Timeout.Infinite, token)
         .ContinueWith(_ => "", token);
   }

   public void Clear()
   {
      _output.Clear();
   }

   public override string ToString()
   {
      return _output.ToString();
   }
}