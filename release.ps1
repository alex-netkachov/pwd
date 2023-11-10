##
# Builds a self-contained executables for windows and linix.
#

$command = "dotnet publish .\\pwd\\pwd.csproj -p:Version=2023.11.4.1 --configuration Release --self-contained"

Remove-Item -Path .\bin\Release -Recurse -Force -ErrorAction SilentlyContinue
Invoke-Expression "${command} -r win-x64"
Invoke-Expression "${command} -r linux-x64"
