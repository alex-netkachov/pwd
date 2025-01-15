#r "../../bin/Release/publish/pwd.core.dll"

open pwd.core
open pwd.core.abstractions

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
