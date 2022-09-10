using System;

namespace pwd;

public interface ILogger
{
   void Info(
      string message);

   void Error(
      string message);
}

public sealed class ConsoleLogger
   : ILogger
{
   public void Info(
      string message)
   {
      Console.WriteLine(message);
   }

   public void Error(
      string message)
   {
      Console.Error.WriteLine(message);
   }
}