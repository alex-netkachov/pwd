#r "../../bin/Release/publish/pwd.console.dll"

open System.Threading
open pwd.console

let view = new View()
let console = new Console()
view.Activate console
let password =
    view.ReadPasswordAsync("password> ", CancellationToken.None)
    |> Async.AwaitTask
    |> Async.RunSynchronously
printfn $"%s{password}"
