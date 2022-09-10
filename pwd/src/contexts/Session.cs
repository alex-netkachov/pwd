using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
   : AbstractContext,
      ISession
{
   private readonly IExporter _exporter;
   private readonly IFileFactory _fileFactory;
   private readonly INewFileFactory _newFileFactory;
   private readonly ILock _lock;
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;

   public Session(
      IExporter exporter,
      IRepository repository,
      IState state,
      IView view,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory,
      ILock @lock)
   {
      _exporter = exporter;
      _repository = repository;
      _state = state;
      _view = view;
      _fileFactory = fileFactory;
      _newFileFactory = newFileFactory;
      _lock = @lock;
   }

   public override async Task Process(
      string input)
   {
      await base.Process(input);

      switch (Shared.ParseCommand(input))
      {
         case (_, "add", var path):
            await Add(path);
            break;
         case (_, "export", var path):
            await Export(path);
            break;
         case (_, "help", _):
            await Help();
            break;
         case (_, "open", var path):
            await Open(path);
            break;
         default:
            if (await Shared.Process(input, _view, _state, _lock))
               break;

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
                  await Open(chosen);
            }

            break;
      }
   }

   public override string[] GetInputSuggestions(
      string input,
      int index)
   {
      if (!input.StartsWith('.'))
      {
         var p = input.LastIndexOf('/');
         var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
         return _repository.List(folder == "" ? "." : folder)
            .Where(item => item.Path.StartsWith(input))
            .Select(item => item.Path)
            .ToArray();
      }

      return new[]
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
         .ToArray();
   }

   private async Task Open(
      string name)
   {
      var content = await _repository.ReadAsync(name);
      var file = _fileFactory.Create(_repository, _lock, name, content);
      _state.Open(file);
   }

   private async Task Export(
      string path)
   {
      await _exporter.Export(
         string.IsNullOrEmpty(path)
            ? "_index.html"
            : path);
   }

   private async Task Add(
      string name)
   {
      _state.Open(_newFileFactory.Create(_repository, name));
      await Open(name);
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
   private readonly IState _state;
   private readonly IView _view;

   public SessionFactory(
      IState state,
      IView view,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory)
   {
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
         exporter,
         repository,
         _state,
         _view,
         _fileFactory,
         _newFileFactory,
         @lock);
   }
}