using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console;
using pwd.console.abstractions;
using pwd.core.abstractions;
using pwd.ui.abstractions;

namespace pwd.contexts;

public interface INewFile
   : IContext
{
}

public interface INewFileFactory
{
   INewFile Create(
      IRepository repository,
      string name);
}

public sealed class NewFile
   : Views,
     INewFile,
     ISuggestions
{
   private readonly CancellationTokenSource _cts;

   private readonly IRepository _repository;
   private readonly ILogger<NewFile> _logger;
   private readonly IState _state;
   private readonly IView _view;

   private readonly StringBuilder _content;
   private readonly string _name;

   public NewFile(
      ILogger<NewFile> logger,
      IRepository repository,
      IState state,
      Func<IView> viewFactory,
      string name)
   {
      _logger = logger;
      _state = state;
      _repository = repository;
      _view = viewFactory();
      _name = name;

      _cts = new();

      _content = new();
      
      Publish(_view);
   }
   
   public async Task Activate()
   {
      while (true)
      {
         string input;
         try
         {
            input = (await _view.ReadAsync(new($"+> "), this, null, _cts.Token)).Trim();
         }
         catch (OperationCanceledException e) when (e.CancellationToken == _cts.Token)
         {
            // StopAsync() is called, exit gracefully. 
            break;
         }

         // TODO: it does not look like this is the right wau to handle it
         if (input == ".quit")
            break;

         try
         {
            switch (input)
            {
               case "":
                  await _repository.WriteAsync(_name, _content.ToString());
                  await _state.BackAsync();
                  return;
               case ".help":
                  await Help();
                  break;
               default:
                  _content.AppendLine(input).Replace("***", Shared.Password());
                  break;
            }
         }
         catch (Exception e)
         {
            _logger.LogError($"Executing the command '{input}' caused the following exception: {e}");
         }
      }
   }

   public Task Deactivate()
   {
      _cts.Cancel();
      return Task.CompletedTask;
   }

   public IReadOnlyList<string> Get(
      string input,
      int position)
   {
      return new[]
         {
            ".help",
            "user",
            "password"
         }
         .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
         .ToArray();
   }
   
   private async Task Help()
   {
      await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.res.context_new_file_help.txt");
      if (stream == null)
      {
         _view.WriteLine("help file is missing");         
         return;
      }

      using var reader = new StreamReader(stream);
      var content = await reader.ReadToEndAsync();
      _view.WriteLine(content.TrimEnd());
   }

   public void Dispose()
   {
   }

   public async Task ExecuteAsync()
   {
      await Activate();
   }
}

public sealed class NewFileFactory(
      ILoggerFactory loggerFactory,
      IState state,
      Func<IView> viewFactory)
   : INewFileFactory
{
   public INewFile Create(
      IRepository repository,
      string name)
   {
      return new NewFile(
         loggerFactory.CreateLogger<NewFile>(),
         repository,
         state,
         viewFactory,
         name);
   }
}