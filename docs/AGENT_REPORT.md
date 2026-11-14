# Mars Outpost — Agent Report

## Kilometre Taşı 1: T03 — Ekip, görev atama ve stamina (14 Kasım 2026)

- **Durum:** Tamamlandı ve Unity 6000.3.28f1 EditMode'da doğrulandı.
- **Değişen / eklenen dosyalar:**
  - `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`
  - `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`
  - `Assets/MarsOutpost/Tests/EditMode/CrewAndTaskSimulationTests.cs` (ve Unity tarafından üretilen `.meta`)
  - `docs/validation/t03-editmode-results.xml`
- **API ve davranış özeti:**
  - `CrewState`: `Stamina`, `Location` (`Inside`/`Outside`), `Status` (`Idle`, `Working`, `Resting`, `Returning`, `Sheltered`, `Sleeping`, `Exhausted`), `ActiveTaskId`, `ReturnRemainingSeconds`, `RiskScore`.
  - `TaskState`: `Id`, `Title`, `Kind` (`PanelMaintenance`, `SystemMaintenance`, `Research`), `Location`, `IsContinuous`, `RequiredSeconds`, `StaminaRatePerSecond`, `ProgressSeconds`, `AssignedCrewId`, `IsCompleted`, `RewardApplied`.
  - Komutlar ve ret nedenleri:
    - `AssignTask(crewId, taskId)`: Bilinmeyen ID, uyku, dönüş, zaten çalışan astronot, bitkin/0 stamina, tamamlanmış görev, dolu istasyon ve dışarıdayken iç görev atamaları açıklayıcı `CommandResult.Reason` ile reddedilir.
    - `Rest(crewId)`: Dışarıdaysa, dönüşteyse, uykudaysa, zaten dinleniyorsa veya stamina doluysa reddedilir; içerideki aktif işi serbest bırakıp dinlenmeye geçirir.
    - `ReturnToShelter(crewId)`: İçerideyse/dönüyorsa/uykudaysa reddedilir; aktif dış işi serbest bırakır, 10 sn dönüş başlatır (0 stamina ile de tamamlanır).
    - `CancelTask(crewId)`: Aktif işi veya dinlenmeyi keser, görev ilerlemesini korur.
  - Araştırma ilerlemesi yalnızca çalışan astronot + `ResearchEnabled` + kalan enerji varken orantılı ilerler; `0` enerjide ilerleme veya stamina kaybı üretmez.
- **Çalıştırılan komut ve sonuç:**
  - `Unity -batchmode -nographics -runTests -testPlatform EditMode` → **26/26 geçti** (`docs/validation/t03-editmode-results.xml`).
- **Bilinen açık işler:** T04 (olaylar, korunak risk puanı ve görev sonu), T05 (uyku ve sol raporu), T06+ sahne ve build.
- **Sonraki adım:** T04.

## T04 — Olaylar, korunak ve görev sonucu (14 Kasım 2026)

- **Durum:** Tamamlandı ve Unity EditMode'da doğrulandı (**32/32 geçti**, `docs/validation/t04-editmode-results.xml`).
- **Özet:** Sabit senaryo olayları (`rad-1`, `rad-2`, `dust-1`), 5 dk uyarı / başlangıç / bitiş entegrasyon sınırları, iç/dış radyasyon risk oranları, dönüş sırasında dış maruziyetin sürmesi, atmosferik tozun panel kirlenmesinden ayrı tutulması, kesintisiz oksijen eksikliği sayacının toparlanınca sıfırlanması, başarısızlık kontrollerinin başarıdan önce gelmesi ve terminal durumda simülasyonun donması uygulandı. `OverflowAndWorkLimitFailAtomically` testi açık uçlu takvim parametreleriyle (`targetSols: 0, oxygenOutageFailureSeconds: 0`) korunarak `MaxTransitions` sınırını doğrulamaya devam etti.
- **Sonraki adım:** T05.

## Kilometre Taşı 2: T05 — Gece uykusu ve sol raporu (14 Kasım 2026)

- **Durum:** Tamamlandı ve Unity EditMode'da doğrulandı (**38/38 geçti**, `docs/validation/t05-editmode-results.xml`).
- **Değişen / eklenen dosyalar:** `MissionSimulation.cs`, `Resources.cs`, `SleepAndSolReportTests.cs`, `docs/validation/t05-editmode-results.xml`.
- **Özet:**
  - `BeginSleep()`: yalnızca gece ve tüm ekip içerideyken başlar; içerideki görevleri ilerlemeyi koruyarak serbest bırakır; `RecommendedTimeScale = 8.0` önerir (domain `dt` çarpmaz).
  - `CancelSleep()`: uykuyu iptal eder ve o ana dek kazanılan stamina'yı korur; gündüz/uyanık sol geçişinde ücretsiz stamina üretilmez.
  - Kritik oksijen (< 20 birim) aşağı geçişinde tek seferde uykuyu keser, `CriticalAlertPauseRequested` üretir; `SolReport` ayrı `SolReportPauseRequested` üretir ve birini kapatmak diğerini silmez.
  - `1x` gece adımları (`960 * 0.25s`) ile `8x` uyku runner adımları (`120 * 2.0s`) özdeş simülasyon sonucu verir.
- **Sonraki adım:** T06.

## Kilometre Taşı 3: T06 — Oynanabilir Unity sahnesi (14 Kasım 2026)

- **Durum:** Tamamlandı ve Unity EditMode + PlayMode'da doğrulandı.
- **Değişen / eklenen dosyalar:** `Runtime/Presentation/*` (`SimulationStepper`, `SimulationRunner`, `MarsOutpostWorldView`, `MarsOutpostHud`, `OutpostStrings`), `Editor/MarsOutpostSceneBuilder.cs`, `Scenes/MarsOutpost.unity`, `ProjectSettings/EditorBuildSettings.asset`, `Tests/EditMode/SimulationStepperTests.cs`, `Tests/PlayMode/MarsOutpostScenePlayTests.cs`.
- **Çalıştırma adımları:**
  1. Unity Editor'da `Assets/MarsOutpost/Scenes/MarsOutpost.unity` sahnesini açın (veya menüden `MarsOutpost -> Build or Update Playable Scene` çalıştırın).
  2. Play tuşuna basın; soldan/haritadan astronot seçin (`1-3` veya tıklama), sağdan görev/dinlenme/dönüş/uyku/duraklatma (`Space`) komutlarını verin.
- **Test sonuçları:**
  - EditMode: **42/42 geçti** (`docs/validation/t06-editmode-results.xml`).
  - PlayMode: **1/1 geçti** (`docs/validation/t06-playmode-results.xml`).
- **Görsel not / açık kalem:** PlayMode koşusu `-nographics` başsız ortamda yapıldığı için görsel piksel kontrolü henüz yapılmadı (T10/T11'de ekran görüntüsü ile doğrulanacak).
- **Sonraki adım:** T07.

## T07 — Metin, gösterge ve kullanıcı denemesi (14 Kasım 2026)

- **Durum:** Tamamlandı; PlayMode testi geçti (**1/1**, `docs/validation/t07-playmode-results.xml`).
- **Değişenler:** `OutpostStrings.cs`, `MarsOutpostHud.cs`, `docs/PLAYTEST.md`.
- **Özet:** 3 adımlı etkileşimli öğretici şerit (`seç → görev ver → tehlike/uyku`), eğitim risk puanı açıklaması, iki kararlı sonuç özeti ve canlı playtest gözlem rehberi eklendi. Gerçek kullanıcı denemesi henüz yapılmadı ve raporda açıkça önerilen adım olarak ayrıldı.
- **Sonraki adım:** T08.

## T08 — NASA kaynağı ve yarışmaya uygunluk (14 Kasım 2026)

- **Durum:** Tamamlandı; EditMode **43/43** (`docs/validation/t08-editmode-results.xml`) ve PlayMode **1/1** (`docs/validation/t08-playmode-results.xml`) geçti.
- **Değişen / eklenenler:** `NasaScienceData.cs`, `MissionSimulation.cs`, `MarsOutpostHud.cs` (`Sources` modalı), `EventAndOutcomeSimulationTests.cs`, `docs/SCIENCE_SOURCES.md`.
- **Özet:** NASA DONKI API'den doğrudan çekilen iki Mayıs 2024 SEP kaydı (`2024-05-09T13:59:00-SEP-001` ve `2024-05-10T13:35:00-SEP-001`), NASA ECLSS ve 2018 Mars toz fırtınası kaynakları varsayılan senaryo olaylarına kimlik bazlı bağlandı; ölçülmüş veri ile tasarlanmış eğitim katsayıları arayüzde ve belgede ayrıldı.
- **Sonraki adım:** T09 (günlük öğün hazırlığı ve 45 sn gecikmeli Dünya tavsiyesi).

## Kilometre Taşı 4: T09 — Gıda hazırlama ve 45 sn gecikmeli Dünya tavsiyesi (14 Kasım 2026)

- **Durum:** Tamamlandı ve Unity EditMode + PlayMode'da doğrulandı.
- **Değişen / eklenenler:** `MissionSimulation.cs`, `Resources.cs`, `SimulationRunner.cs`, `OutpostStrings.cs`, `MarsOutpostHud.cs`, `SleepAndSolReportTests.cs`, `FoodAndCommsSimulationTests.cs`.
- **Özet:**
  - Her sol için 1 hazır ekip öğünü stoğu (`InitialFoodRations = 2`), gece başına tek kez düşüş ve 30 sn iç mekân hazırlık görevi (`-0.10/s` stamina) eklendi; hazırlık yapılmazsa gece uyku toparlanması `0.75x` katsayısında kilitlenir ve uykuya çıkıp girerek sıfırlanamaz.
  - Tek kullanımlık, 45 simülasyon saniyesi gecikmeli kural tabanlı Dünya tavsiyesi (`RequestEarthAdvice`) eklendi; mesaj gerçek Mars iletişim süresinin konuma bağlı olduğunu ve 45 sn'nin sıkıştırıldığını açıklar, pause'da durur, uykuda ilerler ve geçmişte kalan olayları zaman damgasıyla bildirir.
- **Test sonuçları:**
  - EditMode: **47/47 geçti** (`docs/validation/t09-editmode-results.xml`).
  - PlayMode: **1/1 geçti** (`docs/validation/t09-playmode-results.xml`).
- **Sonraki adım:** T10 (görsel bütünlük ve ses düzeni).

## Kilometre Taşı 5 (Son Rapor): T10 – T12 — Görsel/Ses, Linux Build ve Yarışma Teslimi (14 Kasım 2026)

- **Durum:** T03–T12 tamamlandı (aşağıdaki açık kalemler kullanıcı kararına/aracına bırakıldı).
- **Eklenen / güncellenen ana dosyalar:**
  - Görsel varlıklar (`Assets/MarsOutpost/Art/` + `.meta`): `mars_terrain.png`, `habitat_shelter.png`, `station_lifesupport.png`, `station_galley.png`, `station_lab.png`, `solar_array.png`, `astronaut_suit.png` (proje içi üretilmiş özel 2D PNG sprite'lar).
  - Sunum ve Ses: `MarsOutpostAudio.cs`, `SimulationRunner.cs` (90 sn Demo Modu), `MarsOutpostWorldView.cs` (özel sprite alanları + prosedürel yedek), `MarsOutpostHud.cs`, `OutpostStrings.cs`, `MarsOutpostSceneBuilder.cs` (`LoadSpriteAsset` + `BuildLinuxDesktop`).
  - Testler: `FoodAndCommsSimulationTests.cs`, `MarsOutpostScenePlayTests.cs` (`AudioSourcesFoodCommsAndNinetySecondDemoCompleteEndToEnd`).
  - Teslim belgeleri: `README.md`, `docs/SCIENCE_SOURCES.md`, `docs/PLAYTEST.md`, `docs/DEMO_SCRIPT.md`, `docs/PROJECT_SUBMISSION_EN.md`, `docs/VALIDATION.md`, `docs/IMPLEMENTATION_TASKS.md`.
- **Doğrulanan build ve testler (son düzenleme sonrası):**
  - Masaüstü build: `Build/Linux/MarsOutpost.x86_64` (çıkış 0).
  - Gerçek ekran görüntüsü kontrolü (`DISPLAY=:0`): `docs/validation/screenshot-1280x720.png` ve `docs/validation/screenshot-1920x1080.png` — özel 2D sprite'larla çakışmasız, okunaklı yerleşim doğrulandı.
  - EditMode: **47/47 geçti** (`docs/validation/t11-editmode-results.xml`, `1.26s`).
  - PlayMode: **2/2 geçti** (`docs/validation/t11-playmode-results.xml`, `3.71s`).
- **Dış asset / lisans durumu:** Harici ücretli veya üçüncü taraf asset kullanılmadı; `Assets/MarsOutpost/Art/*.png` sprite'ları proje içi üretildi ve ses efektleri çalışma anında prosedürel olarak oluşturuldu. Açık kaynak lisans (`MIT`/`Apache-2.0` vb.) kullanıcı seçimi için bekleniyor.
- **Açık kalan / yapılmamış kalemler (dürüst özet):**
  1. Kurulu Unity `6000.3.28f1` içinde **WebGL modülü eksik** olduğundan WebGL build alınamadı (yalnızca Linux masaüstü build teslim edildi).
  2. Gerçek çocuk/hedef kitleyle **canlı kullanıcı denemesi** (`docs/PLAYTEST.md`) henüz yapılmadı.
  3. **30 sn demo videosu veya 7 slayt PDF'i** taslak olarak yazıldı (`docs/DEMO_SCRIPT.md`), ancak video kaydı/PDF render ve yarışma portalına başvuru gönderimi kapsam gereği kullanıcıya bırakıldı.
