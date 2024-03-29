﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pwd.context.repl;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts.file.commands;

public sealed class CopyField(
      IClipboard clipboard,
      repository.IFile file)
   : CommandServicesBase
{
   private readonly IClipboard _clipboard = clipboard;
   private readonly repository.IFile _file = file;

   private string _content = "";

    public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "ccp", _) => new DelegateCommand(async _ => await Copy("password")),
         (_, "ccu", _) => new (async _ => await Copy("user")),
         (_, "cc", var name) when !string.IsNullOrEmpty(name) => new (async _ => await Copy(name)),
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
      var content = await _file.ReadAsync();

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

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      if (string.Equals(input, ".ccp", StringComparison.OrdinalIgnoreCase) ||
          input.StartsWith(".ccp ", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(input, ".ccu", StringComparison.OrdinalIgnoreCase) ||
          input.StartsWith(".ccu ", StringComparison.OrdinalIgnoreCase))
      {
         return Array.Empty<string>(); 
      }

      const string key = ".cc ";
      if (!input.StartsWith(key, StringComparison.OrdinalIgnoreCase))
         return Array.Empty<string>();

      using var reader = new StringReader(_content);
      var yaml = new YamlStream();
      yaml.Load(reader);
      if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
         return Array.Empty<string>();

      // 4 is the length of the ".cc " string
      var prefix = input[4..];

      return mappingNode
         .Children
         .Select(item => item.Key.ToString())
         .Where(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
         .Select(item => $".cc {item}")
         .ToArray();
   }
}