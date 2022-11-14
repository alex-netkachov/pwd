using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using pwd.context.repl;
using pwd.contexts.shared;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts;

public static class Shared
{
   public static (string, string, string) ParseCommand(
      string input)
   {
      var match = Regex.Match(input, @"^\.(\w+)(?: +(.+))?$");
      return match.Success
         ? ("", match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value)
         : (input, "", "");
   }

   public static Exception? CheckYaml(
      string text)
   {
      try
      {
         using var input = new StringReader(text);
         new YamlStream().Load(input);
         return default;
      }
      catch (Exception e)
      {
         return e;
      }
   }

   public static string Password()
   {
      var letters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
      var capitals = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
      var digits = "01234567890".ToCharArray();
      var special = "!$%&@{}*+".ToCharArray();

      var selected =
         Pick(letters, 5)
            .Concat(Pick(capitals, 4))
            .Concat(Pick(digits, 4))
            .Concat(Pick(special, 2));

      return new(Shuffle(selected).ToArray());
   }

   private static IReadOnlyCollection<char> Pick(char[] chars, int count)
   {
      if (chars == null)
         throw new ArgumentNullException(nameof(chars));
      if (count < 0)
         throw new ArgumentOutOfRangeException(nameof(count));

      var list = new List<char>(count);
      for (var i = 0; i < count; i++)
         list.Add(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
      return list;

   }

   private static IReadOnlyList<char> Shuffle(IEnumerable<char> chars)
   {
      if (chars == null)
         throw new ArgumentNullException(nameof(chars));

      var list = new List<char>(chars);
      var n = list.Count;
      while (n > 1)
      {
         n--;
         var k = RandomNumberGenerator.GetInt32(n + 1);
         (list[n], list[k]) = (list[k], list[n]);
      }
      return list;
   }

   public static IReadOnlyCollection<ICommandFactory> CommandFactories(
      IState state,
      ILock @lock,
      IView view)
   {
      return new ICommandFactory[]
      {
         new Clear(view),
         new Pwd(view),
         new shared.Lock(state, view, @lock),
         new Quit(state)
      };
   }
}