#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION_PATH="$ROOT_DIR/src/Greenshot.Maui.slnx"
PROJECT_PATH="$ROOT_DIR/src/Greenshot.Maui/Greenshot.Maui.csproj"
TEST_PROJECT_PATH="$ROOT_DIR/src/Greenshot.Maui.Core.Tests/Greenshot.Maui.Core.Tests.csproj"

CONFIGURATION="${GRENSHOT_MAC_CONFIGURATION:-Debug}"
RUNTIME_IDENTIFIER="${GRENSHOT_MAC_RUNTIME_IDENTIFIER:-maccatalyst-arm64}"
VALIDATE_XCODE_VERSION="${GRENSHOT_VALIDATE_XCODE_VERSION:-false}"

print_usage() {
  cat <<'EOF'
Usage: scripts/maui-maccatalyst-dev.sh <command>

Commands:
  restore   Restore the MAUI migration solution.
  test      Run the MAUI core test suite.
  build     Build the MacCatalyst app with the canonical local-development flags.
  run       Run the MacCatalyst app with the canonical local-development flags.
  reset-screen-recording  Reset macOS Screen Recording approval for org.greenshot.maui.
  all       Restore, test, and build in order.

Environment overrides:
  GRENSHOT_MAC_CONFIGURATION      Defaults to Debug
  GRENSHOT_MAC_RUNTIME_IDENTIFIER Defaults to maccatalyst-arm64
  GRENSHOT_VALIDATE_XCODE_VERSION Defaults to false
EOF
}

restore_solution() {
  dotnet restore "$SOLUTION_PATH"
}

run_tests() {
  dotnet test "$TEST_PROJECT_PATH" -v minimal
}

build_app() {
  dotnet build "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -f net10.0-maccatalyst \
    -p:RuntimeIdentifier="$RUNTIME_IDENTIFIER" \
    -p:ValidateXcodeVersion="$VALIDATE_XCODE_VERSION" \
    -v minimal
}

run_app() {
  dotnet run --project "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -f net10.0-maccatalyst \
    -p:RuntimeIdentifier="$RUNTIME_IDENTIFIER" \
    -p:ValidateXcodeVersion="$VALIDATE_XCODE_VERSION"
}

reset_screen_recording() {
  tccutil reset ScreenCapture org.greenshot.maui
}

COMMAND="${1:-}"

case "$COMMAND" in
  restore)
    restore_solution
    ;;
  test)
    run_tests
    ;;
  build)
    build_app
    ;;
  run)
    run_app
    ;;
  reset-screen-recording)
    reset_screen_recording
    ;;
  all)
    restore_solution
    run_tests
    build_app
    ;;
  *)
    print_usage
    exit 1
    ;;
esac
