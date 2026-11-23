#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
exec dotnet run --project StudyPlanner.csproj --no-launch-profile --urls http://127.0.0.1:5088
