# Greenshot MacCatalyst Migration

## What This Is

This is a brownfield migration track inside the Greenshot repository that is rebuilding a Mac-friendly screenshot workflow on top of .NET MAUI and MacCatalyst. The goal is not to replace the mature Windows product in one pass, but to carve out stable cross-platform seams for capture, permissions, preview, and eventually editor handoff.

## Core Value

A Mac build can capture the screen reliably enough to start a real Greenshot workflow without surprising the user with broken permissions, broken selection math, or untestable UI logic.

## Requirements

### Validated

- ✓ The existing Windows Greenshot codebase remains the behavioral reference for capture, tray, hotkeys, clipboard, and editor flows.
- ✓ The MAUI shell can open screenshots from disk and render them in a preview surface.
- ✓ The MacCatalyst shell can capture the primary display, capture a selected window, and crop an in-app selected region into PNG cache artifacts.

### Active

- [ ] Stabilize macOS screen-recording permissions for local development with a stable signing identity.
- [ ] Continue expanding automated coverage around capture seams and regression-prone selection logic.
- [ ] Port higher-level Greenshot workflows such as desktop overlay capture, tray/menu bar triggers, clipboard handoff, and editor handoff.

### Out of Scope

- Full WinForms editor parity in the current MAUI slice — this is a separate migration track with much larger UI surface area.
- Non-macOS platform parity for the MAUI host in this milestone — the current focus is MacCatalyst capture viability.
- Reusing Windows-native capture internals unchanged — those APIs and permission models do not translate cleanly to macOS.

## Context

The repository is an established Windows-first Greenshot codebase with a new MAUI host being introduced in parallel. The MacCatalyst path already uses ScreenCaptureKit and CoreGraphics permission checks, and it now supports primary-display capture, window capture, and in-app region cropping. Local validation is still limited by ad hoc signing, which makes TCC-backed Screen Recording approval unreliable across rebuilds. The most regression-prone logic so far is the region-selection projection from aspect-fit preview coordinates into source-image pixel bounds, so that logic has been extracted into core code and covered with unit tests.

## Constraints

- **Tech stack**: The Mac host is .NET 10 MAUI + MacCatalyst — capture must fit Apple platform APIs rather than legacy Windows capture helpers.
- **Compatibility**: Local capture validation depends on Xcode and a stable signing identity — ad hoc signatures break TCC tracking for Screen Recording.
- **Brownfield**: Changes must coexist with the large Windows codebase — migration seams need to stay narrow and explicit.
- **Quality**: Core capture math must be testable outside UI code — otherwise regressions will hide in `MainPage`.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use ScreenCaptureKit for Mac capture | Apple-native display capture is the correct permission and API path on macOS | ✓ Good |
| Start region capture with in-app preview selection | It unlocks usable region capture without waiting for a desktop overlay implementation | ✓ Good |
| Move selection projection math into `Greenshot.Maui.Core` | The coordinate transform is deterministic and high-risk, so it should be unit tested away from MAUI UI code | ✓ Good |
| Add window capture before desktop overlay | `SCShareableContent` window targeting is much lower risk than building a full-screen overlay first | ✓ Good |
| Treat stable signing as a roadmap item | TCC reliability is a product risk, not just an environment quirk | — Pending |

---
*Last updated: 2026-03-23 after window-capture implementation and capture-seam tests*
