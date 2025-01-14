using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;

namespace pwd.console.readers;

public sealed class PasswordReader(
      IConsole console)
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

         _promptCts?.Dispose();

         if (_cts is { } cts)
         {
            cts.Cancel();
            cts.Dispose();
         }
      }
   }

   /// <summary>Simple async reading from the console. Supports BS, Ctrl+U. Ignores control keys (e.g. tab).</summary>
   public Task<string> ReadAsync(
      string prompt,
      CancellationToken token)
   {
      if (_disposed)
         throw new ObjectDisposedException(nameof(PasswordReader));

      lock (_lock)
      {
         if (_disposed)
            throw new ObjectDisposedException(nameof(PasswordReader));
         
         var promptTcs =
            new TaskCompletionSource<string>();

         var promptCts =
            CancellationTokenSource.CreateLinkedTokenSource(
               _cts.Token,
               token);

         promptCts.Token.Register(() => promptTcs.TrySetCanceled());

         _promptCts = promptCts;
         _promptTcs = promptTcs;

         _input = new();
         _position = 0;

         _interceptor =
            console.Intercept(
               key =>
               {
                  if (_disposed)
                     return;

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

      switch (key.Modifiers == ConsoleModifiers.Control, key.Key)
      {
         case (false, ConsoleKey.Enter):
            console.WriteLine("");
            _interceptor?.Dispose();
            _promptTcs?.TrySetResult(new(input.ToArray()));
            return;
         case (false, ConsoleKey.Backspace):
            DeletePrevious(input, ref _position);
            break;
         case (true, ConsoleKey.U):
            DeleteFromStartToCursor(input, ref _position);
            break;
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

      console.WriteAndMoveBack('*' + tail);
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