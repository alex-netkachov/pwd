# pwd.core

- AES Cipher
- Base64 URL encoding
- Filesystem Repository

## Examples

Open a repository from the folder "pwd" and list the files (F#):

```fsharp
#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

// create or open repository in the current folder
let repository = Repository.Open ("$9cre7", ".")

// list files from the repository root folder
repository.Root
|> repository.List 
|> Seq.iter (fun item -> printfn $"%s{item.Name.Value}")
```

Creates an encrypted file in the repository (F#):

```fsharp
#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

let repository = Repository.Open ("$9cre7", ".")
let location = repository.Root.Down "test"
repository.Write (location, "content")  
```

Reads content of an encrypted file in the repository (F#):

```fsharp
#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

let repository = Repository.Open ("$9cre7", ".")
let location = repository.Root.Down "test"
repository.Read location
|> printfn "%s"
```
