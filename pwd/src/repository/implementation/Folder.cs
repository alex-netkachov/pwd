using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.repository.interfaces;

namespace pwd.repository.implementation;

public class Folder(
      Repository repository,
      Name name,
      Name encryptedName,
      IContainer container)
   : IFolder
{
   private readonly Repository _repository = repository;

   public IRepository Repository => _repository;
   public Name Name { get; set; } = name;
   public Name EncryptedName { get; set; } = encryptedName;
   public IContainer Container { get; set; } = container;

   public Path GetPath()
   {
      return Container switch
      {
         RootFolder _ => Path.From(Name),
         Folder folder => folder.GetPath().Down(Name),
         _ => throw new NotSupportedException(
            $"Unsupported container type '{Container.GetType().FullName}'.")
      };
   }

   public Path GetEncryptedPath()
   {
      return Container switch
      {
         RootFolder _ => Path.From(EncryptedName),
         Folder folder => folder.GetEncryptedPath().Down(EncryptedName),
         _ => throw new NotSupportedException(
            $"Unsupported container type '{Container.GetType().FullName}'.")
      };
   }

   public INamedItem? Get(
      Name name)
   {
      return _repository.Get(GetPath().Down(name)) as INamedItem;
   }

   public async Task<INamedItem?> GetAsync(
      Name name)
   {
      return (await _repository.GetAsync(GetPath().Down(name))) as INamedItem;
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
