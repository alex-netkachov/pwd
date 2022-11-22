using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace pwd;

public interface IRunner
{
   public Exception? Run(
      string executable,
      string? arguments = null,
      string? input = null);
   
   public Task<Exception?> RunAsync(
      string executable,
      string? arguments = null,
      string? input = null,
      CancellationToken cancellationToken = default);
}

public class Runner
   : IRunner
{
   public Exception? Run(
      string executable,
      string? arguments = null,
      string? input = null)
   {
      if (input == null)
      {
         try
         {
            var startInfo = new ProcessStartInfo(executable, arguments ?? "");

            var process = Process.Start(startInfo);
            if (process == null)
               throw new($"Starting the executable '{executable}' failed.");
            process.WaitForExit();
         }
         catch (Exception e)
         {
            return e;
         }

         return null;
      }

      try
      {
         var startInfo =
            new ProcessStartInfo(executable, arguments ?? "")
            {
               RedirectStandardInput = true
            };

         var process = Process.Start(startInfo);
         if (process == null)
            throw new($"Starting the executable '{executable}' failed.");

         var stdin = process.StandardInput;
         stdin.Write(input);
         stdin.Close();
         
         process.WaitForExit();
      }
      catch (Exception e)
      {
         return e;
      }

      return null;
   }

   public async Task<Exception?> RunAsync(
      string executable,
      string? arguments = null,
      string? input = null,
      CancellationToken cancellationToken = default)
   {
      Process? process = null;

      try
      {

         if (input == null)
         {
            try
            {
               var startInfo = new ProcessStartInfo(executable, arguments ?? "");

               process = Process.Start(startInfo);
               if (process == null)
                  throw new($"Starting the executable '{executable}' failed.");
               await process.WaitForExitAsync(cancellationToken);
            }
            catch (Exception e)
            {
               return e;
            }

            return null;
         }

         try
         {
            var startInfo =
               new ProcessStartInfo(executable, arguments ?? "")
               {
                  RedirectStandardInput = true
               };

            process = Process.Start(startInfo);
            if (process == null)
               throw new($"Starting the executable '{executable}' failed.");

            var stdin = process.StandardInput;
            await stdin.WriteAsync(input);
            stdin.Close();

            await process.WaitForExitAsync(cancellationToken);
         }
         catch (Exception e)
         {
            return e;
         }

         return null;
      }
      finally
      {
         process?.Kill();
      }
   }
}