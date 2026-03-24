# MacCatalyst Capture Slice

## Current scope

The MAUI shell now has a platform capture seam and a first Mac-native implementation:

- `IScreenshotCaptureService` lives in `Greenshot.Maui.Core`
- `MacCatalystScreenshotCaptureService` lives under `Platforms/MacCatalyst/Services`
- the current UI can capture the primary display, capture a selected window, and crop a user-selected region into PNG files in the app cache directory
- the current UI can hand the active PNG off to the platform's default image editor from inside the workspace

## Native path

The MacCatalyst implementation combines two native layers:

- `CGPreflightScreenCaptureAccess` / `CGRequestScreenCaptureAccess` from CoreGraphics for screen-recording permission checks
- `ScreenCaptureKit` for display enumeration and screenshot capture
- `CGImage.WithImageInRect` for cropping the cached full-screen image after the user drags a region inside the MAUI preview

This keeps the current slice small while using Apple-native APIs instead of trying to reuse the legacy Windows capture pipeline.

## Deliberate limits

This implementation only covers:

- primary-display capture
- window-targeted capture through `SCShareableContent` and an in-app action sheet picker
- in-app region selection against a cached primary-display capture
- cursor-inclusive screenshots
- file-backed preview inside the MAUI shell

It does not yet cover:

- desktop-wide overlay selection outside the app window
- global hotkeys
- tray or menu-bar triggers
- clipboard image export
- a native Greenshot-style annotation surface inside the MAUI host

## Local development note

On macOS, Screen Recording approval is tracked against the app's code signature requirements, not just its bundle identifier. In practice that means ad hoc-signed debug builds are unreliable for TCC-backed capture tests: a rebuild changes the code hash and macOS may treat the new app bundle as a different binary even when `CFBundleIdentifier` stays at `org.greenshot.maui`.

For repeatable MacCatalyst capture testing, build with a stable signing identity instead of an ad hoc local signature.

See `docs/maccatalyst-signing.md` for the concrete local setup, environment variables, and diagnostic commands.

## Next follow-ups

The next Mac capture milestones should be:

1. replace the in-app selection rectangle with a Greenshot-style desktop overlay
2. wire screenshot output into clipboard workflows
3. move capture invocation behind tray and hotkey services
4. replace editor handoff with a native MAUI annotation surface
