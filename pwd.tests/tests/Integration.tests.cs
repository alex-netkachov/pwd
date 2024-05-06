using System.Threading.Channels;
using Moq;
using pwd.mocks;
using pwd.repository;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System;
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
      var logger = new ConsoleLogger();

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
            logger.Info("waiting for Read");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<Read>());

            logger.Info($"writing `{instruction}`");
            await writer.WriteAsync(instruction + "\n");
         }

         try
         {
            logger.Info("waiting for ReadPassword");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.Info("writing `secret`");
            await writer.WriteAsync("secret\n");

            logger.Info("waiting for ReadPassword (confirmation)");
            Assert.That(await reader.ReadAsync(), Is.InstanceOf<ReadPassword>());

            logger.Info("writing `secret`");
            await writer.WriteAsync("secret\n");

            await WaitForReadAndType(".add website.com");
            await WaitForReadAndType("user: tom");
            await WaitForReadAndType("password: secret");
            await WaitForReadAndType("");
            await WaitForReadAndType("web{TAB}");
            await WaitForReadAndType(".ccp");
            await WaitForReadAndType("..");
            await WaitForReadAndType(".quit");

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
      var cipherFactory = new FastTestCipherFactory();

      using var host = Program.SetupHost(logger, console, fs, cipherFactory, view);

      logger.Info("Before Program.Run(...)");
      await Program.Run(host, DefaultSettings);
      logger.Info("After Program.Run(...)");

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
      var cipherFactory = new FastTestCipherFactory();

      using var host = Program.SetupHost(logger, console, fs, cipherFactory, view);

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
      var cipher = FastTestCipher.Instance;
      var repository = Shared.CreateRepository(fs);
      await repository
         .CreateFile(Path.From(Name.Parse(fs, "file1")))
         .WriteAsync("content1");

      using var console = new TestConsole(input.Reader);
      using var reader = new ConsoleReader(console);
      var view = new ViewWithNotifications(new ConsoleView(console, reader), notifications.Writer);
      var cipherFactory = new FastTestCipherFactory();

      using var host = Program.SetupHost(Mock.Of<ILogger>(), console, fs, cipherFactory, view);
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
      var cipherFactory = new FastTestCipherFactory();

      using var host = Program.SetupHost(Mock.Of<ILogger>(), console, fs, cipherFactory, view);
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
      var cipherFactory = new FastTestCipherFactory();

      using var host = Program.SetupHost(Mock.Of<ILogger>(), console, fs, cipherFactory, view);

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