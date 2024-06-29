using System.Threading.Channels;
using pwd.mocks;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System;
using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pwd.ui;
using pwd.ui.readline;

namespace pwd.tests;

public class Integration_Tests
{
   private static readonly Settings DefaultSettings = new(Timeout.InfiniteTimeSpan);

   [Test]
   [CancelAfter(5000)]
   public async Task QuickStart()
   {
      var logger =
         new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .BuildServiceProvider()
            .GetRequiredService<ILogger<Integration_Tests>>();

      var fs = Shared.GetMockFs();
      
      var notifications = Channel.CreateUnbounded<IViewNotification>();
      var input = Channel.CreateUnbounded<string>();

      Shared.Run(async () =>
      {
         var reader = notifications.Reader;
         var writer = input.Writer;

         async Task WaitForReadAndType(
            string instruction)
         {
            logger.LogInformation("waiting for Read");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());

            logger.LogInformation($"writing `{instruction}`");
            await writer.WriteAsync(instruction + "\n");
         }

         try
         {
            logger.LogInformation("waiting for ReadPassword");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.LogInformation("writing `secret`");
            await writer.WriteAsync("secret\n");

            logger.LogInformation("waiting for ReadPassword (confirmation)");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.LogInformation("writing `secret`");
            await writer.WriteAsync("secret\n");

            await WaitForReadAndType(".add website.com");
            await WaitForReadAndType("user: tom");
            await WaitForReadAndType("password: secret");
            await WaitForReadAndType("");
            await WaitForReadAndType("web{TAB}");
            await WaitForReadAndType(".ccp");
            await WaitForReadAndType("..");
            await WaitForReadAndType(".quit");

            logger.LogInformation("done writing");
         }
         catch (Exception e)
         {
            logger.LogError(e.ToString());
         }
      });
      
      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);

      using var host = Program.SetupHost(console, fs, view);

      logger.LogInformation("Before Program.Run(...)");
      await Program.Run(host, DefaultSettings);
      logger.LogInformation("After Program.Run(...)");

      var expected = string.Join("\n",
         "Password: ******",
         "",
         "repository contains 0 files",
         "It seems that you are creating a new repository. Please confirm password: ******",
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
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }

   [Test]
   [CancelAfter(5000)]
   public async Task Initialise_from_empty_repository()
   {
      var logger = new ConsoleLogger();

      var fs = Shared.GetMockFs();

      var notifications = Channel.CreateUnbounded<IViewNotification>();
      var input = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         var reader = notifications.Reader;
         var writer = input.Writer;

         try
         {
            logger.Info("waiting for ReadPassword");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.Info("writing `secret`");
            await writer.WriteAsync("secret\n");

            logger.Info("waiting for ReadPassword");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.Info("writing `secret`");
            await writer.WriteAsync("secret\n");

            logger.Info("waiting for Read");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());

            logger.Info("writing `.quit`");
            await writer.WriteAsync(".quit\n");

            logger.Info("done writing");
         }
         catch (Exception e)
         {
            logger.Error(e.ToString());
         }
      });

      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);

      using var host = Program.SetupHost(console, fs, view);

      logger.Info("Before Program.Run(...)");
      await Program.Run(host, DefaultSettings);
      logger.Info("After Program.Run(...)");

      var expected = string.Join("\n",
         "Password: ******",
         "",
         "repository contains 0 files",
         "It seems that you are creating a new repository. Please confirm password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [CancelAfter(3000)]
   public async Task Initialise_with_repository_with_files()
   {
      var notifications = Channel.CreateUnbounded<IViewNotification>();
      var input = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         var reader = notifications.Reader;
         var writer = input.Writer;

         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync("file1\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync(".rm\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Confirm>());
         await writer.WriteAsync("y\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync(".quit\n");
      });

      var fs = Shared.GetMockFs();
      var repository = Shared.CreateRepository(fs);
      await repository.WriteAsync(
         repository.Root.Down("file1"),
         "content1");

      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);

      using var host = Program.SetupHost(console, fs, view);
      await Program.Run(host, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         ".",
         "repository contains 1 file",
         "> file1",
         "content1",
         "file1> .rm",
         "Delete 'file1'? (y/N) y",
         "'file1' has been deleted.",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }
   
   [Test]
   [CancelAfter(4000)]
   public async Task Initialise_from_empty_repository_plus_locking()
   {
      var notifications = Channel.CreateUnbounded<IViewNotification>();
      var input = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         var reader = notifications.Reader;
         var writer = input.Writer;

         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync(".lock\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync(".quit\n");
      });

      var fs = Shared.GetMockFs();

      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);

      using var host = Program.SetupHost(console, fs, view);
      await Program.Run(host, DefaultSettings);
      var expected = string.Join("\n",
         "Password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }

   [Test]
   [CancelAfter(20000)]
   public async Task Initialise_from_empty_repository_plus_timeout_lock()
   {
      var notifications = Channel.CreateUnbounded<IViewNotification>();
      var input = Channel.CreateUnbounded<string>();
      Shared.Run(async () =>
      {
         var reader = notifications.Reader;
         var writer = input.Writer;

         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");

         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");
         
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());
         await writer.WriteAsync("secret\n");

         Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());
         await writer.WriteAsync(".quit\n");
      });

      var fs = Shared.GetMockFs();

      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);

      using var host = Program.SetupHost(console, fs, view);

      await Program.Run(host, new(TimeSpan.FromSeconds(1)));

      var expected = string.Join("\n",
         "Password: ******",
         "> .quit\n");
      var actual = console.GetScreen();
      Assert.That(actual, Is.EqualTo(expected));
   }

   private interface IViewNotification
   {
   }

   private sealed class Confirm
      : IViewNotification
   {
   }

   private sealed class Read
      : IViewNotification
   {
   }

   private sealed class ReadPassword
      : IViewNotification
   {
   }

   private sealed class ViewWithNotifications
      : IView
   {
      private readonly IView _view;
      private readonly ChannelWriter<IViewNotification> _writer;

      public ViewWithNotifications(
         IView view,
         ChannelWriter<IViewNotification> writer)
      {
         _view = view;
         _writer = writer;
      }

      public void Write(
         string text)
      {
         _view.Write(text);
      }

      public void WriteLine(
         string text)
      {
         _view.WriteLine(text);
      }

      public Task<bool> ConfirmAsync(
         string question,
         Answer @default = Answer.No,
         CancellationToken token = default)
      {
         var task = _view.ConfirmAsync(question, @default, token);
         Notify(new Confirm());
         return task;
      }

      public Task<string> ReadAsync(
         string prompt = "",
         ISuggestionsProvider? suggestionsProvider = null,
         CancellationToken token = default)
      {
         var task = _view.ReadAsync(prompt, suggestionsProvider, token);
         Notify(new Read());
         return task;
      }

      public Task<string> ReadPasswordAsync(
         string prompt = "",
         CancellationToken token = default)
      {
         var task = _view.ReadPasswordAsync(prompt, token);
         Notify(new ReadPassword());
         return task;
      }

      public void Clear()
      {
         _view.Clear();
      }

      private void Notify(
         IViewNotification notification)
      {
         // If notification goes immediately after read request it may be read before reading starts
         // making it frozen. Adding small delay makes things not ideal but better.
         Task.Delay(50).ContinueWith(_ =>
         {
            while (!_writer.TryWrite(notification))
            {
            }
         });
      }
   }
}