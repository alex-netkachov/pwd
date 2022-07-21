using System;
using System.Diagnostics;
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
    private readonly Timer _cleaner;
    private readonly Channel<string> _channel;

    public Clipboard()
    {
        _cleaner = new(_ => Clear());

        _channel = Channel.CreateUnbounded<string>();
        Task.Run(async () =>
        {
            var reader = _channel.Reader;
            while (!reader.Completion.IsCompleted)
            {
                var text = await reader.ReadAsync();
                CopyText(text);
            }
        });
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
    }

    public void Put(
        string text,
        TimeSpan clearAfter)
    {
        _cleaner.Change(clearAfter, Timeout.InfiniteTimeSpan);
        _channel.Writer.TryWrite(text);
    }

    public void Clear()
    {
        _channel.Writer.TryWrite("");
        _cleaner.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private static void CopyText(
        string text)
    {
        Exception? Run(
            string executable)
        {
            try
            {
                var startInfo = new ProcessStartInfo(executable)
                {
                    RedirectStandardInput = true
                };

                var process = Process.Start(startInfo);
                if (process == null)
                    throw new($"Starting the executable '{executable}' failed.");

                var stdin = process.StandardInput;
                stdin.Write(text);
                stdin.Close();
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }

        if (Run("clip.exe") is not null &&
            Run("pbcopy") is not null &&
            Run("xsel") is not null)
        {
            Console.WriteLine("Cannot copy to the clipboard.");
        }
    }
}