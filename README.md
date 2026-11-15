# Mars Outpost

A 2.5D educational Mars habitat & mission management game built in **Unity 6000.3.28f1** with URP and Input System. The default **Outpost Operations** mode runs continuously with independent random hazards, repairable equipment, water extraction, spendable science, and seven kinds of Earth orders. **Training** retains the fixed two-sol mission; **Demo** compresses training into 90 seconds.

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

## Operations V2

- Times below are **simulation minutes**: 1x approximates wall time, pause stops them, night sleep accelerates them. Each channel has an independent seeded random stream. A new wait begins after an event ends or a fault is repaired.
- Radiation: wait 15–20 min; 60s warning; active for 1–2 min. Keep exterior crew inside. The risk meter is an acute educational index, not biological dose.
- Dust storm: wait 10–15 min; lasts 1–3 min; solar output falls by 80%. It can overlap radiation and equipment faults.
- One of three solar units fails after 20–30 min: select a rested astronaut and use **Repair Panel** on the right systems card (60s outside). Cleaning and fault repair are separate tasks.
- Oxygen capacity falls to 50% after 20–30 min: **O2 Repair** takes 40s and 20 energy in total. An outside astronaut must return before starting indoor work.
- Water extraction falls to 50% after 20–25 min: **Repair Water Extractor** spends exactly 10 science. Extraction is separate from recycling and can be switched off on the systems card.
- Research earns a spendable science balance as well as cumulative research. Earth orders award zero science. A bounded daily meal reserve supports long operations.
- **Earth Orders** arrive at the start of Sol 3, 5, 7, etc. Accept an order, select an astronaut, then assign its current stage. After exterior work use **Return to Shelter**, then assign the indoor stage. All seven kinds are drawn before the bag repeats. Occupied labs/life-support stations/galley cannot host a second job.
- **Save / Load** use `operations_save.json` in Unity's persistent data directory. Loading restores the clock, configuration, PRNG states, hazards, equipment, orders and Sol reports, then opens paused. No offline progress occurs.
- Engineering plan and current evidence: [`docs/operations-v2/PROGRESS.md`](docs/operations-v2/PROGRESS.md), [`docs/operations-v2/REVIEW.md`](docs/operations-v2/REVIEW.md).

## Planned Colony Expansion V3

Parcel claims, player-built solar panels/batteries/extractors, exploration,
additional shelter and a rover are **planned, not implemented**. The actionable
33-checkpoint plan and Hydrogen handoff are in [`docs/expansion-v3/PLAN.md`](docs/expansion-v3/PLAN.md),
[`PROGRESS.md`](docs/expansion-v3/PROGRESS.md), and [`START_PROMPT.md`](docs/expansion-v3/START_PROMPT.md).
The older FUTURE_ROADMAP remains a historical proposal.

## Controls & Gameplay

- **Settings:** Click **Ayarlar / Settings** or press **Escape** for volume/mute, graphics quality, resolution and fullscreen. **Apply** saves preferences; **Cancel** discards the draft. The menu pauses the mission and preserves an existing pause. The default High preset renders at full resolution with 4x MSAA.
- **Training mission balance:** Research now consumes meaningful power and crew effort. Rest/crew rotation or prepared night sleep are alternative tested ways to reach the target; assigning one researcher and waiting is insufficient.


- **Select Astronaut:** Click an astronaut card (left panel), click an astronaut sprite in the world view, or press keys **`1`**, **`2`**, **`3`**.
- **Pause / Resume:** **`Space`** or click **`Pause` / `Resume`**. Losing window focus pauses cleanly.
- **Routine Tasks (Bottom Dock):**
  - `Panel Maint. (Out)` — exterior solar panel cleaning (reduces panel fouling)
  - `System Maint. (In)` — interior life-support servicing (reduces system wear)
  - `Prep Meal (In)` — 30s reserve meal preparation (`-0.10/s` stamina); keeps crew night sleep recovery at `1.0x` instead of `0.75x` (once per sol, 1 ration consumed per night)
  - `Research Lab (In, Cont.)` — advances mission research proportional to remaining power when crew is assigned
- **Crew Safety & Sleep:** `Rest Inside`, `Return (10s)` (recall outside crew inside before Solar Particle radiation hits), `Sleep Crew (8x)` (nighttime only, all crew inside; stops cleanly on critical low-O2 alert `<20` or at Sol boundary).
- **Earth Comms (`Ask Earth Advice (45s)`):** Single-use rule-based orbital advice with compressed 45s simulation delay explaining real distance-dependent Mars radio latency (`~3-22 min`).
- **Mode & Audio (Top Bar):** Cycle **Operations → 90s Demo → 2-Sol Training** (demo uses `2 x 45s` sols), **`Sound: ON / MUTED`**, **`Vol -` / `Vol +`**, and **`NASA Sources`**.

## Automated Verification Commands

- **EditMode Tests (pure C# simulation + stepper):**
  ```bash
  /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics \
    -projectPath /run/media/senesatka/storage/projects/marsgame \
    -runTests -testPlatform EditMode \
    -testResults docs/validation/v2-review-editmode.xml
  ```
- **PlayMode Tests (full scene + HUD + audio + 90s demo + restart):**
  ```bash
  /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics \
    -projectPath /run/media/senesatka/storage/projects/marsgame \
    -runTests -testPlatform PlayMode \
    -testResults docs/validation/v2-review-playmode.xml
  ```

## NASA Open Data & Educational Transparency

- See [`docs/SCIENCE_SOURCES.md`](docs/SCIENCE_SOURCES.md) and the in-game **`NASA Sources`** modal.
- Verified NASA DONKI SEP archive entries used for Training hazard windows (Operations random timing is a game design choice):
  - `2024-05-09T13:59:00-SEP-001` (ACE/EPAM proton event, linked flare `2024-05-09T08:45:00-FLR-001`)
  - `2024-05-10T13:35:00-SEP-001` (GOES-P SEISS `>10 MeV`, linked CME `2024-05-08T05:36:00-CME-001`)
- Plus NASA ECLSS (life-support power/water/O2 coupling) and 2018 Mars Curiosity/Opportunity dust-storm solar context.
- **Simulation vs. Real Data:** All in-game risk scores (`0–100`), 720s / 45s compressed sols, 45s comms delay, and resource rates are **designed educational gameplay models**, not real Mars radiation dosimetry or flight-certified spacecraft telemetry.

## Asset & License Status

- **External Paid/Third-Party Assets:** None. Seven custom project-original 2D PNG sprites in [`Assets/MarsOutpost/Art/`](Assets/MarsOutpost/Art/) (`mars_terrain.png`, `habitat_shelter.png`, `station_lifesupport.png`, `station_galley.png`, `station_lab.png`, `solar_array.png`, `astronaut_suit.png`) are wired into [`MarsOutpostWorldView.cs`](Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs) (with procedural fallback), and all HUD panels, C# simulation, and runtime `AudioClip` effects ([`MarsOutpostAudio.cs`](Assets/MarsOutpost/Runtime/Presentation/MarsOutpostAudio.cs)) are original project artifacts built on standard Unity Engine packages.
- **Repository License:** Open-source repository license (e.g., MIT or Apache-2.0) is **pending user selection** before public submission; a `LICENSE` file has not yet been chosen/committed.

## Known Limitations & Remaining Open Items

1. **WebGL Module Missing Locally:** Installed Unity `6000.3.28f1` on this machine includes Linux, Windows, and Android PlaybackEngines, **not WebGL**. Desktop Linux output is `Build/Linux/MarsOutpost.x86_64`; current verification is recorded in `docs/RELEASE_NOTES.md`; WebGL build requires installing Unity's WebGL Build Support module.
2. **Live Student/Kid Playtest Pending:** Automated PlayMode & EditMode tests and scripted screenshot verification ran cleanly, while real live observation with target young players (`docs/PLAYTEST.md`) remains recommended open work.
3. **Demo Video Not Rendered:** `docs/DEMO_SCRIPT.md` contains the complete 7-slide deck outline and 30-second video script; recording/hosting the video itself is left to the user prior to competition submission.
