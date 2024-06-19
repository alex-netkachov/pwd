using System;
using System.Threading;
using System.Threading.Tasks;
using pwd.repository.interfaces;

namespace pwd.repository.implementation;

public class File(
      Repository repository,
      Name name,
      Name encryptedName,
      IContainer container)
   : IFile
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

   public Task<string> ReadAsync(
     CancellationToken token)
   {
      return _repository.ReadAsync(this, token);
   }

   public Task WriteAsync(
      string value,
      CancellationToken token = default)
   {
      return _repository.WriteAsync(this, value, token);
   }
}
