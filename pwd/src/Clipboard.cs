using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd;

public interface IClipboard
{
   /// <summary>Put the text to the clipboard and clear it after specified time.</summary>
   void Put(
      string text,
      TimeSpan clearAfter);

   /// <summary>Replace the clipboard content with an empty string.</summary>
   void Clear();
}

public sealed class Clipboard
   : IClipboard,
      IDisposable
{
   private readonly ILogger _logger;
   private readonly IRunner _runner;
   private readonly ChannelWriter<string> _writer;
   private readonly Timer _cleaner;
   private readonly CancellationTokenSource _cts;

   public Clipboard(
      ILogger logger,
      IRunner runner)
   {
      _logger = logger;
      _runner = runner;

      _cleaner = new(_ => Clear());

      _cts = new();

      var token = _cts.Token;

      var channel = Channel.CreateUnbounded<string>();
      var reader = channel.Reader;

      _writer = channel.Writer;

      Task.Run(async () =>
      {
         while (!reader.Completion.IsCompleted && !token.IsCancellationRequested)
         {
            string text;
            try
            {
               text = await reader.ReadAsync(token);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token)
            {
               break;
            }

            CopyText(text);
         }
      });
   }

   public void Put(
      string text,
      TimeSpan clearAfter)
   {
      _cleaner.Change(clearAfter, Timeout.InfiniteTimeSpan);

      while (!_writer.TryWrite(text))
      {
      }
   }

   public void Clear()
   {
      while (!_writer.TryWrite(""))
      {
      }

      _cleaner.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
   }

   public void Dispose()
   {
      _cts.Cancel();
      _cts.Dispose();
      _writer.Complete();
   }

   private void CopyText(
      string text)
   {
      if (_runner.Run("clip.exe", text) is not null &&
          _runner.Run("pbcopy", text) is not null &&
          _runner.Run("xsel", text) is not null)
      {
         _logger.Error("Cannot copy to the clipboard.");
      }
   }
}