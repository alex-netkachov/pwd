##
# Publishes the project and runs it. Opens the repository from the current folder
# or creates a new one if it doesn't exist.
#

# Get the directory of the script
$SolutionFolder = Split-Path -Parent $MyInvocation.MyCommand.Definition

dotnet publish "$SolutionFolder/pwd.cli/pwd.cli.csproj" --configuration Release --self-contained -r win-x64

if ($LastExitCode -ne 0) {
  Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
  exit $LastExitCode
}

dotnet "$SolutionFolder\pwd.cli\bin\Release\win-x64\pwd.cli.dll"
