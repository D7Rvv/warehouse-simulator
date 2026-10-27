# Thermal integration boundary

The current Godot build has **no native temperature/thermal-state backend**. `MobileQuality.set_thermal_hint()` is a future adapter and deterministic test entry. Its snapshots always retain `thermal_sensor_available=false`, `hardware_thermal_observed=false`, and physical-phone acceptance `NOT_RUN`; low FPS never sets a thermal state.

A future signed iOS module must read `ProcessInfo.thermalState` and observe `thermalStateDidChangeNotification`; an Android module must use supported `PowerManager` thermal/ADPF APIs, with explicit unavailable handling. Such modules must be built against the exact exported Godot SDK, lifecycle-tested, and verified on the real phone before claiming sensor support. This directory contains no stub that pretends those modules ran.

Injected WARM/HOT hints cap render quality and frame rate; CRITICAL limits foreground rendering to 5 FPS and emits `safety_pause_requested` so the application can suspend heavy presentation and actions safely. Backgrounding additionally stops this controller and the render loop. A return to a noncritical hint restores the normal 30 FPS cap but keeps lowered pixels until sustained recovery. It does not manufacture or reset gameplay ownership, financial state, or the simulation clock. A real sensor implementation must preserve that boundary.
