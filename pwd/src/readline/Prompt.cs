using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.readline;

public sealed class Prompt
{
   
   private readonly TimeSpan _interactionTimeout;
   private readonly Timer _interactionTimeoutTimer;

   public event EventHandler? Idle;

   public Prompt(
      TimeSpan interactionTimeout)
   {
      _interactionTimeout = interactionTimeout;
      _interactionTimeoutTimer = new(_ => Idle?.Invoke(this, EventArgs.Empty));
      _interactionTimeoutTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
   }

   /// <summary>Async reading from the console.</summary>
   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      return await Task.Run(() =>
      {
         token.ThrowIfCancellationRequested();

         Console.Write(prompt);

         var input = new List<char>();

         var cursorPosition = 0;

         IReadOnlyList<string>? suggestions = null;
         var suggestionsOffset = 0;
         var suggestionsIndex = 0;
         var suggestionsQueriedPosition = 0;

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

            if (key.Key != ConsoleKey.Tab)
            {
               // cleanup suggestions, i.g. any key except Tab makes the prompt exist from suggestion mode
               suggestions = null;
            }

            switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
            {
               case (false, ConsoleKey.LeftArrow):
                  if (cursorPosition > 0)
                  {
                     MoveLeft();
                     cursorPosition--;
                  }
                  break;
               case (false, ConsoleKey.RightArrow):
                  if (cursorPosition < input.Count)
                  {
                     MoveRight();
                     cursorPosition++;
                  }
                  break;
               case (false, ConsoleKey.Enter):
                  Console.WriteLine();
                  return new string(input.ToArray());
               case (false, ConsoleKey.Backspace):
               {
                  if (cursorPosition > 0)
                  {
                     var tail = cursorPosition < input.Count ? new string(input.ToArray())[cursorPosition..] : "";

                     MoveLeft();
                     WriteAndMoveBack($"{tail} ");

                     input.RemoveAt(cursorPosition - 1);
                     cursorPosition--;
                  }

                  break;
               }
               case (true, ConsoleKey.U):
               {
                  var tail = cursorPosition < input.Count ? new string(input.ToArray())[cursorPosition..] : "";

                  MoveLeft(cursorPosition);
                  WriteAndMoveBack(tail + new string(' ', cursorPosition));

                  input.RemoveRange(0, cursorPosition);
                  cursorPosition = 0;
                  break;
               }
               case (false, ConsoleKey.Delete):
               {
                  if (cursorPosition < input.Count)
                  {
                     var tail = cursorPosition < input.Count - 1 ? new string(input.ToArray())[(cursorPosition+1)..] : "";

                     WriteAndMoveBack($"{tail} ");

                     input.RemoveAt(cursorPosition);
                  }

                  break;
               }
               case (false, ConsoleKey.Tab):
               {
                  if (suggestionsProvider != null)
                  {
                     var suggestion = "";
                     if (suggestions == null)
                     {
                        var (offset, list) = suggestionsProvider.Get(new(input.ToArray()[..cursorPosition]));
                        suggestions = list;
                        suggestionsOffset = offset;
                        suggestionsIndex = -1;
                        suggestionsQueriedPosition = cursorPosition;
                     }
                     if (suggestions.Count > 0)
                     {
                        suggestionsIndex = (suggestionsIndex + 1) % suggestions.Count;
                        suggestion = suggestions[suggestionsIndex];
                     }
                     if (suggestion != "")
                     {
                        var currentTailLength = cursorPosition - suggestionsQueriedPosition;
                        MoveLeft(currentTailLength);
                        WriteAndMoveBack(new(' ', currentTailLength));

                        var suggestionTail = suggestion[suggestionsOffset..];
                        Console.Write(suggestionTail);

                        input.RemoveRange(suggestionsQueriedPosition, input.Count - suggestionsQueriedPosition);
                        cursorPosition = cursorPosition - currentTailLength + suggestionTail.Length;
                        input.AddRange(suggestionTail);
                     }
                  }

                  break;
               }
               default:
                  if (!char.IsControl(key.KeyChar))
                  {
                     var tail = cursorPosition < input.Count ? new string(input.ToArray())[cursorPosition..] : "";

                     WriteAndMoveBack(key.KeyChar + tail);
                     MoveRight();

                     input.Insert(cursorPosition, key.KeyChar);
                     cursorPosition++;
                  }
                  break;
            }
         }
      }, token);
   }
   
   /// <summary>Simple async reading from the console. Supports BS, Ctrl+U. Ignores control keys (e.g. tab).</summary>
   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      return await Task.Run(() =>
      {
         token.ThrowIfCancellationRequested();

         Console.Write(prompt);

         var input = new List<char>();
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
                  return new string(input.ToArray());
               case (false, ConsoleKey.Backspace):
                  if (input.Count > 0)
                  {
                     MoveLeft();
                     WriteAndMoveBack(" ");
                     input.RemoveAt(input.Count);
                  }
                  break;
               case (true, ConsoleKey.U):
                  MoveLeft(input.Count);
                  WriteAndMoveBack(new(' ', input.Count));
                  input.Clear();
                  break;
               default:
                  if (!char.IsControl(key.KeyChar))
                  {
                     Console.Write('*');
                     input.Add(key.KeyChar);
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

   private static void MoveRight(
      int steps = 1)
   {
      var width = Console.BufferWidth;
      var (left, top) = Console.GetCursorPosition();
      for (var i = 0; i < steps; i++)
      {
         left++;
         if (left != width)
            continue;
         left = 0;
         top++;
      }
      Console.SetCursorPosition(left, top);
   }
   
   private static void MoveLeft(
      int steps = 1)
   {
      var (left, top) = Console.GetCursorPosition();
      for (var i = 0; i < steps; i++)
      {
         left--;
         if (left != -1) continue;
         left = Console.BufferWidth - 1;
         top--;
      }
      Console.SetCursorPosition(left, top);
   }

   private static void WriteAndMoveBack(
      string text)
   {
      var (left, top) = Console.GetCursorPosition();
      Console.Write(text);
      Console.SetCursorPosition(left, top);
   }
}