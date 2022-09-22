using System.Threading.Tasks;

namespace pwd.contexts;

public interface IContext
{
   Task Process(
      string input);

   Task<string> ReadAsync();

   Task Open();
}

public abstract class AbstractContext
   : IContext
{
   public virtual Task Process(
      string input)
   {
      return Task.CompletedTask;
   }

   public virtual Task<string> ReadAsync()
   {
      return Task.FromResult("");
   }

   public virtual Task Open()
   {
      return Task.CompletedTask;
   }
}

public sealed class NullContext
   : AbstractContext
{
   public static IContext Instance { get; } = new NullContext();
}