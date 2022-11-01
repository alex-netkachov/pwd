using System;

namespace pwd;

public interface ILogger
{
   void Info(
      string message);

   void Error(
      string message);
}

public sealed class NullLogger
   : ILogger
{
   public void Info(
      string message)
   {
   }

   public void Error(
      string message)
   {
   }
}

public sealed class ConsoleLogger
   : ILogger
{
   private readonly object _sync = new { };

   public void Info(
      string message)
   {
      lock (_sync)
         Console.WriteLine(message);
   }

   public void Error(
      string message)
   {
      lock (_sync)
         Console.Error.WriteLine(message);
   }
}