@echo off

rem Publishes and runs pwd. Can be called from any folder.

SET SOLUTION_FOLDER=%~dp0

dotnet publish "%SOLUTION_FOLDER%/pwd/pwd.csproj" --configuration Release --runtime win-x64 -p:PublishSingleFile=true --self-contained
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet %SOLUTION_FOLDER%\pwd\bin\Release\net6.0\pwd.dll