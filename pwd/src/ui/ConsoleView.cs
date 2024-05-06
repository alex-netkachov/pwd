using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using pwd.ui.console;
using pwd.ui.readline;

namespace pwd.ui;

public sealed class ConsoleView(
      IConsole console,
      IReader reader)
   : IView
{
   private ImmutableList<CancellationTokenSource> _ctss = ImmutableList<CancellationTokenSource>.Empty;
   private CancellationTokenSource _cts = new();

   public void Write(
      string text)
   {
      console.Write(text);
   }

   public void WriteLine(
      string text)
   {
      console.WriteLine(text);
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
         input = await reader.ReadAsync($"{question} ({yes}/{no}) ", token: cts.Token);
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
         result = await reader.ReadAsync(prompt, suggestionsProvider, cts.Token);
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
         result = await reader.ReadPasswordAsync(prompt, cts.Token);
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

      console.Clear();
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