using System.Collections.Immutable;
using System.Threading.Channels;
using pwd.console.delegated;
using pwd.console.abstractions;

namespace pwd.console;

public sealed class Reader
   : IReader,
     IDisposable
{
   private record State(
      bool Disposed,
      ImmutableQueue<QueueItem> Queue);

   private record QueueItem(
      Func<Task> Task,
      TaskCompletionSource<string> Complete,
      CancellationToken CancellationToken);

   private State _state;

   private readonly IConsole _console;
   private readonly ChannelWriter<bool> _requests;
   private readonly CancellationTokenSource _cts;

   public Reader(
      IConsole console)
   {
      _console = console;

      _state = new(false, ImmutableQueue<QueueItem>.Empty);

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
            try
            {
               await Task.Run(task, cts.Token);
            }
            catch (OperationCanceledException e)
            {
               if (e.CancellationToken == taskToken)
               {
                  // single reader is cancelled
               }
               // when the reading operation is cancelled, this 
               // ignore and continue
            }
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
      IHistoryProvider? historyProvider = null,
      CancellationToken token = default)
   {
      return Enqueue(
         tcs => ReadAsyncInt(prompt, suggestionsProvider, tcs, token),
         token);
   }
   
   /// <summary>Simple async reading from the console. Supports BS, Ctrl+U. Ignores control keys (e.g. tab).</summary>
   public Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      return Enqueue(
         tcs => ReadPasswordAsyncInt(prompt, tcs, token),
         token);
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
         var updated = _state with { Queue = initial.Queue.Enqueue(new(TaskFactory, tcs, cancellationToken)) };
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
      CancellationToken token = default)
   {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
      var localToken = cts.Token;
      localToken.Register(() => tcs.TrySetCanceled());

      localToken.ThrowIfCancellationRequested();

      _console.Write(prompt);

      var input = new List<char>();

      var position = 0;

      IReadOnlyList<string>? suggestions = null;
      var suggestionsOffset = 0;
      var suggestionsIndex = 0;
      var suggestionsQueriedPosition = 0;

      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
      var subscription =
         _console.Subscribe(
            new Observer<ConsoleKeyInfo>(key =>
            {
               while (!channel.Writer.TryWrite(key)) /* empty*/;
            }));

      while (!localToken.IsCancellationRequested)
      {
         var key = await channel.Reader.ReadAsync(localToken);

         if (key.Key != ConsoleKey.Tab)
         {
            // cleanup suggestions, i.g. any key except Tab makes the prompt exist from suggestion mode
            suggestions = null;
         }

         switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
         {
            case (false, ConsoleKey.Enter):
               _console.WriteLine();
               subscription.Dispose();
               tcs.TrySetResult(new(input.ToArray()));
               return;
            case (false, ConsoleKey.LeftArrow):
               MoveLeft(1, ref position);
               break;
            case (false, ConsoleKey.RightArrow):
               MoveRight(1, input.Count, ref position);
               break;
            case (false, ConsoleKey.Backspace):
               DeletePrevious(input, ref position);
               break;
            case (true, ConsoleKey.U):
               DeleteFromStartToCursor(input, ref position);
               break;
            case (false, ConsoleKey.Delete):
            {
               if (position < input.Count)
               {
                  var tail = position < input.Count - 1
                     ? new string(input.ToArray())[(position + 1)..]
                     : "";

                  WriteAndMoveBack($"{tail} ");

                  input.RemoveAt(position);
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
                     var list = suggestionsProvider.Suggestions(new(input.ToArray()[..position]));
                     suggestions = list;
                     suggestionsOffset = position;
                     suggestionsIndex = -1;
                     suggestionsQueriedPosition = position;
                  }

                  if (suggestions.Count > 0)
                  {
                     suggestionsIndex = (suggestionsIndex + 1) % suggestions.Count;
                     suggestion = suggestions[suggestionsIndex];
                  }

                  if (suggestion != "")
                  {
                     var currentTailLength = position - suggestionsQueriedPosition;
                     var suggestionTail = suggestion[suggestionsOffset..];

                     input.RemoveRange(suggestionsQueriedPosition, input.Count - suggestionsQueriedPosition);
                     input.AddRange(suggestionTail);

                     MoveLeft(currentTailLength, ref position);
                     WriteAndMoveBack(new(' ', currentTailLength));
                     WriteAndMoveBack(suggestionTail);
                     MoveRight(suggestionTail.Length, input.Count, ref position);
                  }
               }

               break;
            }
            default:
               CharacterKey(input, key.KeyChar, false, ref position);
               break;
         }
      }
   }

   private async Task ReadPasswordAsyncInt(
      string prompt,
      TaskCompletionSource<string> tcs,
      CancellationToken token = default)
   {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
      var localToken = cts.Token;
      localToken.Register(() => tcs.TrySetCanceled());

      localToken.ThrowIfCancellationRequested();

      _console.Write(prompt);
      
      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
      var subscription =
         _console.Subscribe(
            new Observer<ConsoleKeyInfo>(key =>
            {
               while (!channel.Writer.TryWrite(key)) /* empty*/;
            }));

      var input = new List<char>();
      var position = 0;
      while (!localToken.IsCancellationRequested)
      {
         var key = await channel.Reader.ReadAsync(localToken);

         switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
         {
            case (false, ConsoleKey.Enter):
               _console.WriteLine();
               subscription.Dispose();
               tcs.TrySetResult(new(input.ToArray()));
               return;
            case (false, ConsoleKey.Backspace):
               DeletePrevious(input, ref position);
               break;
            case (true, ConsoleKey.U):
               DeleteFromStartToCursor(input, ref position);
               break;
            default:
               CharacterKey(input, key.KeyChar, true, ref position);
               break;
         }
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

      WriteAndMoveBack($"{tail} ");
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

      WriteAndMoveBack(tail + new string(' ', length - tail.Length));
   }

   private void CharacterKey(
      List<char> input,
      char @char,
      bool obscure,
      ref int position)
   {
      if (char.IsControl(@char))
         return;

      var tail =
         position < input.Count
            ? new string(input.ToArray())[position..]
            : "";

      input.Insert(position, @char);

      WriteAndMoveBack((obscure ? '*' : @char) + tail);
      MoveRight(1, input.Count, ref position);
   }

   private void MoveRight(
      int steps,
      int limit,
      ref int position)
   {
      if (position >= limit)
         return;

      var width = _console.BufferWidth;
      var cursorPosition = _console.GetCursorPosition();
      var (left, top) = (cursorPosition.X, cursorPosition.Y);
      for (var i = 0; i < steps; i++)
      {
         left++;
         if (left != width)
            continue;
         left = 0;
         top++;
      }
      _console.SetCursorPosition(new(left, top));

      position += steps;
   }
   
   private void MoveLeft(
      int steps,
      ref int position)
   {
      if (position == 0)
         return;

      var cursorPosition = _console.GetCursorPosition();
      var (left, top) = (cursorPosition.X, cursorPosition.Y);
      for (var i = 0; i < steps; i++)
      {
         left--;
         if (left != -1) continue;
         left = _console.BufferWidth - 1;
         top--;
      }

      _console.SetCursorPosition(new(left, top));
      
      position -= steps;
   }

   private void WriteAndMoveBack(
      string text)
   {
      var position = _console.GetCursorPosition();
      _console.Write(text);
      _console.SetCursorPosition(position);
   }
}