using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Html
   : ICommandFactory
{
   private readonly IExporter _exporter;

   public Html(
      IExporter exporter)
   {
      _exporter = exporter;
   }

   public ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "html", var path) =>
            new DelegateCommand(async _ =>
            {
               await _exporter.Export(
                  string.IsNullOrEmpty(path)
                     ? "_index.html"
                     : path);
            }),
         _ => null
      };
   }
}