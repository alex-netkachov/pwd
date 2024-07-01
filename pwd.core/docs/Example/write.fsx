#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

let repository = FolderRepository.Open ("$9cre7", ".")
let location = repository.Root.Down "test"
repository.Write (location, "content")
