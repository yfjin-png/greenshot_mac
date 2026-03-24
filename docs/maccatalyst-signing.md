# MacCatalyst Signing And Permissions

## Why this matters

For local macOS capture testing, Screen Recording approval is only reliable when the app is signed with a stable identity. Ad hoc-signed debug builds can keep the same bundle identifier while changing code signature requirements on every rebuild, which makes TCC-backed permission approval unreliable.

## Current app facts

- Bundle identifier: `org.greenshot.maui`
- Target framework: `net10.0-maccatalyst`
- Default unsigned/debug fallback: ad hoc

## Recommended local setup

For the canonical local build and test entrypoints, use `docs/maccatalyst-development.md` and `scripts/maui-maccatalyst-dev.sh`.

1. In Xcode, add your Apple ID under `Settings > Accounts`.
2. Create an `Apple Development` certificate if one is missing.
3. Export the signing identity into your shell session:

```bash
export GRENSHOT_MAC_CODESIGN_KEY="Apple Development: Your Name (TEAMID)"
```

4. If your environment requires an explicit provisioning profile, also export:

```bash
export GRENSHOT_MAC_CODESIGN_PROVISION="Your Provisioning Profile Name"
```

5. Build the app normally:

```bash
dotnet build src/Greenshot.Maui/Greenshot.Maui.csproj \
  -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:ValidateXcodeVersion=false
```

The project now picks up `GRENSHOT_MAC_CODESIGN_KEY` and `GRENSHOT_MAC_CODESIGN_PROVISION` automatically for MacCatalyst builds.

## Useful diagnostics

Check signing identities available on this Mac:

```bash
security find-identity -v -p codesigning
```

As of 2026-03-23 on this development Mac, the command still reports:

```text
0 valid identities found
```

Inspect the built app signature:

```bash
codesign -dv --verbose=4 src/Greenshot.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Greenshot.app
```

If no `Apple Development` identity is configured, the current build remains ad hoc-signed. On 2026-03-23 the output included:

```text
Identifier=org.greenshot.maui
Signature=adhoc
TeamIdentifier=not set
```

That is sufficient for local binaries, but not for closing the TCC validation work in Phase 2.

Reset Screen Recording approval for the current bundle id:

```bash
tccutil reset ScreenCapture org.greenshot.maui
```
