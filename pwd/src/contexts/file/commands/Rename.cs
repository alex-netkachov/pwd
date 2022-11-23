using pwd.context.repl;
using pwd.repository;

namespace pwd.contexts.file.commands;

public sealed class Rename
   : ICommandFactory
{
   private readonly IRepository _repository;
   private readonly IRepositoryItem _item;

   public Rename(
      IRepository repository,
      IRepositoryItem item)
   {
      _repository = repository;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rename", var name) => new DelegateCommand(() => _repository.Rename(_item.Name, name)),
         _ => null
      };
   }
}