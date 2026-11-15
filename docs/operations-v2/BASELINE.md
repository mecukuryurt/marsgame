# V2 Başlangıç Durumu (BASELINE)

Tarih: 2026-11-14 22:47 (+03:00)

## Checkout ve Ortam

- **Branch:** `master`
- **HEAD Commit:** `f56730c8bbbe20247c38961633a9a865a4480e29` (`fix(presentation): fix interior working pose, enable 4x MSAA, add regolith detail & 2D HUD icons`)
- **Unity Sürümü (`ProjectSettings/ProjectVersion.txt`):** `6000.3.28f1` (`8f082d2fe476`)
- **Unity Editor Yolu:** `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity`

## Korunacak Kullanıcı Değişiklikleri (`git status --short`)

### Değiştirilmiş (M — 20 dosya)
- `Assets/MarsOutpost/Editor/MarsOutpostSceneBuilder.cs`
- `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostAudio.cs`
- `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs`
- `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs`
- `Assets/MarsOutpost/Runtime/Presentation/SimulationRunner.cs`
- `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`
- `Assets/MarsOutpost/Scenes/MarsOutpost.unity`
- `Assets/MarsOutpost/Tests/EditMode/CrewAndTaskSimulationTests.cs`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `README.md`
- `docs/ANTIGRAVITY_PROMPT.md`
- `docs/FUTURE_ROADMAP.md`
- `docs/GAME_PLAN.md`
- `docs/IMPLEMENTATION_TASKS.md`
- `docs/MASTER_PLAN.md`
- `docs/SCIENCE_SOURCES.md`
- `docs/VALIDATION.md`
- `docs/iyilestirme-plani.md`

### Takip Edilmeyen (?? — yeni dosya/dizinler)
- `.vscode/`
- `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostSettingsMenu.cs` (+ `.meta`)
- `Assets/MarsOutpost/Tests/EditMode/DefaultMissionBalanceTests.cs` (+ `.meta`)
- `Assets/MarsOutpost/Tests/EditMode/RenderingConfigurationTests.cs` (+ `.meta`)
- `Assets/MarsOutpost/Tests/PlayMode/CrewMovementVisualTests.cs` (+ `.meta`)
- `Assets/MarsOutpost/Tests/PlayMode/SettingsMenuTests.cs` (+ `.meta`)
- `Assets/Settings/MarsRenderer3D.asset` (+ `.meta`)
- `docs/PROJECT_REVIEW.md`
- `docs/RELEASE_NOTES.md`
- `docs/operations-v2/`
- `docs/validation/current-editmode-results.xml`
- `docs/validation/current-playmode-results.xml`
- `docs/validation/game-1920x1080.png`
- `docs/validation/review-editmode-results.xml`
- `docs/validation/review-playmode-results.xml`
- `docs/validation/settings-1920x1080.png`

## Gerçek Başlangıç Test Sonuçları (Unity 6000.3.28f1)

- **EditMode:** **53 / 53 geçti** (0 failed, 0 skipped), çıkış kodu `0`.
  - Kanıt: `docs/validation/v2-cp00-editmode.xml`
- **PlayMode:** **10 / 11 geçti, 1 başarısız** (0 skipped), çıkış kodu `2`.
  - Kanıt: `docs/validation/v2-cp00-playmode.xml`
  - Başarısız test: `MarsOutpost.Tests.PlayMode.MarsOutpostScenePlayTests.AudioSourcesFoodCommsAndNinetySecondDemoCompleteEndToEnd` (`MarsOutpostScenePlayTests.cs:84`)
  - Hata: `Expected: less than 0.550000012f, But was: 0.683709025f` (`audio.Volume`, büyük olasılıkla `PlayerPrefs`'teki kayıtlı ses düzeyinden dolayı).
