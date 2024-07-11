# pwd.core

`pwd` core library. Contains the basic functionality for the `pwd` application. 

- AES Cipher
- Base64 URL encoding
- Filesystem Repository

More details are in the [types](<types.md>) article.

## Examples

Add reference to the library and open its namespace:

```fsharp
#r "../../bin/Release/net8.0/publish/pwd.core.dll"
open pwd.core
```

Open a repository from the folder "pwd" and list the files (F#):

```fsharp
let repository = FolderRepository.Open ("$9cre7", ".")
repository.List "/" 
|> Seq.iter (fun item -> printfn $"%s{item}")
```

Creates an encrypted file in the repository (F#):

```fsharp
let repository = FolderRepository.Open ("$9cre7", ".")
repository.Write ("/test", "content") 
```

Reads content of an encrypted file in the repository (F#):

```fsharp
let repository = Repository.Open ("$9cre7", ".")
printfn "%s" (repository.Read "/test")
```
