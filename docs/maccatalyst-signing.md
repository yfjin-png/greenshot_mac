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
3. Export the signing identity into your shell session if you want to pin a specific certificate:

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
The local helper script also auto-detects the first `Apple Development` identity in your keychain when `GRENSHOT_MAC_CODESIGN_KEY` is unset.

## Useful diagnostics

Check signing identities available on this Mac:

```bash
security find-identity -v -p codesigning
```

As of 2026-03-24 on this development Mac, the command reports:

```text
1) DDBC333F3DCEDEF09FEB74D0845C0F3036E15514 "Apple Development: kanri462afterfit@icloud.com (F285Z2WHPA)"
   1 valid identities found
```

Inspect the built app signature:

```bash
codesign -dv --verbose=4 src/Greenshot.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Greenshot.app
```

With the detected `Apple Development` identity configured, the current build is no longer ad hoc-signed. On 2026-03-24 the output included:

```text
Identifier=org.greenshot.maui
Authority=Apple Development: kanri462afterfit@icloud.com (F285Z2WHPA)
TeamIdentifier=99T25R3367
```

That is the stable-signing state you want for local TCC validation.

Reset Screen Recording approval for the current bundle id:

```bash
tccutil reset ScreenCapture org.greenshot.maui
```

## Remembered local launch method

For this repo, the permission-safe launch flow to remember is:

```bash
scripts/maui-maccatalyst-dev.sh build
scripts/maui-maccatalyst-dev.sh open
```

If Screen Recording gets stuck again, use:

```bash
scripts/maui-maccatalyst-dev.sh recover-screen-recording
```

That keeps reopening the same signed `.app` instead of rebuilding it with `dotnet run`.
