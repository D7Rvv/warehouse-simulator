# Developer / Codex instructions — Warehouse Simulator

## Source of truth
1. Read `docs/spec/00_MASTER_INDEX.md` first (project constraints, status tags, requirements, phase gates).
2. Read `docs/spec/01_REFERENCE_PARITY.md` (feature trace, evidence and date).
3. Read `docs/spec/02_GAMEFLOW_PROGRESSION.md` through `docs/spec/15_DOMAIN_CONTRACTS.md` for the feature you're implementing; SPEC-15 resolves cross-system contract ambiguities.
4. `docs/spec/11_QA_ACCEPTANCE.md` defines regression and sign-off.
5. `docs/spec/13_CODEX_EXECUTION.md` defines work units and reporting format.
6. Old `docs/GDD_V0_2_STATION_BASED.md` and `docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md` are superseded for conflicting details.

## Product requirements
- **Native iOS/Android high-fidelity 3D warehouse game**; final product is not a browser-only prototype.
- Preserve Parcel Simulator's manual box inspections, approvals/rejections, cargo categories, weekday export rules, racks, pallets, shop inventory, license unlocks, warehouse tiers, grid-based ghost construction, conveyors, routing, scanners and progressive automation.
- **Never add free-walk characters, walking joysticks, WASD character locomotion, or navigation travel time.** Taps/menu select immediate 3D station camera changes.
- Keep direct 3D parcel work and construction. Avoid replacing gameplay with a 2D management button that automatically finishes all parcels.
- A parcel's transport owner, inspection truth, visible label, submitted batch, export gate, shipment and physical truck are distinct concepts. Prevent data duplication/loss.
- Logical trucks follow game-time schedules and remain consistent whether visible or not, including incoming/outgoing/road travel.
- High-quality realistic warehouse graphics must use original art/meshes/textures/UI/audio — not copied copyrighted reference-game assets/screenshots.
- High visual quality plus stable mobile 30 FPS, optional 60 where supported, plus thermal testing. **Never report verified FPS/low heating without actual representative phone tests.**

## Evidence and uncertainty
- The documentary requirements are tagged [R] reference-observed, [D] project-decision, [P] provisional implementation rule, [U] reference-unknown.
- [R] from 2025 may be historical. Values (prices, Tier unlocks, exact capacities, clock speed, export batch rules, payout) require current original-game checking before "exact match".
- **2026-09-25 official patch labels partial-gate Clear Doors as still being worked on; do not assume arbitrary partial shipments are available. Use source-cited FULL_ONLY provisional rule.**
- 2025-09-29 official Update 1.0.4 Launcher: adjustable power and angle separately; upgrade level changes belt speed, not launch distance.
- Add reproducible screenshots/videos/game version and source dates for reference-parity claims.
- Unverified reference mechanics must be source-tagged configuration values rather than fabricated constants.

## Engineering
- Simulation authority belongs to deterministic, testable domain data. 3D game objects are presentation; camera change cannot modify parcel owner/clock/money.
- Owned parcels are identified by unique IDs and have exactly one owner at a time.
- Export, item purchase and truck events must be idempotent; no double charges/rewards.
- Building ghost does not spend money or alter the actual conveyor graph until committed.
- Manual HandSlot transfer requires explicit player action; do not silently teleport boxes because camera changed.
- Moving equipment with cargo, full export gates and stalled conveyors must have safe error behavior and tests.
- All scanner/launcher/upgrades change deterministic data; animation/render frequency does not change logical results.
- Mobile-oriented LOD, static lighting, pool, cull hidden geometry and avoid continuous hundreds of Rigidbodies.
- Build/compile in Unity Editor when available and test native mobile. Report what was actually tested, never invent verified results.

## Legacy and scope
- `Assets/Scripts/Phase1/` is legacy **walking prototype code**, NOT current functionality. Do not allow runtime auto-bootstrap into new project scene; isolate safely without losing git history.
- Stage implementation M00..M14 in `docs/spec/13_CODEX_EXECUTION.md`; start with small vertical slice, test each and report.
- Prioritize fixing spec discrepancies over blindly expanding features; no broad destructive rewrite without assessing impact.
