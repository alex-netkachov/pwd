using System;

namespace pwd;

public interface IView
{
    void WriteLine(
        string text);

    void Write(
        string text);

    bool Confirm(
        string question);

    string Read(
        string prompt);

    string ReadPassword(
        string prompt);

    void Clear();
}

public sealed class View
    : IView
{
    public void WriteLine(
        string text)
    {
        Console.WriteLine(text);
    }

    public void Write(
        string text)
    {
        Console.Write(text);
    }

    public bool Confirm(
        string question)
    {
        Console.Write($"{question} (y/N) ");
        return Console.ReadLine()?.ToUpperInvariant() == "Y";
    }

    public string Read(
        string prompt)
    {
        return ReadLine.Read(prompt);
    }

    public string ReadPassword(
        string prompt)
    {
        return ReadLine.ReadPassword(prompt);
    }

    public void Clear()
    {
        Console.Clear();
    }
}