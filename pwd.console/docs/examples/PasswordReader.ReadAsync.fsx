#r "../../bin/Release/publish/pwd.console.dll"

open System.Threading
open pwd.console
open pwd.console.readers

let console = new Console()
let reader = new PasswordReader(console)
let password =
    reader.ReadAsync("password> ", CancellationToken.None)
    |> Async.AwaitTask
    |> Async.RunSynchronously
printfn $"%s{password}"
