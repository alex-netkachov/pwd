#!/bin/bash

# Publishes and runs pwd. Can be called from any folder.

SOLUTION_FOLDER=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

dotnet publish "$SOLUTION_FOLDER/pwd/pwd.csproj" --configuration Release --runtime linux-x64 --self-contained
if [ $? -ne 0 ]; then exit 1; fi

dotnet $SOLUTION_FOLDER/pwd/bin/Release/pwd.dll