using Microsoft.Extensions.DependencyInjection;
using pwd.cli.contexts.file.commands;
using pwd.core.abstractions;

namespace pwd.cli.contexts.file;

public delegate ICheck CheckFactory(
   IRepository repository,
   string path);

public delegate ICopyField CopyFieldFactory(
   IRepository repository,
   string path);

public static class FileContextServicesExtension
{
   public static IServiceCollection AddFileContextServices(
      this IServiceCollection services)
   {
      services.AddSingleton<CheckFactory>(
         _ =>
            (repository, path) =>
               new Check(
                  repository,
                  path));

      services.AddSingleton<CopyFieldFactory>(
         provider =>
            (repository, path) =>
               new CopyField(
                  provider.GetRequiredService<IClipboard>(),
                  repository,
                  path));

      return services;
   }
}