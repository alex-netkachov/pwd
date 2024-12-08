using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.ui.abstractions;

namespace pwd.ui;

public sealed class View(
      IConsole console,
      IReader reader)
   : IView
{
   private CancellationTokenSource? _cts;
   private readonly List<(string Text, bool NewLine)> _writes = new();
   private object? _read;
   private readonly object _sync = new { };
   private bool _activated = true;

   private class ConfirmCallArgs(
      string question,
      Answer @default,
      CancellationToken token,
      TaskCompletionSource<bool> taskCompletionSource)
   {
      public string Question => question;
      public Answer Default => @default;
      public CancellationToken Token => token;
      public TaskCompletionSource<bool> TaskCompletionSource => taskCompletionSource;
   }

   private class ReadCallArgs(
      string prompt,
      ISuggestionsProvider? suggestionsProvider,
      IHistoryProvider? historyProvider,
      CancellationToken token,
      TaskCompletionSource<string> taskCompletionSource)
   {
      public string Prompt => prompt;
      public ISuggestionsProvider? SuggestionsProvider => suggestionsProvider;
      public IHistoryProvider? HistoryProvider => historyProvider;
      public CancellationToken Token => token;
      public TaskCompletionSource<string> TaskCompletionSource => taskCompletionSource;
   }
   
   private class ReadPasswordCallArgs(
      string prompt,
      CancellationToken token,
      TaskCompletionSource<string> taskCompletionSource)
   {
      public string Prompt => prompt;
      public CancellationToken Token => token;
      public TaskCompletionSource<string> TaskCompletionSource => taskCompletionSource;
   }

   public void Write(
      string text)
   {
      lock (_sync)
      {
         console.Write(text);
         _writes.Add((text, false));
      }
   }

   public void WriteLine(
      string text)
   {
      lock (_sync)
      {
         console.WriteLine(text);
         _writes.Add((text, true));
      }
   }

   public Task<bool> ConfirmAsync(
      string question,
      Answer @default = Answer.No,
      CancellationToken token = default)
   {
      var tcs = new TaskCompletionSource<bool>();

      var call =
         new ConfirmCallArgs(
            question,
            @default,
            token,
            tcs);

      ConfirmWithTask(call);

      return tcs.Task;
   }

   private void ConfirmWithTask(
      ConfirmCallArgs args)
   {
      CancellationTokenSource cts;
      lock (_sync)
      {
         if (_cts != null)
            throw new InvalidOperationException("Another operation is in progress.");
         cts = new CancellationTokenSource();
         _cts = cts;
         _read = args;
      }

      var question = args.Question;
      var @default = args.Default;
      var token = args.Token;
      var tcs = args.TaskCompletionSource;

      var registration = token.Register(() => cts.Cancel());

      Task.Run(
         async () =>
         {
            bool result;
            try
            {
               var yes = @default == Answer.Yes ? 'Y' : 'y';
               var no = @default == Answer.No ? 'N' : 'n';

               var input =
                  await reader.ReadAsync(
                     $"{question} ({yes}/{no}) ",
                     null,
                     null,
                     cts.Token);

               var answer = input.ToUpperInvariant();

               result =
                  @default == Answer.Yes
                     ? answer != "N"
                     : answer == "Y";
            }
            catch (Exception e)
            {
               tcs.TrySetException(e);
               return;
            }
            finally
            {
               await registration.DisposeAsync();
               lock (_sync)
               {
                  cts.Dispose();
                  _cts = null;
                  _read = null;
               }
            }

            tcs.TrySetResult(result);
         },
         token);
   }

   public Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      IHistoryProvider? historyProvider = null,
      CancellationToken token = default)
   {
      var tcs = new TaskCompletionSource<string>();

      var call =
         new ReadCallArgs(
            prompt,
            suggestionsProvider,
            historyProvider,
            token,
            tcs);

      ReadWithTask(call);

      return tcs.Task;
   }
   
   private void ReadWithTask(
      ReadCallArgs args)
   {
      CancellationTokenSource cts;
      lock (_sync)
      {
         if (_cts != null)
            throw new InvalidOperationException("Another operation is in progress.");
         cts = new CancellationTokenSource();
         _cts = cts;
         _read = args;
      }

      var prompt = args.Prompt;
      var suggestionsProvider = args.SuggestionsProvider;
      var historyProvider = args.HistoryProvider;
      var token = args.Token;
      var tcs = args.TaskCompletionSource;

      var registration = token.Register(() => cts.Cancel());

      Task.Run(
         async () =>
         {
            string result;
            try
            {
               result =
                  await reader.ReadAsync(
                     prompt,
                     suggestionsProvider,
                     historyProvider,
                     cts.Token);
            }
            catch (Exception e)
            {
               tcs.TrySetException(e);
               return;
            }
            finally
            {
               await registration.DisposeAsync();
               lock (_sync)
               {
                  cts.Dispose();
                  _cts = null;
                  _read = null;
               }
            }

            tcs.TrySetResult(result);
         },
         token);
   }
   
   public Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default)
   {
      var tcs = new TaskCompletionSource<string>();

      var call =
         new ReadPasswordCallArgs(
            prompt,
            token,
            tcs);

      ReadPasswordWithTask(call);

      return tcs.Task;
   }
   
   private void ReadPasswordWithTask(
      ReadPasswordCallArgs args)
   {
      CancellationTokenSource cts;
      lock (_sync)
      {
         if (_cts != null)
            throw new InvalidOperationException("Another operation is in progress.");
         cts = new CancellationTokenSource();
         _cts = cts;
         _read = args;
      }

      var prompt = args.Prompt;
      var token = args.Token;
      var tcs = args.TaskCompletionSource;

      var registration = token.Register(() => cts.Cancel());

      Task.Run(
         async () =>
         {
            string result;
            try
            {
               result =
                  await reader.ReadPasswordAsync(
                     prompt,
                     cts.Token);
            }
            catch (Exception e)
            {
               tcs.TrySetException(e);
               return;
            }
            finally
            {
               await registration.DisposeAsync();
               lock (_sync)
               {
                  cts.Dispose();
                  _cts = null;
                  _read = null;
               }
            }

            tcs.TrySetResult(result);
         },
         token);
   }

   public void Clear()
   {
      _cts?.Cancel();
      _cts?.Dispose();

      _cts = null;

      console.Clear();
      _writes.Clear();
   }

   public void Activate()
   {
      lock (_sync)
      {
         if (_activated)
            return;

         _activated = true;

         foreach (var (text, newLine) in _writes)
         {
            if (newLine)
               WriteLine(text);
            else
               Write(text);
         }

         if (_read is ConfirmCallArgs confirmCall)
            ConfirmWithTask(confirmCall);
      }
   }

   public void Deactivate()
   {
      lock (_sync)
      {
         _activated = false;
         _cts?.Cancel();
      }
   }
}