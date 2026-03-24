# Requirements: Greenshot MacCatalyst Migration

**Defined:** 2026-03-23
**Core Value:** A Mac build can capture the screen reliably enough to start a real Greenshot workflow without surprising the user with broken permissions, broken selection math, or untestable UI logic.

## v1 Requirements

### Capture Foundation

- [x] **CAP-01**: User can load an existing screenshot file into the MAUI preview surface.
- [x] **CAP-02**: User can capture the primary display from the MacCatalyst shell into a PNG file.
- [x] **CAP-03**: User can drag a region over the previewed capture and save only that selected area.
- [ ] **CAP-04**: User receives actionable feedback when screen-recording permission or host-platform support blocks capture.

### Verification

- [x] **TEST-01**: Aspect-fit region-selection projection logic is covered by unit tests.
- [x] **TEST-02**: Selection clamping and minimum-size rejection are verified without requiring MAUI UI automation.
- [x] **TEST-03**: Additional capture seam behaviors can be regression-tested without touching platform UI code.

### Developer Workflow

- [x] **DEV-01**: Contributors can identify macOS signing and Screen Recording prerequisites from project planning docs.
- [x] **DEV-02**: The GSD framework is initialized for this repo with PROJECT, REQUIREMENTS, ROADMAP, STATE, and config artifacts.

## v2 Requirements

### Advanced Capture

- [x] **ADV-01**: User can capture a specific window instead of the entire primary display.
- **ADV-02**: User can select a region through a Greenshot-style desktop overlay instead of only inside the app window.

### Workflow Integration

- **INT-01**: User can trigger capture from tray or menu-bar integration.
- **INT-02**: User can hand captured output to clipboard and editor workflows from the MAUI host.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Full WinForms editor parity in MAUI | Too large for the current migration slice |
| Linux capture support | Current MAUI capture work is MacCatalyst-specific |
| Reusing Windows capture internals directly on macOS | API, permission, and shell models differ too much |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CAP-01 | Phase 1 | Complete |
| CAP-02 | Phase 1 | Complete |
| CAP-03 | Phase 1 | Complete |
| CAP-04 | Phase 2 | Pending |
| TEST-01 | Phase 1 | Complete |
| TEST-02 | Phase 1 | Complete |
| TEST-03 | Phase 2 | Complete |
| DEV-01 | Phase 2 | Complete |
| DEV-02 | Phase 1 | Complete |
| ADV-01 | Phase 3 | Complete |

**Coverage:**
- v1 requirements: 9 total
- Mapped to phases: 9
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-23*
*Last updated: 2026-03-23 after window capture landed on the MacCatalyst shell*
