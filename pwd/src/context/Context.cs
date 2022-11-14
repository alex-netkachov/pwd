using System.Threading.Tasks;

namespace pwd.context;

public interface IContext
{
   /// <summary>Starts the context. The returned task completes when the context is started.</summary>
   /// <remarks>The context can be started and stopped multiple times. Multiple calls to the method returns the same
   /// task. If the context is started, this method does nothing and returns a completed task.</remarks>
   Task StartAsync();

   /// <summary>Stops the context. Completes when the context is stopped.</summary>
   Task StopAsync();
}