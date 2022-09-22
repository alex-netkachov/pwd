using System;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd;

/// <summary>Answers to confirmation questions.</summary>
public enum Answer
{
   Yes,
   No
}

public interface IView
{
   /// <summary>Notifies listeners about reaching the user interaction timeout.</summary>
   event EventHandler Idle;

   void WriteLine(
      string text);

   void Write(
      string text);

   /// <summary>Asks yes/no question.</summary>
   Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default);

   /// <summary>Reads the input from the console.</summary>
   Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default);

   /// <summary>Reads UTF8 input from the console without echoing to the output until Enter is pressed.</summary>
   /// <remarks>Ctrl+U resets the input, Backspace removes the last character from the input, other control
   /// keys (e.g. tab) are ignored.</remarks>
   Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default);

   /// <summary>Clears the console and its buffer, if possible.</summary>
   void Clear();
}

public sealed class View
   : IView
{
   private readonly Prompt _prompt;

   public event EventHandler? Idle;

   public View(
      TimeSpan interactionTimeout)
   {
      _prompt = new(interactionTimeout);
      _prompt.Idle += (_, _) => Idle?.Invoke(this, EventArgs.Empty);
   }

   public void WriteLine(
      string text)
   {
      Console.WriteLine(text);
   }

   public void Write(
      string text)
   {
      Console.Write(text);
   }

   public async Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      var yes = @default == Answer.Yes ? 'Y' : 'y';
      var no = @default == Answer.No ? 'N' : 'n';

      var input = await _prompt.ReadAsync($"{question} ({yes}/{no}) ", token: token);
      var answer = input.ToUpperInvariant();
      return @default == Answer.Yes ? answer != "N" : answer == "Y";
   }

   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      return await _prompt.ReadAsync(prompt, suggestionsProvider, token);
   }

   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      return await _prompt.ReadPasswordAsync(prompt, token);
   }

   public void Clear()
   {
      Console.Clear();

      // clears the console and buffer on xterm-compatible terminals
      if (Environment.GetEnvironmentVariable("TERM")?.StartsWith("xterm") == true)
         Console.Write("\x1b[3J");
   }
}