# Warehouse Simulator — Codex/agent rules

## Source of truth (read in order)
1. docs/spec/00_MASTER_INDEX.md — CURRENT master specification, read before any code changes.
2. docs/spec/01_REFERENCE_PARITY.md and 14_GAPS_AND_EVIDENCE.md — reference scope and blocked/unknown behaviors.
3. docs/spec/02..12 and 15_DOMAIN_CONTRACTS_AND_FIXTURES.md — feature contracts and quality tests.
4. docs/spec/13_CODEX_EXECUTION.md — implementation plan and acceptance policy.
5. docs/spec/16_2025_REFERENCE_NUMBERS.md — legacy reference numbers only, NOT current verified values.
6. docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md and older research docs — historical context only where not overridden.
Never infer that unresolved values are approved. When source evidence conflicts, identify the version and consult the gaps list.

## Must preserve
- Product: NATIVE iOS/Android 3D warehouse simulation. Unity/URP is the current candidate.
- Preserve Parcel Simulator's manual parcel inspection/approval/denial, cargo type exports, weekday restrictions, storage, conveyors, scanners, stickers, licenses, Warehouse Tiers, shops and expansion.
- Replace only navigation UI: NO free walking character, virtual walking joystick, WASD, or time-consuming player movement.
- Menu/equipment tap instantly switches station camera, with 3D inspection and warehouse graphics (not a 2D-only business dashboard).
- A cargo category, an export gate, a shipment and a truck run are SEPARATE logical entities.
- Truck incoming/unloading/outgoing should be visible and tied to game time, not spawned randomly per camera view.
- Manual pickup still involves a real parcel ID and transfer of ownership. Do not teleport items without an explicit player transfer action or auto conveyor route.
- Green ghost-preview grid-based placement with rotation and flip is crucial to the game.
- High visual fidelity using independently authored textures, meshes and UI — do not copy actual Parcel Simulator art/code or copyrighted assets.

## Legacy and validation
- Assets/Scripts/Phase1 contains superseded walking prototype code; do not treat it as Phase 1 success.
- Build in vertical slices: station navigation/3D → manual intake/inspect/export → automation → logistics and expansion.
- Each feature must have states, failure cases, and acceptance tests before full implementation.
- Target stable 30fps; optional 60fps after REAL phone tests. No unverified performance/thermal claims.
- Separate simulation data from 3D rendering. Avoid continuous rigidbody simulation for large volumes of parcels.
- Verify with Unity and actual iPhone/Android where possible; report untested parts clearly.
- A proposed cost/capacity/tier rule not verified in the current reference must be labeled provisional.
- Never turn the game into a menu-only instant auto-clicker just to remove player movement.

## Project scope
Complete the design review first. Implement only an explicitly approved milestone. No destructive edits without review. Save source evidence/QA results alongside code. The native phone build and thermal verification are not yet done.
