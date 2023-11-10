##
# Restores packages, cleans, builds, publishes, and tests pwd.
# Supposed to be executed from the solution folder.
#

dotnet restore
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet clean
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet publish .\pwd\pwd.csproj --configuration Release --self-contained -r win-x64
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet build .\pwd.tests\pwd.tests.csproj --configuration Release
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet test .\pwd.tests\pwd.tests.csproj --configuration Release