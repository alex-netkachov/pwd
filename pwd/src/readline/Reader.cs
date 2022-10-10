using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.readline;

/// <summary>Provides reading user input routines.</summary>
/// <remarks>Reading requests are queued up and processed sequentially.</remarks>
public interface IReader
{
   Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken cancellationToken = default);

   Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken cancellationToken = default);
}

public sealed class Reader
   : IReader,
      IDisposable
{
   private State _state;
   private readonly IConsole _console;
   private readonly ChannelWriter<bool> _requests;
   private readonly CancellationTokenSource _cts;

   public Reader(
      IConsole console)
   {
      _console = console;

      _state = new(false, ImmutableQueue<(Func<Task>, TaskCompletionSource<string>, CancellationToken)>.Empty);

      _cts = new();

      var token = _cts.Token;

      var requests = Channel.CreateUnbounded<bool>();
      _requests = requests.Writer;
      Task.Run(async () =>
      {
         var reader = requests.Reader;
         while (!token.IsCancellationRequested)
         {
            await reader.ReadAsync(token);

            Func<Task>? task = null;
            CancellationToken taskToken = default;

            while (true)
            {
               var initial = _state;
               if (initial.Disposed)
                  return;
               if (initial.Queue.IsEmpty)
                  break;
               var updated = initial with { Queue = initial.Queue.Dequeue(out var item) };
               if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
                  continue;
               (task, _, taskToken) = item;
               break;
            }

            if (task == null)
               continue;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, taskToken);
            await Task.Run(task, cts.Token);
         }
      }, token);
   }

   public void Dispose()
   {
      State initial;
      while (true)
      {
         initial = _state;
         if (initial.Disposed)
            return;
         var updated = new State(true, initial.Queue.Clear());
         if (initial == Interlocked.CompareExchange(ref _state, updated, initial))
            break;
      }

      _requests.Complete();
      _cts.Cancel();
      _cts.Dispose();

      foreach (var (_, tcs, _) in initial.Queue)
         tcs.TrySetCanceled();
   }

   /// <summary>Async reading from the console.</summary>
   /// <remarks>Task can be cancelled either by the cancellation token or timeout.</remarks>
   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken cancellationToken = default)
   {
      return Enqueue(
         tcs => ReadAsyncInt(prompt, suggestionsProvider, tcs, cancellationToken),
         cancellationToken);
   }
   
   /// <summary>Simple async reading from the console. Supports BS, Ctrl+U. Ignores control keys (e.g. tab).</summary>
   public Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken cancellationToken = default)
   {
      return Enqueue(
         tcs => ReadPasswordAsyncInt(prompt, tcs, cancellationToken),
         cancellationToken);
   }

   private Task<string> Enqueue(
      Func<TaskCompletionSource<string>, Task> action,
      CancellationToken cancellationToken)
   {
      var tcs = new TaskCompletionSource<string>();

      Task TaskFactory() => action(tcs);

      while (true)
      {
         var initial = _state;
         if (_state.Disposed)
            throw new ObjectDisposedException(nameof(Reader));
         var updated = _state with { Queue = initial.Queue.Enqueue((TaskFactory, tcs, cancellationToken)) };
         if (initial != Interlocked.CompareExchange(ref _state, updated, initial))
            continue;
         while (!_requests.TryWrite(true))
         {
         }
         break;
      }

      return tcs.Task;
   }

   private async Task ReadAsyncInt(
      string prompt,
      ISuggestionsProvider? suggestionsProvider,
      TaskCompletionSource<string> tcs,
      CancellationToken cancellationToken)
   {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
      var token = cts.Token;
      token.Register(() => tcs.TrySetCanceled());

      token.ThrowIfCancellationRequested();

      _console.Write(prompt);

      var input = new List<char>();

      var cursorPosition = 0;

      IReadOnlyList<string>? suggestions = null;
      var suggestionsOffset = 0;
      var suggestionsIndex = 0;
      var suggestionsQueriedPosition = 0;

      using var reader = _console.Subscribe();
      while (!token.IsCancellationRequested)
      {
         var key = await reader.ReadAsync(token);

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
               reader.Dispose();
               tcs.TrySetResult(new(input.ToArray()));
               return;
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
   }

   private async Task ReadPasswordAsyncInt(
      string prompt,
      TaskCompletionSource<string> tcs,
      CancellationToken cancellationToken)
   {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
      var token = cts.Token;
      token.Register(() => tcs.TrySetCanceled());

      token.ThrowIfCancellationRequested();

      _console.Write(prompt);
      
      var input = new List<char>();
      using var reader = _console.Subscribe();
      while (!token.IsCancellationRequested)
      {
         var key = await reader.ReadAsync(token);

         switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
         {
            case (false, ConsoleKey.Enter):
               _console.WriteLine();
               reader.Dispose();
               tcs.TrySetResult(new(input.ToArray()));
               return;
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
   
   private record State(
      bool Disposed,
      ImmutableQueue<(Func<Task>, TaskCompletionSource<string>, CancellationToken)> Queue);
}