using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
      string prompt,
      CancellationToken token = default);

   /// <summary>Reads UTF8 input from the console without echoing to the output until Enter is pressed.</summary>
   /// <remarks>Ctrl+U resets the input, Backspace removes the last character from the input, other control
   /// keys (e.g. tab) are ignored.</remarks>
   Task<string> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default);

   /// <summary>Clears the console and its buffer, if possible.</summary>
   void Clear();
}

public sealed class View
   : IView
{
   private readonly TimeSpan _interactionTimeout;
   private readonly Timer _interactionTimeoutTimer;

   public event EventHandler? Idle;

   public View(
      IState state,
      TimeSpan interactionTimeout)
   {
      _interactionTimeout = interactionTimeout;
      _interactionTimeoutTimer = new(_ => Idle?.Invoke(this, EventArgs.Empty));
      _interactionTimeoutTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);

      ReadLine.HistoryEnabled = true;
      ReadLine.AutoCompletionHandler = new AutoCompletionHandler(state);
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

      var input = await ReadLineAsync($"{question} ({yes}/{no}) ", true, token);
      var answer = input.ToUpperInvariant();
      return @default == Answer.Yes ? answer != "N" : answer == "Y";
   }

   public Task<string> ReadAsync(
      string prompt,
      CancellationToken token = default)
   {
      return Task.Run(() =>
      {
         UpdateInteractionTimeoutTimer();
         return ReadLine.Read(prompt);
      }, token);
   }

   public async Task<string> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default)
   {
      return await ReadLineAsync(prompt, false, token);
   }

   public void Clear()
   {
      Console.Clear();

      // clears the console and buffer on xterm-compatible terminals
      if (Environment.GetEnvironmentVariable("TERM")?.StartsWith("xterm") == true)
         Console.Write("\x1b[3J");
   }
   
   /// <summary>Simple async reading from the console. Supports BS, Ctrl+U. Ignores control keys (e.g. tab).</summary>
   private async Task<string> ReadLineAsync(
      string prompt,
      bool echo,
      CancellationToken token = default)
   {
      if (!string.IsNullOrEmpty(prompt))
         Console.Write(prompt);

      return await Task.Run(() =>
      {
         var input = new StringBuilder();
         while (true)
         {
            token.ThrowIfCancellationRequested();

            if (!Console.KeyAvailable)
            {
               // Delay between user pressing the key and processing this key by the app.
               // Should be small enough so the user does not notice an input lag. 
               Thread.Sleep(10);
               continue;
            }

            UpdateInteractionTimeoutTimer();

            var key = Console.ReadKey(true);
            switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
            {
               case (false, ConsoleKey.Enter):
                  Console.WriteLine();
                  return input.ToString();
               case (false, ConsoleKey.Backspace):
                  if (input.Length > 0)
                  {
                     if (echo)
                        Console.Write("\b \b");
                     input.Remove(input.Length - 1, 1);
                  }
                  break;
               case (true, ConsoleKey.U):
                  if (echo)
                  {
                     var back = new string('\b', input.Length);
                     var space = new string(' ', input.Length);
                     Console.Write($"{back}{space}{back}");
                  }
                  input.Clear();
                  break;
               default:
                  if (!char.IsControl(key.KeyChar))
                  {
                     if (echo)
                        Console.Write(key.KeyChar);
                     input.Append(key.KeyChar);
                  }
                  break;
            }
         }
      }, token);
   }

   private void UpdateInteractionTimeoutTimer()
   {
      _interactionTimeoutTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
   }
}