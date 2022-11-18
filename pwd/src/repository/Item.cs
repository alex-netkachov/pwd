using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace pwd.repository;

public interface IRepositoryItem
{
   /// <summary>Plaintext name.</summary>
   public string Name { get; }

   /// <summary>Plaintext path.</summary>
   public string Path { get; }

   /// <summary>Encrypted name.</summary>
   public string EncryptedName { get; }

   /// <summary>Encrypted filenames path.</summary>
   public string EncryptedPath { get; }

   /// <summary>Whether the item is a folder or a file. Null when the item does not exist.</summary>
   public bool? IsFolder { get; }

   /// <summary>Whether the corresponding file exists.</summary>
   public bool Exists { get; }

   /// <summary>Subscribe for the events related to this repository item.</summary>
   IRepositoryUpdatesReader Subscribe();
}

public sealed class RepositoryItem
   : IRepositoryItem
{
   private ImmutableList<Channel<IRepositoryUpdate>> _subscribers;
   private string _path;

   private RepositoryItem(
      string name,
      string encryptedName,
      IRepositoryItem? item = null)
   {
      Name = name;
      EncryptedName = encryptedName;
      EncryptedPath = Repository.PathCombine(item?.EncryptedPath, encryptedName);

      _path = Repository.PathCombine(item?.Path, name);

      _subscribers = ImmutableList<Channel<IRepositoryUpdate>>.Empty;
   }

   public List<RepositoryItem> Items { get; } = new();

   public string Name { get; set; }

   public string Path
   {
      get => _path;
      private set
      {
         if (string.Equals(_path, value, StringComparison.Ordinal))
            return;

         _path = value;

         NotifySubscribers(new Moved(value));
      }
   }
   
   public string EncryptedName { get; set; }
   public string EncryptedPath { get; private set; }
   public bool? IsFolder { get; set; }
   public bool Exists { get; set; }

   public void Modified()
   {
      NotifySubscribers(new Modified());
   }

   public void Deleted()
   {
      NotifySubscribers(new Deleted());
   }

   public RepositoryItem? Get(
      string name)
   {
      return Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
   }

   public RepositoryItem Create(
      string name,
      string encryptedName)
   {
      return new(name, encryptedName, this);
   }

   public static RepositoryItem Root()
   {
      return new("", "")
      {
         IsFolder = true,
         Exists = true
      };
   }

   public void UpdatePaths()
   {
      foreach (var item in Items)
      {
         item.Path = Repository.PathCombine(Path, item.Name);
         item.EncryptedPath = Repository.PathCombine(EncryptedPath, item.EncryptedName);
         item.UpdatePaths();
      }
   }

   public IRepositoryUpdatesReader Subscribe()
   {
      var channel = Channel.CreateUnbounded<IRepositoryUpdate>();

      var reader = new RepositoryUpdatesReader(channel.Reader, () =>
      {
         while (true)
         {
            var initial = _subscribers;
            var updated = initial.Remove(channel);
            if (initial != Interlocked.CompareExchange(ref _subscribers, updated, initial))
               continue;
            channel.Writer.Complete();
            break;
         }
      });

      while (true)
      {
         var initial = _subscribers;
         var updated = _subscribers.Add(channel);
         if (initial == Interlocked.CompareExchange(ref _subscribers, updated, initial))
            break;
      }

      return reader;
   }

   private void NotifySubscribers(
      IRepositoryUpdate update)
   {
      var subscribers = _subscribers;

      foreach (var subscriber in subscribers)
      {
         while (subscriber.Writer.TryWrite(update))
         {
         }
      }
   }
}