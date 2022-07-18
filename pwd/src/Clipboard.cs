using System;
using System.Diagnostics;
using System.Threading;

namespace pwd;

public interface IClipboard
{
    void Put(
        string text,
        TimeSpan clearAfter);

    void Clear();
}

public sealed class Clipboard
    : IClipboard
{
    private readonly Timer _cleaner;

    public Clipboard()
    {
        _cleaner = new(_ => Clear());
    }

    public void Put(
        string text,
        TimeSpan clearAfter)
    {
        _cleaner.Change(clearAfter, Timeout.InfiniteTimeSpan);
        CopyText(text);
    }

    public void Clear()
    {
        CopyText("");
        _cleaner.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private static void CopyText(
        string text)
    {
        var process = default(Process);
        new Action(() =>
                process = Process.Start(new ProcessStartInfo("clip.exe") {RedirectStandardInput = true}))
            .Try()
            .Map(_ => new Action(() =>
                process = Process.Start(new ProcessStartInfo("pbcopy") {RedirectStandardInput = true})).Try())
            .Map(_ => new Action(() =>
                process = Process.Start(new ProcessStartInfo("xsel") {RedirectStandardInput = true})).Try())
            .Apply(e => Console.WriteLine($"Cannot copy to the clipboard. Reason: {e.Message}"));
        process?.StandardInput.Apply(stdin => stdin.Write(text)).Apply(stdin => stdin.Close());

    }
}