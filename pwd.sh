#!/bin/bash

SOLUTION_FOLDER=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

dotnet publish "$SOLUTION_FOLDER/pwd/pwd.csproj" --configuration Release --runtime linux-x64 -p:PublishSingleFile=true --self-contained
if [ $? -ne 0 ]; then
  exit 1
fi

$SOLUTION_FOLDER/pwd/bin/Release/net6.0/linux-x64/publish/pwd