#!/bin/bash

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
dotnet run --project "$SCRIPT_DIR/pwd/pwd.csproj" --runtime linux-x64 --self-contained "$@"
