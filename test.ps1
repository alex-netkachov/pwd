##
# Restores packages, cleans, and tests pwd.
#
# Run it from the solution's folder.
#

dotnet restore
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet clean
if ($LastExitCode -ne 0) { exit $LastExitCode }

dotnet test .\pwd.cli.tests\pwd.cli.tests.csproj --configuration Release