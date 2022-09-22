using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;
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
   : AbstractContext,
      INewFile,
      ISuggestionsProvider
{
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;

   private readonly StringBuilder _content;
   private readonly string _name;

   public NewFile(
      IRepository repository,
      IState state,
      IView view,
      string name)
   {
      _state = state;
      _repository = repository;
      _view = view;
      _name = name;

      _content = new();
   }

   public override async Task Process(
      string input)
   {
      switch (input)
      {
         case "":
            await _repository.WriteAsync(_name, _content.ToString());
            _state.Back();
            return;
         case ".help":
            _view.WriteLine("Enter new file content line by line. Empty line completes the file.");
            break;
         default:
            _content.AppendLine(input).Replace("***", new Password().Next());
            break;
      }
   }

   public override async Task<string> ReadAsync()
   {
      return (await _view.ReadAsync(new("+> "), this)).Trim();
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
   private readonly IState _state;
   private readonly IView _view;

   public NewFileFactory(
      IState state,
      IView view)
   {
      _state = state;
      _view = view;
   }

   public INewFile Create(
      IRepository repository,
      string name)
   {
      return new NewFile(
         repository,
         _state,
         _view,
         name);
   }
}