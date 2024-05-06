using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using pwd.context.repl;
using pwd.ui;

namespace pwd.contexts.session.commands;

public sealed class Help
   : CommandServicesBase
{
   private readonly IView _view;

   public Help(
      IView view)
   {
      _view = view;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "help", _) => new DelegateCommand(async _ =>
         {
            await using var stream =
               Assembly.GetExecutingAssembly()
                  .GetManifestResourceStream("pwd.res.context_session_help.txt");
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

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}