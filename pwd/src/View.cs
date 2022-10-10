using System;
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
   /// <summary>Notifies listeners about reaching the user interaction timeout.</summary>
   /// <remarks>Writing to the view with Write() or WriteLine() and Clear() reset the timer.</remarks>
   event EventHandler Idle;

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

   private CancellationTokenSource? _cts;

   public event EventHandler? Idle;

   public View(
      IConsole console,
      IReader reader,
      TimeSpan interactionTimeout)
   {
      _console = console;
      _reader = reader;

      Task.Run(async () =>
      {
         var keys = _console.Subscribe();
         while (true)
         {
            await keys.ReadAsync();
            // TODO reset idle timer
         }
      });
      //_reader.Idle += (_, _) => Idle?.Invoke(this, EventArgs.Empty);
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
      var cts = new CancellationTokenSource();
      _cts = cts;
      token.Register(() => cts.Cancel());

      var yes = @default == Answer.Yes ? 'Y' : 'y';
      var no = @default == Answer.No ? 'N' : 'n';

      var input = await _reader.ReadAsync($"{question} ({yes}/{no}) ", cancellationToken: token);
      var answer = input.ToUpperInvariant();
      return @default == Answer.Yes ? answer != "N" : answer == "Y";
   }

   public async Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default)
   {
      var cts = new CancellationTokenSource();
      _cts = cts;
      token.Register(() => cts.Cancel());
      return await _reader.ReadAsync(prompt, suggestionsProvider, cts.Token);
   }

   public async Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      var cts = new CancellationTokenSource();
      _cts = cts;
      token.Register(() => cts.Cancel());
      return await _reader.ReadPasswordAsync(prompt, cts.Token);
   }

   public void Clear()
   {
      _cts?.Cancel();

      _console.Clear();
   }
}