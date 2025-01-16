using System;

namespace pwd.cli.library.interfaced;

public interface IEnvironmentVariables
{
   string GetEnvironmentVariable(
      string key);
}

public sealed class EnvironmentVariables
   : IEnvironmentVariables
{
   public string GetEnvironmentVariable(
      string key)
   {
      return Environment.GetEnvironmentVariable(key) ?? "";
   }
}