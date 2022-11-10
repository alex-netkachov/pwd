using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.contexts;

public interface ISession
   : IContext
{
}

public interface ISessionFactory
{
   ISession Create(
      IRepository repository,
      IExporter exporter,
      ILock @lock);
}


/// <summary>Repository working session context.</summary>
public sealed class Session
   : Repl,
      ISession
{
   private readonly ILogger _logger;
   private readonly IExporter _exporter;
   private readonly IFileFactory _fileFactory;
   private readonly INewFileFactory _newFileFactory;
   private readonly ILock _lock;
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;

   public Session(
      ILogger logger,
      IExporter exporter,
      IRepository repository,
      IState state,
      IView view,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory,
      ILock @lock)
      : base(
         logger,
         view)
   {
      _logger = logger;
      _exporter = exporter;
      _repository = repository;
      _state = state;
      _view = view;
      _fileFactory = fileFactory;
      _newFileFactory = newFileFactory;
      _lock = @lock;
   }

   public override async Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      _logger.Info($"Session.ProcessAsync({input})");

      await base.ProcessAsync(input, cancellationToken);

      switch (Shared.ParseCommand(input))
      {
         case (_, "add", var path):
            await Add(path, cancellationToken);
            break;
         case (_, "export", var path):
            await Export(path);
            break;
         case (_, "html", var path):
            await ExportToHtml(path);
            break;
         case (_, "help", _):
            await Help();
            break;
         case (_, "open", var path):
            await Open(path, cancellationToken);
            break;
         default:
            _logger.Info($"Session.ProcessAsync({input}) : fallback to shared handlers");

            if (await Shared.Process(input, _view, _state, _lock, cancellationToken))
               break;

            _logger.Info($"Session.ProcessAsync({input}) : fallback to file list handlers");

            if (input == "")
            {
               // show all files if there is no user input
               var items =
                  _repository
                     .List(".")
                     .Select(item => item.Path)
                     .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                     .ToList();

               _view.WriteLine(string.Join("\n", items));
            }
            else
            {
               // show files and folders
               var items =
                  _repository.List(".", (false, true, false))
                     .Where(item => item.Path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                     .ToList();

               var match =
                  items.FirstOrDefault(
                     item => string.Equals(item.Path, input, StringComparison.OrdinalIgnoreCase));

               var chosen =
                  match == default
                     ? items.Count == 1 && input != "" ? items[0].Path : default
                     : match.Path;

               if (chosen == null)
                  _view.WriteLine(string.Join("\n", items.Select(item => item.Path).OrderBy(item => item)));
               else
                  await Open(chosen, cancellationToken);
            }

            break;
      }
   }

   public override (int, IReadOnlyList<string>) Get(
      string input)
   {
      if (!input.StartsWith('.'))
      {
         var p = input.LastIndexOf('/');
         var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
         return (
            input.Length,
            _repository.List(folder == "" ? "." : folder)
               .Where(item => item.Path.StartsWith(input))
               .Select(item => item.Path)
               .ToArray());
      }

      return (input.Length, new[]
         {
            ".add",
            ".archive",
            ".clear",
            ".export",
            ".lock",
            ".pwd",
            ".quit"
         }
         .Where(item => item.StartsWith(input))
         .ToArray());
   }

   private async Task Open(
      string name,
      CancellationToken cancellationToken)
   {
      var content = await _repository.ReadAsync(name);
      var file = _fileFactory.Create(_repository, _lock, name, content);
      await _state.OpenAsync(file).WaitAsync(cancellationToken);
   }
   
   private Task Export(
      string path)
   {
      _view.WriteLine("Not implemented");
      return Task.CompletedTask;
   }

   private async Task ExportToHtml(
      string path)
   {
      await _exporter.Export(
         string.IsNullOrEmpty(path)
            ? "_index.html"
            : path);
   }

   private async Task Add(
      string name,
      CancellationToken cancellationToken)
   {
      await _state.OpenAsync(_newFileFactory.Create(_repository, name)).WaitAsync(cancellationToken);
   }

   private async Task Help()
   {
      await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.res.context_session_help.txt");
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

public sealed class SessionFactory
   : ISessionFactory
{
   private readonly IFileFactory _fileFactory;
   private readonly INewFileFactory _newFileFactory;
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;

   public SessionFactory(
      ILogger logger,
      IState state,
      IView view,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory)
   {
      _logger = logger;
      _state = state;
      _view = view;
      _fileFactory = fileFactory;
      _newFileFactory = newFileFactory;
   }

   public ISession Create(
      IRepository repository,
      IExporter exporter,
      ILock @lock)
   {
      return new Session(
         _logger,
         exporter,
         repository,
         _state,
         _view,
         _fileFactory,
         _newFileFactory,
         @lock);
   }
}