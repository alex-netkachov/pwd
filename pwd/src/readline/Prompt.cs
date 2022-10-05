using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.readline;

public sealed class Prompt
{
   private readonly TimeSpan _interactionTimeout;
   private readonly Timer _interactionTimeoutTimer;
   private readonly IConsole _console;

   public event EventHandler? Idle;

   public Prompt(
      TimeSpan interactionTimeout,
      IConsole console)
   {
      _interactionTimeout = interactionTimeout;
      _console = console;
      _interactionTimeoutTimer = new(_ => Idle?.Invoke(this, EventArgs.Empty));
      _interactionTimeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
   }

   /// <summary>Async reading from the console.</summary>
   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      return Task.Run(async () =>
      {
         token.ThrowIfCancellationRequested();

         _console.Write(prompt);

         UpdateInteractionTimeoutTimer();

         var input = new List<char>();

         var cursorPosition = 0;

         IReadOnlyList<string>? suggestions = null;
         var suggestionsOffset = 0;
         var suggestionsIndex = 0;
         var suggestionsQueriedPosition = 0;

         var cts = new CancellationTokenSource();
         token.Register(() => cts.Cancel());
         var reader = _console.Subscribe(cts.Token);
         while (true)
         {
            var key = await reader.ReadAsync(token);
            UpdateInteractionTimeoutTimer();

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
                  _console.WriteLine();
                  cts.Cancel();
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
                     var tail = cursorPosition < input.Count - 1
                        ? new string(input.ToArray())[(cursorPosition + 1)..]
                        : "";

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
                        _console.Write(suggestionTail);

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
   public Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      return Task.Run(async () =>
      {
         token.ThrowIfCancellationRequested();

         _console.Write(prompt);

         UpdateInteractionTimeoutTimer();

         var input = new List<char>();
         var cts = new CancellationTokenSource();
         token.Register(() => cts.Cancel());
         var reader = _console.Subscribe(cts.Token);
         while (true)
         {
            var key = await reader.ReadAsync(token);

            UpdateInteractionTimeoutTimer();

            switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
            {
               case (false, ConsoleKey.Enter):
                  _console.WriteLine();
                  cts.Cancel();
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
                     _console.Write('*');
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

   private void MoveRight(
      int steps = 1)
   {
      var width = _console.BufferWidth;
      var (left, top) = _console.GetCursorPosition();
      for (var i = 0; i < steps; i++)
      {
         left++;
         if (left != width)
            continue;
         left = 0;
         top++;
      }
      _console.SetCursorPosition(left, top);
   }
   
   private void MoveLeft(
      int steps = 1)
   {
      var (left, top) = _console.GetCursorPosition();
      for (var i = 0; i < steps; i++)
      {
         left--;
         if (left != -1) continue;
         left = _console.BufferWidth - 1;
         top--;
      }
      _console.SetCursorPosition(left, top);
   }

   private void WriteAndMoveBack(
      string text)
   {
      var (left, top) = _console.GetCursorPosition();
      _console.Write(text);
      _console.SetCursorPosition(left, top);
   }
}