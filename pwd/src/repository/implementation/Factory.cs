using System.IO.Abstractions;

namespace pwd.repository.implementation;

public sealed class RepositoryFactory(
   IFileSystem fs,
   ICipherFactory cipherFactory,
   IEncoder encoder)
   : IFactory
{
   public IRepository Create(
      string password,
      string path)
   {
      return new Repository(
         fs,
         cipherFactory.Create(password),
         encoder,
         path);
   }
}