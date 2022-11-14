using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Archive
   : ICommand
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IRepository _repository;
   private readonly string _name;

   public Archive(
      ILogger logger,
      IState state,
      IRepository repository,
      string name)
   {
      _logger = logger;
      _state = state;
      _repository = repository;
      _name = name;
   }

   public async Task DoAsync(
      CancellationToken cancellationToken)
   {
      _logger.Info($"Executing the command {nameof(Archive)}");

      _repository.Archive(_name);
      await _state.BackAsync().WaitAsync(cancellationToken);
   }
}

public sealed class ArchiveFactory
   : ICommandFactory
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IRepository _repository;
   private readonly string _name;

   public ArchiveFactory(
      ILogger logger,
      IState state,
      IRepository repository,
      string name)
   {
      _logger = logger;
      _state = state;
      _repository = repository;
      _name = name;
   }

   public ICommand? Parse(
      string input)
   {
      return input == ".archive"
         ? new Archive(_logger, _state, _repository, _name)
         : null;
   }
}