# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-23)

**Core value:** A Mac build can capture the screen reliably enough to start a real Greenshot workflow without surprising the user with broken permissions, broken selection math, or untestable UI logic.
**Current focus:** Phase 2 blocked by signing validation; Phase 3 window capture started in parallel

## Current Position

Phase: 2 of 4 (Signing And Permission Stability), with Phase 3 in progress
Plan: 3 of 3 in current phase, plus 1 of 2 complete in Phase 3
Status: In progress
Last activity: 2026-03-23 — Isolated MAUI build settings, added canonical Mac development entrypoints, and refactored workspace state/capture helpers

Progress: [██████░░░░] 56%

## Performance Metrics

**Velocity:**
- Total plans completed: 2
- Total plans completed: 5
- Average duration: n/a (bootstrapped retroactively)
- Total execution time: n/a

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1 | 2 | n/a | n/a |
| 2 | 2 | n/a | n/a |
| 3 | 1 | n/a | n/a |

**Recent Trend:**
- Last 5 plans: bootstrap only
- Trend: Stable

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 1]: Use ScreenCaptureKit plus CoreGraphics permission checks for Mac capture.
- [Phase 1]: Keep region capture in-app first, then replace it with a desktop overlay later.
- [Phase 1]: Extract selection projection math into `Greenshot.Maui.Core` and test it directly.

### Pending Todos

None yet.

### Blockers/Concerns

- Stable code signing identity is still missing on this Mac, so TCC-backed Screen Recording validation is unreliable across rebuilds.
- Verification snapshot on 2026-03-23: `security find-identity -v -p codesigning` returned `0 valid identities found`, and the built `Greenshot.app` is still `Signature=adhoc`.

### Quick Tasks Completed

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-23 | Add unit tests for screenshot selection mapping and bootstrap GSD docs | Complete |
| 2026-03-23 | Add signing guidance, build config hooks, and capture failure tests | Complete |
| 2026-03-23 | Add Mac window capture with action-sheet targeting and label-builder tests | Complete |
| 2026-03-23 | Verify MAUI build boundary fixes and signing prerequisites | Blocked by missing Apple Development identity |
| 2026-03-23 | Add canonical MacCatalyst build/test entrypoints and split workspace/capture responsibilities | Complete |

## Session Continuity

Last session: 2026-03-23 15:35
Stopped at: Signed-capture validation still waits on an Apple Development identity; build/test entrypoints are now stable and the next remaining work is overlay/tray/clipboard feature delivery
Resume file: None
