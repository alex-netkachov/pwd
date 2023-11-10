@echo off

rem Publishes and runs pwd. Can be called from any folder.

SET SOLUTION_FOLDER=%~dp0

dotnet publish "%SOLUTION_FOLDER%/pwd/pwd.csproj" --configuration Release --self-contained -r win-x64
if NOT %ERRORLEVEL% == 0 GOTO :EOF

dotnet %SOLUTION_FOLDER%\pwd\bin\Release\win-x64\pwd.dll