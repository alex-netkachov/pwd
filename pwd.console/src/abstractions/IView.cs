using System.Threading;
using System.Threading.Tasks;

namespace pwd.console.abstractions;

/// <summary>Answers to a confirmation question.</summary>
public enum Answer
{
   Yes,
   No
}

public interface IView
   : IConsole
{
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
      ISuggestions? suggestions = null,
      IHistory? history = null,
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

   /// <summary>
   ///   Restores the view state, including the read operation.
   /// </summary>
   void Activate(
      IConsole console);

   /// <summary>
   ///   Stops reading the input.
   /// </summary>
   void Deactivate();
}