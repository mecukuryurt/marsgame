# Mars Outpost — English Project Description (Space Apps Submission Draft)

## High-Level Summary
**Mars Outpost** is a 2D educational Mars habitat management game built in **Unity 6000.3.28f1**. Acting as a junior habitat commander, the player guides a three-astronaut crew through Mars day/night cycles (full two-sol mode or a 90-second live demo mode), balancing closed-loop oxygen, water recycling, solar battery reserves, crew stamina, daily reserve meal preparation, and radiation/dust hazards inspired by real NASA open data.

## How It Works
- **Deterministic Life-Support & Power Loop:** Solar generation scales with panel fouling and atmospheric dust storms during daylight and drops to zero at night. Life support prioritizes crew oxygen and water recycling from battery/solar power, while optional continuous research consumes spare energy and pauses smoothly if reserves tighten.
- **Crew Safety & Stamina Discipline:** Each astronaut tracks location (`Inside` vs. `Outside`), stamina, and an educational training risk score (`0–100`). Exterior panel maintenance removes fouling faster but exposes astronauts to 8x higher risk during Solar Particle Events; recalling crew takes 10 simulation seconds, teaching proactive planning on hazard warnings.
- **Daily Galley & Night Recovery:** Preparing a 30-second reserve meal keeps nighttime sleep recovery at `1.0x` (`+0.40/s`), while skipping meal prep locks that night to `0.75x` (`+0.30/s`) — enforcing authentic crew logistics without extra clutter or hunger-death failure loops.
- **Compressed Earth-Mars Radio Delay:** Players have one rule-based `Ask Earth Advice` request per mission with a 45-second compressed simulation delay (explicitly noting real orbital one-way radio latency of ~3–22 minutes), rewarding autonomous local safety calls.
- **Clear Feedback & Audio:** Cohesive single-style 2D view (`Habitat`, `O2/Sys`, `Galley`, `Lab`, `Solar Array`), interactive 3-step guide, end-of-sol cause-and-effect reports, two-decision mission debriefs, and procedural runtime SFX (`Mute` + volume controls).

## NASA Data & Scientific Honesty
- Solar Particle Event timing & sequence in the default scenario trace directly to verified **NASA DONKI SEP** archive records from May 2024 (`2024-05-09T13:59:00-SEP-001` via ACE/EPAM and `2024-05-10T13:35:00-SEP-001` via GOES-P SEISS `>10 MeV`).
- Life-support coupling references **NASA ECLSS**, and dust storm solar reduction draws educational context from **NASA Mars Exploration Rovers (2018 global dust storm)**.
- All simulation timescales, resource rates, and training risk scores are **explicitly separated** in both documentation (`docs/SCIENCE_SOURCES.md`) and the in-game **`NASA Sources`** modal as designed educational game parameters rather than Mars surface radiation measurements.

## Technical Implementation & Current Limitations
- Pure C# simulation engine (`MarsOutpost.Simulation`) isolated from Unity presentation (`MarsOutpost.Presentation`), verified by automated Unity EditMode and PlayMode test suites plus Linux standalone builds tested at `1280x720` and `1920x1080`.
- **Open Limitations:** WebGL browser build is currently omitted because the local Unity `6000.3.28f1` installation does not have the WebGL Build Support module installed; live student playtesting (`docs/PLAYTEST.md`) and repository open-source license selection remain to be finalized before submission.
