using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace pwd;

public static class Extensions
{
    public static Exception? Try(
        this Action action)
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
    
    public static async Task<Exception?> Try(
        this Func<Task> action)
    {
        try
        {
            await action();
            return default;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public static T? Apply<T>(
        this T? value,
        Action<T> action)
    {
        if (!EqualityComparer<T>.Default.Equals(value, default))
            action(value!);
        return value;
    }

    public static TResult? Map<TValue, TResult>(
        this TValue? value,
        Func<TValue, TResult?> func)
    {
        return EqualityComparer<TValue>.Default.Equals(value, default)
            ? default
            : func(value!);
    }

    public static Exception? CheckYaml(
        this string text)
    {
        return new Action(() =>
        {
            using var input = new StringReader(text);
            new YamlStream().Load(input);
        }).Try();
    }
    
    public static (string, string, string) ParseCommand(this string input)
    {
        return Regex.Match(input, @"^\.(\w+)(?: +(.+))?$").Map(match =>
            match.Success ? ("", match.Groups[1].Value, match.Groups[2].Value) : (input, "", ""));
    }
}