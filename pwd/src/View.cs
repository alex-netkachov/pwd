using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd;

/// <summary>Answers to a confirmation question.</summary>
public enum Answer
{
   Yes,
   No
}

public interface IView
{
   void Write(
      string text);

   void WriteLine(
      string text);

   /// <summary>Requests a confirmation from the user, i.e. asks yes/no question.</summary>
   /// <remarks>Cancelling the request with the cancellation token raises TaskCanceledException.</remarks>
   Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default);

   /// <summary>Reads the line from the console.</summary>
   /// <remarks>
   ///   Supports navigation with Home, End, Left, Right, Ctrl+Left, Ctrl+Right. Supports editing with
   ///   Backspace, Delete, Ctrl+U. Provides a suggestion when Tab is pressed.  
   ///
   ///   Cancelling the request with the cancellation token raises TaskCanceledException.
   /// </remarks>
   Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default);

   /// <summary>Reads the line from the console.</summary>
   /// <remarks>
   ///   Supports navigation with Home, End, Left, Right. Supports editing with Backspace, Delete, Ctrl+U.  
   ///
   ///   Cancelling the request with the cancellation token raises TaskCanceledException.
   /// </remarks>
   Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default);

   /// <summary>Clears the console and its buffer, if possible.</summary>
   void Clear();
}

public sealed class View
   : IView
{
   private readonly IConsole _console;
   private readonly IReader _reader;

   private ImmutableList<CancellationTokenSource> _ctss;
   private CancellationTokenSource _cts;

   public View(
      IConsole console,
      IReader reader)
   {
      _console = console;
      _reader = reader;

      _ctss = ImmutableList<CancellationTokenSource>.Empty;
      _cts = new();
   }

   public void Write(
      string text)
   {
      _console.Write(text);
   }

   public void WriteLine(
      string text)
   {
      _console.WriteLine(text);
   }

   public async Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      var yes = @default == Answer.Yes ? 'Y' : 'y';
      var no = @default == Answer.No ? 'N' : 'n';

      var cts = CreateLinkedCancellationTokenSource(token);

      string input;
      try
      {
         input = await _reader.ReadAsync($"{question} ({yes}/{no}) ", token: cts.Token);
      }
      finally
      {
         RemoveLinkedCancellationTokenSource(cts);
      }

      var answer = input.ToUpperInvariant();
      var result = @default == Answer.Yes ? answer != "N" : answer == "Y";

      return result;
   }

   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      var cts = CreateLinkedCancellationTokenSource(token);

      string result;
      try
      {
         result = await _reader.ReadAsync(prompt, suggestionsProvider, cts.Token);
      }
      finally
      {
         RemoveLinkedCancellationTokenSource(cts);
      }

      return result;
   }

   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      var cts = CreateLinkedCancellationTokenSource(token);

      string result;
      try
      {
         result = await _reader.ReadPasswordAsync(prompt, cts.Token);
      }
      finally
      {
         RemoveLinkedCancellationTokenSource(cts);
      }

      return result;
   }

   public void Clear()
   {
      _cts.Cancel();
      _cts.Dispose();

      _cts = new();

      _console.Clear();
   }

   private CancellationTokenSource CreateLinkedCancellationTokenSource(
      CancellationToken token)
   {
      var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
      while (true)
      {
         var initial = _ctss;
         var updated = initial.Add(linked);
         if (Interlocked.CompareExchange(ref _ctss, updated, initial) == initial)
            break;
      }
      return linked;
   }

   private void RemoveLinkedCancellationTokenSource(
      CancellationTokenSource cts)
   {
      while (true)
      {
         var initial = _ctss;
         var updated = initial.Remove(cts);
         if (Interlocked.CompareExchange(ref _ctss, updated, initial) != initial)
            continue;
         cts.Dispose();
         break;
      }
   }
}