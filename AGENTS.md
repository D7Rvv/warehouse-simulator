# Warehouse Simulator — Codex/agent rules

## Source of truth (read in order)
1. docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md — CURRENT game design, review draft.
2. docs/PARCEL_SIMULATOR_RESEARCH_2026-10-26.md — grounded research, three user screenshots, uncertainties, 2026 updates.
3. docs/PARCEL_FEATURE_MATRIX_V0_1.md — earlier checklist; use only with the more recent research.
Previous GDD v0.1/v0.2 are superseded for conflicting matters. Never infer that unresolved design numbers are approved.

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
Do not rework everything automatically just because research docs were updated. Use the user's next explicit implementation milestone. Avoid destructive deletes without review.
