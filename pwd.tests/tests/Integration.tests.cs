using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using pwd.console.abstractions;
using pwd.core.abstractions;
using pwd.mocks;

namespace pwd.tests;

public class Integration_Tests
{
   private static readonly Settings DefaultSettings = new(Timeout.InfiniteTimeSpan);

   [Test]
   [CancelAfter(5000)]
   public async Task QuickStart(
      CancellationToken token)
   {
      var logger =
         new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .BuildServiceProvider()
            .GetRequiredService<ILogger<Integration_Tests>>();

      var fs = Shared.GetMockFs();

      var view =
         new TestView([
            "secret",
            "secret",
            ".add website.com",
            "user: tom",
            "password: secret",
            "",
            "website.com",
            ".ccp",
            "..",
            ".quit"
         ]);

      using var host =
         Program.SetupHost(
            fs,
            Mock.Of<IConsole>(),
            Mock.Of<IPresenter>(),
            () => view,
            configureLogging: _ => { });

      logger.LogInformation("Before Program.Run(...)");
      await Program.Run(host, DefaultSettings);
      logger.LogInformation("After Program.Run(...)");

      var expected = string.Join("\n",
         "Password: ******",
         "",
         "It looks that you are creating a new repository. Please confirm your password: ******",
         "> .add website.com",
         "+> user: tom",
         "+> password: secret",
         "+> ",
         "> website.com",
         "user: tom\r",
         "password: ************\r",
         "",
         "website.com> .ccp",
         "website.com> ..",
         "> .quit",
         "");
      var actual = view.GetOutput();
      Assert.That(actual, Is.EqualTo(expected));
   }

   [Test]
   [CancelAfter(5000)]
   public async Task Initialise_from_empty_repository()
   {
      var logger = new ConsoleLogger();

      var fs = Shared.GetMockFs();

      var view =
         new TestView([
            "secret",
            "secret",
            ".quit"
         ]);

      using var host =
         Program.SetupHost(
            fs,
            Mock.Of<IConsole>(),
            Mock.Of<IPresenter>(),
            () => view);

      logger.Info("Before Program.Run(...)");
      await Program.Run(host, DefaultSettings);
      logger.Info("After Program.Run(...)");

      var expected =
         string.Join("\n",
            "Password: ******",
            "It looks that you are creating a new repository. Please confirm your password: ******",
            "> .quit\n");
      var actual = view.GetOutput();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [CancelAfter(3000)]
   public async Task Initialise_with_repository_with_files(
      CancellationToken token)
   {
      var fs = Shared.GetMockFs();

      var view =
         new TestView([
            "secret",
            "secret",
            "file1",
            ".rm",
            "y",
            ".quit"
         ]);

      using var host =
         Program.SetupHost(
            fs,
            Mock.Of<IConsole>(),
            Mock.Of<IPresenter>(),
            () => view);

      var repositoryFactory = host.Services.GetRequiredService<RepositoryFactory>();
      var repository = repositoryFactory("/container/test", "secret");
      await repository.WriteAsync(
         "file1",
         "content1");

      await Program.Run(host, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         ".",
         "> file1",
         "content1",
         "file1> .rm",
         "Delete 'file1'? (y/N) y",
         "'file1' has been deleted.",
         "> .quit\n");
      var actual = view.GetOutput();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [CancelAfter(4000)]
   public async Task Initialise_from_empty_repository_plus_locking()
   {
      var fs = Shared.GetMockFs();

      var view =
         new TestView([
            "secret",
            "secret",
            ".lock",
            "secret",
            ".quit"
         ]);

      using var host =
         Program.SetupHost(
            fs,
            Mock.Of<IConsole>(),
            Mock.Of<IPresenter>(),
            () => view);

      await Program.Run(host, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         "> .quit\n");
      var actual = view.GetOutput();
      Assert.That(actual, Is.EqualTo(expected));
   }
}