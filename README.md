# Mars Outpost

A 2D educational Mars habitat & mission management game built in **Unity 6000.3.28f1** using the Universal Render Pipeline (URP) and Input System. Players manage a three-astronaut crew across Mars day/night cycles, keeping oxygen, battery, water recycling, crew stamina, reserve meal prep, and radiation/dust safety balanced across two sols — or a 90-second compressed demo mode.

## Project Setup & How to Run

- **Unity Editor Version:** `6000.3.28f1` (URP `17.3.0`, Input System `1.20.0`, uGUI `2.0.0`, Test Framework `1.6.0`)
- **Playable Scene:** [`Assets/MarsOutpost/Scenes/MarsOutpost.unity`](Assets/MarsOutpost/Scenes/MarsOutpost.unity) (first enabled scene in `ProjectSettings/EditorBuildSettings.asset`; original [`Assets/Scenes/SampleScene.unity`](Assets/Scenes/SampleScene.unity) preserved)
- **Standalone Linux Build Output:** `Build/Linux/MarsOutpost.x86_64`

### Rebuild Scene or Desktop Binary from Menu / CLI
- In Unity Editor: `MarsOutpost -> Build or Update Playable Scene` or `MarsOutpost -> Build Linux Desktop`
- Command line:
  ```bash
  /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -quit \
    -projectPath /run/media/senesatka/storage/projects/marsgame \
    -executeMethod MarsOutpost.Editor.MarsOutpostSceneBuilder.BuildLinuxDesktop
  ```

## Controls & Gameplay

- **Select Astronaut:** Click an astronaut card (left panel), click an astronaut sprite in the world view, or press keys **`1`**, **`2`**, **`3`**.
- **Pause / Resume:** **`Space`** or click **`Pause` / `Resume`**. Losing window focus pauses cleanly.
- **Tasks (Right Panel):**
  - `Panel Maint. (Out)` — exterior solar panel cleaning (reduces panel fouling)
  - `System Maint. (In)` — interior life-support servicing (reduces system wear)
  - `Prep Meal (In)` — 30s reserve meal preparation (`-0.10/s` stamina); keeps crew night sleep recovery at `1.0x` instead of `0.75x` (once per sol, 1 ration consumed per night)
  - `Research Lab (In, Cont.)` — advances mission research proportional to remaining power when crew is assigned
- **Crew Safety & Sleep:** `Rest Inside`, `Return (10s)` (recall outside crew inside before Solar Particle radiation hits), `Sleep Crew (8x)` (nighttime only, all crew inside; stops cleanly on critical low-O2 alert `<20` or at Sol boundary).
- **Earth Comms (`Ask Earth Advice (45s)`):** Single-use rule-based orbital advice with compressed 45s simulation delay explaining real distance-dependent Mars radio latency (`~3-22 min`).
- **Mode & Audio (Top Bar):** Toggle **`2-Sol Std`** (`1440s`) vs **`90s Demo`** (`2 x 45s` compressed demo using the exact same simulation clock), **`Sound: ON / MUTED`**, **`Vol -` / `Vol +`**, and **`NASA Sources`**.

## Automated Verification Commands

- **EditMode Tests (pure C# simulation + stepper):**
  ```bash
  /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics \
    -projectPath /run/media/senesatka/storage/projects/marsgame \
    -runTests -testPlatform EditMode \
    -testResults docs/validation/t11-editmode-results.xml
  ```
- **PlayMode Tests (full scene + HUD + audio + 90s demo + restart):**
  ```bash
  /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics \
    -projectPath /run/media/senesatka/storage/projects/marsgame \
    -runTests -testPlatform PlayMode \
    -testResults docs/validation/t11-playmode-results.xml
  ```

## NASA Open Data & Educational Transparency

- See [`docs/SCIENCE_SOURCES.md`](docs/SCIENCE_SOURCES.md) and the in-game **`NASA Sources`** modal.
- Verified NASA DONKI SEP archive entries used for hazard windows:
  - `2024-05-09T13:59:00-SEP-001` (ACE/EPAM proton event, linked flare `2024-05-09T08:45:00-FLR-001`)
  - `2024-05-10T13:35:00-SEP-001` (GOES-P SEISS `>10 MeV`, linked CME `2024-05-08T05:36:00-CME-001`)
- Plus NASA ECLSS (life-support power/water/O2 coupling) and 2018 Mars Curiosity/Opportunity dust-storm solar context.
- **Simulation vs. Real Data:** All in-game risk scores (`0–100`), 720s / 45s compressed sols, 45s comms delay, and resource rates are **designed educational gameplay models**, not real Mars radiation dosimetry or flight-certified spacecraft telemetry.

## Asset & License Status

- **External Paid/Third-Party Assets:** None. Seven custom project-original 2D PNG sprites in [`Assets/MarsOutpost/Art/`](Assets/MarsOutpost/Art/) (`mars_terrain.png`, `habitat_shelter.png`, `station_lifesupport.png`, `station_galley.png`, `station_lab.png`, `solar_array.png`, `astronaut_suit.png`) are wired into [`MarsOutpostWorldView.cs`](Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs) (with procedural fallback), and all HUD panels, C# simulation, and runtime `AudioClip` effects ([`MarsOutpostAudio.cs`](Assets/MarsOutpost/Runtime/Presentation/MarsOutpostAudio.cs)) are original project artifacts built on standard Unity Engine packages.
- **Repository License:** Open-source repository license (e.g., MIT or Apache-2.0) is **pending user selection** before public submission; a `LICENSE` file has not yet been chosen/committed.

## Known Limitations & Remaining Open Items

1. **WebGL Module Missing Locally:** Installed Unity `6000.3.28f1` on this machine includes Linux, Windows, and Android PlaybackEngines, **not WebGL**. Desktop Linux (`Build/Linux/MarsOutpost.x86_64`) is built and visually verified; WebGL build requires installing Unity's WebGL Build Support module.
2. **Live Student/Kid Playtest Pending:** Automated PlayMode & EditMode tests and scripted screenshot verification ran cleanly, while real live observation with target young players (`docs/PLAYTEST.md`) remains recommended open work.
3. **Demo Video Not Rendered:** `docs/DEMO_SCRIPT.md` contains the complete 7-slide deck outline and 30-second video script; recording/hosting the video itself is left to the user prior to competition submission.
