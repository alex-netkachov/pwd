using System.IO.Abstractions;
using pwd.ciphers;

namespace pwd.repository;

public interface IRepositoryFactory
{
   IRepository Create(
      INameCipher nameCipher,
      IContentCipher contentCipher,
      string path);
}

public sealed class RepositoryFactory
   : IRepositoryFactory
{
   private readonly IFileSystem _fs;

   public RepositoryFactory(
      IFileSystem fs)
   {
      _fs = fs;
   }

   public IRepository Create(
      INameCipher nameCipher,
      IContentCipher contentCipher,
      string path)
   {
      return new Repository(
         _fs,
         nameCipher,
         contentCipher,
         path);
   }
}