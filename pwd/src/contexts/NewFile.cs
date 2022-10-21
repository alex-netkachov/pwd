using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

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
   : INewFile,
      ISuggestionsProvider
{
   private readonly CancellationTokenSource _cts;

   private readonly IRepository _repository;
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;

   private readonly StringBuilder _content;
   private readonly string _name;

   public NewFile(
      ILogger logger,
      IRepository repository,
      IState state,
      IView view,
      string name)
   {
      _logger = logger;
      _state = state;
      _repository = repository;
      _view = view;
      _name = name;

      _cts = new();

      _content = new();
   }
   
   public async Task StartAsync()
   {
      while (true)
      {
         string input;
         try
         {
            input = (await _view.ReadAsync(new($"+> "), this, _cts.Token)).Trim();
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
                  _view.WriteLine("Enter new file content line by line. Empty line completes the file.");
                  break;
               default:
                  _content.AppendLine(input).Replace("***", Shared.Password());
                  break;
            }
         }
         catch (Exception e)
         {
            _logger.Error($"Executing the command '{input}' caused the following exception: {e}");
         }
      }
   }

   public Task StopAsync()
   {
      _cts.Cancel();
      return Task.CompletedTask;
   }

   public (int, IReadOnlyList<string>) Get(
      string input)
   {
      return (input.Length, new[]
         {
            ".help",
            "user",
            "password"
         }
         .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
         .ToArray());
   }
}

public sealed class NewFileFactory
   : INewFileFactory
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;

   public NewFileFactory(
      ILogger logger,
      IState state,
      IView view)
   {
      _logger = logger;
      _state = state;
      _view = view;
   }

   public INewFile Create(
      IRepository repository,
      string name)
   {
      return new NewFile(
         _logger,
         repository,
         _state,
         _view,
         name);
   }
}