#!/bin/bash

rm bin/Release/net6.0/linux-x64/publish/*
dotnet publish -p:Version=2022.1.27.1 --configuration Release -r linux-x64 -p:PublishSingleFile=true --self-contained false

rm bin/Release/net6.0/win-x64/publish/*
dotnet publish -p:Version=2022.1.27.1 --configuration Release -r win-x64 -p:PublishSingleFile=true --self-contained false