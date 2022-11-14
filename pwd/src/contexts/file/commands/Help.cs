using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Help
   : ICommand
{
   private readonly ILogger _logger;
   private readonly IView _view;

   public Help(
      ILogger logger,
      IView view)
   {
      _logger = logger;
      _view = view;
   }

   public async Task DoAsync(
      CancellationToken cancellationToken)
   {
      _logger.Info($"Executing the command {nameof(Help)}");

      await using var stream =
         Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("pwd.res.context_file_help.txt");

      if (stream == null)
      {
         _view.WriteLine("help file is missing");         
         return;
      }

      using var reader = new StreamReader(stream);
      var content = await reader.ReadToEndAsync();
      _view.WriteLine(content.TrimEnd());
   }
}

public sealed class HelpFactory
   : ICommandFactory
{
   private readonly ILogger _logger;
   private readonly IView _view;

   public HelpFactory(
      ILogger logger,
      IView view)
   {
      _logger = logger;
      _view = view;
   }

   public ICommand? Parse(
      string input)
   {
      return input == ".help"
         ? new Help(_logger, _view)
         : null;
   }
}