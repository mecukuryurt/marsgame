# Top bar checkpoint

The header now shows pause, Build and Systems in Operations, Tools, and Settings. Camera reset, dome, mission mode and language are in the Settings scroll; mode switching warns that it resets the mission and keeps the replacement mission paused until Settings closes. Grid is in Build; Tools contains sources, save, load and Earth orders. Legacy audio header controls are hidden; the existing Settings mixer remains available. Existing action IDs were retained and moved controls survive Settings recreation.

PlayMode: 36/36 passed (`docs/validation/hackathon-topbar-playmode.xml`) before the subsequent mode-switch pause fix and capture-only hook; focused Settings tests after that fix passed 7/7 (`docs/validation/hackathon-topbar-settings-tests.xml`). A prior 33/36 run exposed panel-transition and settings-lifecycle assumptions and was corrected. Fresh Linux player build and captures are the remaining verification step.

Final Unity Linux build exited 0 at `Build/Hackathon-topbar/MarsOutpost.x86_64`. Actual 1440×900 player captures `docs/validation/topbar-normal-1440.png` and `docs/validation/topbar-utility-1440.png` were inspected. The built `MarsOutpost.Presentation.dll` SHA-256 is `709b6afb549d5c7ef82184c9cdff78f19f3aeceff2c139f67f908f69a10d24c0`.
