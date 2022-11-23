using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pwd.context.repl;
using pwd.repository;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts.file.commands;

public sealed class CopyField
   : ICommandFactory
{
   private readonly IClipboard _clipboard;
   private readonly IRepositoryItem _item;

   public CopyField(
      IClipboard clipboard,
      IRepositoryItem item)
   {
      _clipboard = clipboard;
      _item = item;
   }

   public ICommand? Parse(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "ccp", _) => new DelegateCommand(async _ => await Copy("password")),
         (_, "ccu", _) => new (async _ => await Copy("user")),
         (_, "cc", var name) => new (async _ => await Copy(name)),
         _ => null
      };
   }
   
   private async Task Copy(
      string path)
   {
      var value = await Field(path);
      if (string.IsNullOrEmpty(value))
         _clipboard.Clear();
      else
         _clipboard.Put(value, TimeSpan.FromSeconds(10));
   }
   
   private async Task<string> Field(
      string name)
   {
      var content = await _item.ReadAsync();

      using var input = new StringReader(content);

      var yaml = new YamlStream();

      yaml.Load(input);

      if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
         return "";

      var node =
         mappingNode
            .Children
            .FirstOrDefault(
               item =>
                  string.Equals(
                     item.Key.ToString(), 
                     name,
                     StringComparison.OrdinalIgnoreCase));

      return (node.Value as YamlScalarNode)?.Value ?? "";
   }
}