#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

pushd "$REPO_ROOT" >/dev/null
dotnet ef database update --project src/Infrastructure --startup-project src/Server
popd >/dev/null
echo "Database updated."
