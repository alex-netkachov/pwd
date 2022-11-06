@echo off

dotnet restore
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet clean
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet publish "pwd/pwd.csproj" --configuration Release --runtime win-x64 -p:PublishSingleFile=true --self-contained
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet build "pwd.tests/pwd.tests.csproj" --configuration Release
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet test "pwd.tests/pwd.tests.csproj" --configuration Release