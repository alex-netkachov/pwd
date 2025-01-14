using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.ui.abstractions;

public interface IStateChange
{
}

public sealed class StateDisposed
   : IStateChange
{
}

public interface IStateChangeReader
   : IDisposable
{
   ValueTask<IStateChange> ReadAsync(
      CancellationToken cancellationToken = default);
}

public sealed class StateChangeReader(
      ChannelReader<IStateChange> reader,
      Action? disposing = null)
   : IStateChangeReader
{
   private readonly Action _disposing = disposing ?? (() => { });
   private int _disposed;

   public ValueTask<IStateChange> ReadAsync(
      CancellationToken cancellationToken = default)
   {
      return reader.ReadAsync(cancellationToken);
   }

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) != 1)
         return;
      _disposing();
   }
}

/// <summary>Stack of contexts.</summary>
public interface IState
   : IAsyncDisposable
{
   /// <summary>
   ///   Sends stopping signal to the active context, removes it from the top, and activates
   ///   the context that is on top of the stack. Completes when the new context is active.
   /// </summary>
   Task BackAsync();

   /// <summary>
   ///   Sends stopping signal to the active context, puts the new one on top of the stack, and activates
   ///   it. Completes when the new context is active.
   /// </summary>
   Task OpenAsync(
      IContext context);

   IStateChangeReader Subscribe();
}
