using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;

namespace pwd.contexts;

public interface INewFile
   : IContext
{
}

public interface INewFileFactory
{
   INewFile Create(
      string name);
}

public sealed class NewFile
   : AbstractContext,
      INewFile
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

      _content = new StringBuilder();
   }

   public override string Prompt()
   {
      return "+";
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

   public override string[] GetInputSuggestions(
      string input,
      int index)
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
}

public sealed class NewFileFactory
   : INewFileFactory
{
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;

   public NewFileFactory(
      IRepository repository,
      IState state,
      IView view)
   {
      _state = state;
      _repository = repository;
      _view = view;
   }

   public INewFile Create(
      string name)
   {
      return new NewFile(
         _repository,
         _state,
         _view,
         name);
   }
}
