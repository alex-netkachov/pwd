using System;
using System.Collections.Generic;
using System.IO;

namespace pwd;

public static class Extensions
{
    public static Exception? Try(this Action action)
    {
        try
        {
            action();
            return default;
        }
        catch (Exception e)
        {
            return e;
        }
    }
    
    public static T? Apply<T>(this T? value, Action<T> action)
    {
        if (!EqualityComparer<T>.Default.Equals(value, default)) action(value!);
        return value;
    }

    public static TResult? Map<TValue, TResult>(this TValue? value, Func<TValue, TResult?> func)
    {
        return EqualityComparer<TValue>.Default.Equals(value, default) ? default : func(value!);
    }
    
    public static byte[] ReadBytes(this Stream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset != length)
        {
            var read = stream.Read(buffer, offset, length - offset);
            if (read == 0)
                throw new Exception("Reading from the stream failed.");
            offset += read;
        }
        return buffer;
    }
}