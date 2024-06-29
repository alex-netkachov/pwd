#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

// create or open repository in the current folder
let repository = FolderRepository.Open ("$9cre7", ".")

// list files from the repository root folder
repository.Root
|> repository.List 
|> Seq.iter (fun item -> printfn $"%s{item.Name.Value}")
