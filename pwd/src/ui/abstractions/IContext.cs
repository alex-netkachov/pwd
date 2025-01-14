using System;
using pwd.console.abstractions;

namespace pwd.ui.abstractions;

/// <summary>
///   Represents a currently active operational context, such as a list,
///   an element in view mode, an element in edit mode, an input field,
///   a confirmation dialog, and other similar operational states.
/// </summary>
public interface IContext
   : IObservable<IView>,
     IDisposable;