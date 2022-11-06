@echo off
SET SOLUTION_FOLDER=%~dp0
dotnet run --project "%SOLUTION_FOLDER%/pwd/pwd.csproj" --runtime win-x64 --self-contained "$@"