using System;
using System.Text;
using System.Threading.Tasks;

namespace pwd.tests;

public sealed class MockClipboard
    : IClipboard
{
    public void Put(
        string text,
        TimeSpan clearAfter)
    {
    }

    public void Clear()
    {
    }
}

public sealed class MockView
    : IView
{
    private readonly StringBuilder _builder = new();

    public void WriteLine(
        string text)
    {
        _builder.AppendLine(text);
    }

    public void Write(
        string text)
    {
        _builder.Append(text);
    }

    public bool Confirm(
        string question)
    {
        return true;
    }

    public void Clear()
    {
        _builder.Clear();
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}

public sealed class MockCipher
    : ICipher
{
    public Task<byte[]> Encrypt(
        string text)
    {
        return Task.FromResult(Encoding.UTF8.GetBytes(text));
    }

    public Task<string> Decrypt(
        byte[] data)
    {
        return Task.FromResult(Encoding.UTF8.GetString(data));
    }
}