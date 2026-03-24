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
scripts/maui-maccatalyst-dev.sh reset-screen-recording
```

The script standardizes the local Mac development flags:

- `net10.0-maccatalyst`
- `maccatalyst-arm64`
- `ValidateXcodeVersion=false`

`ValidateXcodeVersion=false` is deliberate for the current local workflow because this machine has Xcode 26.3 while the installed .NET MacCatalyst pack still expects Xcode 26.2. If the local SDK/Xcode pairing changes later, set `GRENSHOT_VALIDATE_XCODE_VERSION=true` before running the script.

## Screen Recording note

The current local build is still ad hoc-signed. That means a rebuild changes the app's code hash, and macOS can keep Screen Recording approval attached to an older build even when System Settings still shows the same bundle id. If capture suddenly stops working after a rebuild, reset TCC approval and approve the current build again:

```bash
scripts/maui-maccatalyst-dev.sh reset-screen-recording
```

## Environment overrides

You can override the defaults without editing the script:

```bash
export GRENSHOT_MAC_CONFIGURATION=Release
export GRENSHOT_MAC_RUNTIME_IDENTIFIER=maccatalyst-arm64
export GRENSHOT_VALIDATE_XCODE_VERSION=false
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
  -v minimal
```

For signing prerequisites and TCC notes, see `docs/maccatalyst-signing.md`.
