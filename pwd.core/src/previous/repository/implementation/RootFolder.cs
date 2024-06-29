using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.core.previous.repository.interfaces;

namespace pwd.core.previous.repository.implementation;

public sealed class RootFolder(
      Repository repository)
   : IRootFolder
{
   private readonly Repository _repository = repository;

   public IRepository Repository => _repository;

   public Path Path =>
      _repository.TryParsePath("", out var path)
         ? path!
         : throw new InvalidOperationException("Invalid root path.");

   public INamedItem? Get(
      Name name)
   {
      var path = Path.From(name);
      var item = _repository.Get(path);
      return (INamedItem?)item;
   }

   public async Task<INamedItem?> GetAsync(
      Name name)
   {
      var path = Path.From(name);
      var item = await _repository.GetAsync(path);
      return (INamedItem?)item;
   }

   public IEnumerable<INamedItem> List(
      ListOptions? options = null)
   {
      return _repository.List(this, options);
   }

   public IAsyncEnumerable<INamedItem> ListAsync(
      ListOptions? options = null,
      CancellationToken token = default)
   {
      return _repository.ListAsync(this, options, token);
   }
}
