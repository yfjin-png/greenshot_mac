#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION_PATH="$ROOT_DIR/src/Greenshot.Maui.slnx"
PROJECT_PATH="$ROOT_DIR/src/Greenshot.Maui/Greenshot.Maui.csproj"
TEST_PROJECT_PATH="$ROOT_DIR/src/Greenshot.Maui.Core.Tests/Greenshot.Maui.Core.Tests.csproj"

CONFIGURATION="${GRENSHOT_MAC_CONFIGURATION:-Debug}"
RUNTIME_IDENTIFIER="${GRENSHOT_MAC_RUNTIME_IDENTIFIER:-maccatalyst-arm64}"
VALIDATE_XCODE_VERSION="${GRENSHOT_VALIDATE_XCODE_VERSION:-false}"
CODESIGN_KEY="${GRENSHOT_MAC_CODESIGN_KEY:-}"
CODESIGN_PROVISION="${GRENSHOT_MAC_CODESIGN_PROVISION:-}"
APP_BUNDLE_PATH="$ROOT_DIR/src/Greenshot.Maui/bin/$CONFIGURATION/net10.0-maccatalyst/$RUNTIME_IDENTIFIER/Greenshot.app"

resolve_codesign_key() {
  if [[ -n "$CODESIGN_KEY" ]]; then
    printf '%s\n' "$CODESIGN_KEY"
    return 0
  fi

  security find-identity -v -p codesigning 2>/dev/null \
    | sed -n 's/.*"\(Apple Development:.*\)".*/\1/p' \
    | head -n 1
}

print_usage() {
  cat <<'EOF'
Usage: scripts/maui-maccatalyst-dev.sh <command>

Commands:
  restore   Restore the MAUI migration solution.
  test      Run the MAUI core test suite.
  build     Build the MacCatalyst app with the canonical local-development flags.
  open      Open the current built .app without rebuilding. Recommended after Screen Recording approval.
  run       Run the MacCatalyst app with the canonical local-development flags.
            Note: dotnet run can rebuild and may disturb TCC validation while permissions are being checked.
  reset-screen-recording  Reset macOS Screen Recording approval for org.greenshot.maui.
  recover-screen-recording  Reset Screen Recording approval and reopen the current built app.
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
  local resolved_codesign_key
  resolved_codesign_key="$(resolve_codesign_key)"

  local -a msbuild_args=(
    -c "$CONFIGURATION"
    -f net10.0-maccatalyst
    -p:RuntimeIdentifier="$RUNTIME_IDENTIFIER"
    -p:ValidateXcodeVersion="$VALIDATE_XCODE_VERSION"
  )

  if [[ -n "$resolved_codesign_key" ]]; then
    msbuild_args+=(-p:CodesignKey="$resolved_codesign_key")
  fi

  if [[ -n "$CODESIGN_PROVISION" ]]; then
    msbuild_args+=(-p:CodesignProvision="$CODESIGN_PROVISION")
  fi

  dotnet build "$PROJECT_PATH" \
    "${msbuild_args[@]}" \
    -v minimal
}

run_app() {
  local resolved_codesign_key
  resolved_codesign_key="$(resolve_codesign_key)"

  local -a msbuild_args=(
    -c "$CONFIGURATION"
    -f net10.0-maccatalyst
    -p:RuntimeIdentifier="$RUNTIME_IDENTIFIER"
    -p:ValidateXcodeVersion="$VALIDATE_XCODE_VERSION"
  )

  if [[ -n "$resolved_codesign_key" ]]; then
    msbuild_args+=(-p:CodesignKey="$resolved_codesign_key")
  fi

  if [[ -n "$CODESIGN_PROVISION" ]]; then
    msbuild_args+=(-p:CodesignProvision="$CODESIGN_PROVISION")
  fi

  dotnet run --project "$PROJECT_PATH" \
    "${msbuild_args[@]}"
}

open_built_app() {
  if [[ ! -d "$APP_BUNDLE_PATH" ]]; then
    build_app
  fi

  open -a "$APP_BUNDLE_PATH"
}

reset_screen_recording() {
  tccutil reset ScreenCapture org.greenshot.maui
}

recover_screen_recording() {
  reset_screen_recording
  open_built_app
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
  open)
    open_built_app
    ;;
  run)
    run_app
    ;;
  reset-screen-recording)
    reset_screen_recording
    ;;
  recover-screen-recording)
    recover_screen_recording
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
