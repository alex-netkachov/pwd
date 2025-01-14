using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.console.readers;

public sealed class CommandReader(
      IConsole console,
      ISuggestions? suggestions = null,
      IHistory? history = null)
   : IReader,
     IDisposable
{
   private readonly object _lock = new { };
   private readonly CancellationTokenSource _cts = new();

   private bool _disposed;
   private TaskCompletionSource<string>? _promptTcs;
   private CancellationTokenSource? _promptCts; 
   private IDisposable? _interceptor;
   private List<char>? _input;
   private int _position;
   
   private IReadOnlyList<string>? _suggestions;
   private int _suggestionsOffset;
   private int _suggestionsIndex;
   private int _suggestionsQueriedPosition;

   public void Dispose()
   {
      if (_disposed)
         return;

      lock (_lock)
      {
         if (_disposed)
            return;

         _disposed = true;

         _interceptor?.Dispose();

         if (_cts is { } cts)
         {
            cts.Cancel();
            cts.Dispose();
         }
      }
   }

   /// <summary>Async reading from the console.</summary>
   /// <remarks>Task can be cancelled either by the cancellation token or timeout.</remarks>
   public Task<string> ReadAsync(
      string prompt,
      CancellationToken token)
   {
      lock (_lock)
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         
         var promptTcs =
            new TaskCompletionSource<string>();

         var promptCts =
            CancellationTokenSource.CreateLinkedTokenSource(
               _cts.Token,
               token);

         promptCts.Token.Register(() => promptTcs.TrySetCanceled());

         _promptTcs = promptTcs;
         _promptCts = promptCts;

         _input = new();
         _position = 0;

         _suggestions = null;
         _suggestionsOffset = 0;
         _suggestionsIndex = 0;
         _suggestionsQueriedPosition = 0;

         _interceptor =
            console.Intercept(
               key =>
               {
                  lock (_lock)
                  {
                     if (_disposed)
                        return;

                     ProcessKey(key);
                  }
               });
         
         console.Write(prompt);

         return _promptTcs.Task;         
      }
   }

   private void ProcessKey(
      ConsoleKeyInfo key)
   {
      if (_input is not { } input)
         return;

      if (_promptCts is not { } cts)
         return;

      cts.Token.ThrowIfCancellationRequested();

      if (key.Key != ConsoleKey.Tab)
      {
         // cleanup suggestions, i.g. any key except Tab makes the prompt exist from suggestion mode
         _suggestions = null;
      }

      switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
      {
         case (false, ConsoleKey.Enter):
            console.WriteLine("");
            history?.Add(new(input.ToArray()));
            _interceptor?.Dispose();
            _promptTcs?.TrySetResult(new(input.ToArray()));
            return;
         case (false, ConsoleKey.LeftArrow):
            MoveLeft(1, ref _position);
            break;
         case (false, ConsoleKey.RightArrow):
            MoveRight(1, input.Count, ref _position);
            break;
         case (false, ConsoleKey.Backspace):
            DeletePrevious(input, ref _position);
            break;
         case (true, ConsoleKey.U):
            DeleteFromStartToCursor(input, ref _position);
            break;
         case (false, ConsoleKey.Delete):
         {
            if (_position < input.Count)
            {
               var tail = _position < input.Count - 1
                  ? new string(input.ToArray())[(_position + 1)..]
                  : "";

               console.WriteAndMoveBack($"{tail} ");

               input.RemoveAt(_position);
            }

            break;
         }
         case (false, ConsoleKey.Tab):
         {
            if (suggestions != null)
            {
               var suggestion = "";
               if (_suggestions == null)
               {
                  var list =
                     suggestions.Get(
                        new(input.ToArray()[.._position]),
                        _position);
                     
                  _suggestions = list;
                  _suggestionsOffset = _position;
                  _suggestionsIndex = -1;
                  _suggestionsQueriedPosition = _position;
               }

               if (_suggestions.Count > 0)
               {
                  _suggestionsIndex = (_suggestionsIndex + 1) % _suggestions.Count;
                  suggestion = _suggestions[_suggestionsIndex];
               }

               if (suggestion != "")
               {
                  var currentTailLength = _position - _suggestionsQueriedPosition;
                  var suggestionTail = suggestion[_suggestionsOffset..];

                  input.RemoveRange(_suggestionsQueriedPosition, input.Count - _suggestionsQueriedPosition);
                  input.AddRange(suggestionTail);

                  MoveLeft(currentTailLength, ref _position);
                  console.WriteAndMoveBack(new(' ', currentTailLength));
                  console.WriteAndMoveBack(suggestionTail);
                  MoveRight(suggestionTail.Length, input.Count, ref _position);
               }
            }

            break;
         }
         default:
            CharacterKey(input, key.KeyChar, ref _position);
            break;
      }
   }

   private void DeletePrevious(
      List<char> input,
      ref int position)
   {
      if (position == 0)
         return;

      input.RemoveAt(position - 1);

      var tail =
         position < input.Count
            ? new string(input.ToArray())[position..]
            : "";

      MoveLeft(1, ref position);

      console.WriteAndMoveBack($"{tail} ");
   }

   private void DeleteFromStartToCursor(
      List<char> input,
      ref int position)
   {
      if (position == 0)
         return;

      var length = input.Count;
      input.RemoveRange(0, position);

      var tail =
         position < input.Count
            ? new string(input.ToArray())[position..]
            : "";

      MoveLeft(position, ref position);

      console.WriteAndMoveBack(tail + new string(' ', length - tail.Length));
   }

   private void CharacterKey(
      List<char> input,
      char @char,
      ref int position)
   {
      if (char.IsControl(@char))
         return;

      var tail =
         position < input.Count
            ? new string(input.ToArray())[position..]
            : "";

      input.Insert(position, @char);

      console.WriteAndMoveBack(@char + tail);
      MoveRight(1, input.Count, ref position);
   }

   private void MoveRight(
      int steps,
      int limit,
      ref int position)
   {
      if (position >= limit)
         return;

      console.MoveRight(steps);

      position += steps;
   }
   
   private void MoveLeft(
      int steps,
      ref int position)
   {
      if (position == 0)
         return;

      console.MoveLeft(steps);

      position -= steps;
   }
}