#!/bin/bash
#
# Builds a self-contained executables for windows and linix.
#

COMMAND="dotnet publish -p:Version=2023.11.4.1 --configuration Release --self-contained"

rm bin/Release/*
$COMMAND -r win-x64
$COMMAND -r linux-x64
