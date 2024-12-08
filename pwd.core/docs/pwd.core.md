# pwd.core

`pwd` core library. Handles encryption, encoding, and storage. 

- AES Cipher
- Base64 URL encoding
- Filesystem Repository

More details are in [types](<types.md>).

## Examples

Add reference to the library and open its namespace:

```fsharp
#r "../../bin/Release/pwd.core.dll"

open pwd.core
open pwd.core.abstractions
```

Open a repository from the folder "pwd" and list the files (F#):

```fsharp
let repository = FolderRepository.Open (".", "[secret]")
repository.List "/" 
|> Seq.iter (fun item -> printfn $"%s{item}")
```

Creates an encrypted file in the repository (F#):

```fsharp
let repository = FolderRepository.Open (".", "[secret]")
repository.Write ("/test", "content") 
```

Reads text from an encrypted file in the repository (F#):

```fsharp
let repository = Repository.Open (".", "[secret]")
printfn "%s" (repository.ReadText "/test")
```

Encodes text into string (F#):

```fsharp
let cipher = new AesCipher ("$9cre7", null)
let encoder = Base64Url ()

let text = "text"

let initialisationData =
    cipher.GetInitialisationData()
    |> encoder.Encode
    
let encrypted =
    text
    |> cipher.Encrypt
    |> encoder.Encode

printfn $"%s{initialisationData}%s{encrypted}"
```

Decode text from string (F#):

```fsharp
let encoder = Base64Url ()

let text =
    "-00DoKU0oxoAM10PP_vhKk2m-k6O2gbOEtwdetbEbyZp2TjXHgmhhg=="

let initialisationData =
    text[0..31]
    |> encoder.Decode

let cipher =
    new AesCipher ("$9cre7", initialisationData)

text[32..]
|> encoder.Decode
|> cipher.DecryptString
|> printfn "%s"
```
