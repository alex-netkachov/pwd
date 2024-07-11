# UI

It is a console application, therefore UI classes represents the console interface.

## Console

```mermaid
classDiagram
    
class IObservable~ConsoleKeyInfo~ {
    <<interface>>
}

class Console {
    <<inteface>>
    int BufferWidth
    void Write(object? value = null)
    void WriteLine(object? value = null)
    Point GetCursorPosition()
    void SetCursorPosition(Point point)
    void Clear()
}

IObservable~ConsoleKeyInfo~ <|-- Console
Console <|.. StandardConsole
```

## Reader

Reader provides methods for reading user input from the console.

```mermaid
classDiagram

class IReader {
    <<interface>>
    Task<string> ReadAsync(string prompt = "", ISuggestionsProvider? suggestionsProvider = null, CancellationToken token = default)
    Task<string> ReadPasswordAsync(string prompt = "", CancellationToken token = default)
}
```

```mermaid
classDiagram
    
class IReader {
    <<interface>>
}


IReader <|.. ConsoleReader
ConsoleReader o-- StandardConsole
```

## View

View is a combination of Reader and Console.

```mermaid
classDiagram

class IView {
    <<interface>>
}

IView <|.. ConsoleView
ConsoleView o-- StandardConsole
ConsoleView o-- ConsoleReader
```

## State and contexts

- Context is a representation of a dialog window.
- State is a collection of contexts, piling up on each other.
