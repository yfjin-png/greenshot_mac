# Roadmap: Greenshot MacCatalyst Migration

## Overview

This roadmap turns the current MacCatalyst proof-of-concept into a trustworthy Greenshot migration track. The first step locks down the existing capture baseline and test seams, then the next phases focus on permission/signing stability, richer capture modes, and deeper integration with the broader Greenshot workflow.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Capture Baseline And Test Harness** - Freeze the current Mac capture slice into a documented, testable starting point.
- [ ] **Phase 2: Signing And Permission Stability** - Make local Screen Recording validation reliable on macOS.
- [ ] **Phase 3: Advanced Capture Modes** - Add window-targeted and overlay-driven capture flows.
- [ ] **Phase 4: Greenshot Workflow Integration** - Connect capture output to tray, clipboard, and editor workflows.

## Phase Details

### Phase 1: Capture Baseline And Test Harness
**Goal**: Extract regression-prone selection math from UI code, add unit coverage, and initialize GSD planning artifacts around the current brownfield state.
**Depends on**: Nothing (first phase)
**Requirements**: CAP-01, CAP-02, CAP-03, TEST-01, TEST-02, DEV-02
**Success Criteria** (what must be TRUE):
  1. Region-selection math lives in core code and no longer depends on `MainPage` internals.
  2. Unit tests cover aspect-fit projection, clamping, and minimum-size rejection.
  3. The repo has a usable `.planning/` baseline for future GSD workflows.
**Plans**: 2 plans

Plans:
- [x] 01-01: Extract screenshot-selection projection logic into a testable core helper.
- [x] 01-02: Add a net10 unit test project covering projection and crop-bound mapping.

### Phase 2: Signing And Permission Stability
**Goal**: Remove ad hoc signing from the local validation path and make Screen Recording permission behavior predictable across rebuilds.
**Depends on**: Phase 1
**Requirements**: CAP-04, TEST-03, DEV-01
**Success Criteria** (what must be TRUE):
  1. Local contributors can build a stably signed app for macOS capture testing.
  2. Permission failures are documented and surfaced clearly enough to diagnose quickly.
  3. Capture service seams are covered by tests or testable helpers where platform APIs allow it.
**Plans**: 3 plans

Plans:
- [x] 02-01: Wire stable signing guidance and project configuration into the MacCatalyst workflow.
- [x] 02-02: Expand capture-related verification around permission and failure states.
- [ ] 02-03: Validate the capture flow on a stably signed build once an Apple Development identity is available.

### Phase 3: Advanced Capture Modes
**Goal**: Move beyond full-screen capture by supporting richer selection and targeting flows on macOS.
**Depends on**: Phase 2
**Requirements**: ADV-01, ADV-02
**Success Criteria** (what must be TRUE):
  1. User can target an individual window when capturing.
  2. User can select a region from a desktop overlay instead of only inside the app preview.
  3. New capture modes reuse the existing core seams rather than reintroducing UI-only logic.
**Plans**: 2 plans

Plans:
- [x] 03-01: Add window-targeted capture on top of `SCShareableContent`.
- [ ] 03-02: Replace in-app region selection with a Greenshot-style overlay workflow.

### Phase 4: Greenshot Workflow Integration
**Goal**: Reconnect the MAUI capture host with the broader Greenshot experience expected by users.
**Depends on**: Phase 3
**Requirements**: INT-01, INT-02
**Success Criteria** (what must be TRUE):
  1. Capture can be launched from tray or menu-bar integration.
  2. Captured output can move into clipboard and editor workflows.
  3. The MAUI host behaves like a real Greenshot entry point instead of a standalone demo shell.
**Plans**: 2 plans

Plans:
- [ ] 04-01: Add tray or menu-bar capture triggers for the Mac host.
- [ ] 04-02: Wire clipboard and editor handoff from capture results.

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Capture Baseline And Test Harness | 2/2 | Complete | 2026-03-23 |
| 2. Signing And Permission Stability | 2/3 | In progress | - |
| 3. Advanced Capture Modes | 1/2 | In progress | - |
| 4. Greenshot Workflow Integration | 0/2 | Not started | - |
