using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd.repository;

public interface IRepositoryUpdate
{
}

public sealed class Moved
   : IRepositoryUpdate
{
   public Moved(
      string path)
   {
      Path = path;
   }

   public string Path { get; }
}

public sealed class Modified
   : IRepositoryUpdate
{
}

public sealed class Deleted
   : IRepositoryUpdate
{
}

public interface IRepositoryUpdatesReader
   : IDisposable
{
   ValueTask<IRepositoryUpdate> ReadAsync(
      CancellationToken cancellationToken = default);
}

public sealed class RepositoryUpdatesReader
   : IRepositoryUpdatesReader
{
   private readonly ChannelReader<IRepositoryUpdate> _reader;
   private readonly Action _disposing;
   private int _disposed;

   public RepositoryUpdatesReader(
      ChannelReader<IRepositoryUpdate> reader,
      Action? disposing = null)
   {
      _reader = reader;
      _disposing = disposing ?? new Action(() => { });
   }

   public ValueTask<IRepositoryUpdate> ReadAsync(
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