using System;
using System.Diagnostics;

namespace pwd;

public interface IRunner
{
   public Exception? Run(
      string executable,
      string input);
}

public class Runner
   : IRunner
{
   public Exception? Run(
      string executable,
      string input)
   {
      try
      {
         var startInfo =
            new ProcessStartInfo(executable)
            {
               RedirectStandardInput = true
            };

         var process = Process.Start(startInfo);
         if (process == null)
            throw new($"Starting the executable '{executable}' failed.");

         var stdin = process.StandardInput;
         stdin.Write(input);
         stdin.Close();
      }
      catch (Exception e)
      {
         return e;
      }

      return null;
   }
}