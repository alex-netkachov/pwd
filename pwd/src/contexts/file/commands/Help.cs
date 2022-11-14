using System.IO;
using System.Reflection;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Help
   : ICommandFactory
{
   private readonly IView _view;

   public Help(
      IView view)
   {
      _view = view;
   }

   public ICommand? Parse(
      string input)
   {
      return input switch
      {
         ".help" => new DelegateCommand(async _ =>
         {
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
         }),
         _ => null
      };
   }
}