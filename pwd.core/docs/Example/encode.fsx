#r "../../bin/Release/pwd.core.dll"

open pwd.core
open pwd.core.abstractions

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
