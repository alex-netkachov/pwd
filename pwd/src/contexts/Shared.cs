using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PasswordGenerator;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts;

public static class Shared
{
   public static Task<bool> Process(
      string input,
      IView view,
      IState state,
      ILock @lock)
   {
      switch (ParseCommand(input))
      {
         case (_, "pwd", _):
            view.WriteLine(new Password().Next());
            return Task.FromResult(true);
         case (_, "clear", _):
            view.Clear();
            return Task.FromResult(true);
         case (_, "lock", _):
            view.Clear();
            state.Open(@lock);
            return Task.FromResult(true);
         default:
            return Task.FromResult(false);
      }
   }

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
}