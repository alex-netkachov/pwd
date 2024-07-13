using System;

namespace pwd.library.interfaced;

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