using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
   bool Confirm(
      string question,
      Answer @default = Answer.No);

   /// <summary>Reads the input from the console.</summary>
   string Read(
      string prompt);

   /// <summary>Reads UTF8 input from the console without echoing to the output until Enter is pressed.</summary>
   /// <remarks>Ctrl+U resets the input, Backspace removes the last character from the input, other control
   /// keys (e.g. tab) are ignored.</remarks>
   byte[] ReadPassword(
      string prompt);

   /// <summary>Clears the console and its buffer, if possible.</summary>
   void Clear();
}

public sealed class View
   : IView
{
   public event EventHandler Interaction;

   public View(
      IState state)
   {
      Interaction += (_, _) => { };

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

   public bool Confirm(
      string question,
      Answer @default = Answer.No)
   {
      var yes = @default == Answer.Yes ? 'Y' : 'y';
      var no = @default == Answer.No ? 'N' : 'n';

      Console.Write($"{question} ({yes}/{no}) ");

      var input = Console.ReadLine()?.ToUpperInvariant();

      Interaction.Invoke(this, EventArgs.Empty);

      return @default == Answer.Yes ? input != "N" : input == "Y";
   }

   public string Read(
      string prompt)
   {
      Interaction.Invoke(this, EventArgs.Empty);

      return ReadLine.Read(prompt);
   }

   public byte[] ReadPassword(
      string prompt)
   {
      Interaction.Invoke(this, EventArgs.Empty);

      Console.Write(prompt);

      var chars = new Stack<char>();
      while (true)
      {
         var input = Console.ReadKey(true);
         switch (input.Modifiers == ConsoleModifiers.Control, input.Key)
         {
            case (false, ConsoleKey.Enter):
               Console.WriteLine();
               return Encoding.UTF8.GetBytes(chars.Reverse().ToArray());
            case (false, ConsoleKey.Backspace):
               if (chars.Count > 0)
                  chars.Pop();
               break;
            case (true, ConsoleKey.U):
               chars.Clear();
               break;
            default:
               if (!char.IsControl(input.KeyChar))
                  chars.Push(input.KeyChar);
               break;
         }
      }
   }

   public void Clear()
   {
      Console.Clear();

      // clears the console and buffer on xterm-compatible terminals
      if (Environment.GetEnvironmentVariable("TERM")?.StartsWith("xterm") == true)
         Console.Write("\x1b[3J");
   }
}