using System;
using System.Collections.Generic;
using System.Linq;

namespace pwd;

public enum Choice
{
   Accept,
   Reject
}

public interface IView
{
   void WriteLine(
      string text);

   void Write(
      string text);

   bool Confirm(
      string question,
      Choice @default = Choice.Reject);

   string Read(
      string prompt);

   string ReadPassword(
      string prompt);

   void Clear();
}

public sealed class View
   : IView
{
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

   public bool Confirm(
      string question,
      Choice @default = Choice.Reject)
   {
      Console.Write($"{question} ({(@default == Choice.Accept ? "Y/n" : "y/N")}) ");
      var input = Console.ReadLine()?.ToUpperInvariant();
      return @default == Choice.Accept ? input != "N" : input == "Y";
   }

   public string Read(
      string prompt)
   {
      return ReadLine.Read(prompt);
   }

   public string ReadPassword(
      string prompt)
   {
      Console.Write(prompt);

      var chars = new Stack<char>();
      while (true)
      {
         var input = Console.ReadKey(true);
         switch (input.Modifiers == ConsoleModifiers.Control, input.Key)
         {
            case (false, ConsoleKey.Enter):
               Console.WriteLine();
               return string.Join("", chars.Reverse());
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
   }
}