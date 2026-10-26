# Development instructions for Warehouse Simulator

## Current source of truth
- Read `docs/GDD_V0_2_STATION_BASED.md` before changing gameplay.
- This file describes a REVIEW DRAFT, not approval of all unresolved game-design choices.
- iOS/Android smartphone game is the end product. Unity/URP is the current candidate.
- **Do not reintroduce free-roam character movement, WASD navigation, virtual walk joystick, or walking between stations.**
- The player taps a menu or equipment to switch instantly to a fixed station camera. Warehouse overhead is reserved for overview and building.
- Preserve inspection depth: labels, barcode, weight, opening/contraband, changing criteria, approval/denial, and automated expansion.
- Truck arrivals, loading, departures and game clock must stay logically consistent with the warehouse systems.
- All expensive visual representations should be separable from the simulation data.
- Prior Phase1 scripts in `Assets/Scripts/Phase1/` belong to the superseded walking prototype; treat them as legacy experiments.

## Engineering quality
- Mobile-first performance: baseline 30 FPS, optional 60 FPS for capable devices after measured validation.
- Minimize CPU/GPU work when switching stations; do not reload the entire warehouse scene for navigation.
- Optimize after measurement, preserve close-up visual quality, avoid excessive Rigidbody usage on many parcels.
- Do not claim Unity compilation, native builds, thermal safety, or verified frame rates without actually running those tests.
- Use independent IDs and state machines for parcels, conveyors, trucks and financial events; prevent duplicate shipment/revenue updates.
- Before implementation of a feature, define interactions, edge cases and test criteria.

## Scope gate
- Do not restart a full implementation solely because this document exists. Wait until design scope is explicitly confirmed.
