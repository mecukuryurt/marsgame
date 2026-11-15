# Koloni Genişlemesi V3 — Başlangıç Durumu (BASELINE)

Tarih: 2026-11-15 09:23 (+03:00)

## 1. Ortam, Sürüm ve Kaynak/Binary Yolları

- **Proje yolu:** `/run/media/senesatka/storage/projects/marsgame`
- **Branch / HEAD:** `master` @ `f56730c8bbbe20247c38961633a9a865a4480e29` (`fix(presentation): fix interior working pose, enable 4x MSAA, add regolith detail & 2D HUD icons`)
- **Unity sürümü (`ProjectSettings/ProjectVersion.txt`):** `6000.3.28f1` (`8f082d2fe476`)
- **Unity Editor yolu:** `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity`
- **Linux player binary yolu:** `Build/Linux/MarsOutpost.x86_64` (14 Kasım 2026 tarihli V2 derlemesi; `MarsOutpostSceneBuilder.BuildLinuxDesktop` sahneyi yeniden ürettiği için V3 binary derlemelerinde `-buildLinux64Player` kullanılacak)
- **Aktif lock / paralel yazar:** `Temp/` yok, çalışan başka Unity işlemi yok. `AGENTS.md` dosyası repoda bulunmuyor.

## 2. Bağımsız Tekrar Koşulan Test Kanıtları (OP00)

- **EditMode:** **91 / 91 geçti** (0 failed, 0 skipped), çıkış kodu `0` (`duration=2.305s`).
  - Komut: `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/v3-op00-editmode.xml -logFile /tmp/mars-v3-op00-editmode.log`
  - Kanıt: `docs/validation/v3-op00-editmode.xml`
- **PlayMode:** **17 / 17 geçti** (0 failed, 0 skipped), çıkış kodu `0` (`duration=26.620s`).
  - Komut: `DISPLAY=:0 /home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform PlayMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/v3-op00-playmode.xml -logFile /tmp/mars-v3-op00-playmode.log`
  - Kanıt: `docs/validation/v3-op00-playmode.xml`

## 3. Mevcut Kod Envanteri (V2 Tabanı vs V3 Eksikleri)

### V3 Sınıfları
- `GridCoord`, `ParcelMap`, `ParcelState`, `FacilityState`, `ConstructionJob`, `SurfaceJob`, `UtilityNetwork`, `ExtractionSiteState`, `RoverState`, `SampleState`, `Blueprint` sınıflarından hiçbiri henüz mevcut değildir.

### Mevcut Simülasyon ve Sunum Yapısı
- **Simulation asmdef (`Assets/MarsOutpost/Runtime/Simulation/MarsOutpost.Simulation.asmdef`):** `noEngineReferences: true`.
- **Paneller:** `MissionSimulation.InitializeFromScenario` Operations modunda `panel-1`, `panel-2`, `panel-3` oluşturur (`Weight = 1.0 / 3.0`). `Resources.GetPanelHealthFactor` sağlıklı panellerin `Weight` toplamını döndürür; `OnSolarPanelFaultTriggered` sağlıklı bir paneli arızalar ve `ch.ActiveTargetId` yazar; ancak `SolarPanelRepair` tamamlanınca tüm `s.Panels` listesini sağlamlaştırır.
- **Su Çıkarıcı:** Operations varsayılanında `WaterExtractionRate = 0.012`, `WaterExtractorEnergyRate = 0.015`, `WaterExtractorEnabled = true`, `WaterExtractorFaulted = false` (tek scalar akış; site listesi yok).
- **Dünya ve Koordinat Sözleşmesi (`MarsOutpostWorldView.cs`):**
  - Terrain mesh: `x in [-36, 36]`, `z in [-28, 44]` (`BuildTerrainMesh`, `GetTerrainHeight(x, z)`).
  - Habitat merkezi `(-2.0f, 0f, 0f)`, base pad `6.5 × 6.1`, duvar x = `1.20f`, hava kilidi iç `(1.20f, 0f, -0.35f)`, dış `(2.45f, 0f, -0.35f)`, solar array `(4.65f, 0f, 0.25f)`, solar work `(4.35f, 0f, -1.15f)`, track walkway `(3.20f, 0.005f, -0.55f)`.
  - Üç yatak anchor'ı: `sleepAnchors[0..2]`.
- **Kamera (`MarsOutpostCameraController.cs`):** Pan sınırları `x in [-3.8f, 4.8f]`, `z in [-2.8f, 3.0f]`, mesafe `[8.0f, 18.5f]`, pitch `43°`.
- **HUD (`MarsOutpostHud.cs`):** `RefreshEquipmentUi` `healthy/3` sabitini gösterir ve global panel/O₂/su onarımı ile çıkarıcı aç/kapat sunar.

### Kayıt Şeması ve Legacy Fixture Yolları
- **Mevcut şema:** `OperationsSaveCodec.CurrentSchemaVersion = 2` (`Assets/MarsOutpost/Runtime/Simulation/SaveLoad.cs`). `schemaVersion = 1` okuması desteklenir; `ValidateSnapshot` şimdilik `r.Panels.Count != 3` reddini uygular.
- **Kayıt/göç test ve fixture kaynakları:**
  - `Assets/MarsOutpost/Tests/EditMode/OperationsSaveLoadTests.cs` (dinamik round-trip, aktif/uyarı tehlike kayıtları, Sol raporu, geçersiz rezerv/sürüm reddi).
  - `Assets/MarsOutpost/Tests/EditMode/OperationsLongRunSoakTests.cs` (100-sol koşu ve Sol 50 yükleme).
  - `Assets/MarsOutpost/Tests/EditMode/OperationsChannelSchedulerTests.cs` ve `EarthOrderInfrastructureTests.cs`.
  - Not: Repoda ayrı statik `.json` v1/v2 fixture dosyası yoktur; OP02 ve OP30'da v1/v2 kayıt göçü için açık JSON fixture'ları testlere/validation altına eklenecektir.

## 4. Bilinen V2 Ekran Doğrulama Açıkları

- **V2 CP04 / CP17:** `1920×1080` ve `1280×720` gerçek normal Linux açılış görüntüleri fiziksel/erişilebilir ekran kısıtı nedeniyle henüz doğrulanmadı (`NeedsVerification`).
- `1440×900` gerçek açılış/ayar/uyku kanıtları mevcuttur (`docs/validation/v2-review-normal-launch.png`, `v2-review-settings.png`, `v2-review-sleep.png`).
- Kullanıcının kendi denemesi ve hedef yaş grubuyla çocuk playtesti henüz yapılmadı.

## 5. Korunacak Mevcut Kullanıcı Değişiklikleri

`git status` çıktısındaki tüm değiştirilmiş (`M`, 23 dosya) ve takip edilmeyen (`??`, V2 testleri, `SaveLoad.cs`, `EarthOrders.cs`, `OperationsChannels.cs`, `MarsOutpostSettingsMenu.cs`, `MarsRenderer3D.asset`, `MarsOutpost.unity`, `docs/` ve `docs/validation/` dosyaları) çalışma ağacı kullanıcı çalışması olarak korunur.
