#!/bin/bash

# Restores packages, cleans, builds, publishes, and tests pwd.
# Supposed to be executed from the solution folder.

dotnet restore
if [ $? -ne 0 ]; then exit 1; fi

dotnet clean
if [ $? -ne 0 ]; then exit 1; fi

dotnet publish "pwd/pwd.csproj" --configuration Release --runtime linux-x64 -p:PublishSingleFile=true --self-contained
if [ $? -ne 0 ]; then exit 1; fi

dotnet build "pwd.tests/pwd.tests.csproj" --configuration Release
if [ $? -ne 0 ]; then exit 1; fi

dotnet test "pwd.tests/pwd.tests.csproj" --configuration Release