#!/bin/bash
# SessionStart hook for Claude Code on the web: the repo's headless build
# (src/*.Mirror, src/GenLevels) needs a .NET 8 SDK, which the remote
# container does not ship. Installs it from the Ubuntu archive when missing
# and warms the NuGet cache so `dotnet test` runs without a network round trip.
# Local sessions are left alone.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "session-start: installing dotnet-sdk-8.0"
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq
  apt-get install -y -qq dotnet-sdk-8.0
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> "$CLAUDE_ENV_FILE"
echo 'export DOTNET_NOLOGO=1' >> "$CLAUDE_ENV_FILE"

echo "session-start: dotnet $(dotnet --version); restoring the mirror solution"
dotnet restore src/GridInfect.sln --nologo -v q
dotnet restore src/GenLevels/GridInfect.GenLevels.csproj --nologo -v q
dotnet build src/Tests.Mirror/GridInfect.Core.Tests.Mirror.csproj --nologo -v q --no-restore
dotnet build src/GenLevels/GridInfect.GenLevels.csproj -c Release --nologo -v q --no-restore
echo "session-start: ready (dotnet test src/Tests.Mirror/GridInfect.Core.Tests.Mirror.csproj)"
