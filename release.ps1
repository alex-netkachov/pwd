##
# Builds a self-contained executables for windows and linix.
#

$version = Get-Date -Format "yyyy.M.d"
$command = "dotnet publish ./pwd.cli/pwd.cli.csproj -p:Version=$($version).1 --configuration Release --self-contained"

Remove-Item -Path ./bin/Release -Recurse -Force -ErrorAction SilentlyContinue
Invoke-Expression "${command} -r win-x64"
Invoke-Expression "${command} -r linux-x64"
