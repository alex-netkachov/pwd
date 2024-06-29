using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using pwd.core.previous.repository.interfaces;

namespace pwd.core.previous.repository.implementation;

public sealed class Moved
   : IUpdate
{
   public Moved(
      string path)
   {
      Path = path;
   }

   public string Path { get; }
}

public sealed class Modified
   : IUpdate
{
}

public sealed class Deleted
   : IUpdate
{
}

public interface IRepositoryUpdatesReader
   : IDisposable
{
   ValueTask<IUpdate> ReadAsync(
      CancellationToken cancellationToken = default);
}

public sealed class RepositoryUpdatesReader
   : IRepositoryUpdatesReader
{
   private readonly ChannelReader<IUpdate> _reader;
   private readonly Action _disposing;
   private int _disposed;

   public RepositoryUpdatesReader(
      ChannelReader<IUpdate> reader,
      Action? disposing = null)
   {
      _reader = reader;
      _disposing = disposing ?? new Action(() => { });
   }

   public ValueTask<IUpdate> ReadAsync(
      CancellationToken cancellationToken = default)
   {
      return _reader.ReadAsync(cancellationToken);
   }

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) != 1)
         return;
      _disposing();
   }
}