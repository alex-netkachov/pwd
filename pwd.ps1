##
# Publishes and runs pwd. Makes a repository in this folder if it doesn't exist.
#

# Get the directory of the script
$SolutionFolder = Split-Path -Parent $MyInvocation.MyCommand.Definition

dotnet publish "$SolutionFolder/pwd/pwd.csproj" --configuration Release --self-contained -r win-x64

if ($LastExitCode -ne 0) {
  Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
  exit $LastExitCode
}

dotnet "$SolutionFolder\pwd\bin\Release\win-x64\pwd.dll"
