using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pwd;

/// <summary>Answers to confirmations.</summary>
public enum Answer
{
   Yes,
   No
}

public interface IView
{
   event EventHandler Interaction;

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
   Task<byte[]> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default);

   /// <summary>Clears the console and its buffer, if possible.</summary>
   void Clear();
}

public sealed class View
   : IView
{
   public event EventHandler? Interaction;

   public View(
      IState state)
   {
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

      Console.Write($"{question} ({yes}/{no}) ");

      var input = new string(await SimpleReadAsync(true, token)).ToUpperInvariant();

      return @default == Answer.Yes ? input != "N" : input == "Y";
   }

   public Task<string> ReadAsync(
      string prompt,
      CancellationToken token = default)
   {
      return Task.Run(() =>
      {
         Interaction?.Invoke(this, EventArgs.Empty);
         return ReadLine.Read(prompt);
      }, token);
   }

   public async Task<byte[]> ReadPasswordAsync(
      string prompt,
      CancellationToken token = default)
   {
      Console.Write(prompt);
      return Encoding.UTF8.GetBytes(await SimpleReadAsync(false, token));
   }

   public void Clear()
   {
      Console.Clear();

      // clears the console and buffer on xterm-compatible terminals
      if (Environment.GetEnvironmentVariable("TERM")?.StartsWith("xterm") == true)
         Console.Write("\x1b[3J");
   }
   
   private async Task<char[]> SimpleReadAsync(
      bool echo,
      CancellationToken token = default)
   {
      return await Task.Run(() =>
      {
         var chars = new Stack<char>();
         while (true)
         {
            if (!Console.KeyAvailable)
            {
               Thread.Sleep(10);
               continue;
            }

            if (token.IsCancellationRequested)
               throw new TaskCanceledException();

            Interaction?.Invoke(this, EventArgs.Empty);

            var input = Console.ReadKey(true);
            switch (input.Modifiers == ConsoleModifiers.Control, input.Key)
            {
               case (false, ConsoleKey.Enter):
                  Console.WriteLine();
                  return chars.Reverse().ToArray();
               case (false, ConsoleKey.Backspace):
                  if (chars.Count > 0)
                  {
                     if (echo)
                        Console.Write("\b \b");
                     chars.Pop();
                  }
                  break;
               case (true, ConsoleKey.U):
                  if (echo)
                     Console.Write(string.Join("", chars.Select(_ => "\b \b")));
                  chars.Clear();
                  break;
               default:
                  if (!char.IsControl(input.KeyChar))
                  {
                     if (echo)
                        Console.Write(input.KeyChar);
                     chars.Push(input.KeyChar);
                  }
                  break;
            }
         }
      }, token);
   }
}