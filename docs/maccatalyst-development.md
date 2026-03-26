# MacCatalyst Development Entry Point

## Canonical files

- Solution: `src/Greenshot.Maui.slnx`
- App project: `src/Greenshot.Maui/Greenshot.Maui.csproj`
- Test project: `src/Greenshot.Maui.Core.Tests/Greenshot.Maui.Core.Tests.csproj`
- Local helper: `scripts/maui-maccatalyst-dev.sh`

## Recommended commands

Use the helper script instead of remembering the full `dotnet` command line:

```bash
scripts/maui-maccatalyst-dev.sh restore
scripts/maui-maccatalyst-dev.sh test
scripts/maui-maccatalyst-dev.sh build
scripts/maui-maccatalyst-dev.sh open
scripts/maui-maccatalyst-dev.sh reset-screen-recording
scripts/maui-maccatalyst-dev.sh recover-screen-recording
```

The script standardizes the local Mac development flags:

- `net10.0-maccatalyst`
- `maccatalyst-arm64`
- `ValidateXcodeVersion=false`
- `CodesignKey=<first Apple Development identity in keychain>` when `GRENSHOT_MAC_CODESIGN_KEY` is not already set

`ValidateXcodeVersion=false` is deliberate for the current local workflow because this machine has Xcode 26.3 while the installed .NET MacCatalyst pack still expects Xcode 26.2. If the local SDK/Xcode pairing changes later, set `GRENSHOT_VALIDATE_XCODE_VERSION=true` before running the script.

## Screen Recording note

When the helper script finds an `Apple Development` identity, local builds are signed with that stable identity and Screen Recording approval is much more reliable across rebuilds. If no signing identity is available, the build falls back to ad hoc signing, which means a rebuild changes the app's code hash and macOS can keep Screen Recording approval attached to an older build even when System Settings still shows the same bundle id. If capture suddenly stops working after a rebuild, reset TCC approval and approve the current build again:

```bash
scripts/maui-maccatalyst-dev.sh reset-screen-recording
```

## Permission-safe launch workflow

If you are validating Screen Recording permission, do not keep using `run` after approval. `run` can rebuild the app and change the code requirement TCC is matching against. Use this sequence instead:

```bash
scripts/maui-maccatalyst-dev.sh build
scripts/maui-maccatalyst-dev.sh open
```

If permission breaks again, use the recovery command that resets TCC and immediately reopens the same built `.app`:

```bash
scripts/maui-maccatalyst-dev.sh recover-screen-recording
```

This is the remembered/canonical local workflow for Screen Recording validation on this repo:

1. `build` once with stable signing.
2. Grant Screen Recording to that build.
3. Keep reopening the same `.app` with `open`.
4. Only rebuild when you actually need new code.

## Environment overrides

You can override the defaults without editing the script:

```bash
export GRENSHOT_MAC_CONFIGURATION=Release
export GRENSHOT_MAC_RUNTIME_IDENTIFIER=maccatalyst-arm64
export GRENSHOT_VALIDATE_XCODE_VERSION=false
export GRENSHOT_MAC_CODESIGN_KEY="Apple Development: Your Name (TEAMID)"
```

## Direct commands

If you need the raw `dotnet` commands, these are the current equivalents:

```bash
dotnet test src/Greenshot.Maui.Core.Tests/Greenshot.Maui.Core.Tests.csproj -v minimal

dotnet build src/Greenshot.Maui/Greenshot.Maui.csproj \
  -c Debug \
  -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:ValidateXcodeVersion=false \
  -p:CodesignKey="Apple Development: Your Name (TEAMID)" \
  -v minimal

open -a src/Greenshot.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Greenshot.app
```

For signing prerequisites and TCC notes, see `docs/maccatalyst-signing.md`.
