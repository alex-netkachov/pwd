using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up
   : ICommand
{
   private readonly ILogger _logger;
   private readonly IState _state;

   public Up(
      ILogger logger,
      IState state)
   {
      _logger = logger;
      _state = state;
   }

   public async Task DoAsync(
      CancellationToken cancellationToken)
   {
      _logger.Info($"Executing the command {nameof(Up)}");

      await _state.BackAsync().WaitAsync(cancellationToken);
   }
}

public sealed class UpFactory
   : ICommandFactory
{
   private readonly ILogger _logger;
   private readonly IState _state;

   public UpFactory(
      ILogger logger,
      IState state)
   {
      _logger = logger;
      _state = state;
   }

   public ICommand? Parse(
      string input)
   {
      return input == ".."
         ? new Up(_logger, _state)
         : null;
   }
}