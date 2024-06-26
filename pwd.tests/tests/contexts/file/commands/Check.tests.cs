﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pwd.contexts.file.commands;
using pwd.core;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.tests.contexts.file.commands;

public class Check_Tests
{
   [TestCase("", false)]
   [TestCase(".", false)]
   [TestCase(".ch", false)]
   [TestCase("check", false)]
   [TestCase(".check", true)]
   [TestCase(".check ", true)]
   [TestCase(".check test", true)]
   public void Create_creates_command(
      string input,
      bool creates)
   {
      var repository = Shared.CreateRepository();

      using var factory =
         new Check(
            Mock.Of<IView>(),
            repository.Root);

      var command = factory.Create(input);

      Assert.That(command, creates ? Is.Not.Null : Is.Null);
   }

   [Test]
   public async Task ExecuteAsync_checks_the_content()
   {
      var fs = Shared.GetMockFs();
      var repository = Shared.CreateRepository(fs);
      var location = repository.Root.Down("file");
      await repository.WriteAsync(location, "content");

      var mockView = new Mock<IView>();

      using var factory =
         new Check(
            mockView.Object,
            location);

      var command = factory.Create(".check");
      if (command == null)
      {
         Assert.Fail("command is null");
         return;
      }

      await command.ExecuteAsync();
   }

   [TestCase("", ".check")]
   [TestCase(".", ".check")]
   [TestCase(".CH", ".check")]
   [TestCase(".Ch", ".check")]
   [TestCase(".ch", ".check")]
   [TestCase(".check", "")]
   [TestCase(".check ", "")]
   [TestCase(".check test", "")]
   public void Suggestions_works_as_expected(
      string input,
      string suggestions)
   {
      var repository = Shared.CreateRepository();

      using var factory =
         new Check(
            Mock.Of<IView>(),
            repository.Root);

      Assert.That(
         factory.Suggestions(input),
         Is.EqualTo(
            string.IsNullOrEmpty(suggestions)
               ? Array.Empty<string>()
               : suggestions.Split(';')));
   }
}