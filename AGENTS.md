# Developer / Codex instructions — Warehouse Simulator

## Source of truth
1. Read `docs/spec/00_MASTER_INDEX.md` first (project constraints, status tags, requirements, phase gates).
2. Read `docs/spec/01_REFERENCE_PARITY.md` (feature trace, evidence and date).
3. Read `docs/spec/17_2026_REFERENCE_VERIFICATION_AUDIT.md` (SPEC-17B) and `docs/spec/18_EXPORT_REVENUE_RULE_RECONCILIATION.md` (SPEC-18) FIRST for current reference mechanics and partial-shipment corrections.
4. Read `docs/spec/15_DOMAIN_CONTRACTS_AND_FIXTURES.md` (SPEC-15) plus `docs/spec/15_DOMAIN_CONTRACTS.md` (SPEC-15B) for domain model and invariants; read `docs/spec/17_CROSS_SPEC_REVIEW.md` (SPEC-17) for technical contradictions.
5. Read the per-feature specification files and `docs/spec/16_2025_REFERENCE_NUMBERS.md` (SPEC-16 historical prices) vs `docs/spec/16_ECONOMY_EXPORT_DEEP_RESEARCH.md` (SPEC-16B pricing/contract/bill research).
6. `docs/spec/11_QA_ACCEPTANCE.md` defines regression and sign-off.
7. `docs/spec/13_CODEX_EXECUTION.md` defines work units and reporting format.
8. Old `docs/GDD_V0_2_STATION_BASED.md` and `docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md` are superseded for conflicting details.

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
- **Important correction (SPEC-18):** A July 21, 2026 Regional Warehouse player describes manual export at 15/16 parcels and developer replies on the same thread. The September 25 'Clear Doors' in development is an end-of-day bulk action, NOT proof that individual manual partial submissions are unavailable. Keep ManualSubmit / AutoWhenFull / EndOfDayClearAll separate and do not enforce FULL_ONLY globally.
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



## New evidence, 2026-10-26
- **Read docs/spec/16_ECONOMY_EXPORT_DEEP_RESEARCH.md (SPEC-16B) together with SPEC-17B and SPEC-18 before working on prices, licenses, warehouse tiers, exports, contracts, income, and calendar rules.**
- Parcel Simulator appears to contain **special contract fulfillment via a dedicated van** and **recurring Bills** according to Steam firsthand player reports; prior exclusion as "not in the reference" was incorrect.
- Known historical numbers include 14 original Tier prices, four license prices/per-box bonuses, parcel-type unlocks, equipment costs, batch and delivery upgrades. All require source/date tags and current-version reconciliation.
- Screenshot IMG_0181 has a past batch of 16 correctly submitted parcels, +$1,008 correct parcel value, -$5 handling charge, +$1,003 net, while the gate itself currently reads 1/16. **Never multiply summary 16 by active gate 1/16** or assume universal unit value.
- Do NOT fabricate all weekday flags for Air/Maritime. Community guide specifically supports Road daily and Rail not on weekends. Country Calendar resets Monday in second warehouse per official news.
- Contracts, dedicated contract van, bill generation, and submit-day eligibility should be included in domain/test planning. Unknown amounts/formulas remain [U], with pluggable test-only [P] values.


## Unique document identifiers and latest 2026 correction
- **There are 22 Markdown specification files**, not 15/16/17/19. Files with equal numeric prefixes are distinct; use full paths and unique IDs. Canonical: `SPEC-15` = 15_DOMAIN_CONTRACTS_AND_FIXTURES, `SPEC-15B` = 15_DOMAIN_CONTRACTS; `SPEC-16` = 16_2025_REFERENCE_NUMBERS, `SPEC-16B` = 16_ECONOMY_EXPORT_DEEP_RESEARCH; `SPEC-17` = 17_CROSS_SPEC_REVIEW, `SPEC-17B` = 17_2026_REFERENCE_VERIFICATION_AUDIT; `SPEC-18` = 18_EXPORT_REVENUE_RULE_RECONCILIATION.
- `docs/data/reference_catalog_2026-10-26.json` is a **research catalog with source status and nulls**. It is **not production-approved economy settings**; do not blindly import without validation.
- 2026 developer-thread report shows **regional gate 15/16 can be manually submitted**. An end-of-day bulk Clear Doors button is a separate in-development feature (as of Sep 25). Resolve any older FULL_ONLY contradictory statements in favor of SPEC-18.
- Regional Sorting Centre purchase ~$500,000 is supported by achievement guide and player statements; Tier 18 unlocks 12 country gates (player report). Source statuses must be retained.
- 2025 automatic exports can submit an incorrectly timed full gate: **AutoWhenFull is not an intelligent weekday limiter**. Do not auto-correct player mistakes.
- Early player reports disagree about pre-stickered parcels sometimes being wrong due to bugs. Do not create random intentional invalid pre-stickers without an explicit evidence-backed rule. Test scanner/inventory race conditions separately.


## Implementation milestone S0/S1 (2026-10-26; next Codex assignment)
- **Work ticket:** https://github.com/D7Rvv/warehouse-simulator/issues/1
- **Read before implementing:** `docs/tasks/CODEX_NEXT_S0_S1.md`, `docs/spec/19_DECISION_REGISTER_AND_SAFE_DEFAULTS.md`, `docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md`, `docs/data/prototype_station_fixture_v0_1.json`.
- User accepts *safe provisional design decisions* for UX/architecture/performance/mock fixtures, but **not fabricated game-reference revenue, weekdays, upgrades or Tier prices**. All unknown reference values remain tagged [U], mocks [P].
- S0/S1 only: create actual Unity LTS/URP project skeleton, disable the **legacy `RuntimeInitializeOnLoadMethod` free-walk `WarehouseBootstrap`**, add fixed camera anchors and mobile tap navigation, 3D warehouse/loading/inspection/export/build views and preview-only conveyor ghost.
- Do **not** implement live purchasing, automatic parcel transport, real export/ledger, or road truck schedules in this sprint. Only preview/demo UI and fixtures; explicitly label prototype values.
- Preserve the mobile-first high-fidelity goal while doing a blockout. Avoid excessive real-time physics, shadow overdraw or synchronous object churn on station switch.
- Acceptance: SPEC-20 S0-01..06 and S1-01..12 with actual PASS/FAIL/NOT_RUN, Unity build/phone hardware tested only when truly run.


## P0 USER QUALITY REJECTION — highest priority (2026-10-28)
**The user played/viewed the current build on iPhone and rated overall quality 0.5/10. Reasons: no recognizable resemblance to reference-game visual quality, essentially no gameplay, very poor 3D fidelity, no newcomer guidance. This is a genuine failed user acceptance gate.**
- Read **`docs/reviews/2026-10-28_USER_PLAYTEST_QUALITY_REJECTION.md`**, **`docs/spec/21_PLAYABLE_FIRST_SESSION_QUALITY_GATE.md`**, and **`docs/tasks/CODEX_NEXT_PLAYABLE_QUALITY_P0.md`** immediately. Tracking: https://github.com/D7Rvv/warehouse-simulator/issues/3
- Previous S0/S1 instructions only delivered a **technical blockout prototype**. They are not the next product acceptance criteria. Work on real parcel ownership/transfer, real manual cargo/country inspection and approve/deny stickers, correct/incorrect export and event-ledger, first-run Japanese guided task flow, and realistic original art of loading dock / inspection / export gate.
- Before calling anything a playable game, **a first-time user must see a clear goal and complete one real parcel flow**. Menu changes without gameplay-state changes do not count. Tutorial action completion must follow successful domain commands, not demo UI.
- Art acceptance requires actual Unity GameView screenshots/videos at 3 corresponding stations, reference-based comparison of material/lighting/3D geometry, original/licensed models. **Do not present cube-only primitives as final realistic 3D**. Do not simply promise Blender quality or a screenshot without producing matching Unity assets.
- Do not assert that iPhone fps/heat were measured: the phone preview method is not confirmed; Unity Remote is not native iPhone GPU evaluation.
- Maintain PC editor-low profile for i7-7700/16GB/Intel HD630 and separate mobile graphics profile for iPhone 13 Pro Max.
- **Do not merge/close PR #2 as a completed game.** It's a draft technical foundation; confirm Unity editor build/tests separately before promoting it. Implementation should use small PRs and update actual QA PASS/FAIL/NOT_RUN evidence.
- Keep free-walk disabled, license/economy/calendar unknown values separate, and all reference assets/art original.
