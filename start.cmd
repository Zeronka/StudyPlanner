@echo off
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet run --project StudyPlanner.csproj --no-launch-profile --urls http://127.0.0.1:5088
pause
