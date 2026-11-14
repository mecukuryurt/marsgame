# Mars Outpost — Competition Demo & Pitch Plan (T12)

> **Honest Status Note:** This document provides both official **NASA Space Apps Challenge** options — a **7-slide deck outline** and a **30-second demo video script**. No slide PDF or `.mp4` video has been generated or uploaded; submission, external hosting, and publishing are outside automation scope.

## Official Submission Format Checklist (Verified Against `spaceappschallenge.org`)

Before uploading to the Space Apps dashboard, double-check the current season's Challenge Submission Guide on `spaceappschallenge.org`:
- Choose **one** Project Demo option:
  - **7-Slide Deck (Max 7 slides):** All text in English, exported as PDF or publicly viewable link.
  - **30-Second Video (Max 30 seconds):** Hosted externally (YouTube, Google Drive, Dropbox, GitHub, etc.) as a public link; English subtitles required if narration is not English.
- Confirm repository visibility and open-source **LICENSE** file choice (MIT / Apache-2.0 / etc.).
- Include clear NASA data attribution (exact DONKI IDs & URLs listed in `docs/SCIENCE_SOURCES.md`).

---

## Option A: 7-Slide Pitch Outline (English)

### Slide 1 — Title & Hook
- **Title:** **Mars Outpost — Train Like a Mars Habitat Flight Controller**
- **Tagline:** An interactive 2D mission trainer where young explorers protect astronauts from solar storms, dust attenuation, and life-support crunches.
- **Visual:** Full gameplay screenshot (`docs/validation/screenshot-1920x1080.png`) showing habitat, solar array, and live resource HUD.

### Slide 2 — The Problem
- Space missions aren't just about rockets — crews survive on tight closed-loop power, water, oxygen, rest, and radiation discipline.
- Younger learners rarely get to *feel* why turning off a science lab to save oxygen, or recalling EVA crew 10 seconds before a Solar Particle Event, matters in real time.

### Slide 3 — Our Solution: Mars Outpost
- Single-screen 2D Mars base simulator built in **Unity 6000.3.28f1**.
- Players command 3 astronauts across **Day & Night Sols** (or a **90-second compressed demo mode**):
  - Clean fouled exterior panels, service interior life support, prep daily reserve meals (avoiding `0.75x` unprepped sleep penalty), run research, and sleep safely inside at 8x speed.

### Slide 4 — Real NASA Open Data Meets Transparent Modeling
- **Hazard Windows Backed by Real NASA DONKI SEP Records:**
  - `2024-05-09T13:59:00-SEP-001` (ACE/EPAM, linked flare `2024-05-09T08:45:00-FLR-001`)
  - `2024-05-10T13:35:00-SEP-001` (GOES-P SEISS `>10 MeV`, linked CME `2024-05-08T05:36:00-CME-001`)
- **Life Support & Dust Context:** NASA ECLSS water/O2 coupling + 2018 Mars dust-storm solar loss context.
- **Honest Science Boundary:** Game risk scores (`0-100`), 45s compressed Earth-Mars radio delay (`~3-22 min` real one-way light time), and 720s sols are clearly labeled inside the **NASA Sources** modal as **educational training models**, never misrepresented as Mars dosimetry.

### Slide 5 — Critical Thinking Under Delayed Earth Comms
- Players can send **one** request to Earth Mission Control (`Ask Earth Advice`), with a compressed **45-second** signal delay.
- When Earth's rule-based reply finally arrives — even referencing already-passed events with exact Sol timestamps — kids discover why astronauts must make local safety decisions rather than waiting for Earth!

### Slide 6 — Engineering Rigor & Accessibility
- **Pure C# Deterministic Simulation Core** separated cleanly from Unity URP presentation (`47+ EditMode` unit tests + `PlayMode` end-to-end integration tests).
- **Audio & Contrast:** Self-contained procedural Web/Desktop SFX (`MarsOutpostAudio.cs`) with Mute/Volume controls; every green/amber/red status bar pairs color with clear text and interactive 3-step onboarding.

### Slide 7 — Impact & Next Steps
- **Impact:** Bridges STEM space weather concepts with cause-and-effect resource trade-offs for classrooms and Space Apps Junior/Student audiences.
- **Next Steps:** Install Unity WebGL module for instant browser play, add localization beyond centralized English strings (`OutpostStrings.cs`), and conduct live student playtests (`docs/PLAYTEST.md`).

---

## Option B: 30-Second Demo Video Script

- **0:00 – 0:05 (Hook & Screen Intro):**
  - *Visual:* Switch to `90s Demo` mode, show Mars Habitat & crew status cards.
  - *Voiceover / Subtitle:* "What happens when a solar storm hits Mars and Earth radio is too slow to save you? Meet **Mars Outpost**."
- **0:05 – 0:12 (Core Decisions & Power Trade-off):**
  - *Visual:* Click Astronaut 1 (`1`) -> assign `Panel Maint.`; click Astronaut 2 -> `Prep Meal` & `Research Lab`. Show Solar % and Stamina moving.
  - *Voiceover / Subtitle:* "Manage three astronauts across day and night: clean dusty panels, prepare reserve meals for full night recovery, and balance oxygen before power runs dry."
- **0:12 – 0:20 (NASA Event Warning & 45s Delayed Earth Advice):**
  - *Visual:* Radiation warning turns Hazard Card amber; click `Ask Earth Advice`; recall Astronaut 1 via `Return (10s)` safely inside the habitat shield as red storm begins.
  - *Voiceover / Subtitle:* "Hazard windows trace to real May 2024 NASA DONKI Solar Particle Events. With simulated Earth radio delay, your crew learns to shelter locally before Earth's reply arrives."
- **0:20 – 0:26 (Night Sleep, Sol Report & NASA Sources Modal):**
  - *Visual:* Enter `Sleep Crew (8x)`, clear Sol Report summary, open `NASA Sources` modal briefly showing verified IDs and educational disclaimer.
  - *Voiceover / Subtitle:* "End-of-sol reports explain *why* reserves rose or fell — while keeping real NASA data distinct from game design."
- **0:26 – 0:30 (Outcome & Call to Action):**
  - *Visual:* Two-decision `MISSION SUCCEEDED` breakdown screen + title banner.
  - *Voiceover / Subtitle:* "**Mars Outpost**: built in Unity with open NASA data, deterministic tests, and zero third-party paid assets."
