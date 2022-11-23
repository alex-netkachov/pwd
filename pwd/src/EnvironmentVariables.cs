using System;

namespace pwd;

public interface IEnvironmentVariables
{
   string Get(
      string key);
}

public sealed class EnvironmentVariables
   : IEnvironmentVariables
{
   public string Get(
      string key)
   {
      return Environment.GetEnvironmentVariable(key) ?? "";
   }
}