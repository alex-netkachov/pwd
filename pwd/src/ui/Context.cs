using System;
using System.Threading.Tasks;

namespace pwd.ui;

/// <summary>
///   Represents a currently active operational context, such as a list,
///   an element in view mode, an element in edit mode, an input field,
///   a confirmation dialog, and other similar operational states.
/// </summary>
public interface IContext
   : IDisposable
{
   /// <summary>
   ///   Starts the context. The returned task completes either when
   ///   the context has successfully started or the context fails
   ///   to start.
   /// </summary>
   /// <remarks>
   ///   The context can be started and stopped multiple times. Multiple
   ///   calls to this method will return the same task. If the context
   ///   is already started, this method does nothing and simply returns
   ///   a completed task.
   /// </remarks>
   Task StartAsync();

   /// <summary>
   ///   Stops the context. Completes either when the context is stopped
   ///   or the context fails to stop.
   /// </summary>
   /// <remarks>
   ///   The content of the context is preserved, and it can be started
   ///   again. To dispose of the context, use <see cref="IDisposable.Dispose"/>.
   /// </remarks>
   Task StopAsync();
}