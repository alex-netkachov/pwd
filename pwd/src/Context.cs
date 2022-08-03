using System;
using System.Threading.Tasks;

namespace pwd;

public interface IContext
{
   Task Process(
      string input);

   string Prompt();

   Task Open();

   string[] GetInputSuggestions(
      string input,
      int index);
}

public abstract class Context
   : IContext
{
   public virtual Task Process(
      string input)
   {
      return Task.CompletedTask;
   }

   public virtual string Prompt()
   {
      return "";
   }

   public virtual Task Open()
   {
      return Task.CompletedTask;
   }

   public virtual string[] GetInputSuggestions(
      string input,
      int index)
   {
      return Array.Empty<string>();
   }
}

public sealed class NullContext
   : Context
{
   public static IContext Instance { get; } = new NullContext();
}