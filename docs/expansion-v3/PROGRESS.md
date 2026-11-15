# V3 uygulama ilerlemesi

Current: OP16 — Deferred
Next: Current OP15 hackathon polish; OP16 deferred by user
LastDone: OP15
Durum: OP15 (Teslim A kalite kapısı) tamamlandı (`v3-op15-editmode.xml` 141/141, `v3-op15-playmode.xml` 35/35, Linux build + `v3-op15-open-1440.png`, `DELIVERY_A.md`). Kullanıcı isteğiyle OP15 sonunda duruldu.
Uygulayıcı: Hydrogen / Antigravity.

Bu tablo V2'nin CP00–CP17 tablosundan ayrıdır. V2'deki 91/17 test sonucu bu yeni
özellikler için başarı kanıtı değildir. V2 CP04/CP17 ekran açıkları taşınır;
mevcut kaynak esas alınarak OP00 baseline yeniden çıkarılır.

| OP | İş | Bağımlılık | Durum | Kanıt / kalan |
|---|---|---|---|---|
| OP00 | Yeni başlangıç ve kapsam fotoğrafı | Yok | Done | `BASELINE.md`, `v3-op00-editmode.xml` (91/91), `v3-op00-playmode.xml` (17/17) |
| OP01 | Parsel veri modeli ve deterministik harita | OP00 | Done | `ParcelGrid.cs`, `ParcelMapTests.cs`, `v3-op01-editmode.xml` (102/102) |
| OP02 | Tahsis, malzeme ve ilk kayıt göçü | OP01 | Done | `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `OperationsExpansionSaveTests.cs`, `v3-op02-editmode.xml` (107/107), `v3-op02-playmode.xml` (17/17) |
| OP03 | Grid görünümü ve parsel seçimi | OP02 | Done | `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsParcelSelectionPlayModeTests.cs`, `v3-op03-editmode.xml` (107/107), `v3-op03-playmode.xml` (20/20), `v3-op03-grid-1440.png` |
| OP04 | Yüzey rotası ve domain iş aşamaları | OP03 | Done | `SurfaceJob.cs`, `OperationsSurfaceNavTests.cs`, `v3-op04-editmode.xml` (112/112), `v3-op04-playmode.xml` (20/20) |
| OP05 | Yüzey yürüyüşü, geri çağırma ve kamera | OP04 | Done | `MarsOutpostWorldView.cs`, `MarsOutpostCameraController.cs`, `OperationsSurfaceMotionPlayModeTests.cs`, `v3-op05-editmode.xml` (112/112), `v3-op05-playmode.xml` (23/23), `v3-op05-surface-1440.png` |
| OP06 | Yapı kataloğu, hayalet ve atomik blueprint | OP05 | Done | `Facilities.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsBlueprintTests.cs`, `OperationsBuildGhostPlayModeTests.cs`, `v3-op06-editmode.xml` (116/116), `v3-op06-playmode.xml` (25/25), `v3-op06-ghost-1440.png` |
| OP07 | Astronotla inşa ve hizmete alma | OP06 | Done | `Resources.cs`, `MissionSimulation.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsConstructionTests.cs`, `OperationsBuildMotionPlayModeTests.cs`, `v3-op07-editmode.xml` (121/121), `v3-op07-playmode.xml` (26/26) |
| OP08 | Bağlantı hattı ve üs ağı | OP07 | Done | `UtilityNetwork.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsNetworkTests.cs`, `OperationsNetworkPlayModeTests.cs`, `v3-op08-editmode.xml` (124/124), `v3-op08-playmode.xml` (27/27) |
| OP09 | Kurulabilir yeni güneş paneli | OP08 | Done | `OperationsChannels.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `MarsOutpostHud.cs`, `OutpostStrings.cs`, `OperationsSolarPanelTests.cs`, `OperationsSolarPanelPlayModeTests.cs`, `v3-op09-editmode.xml` (127/127), `v3-op09-playmode.xml` (28/28) |
| OP10 | Yeni panel hedefi ve tekil tamir | OP09 | Done | `OperationsChannels.cs`, `SurfaceJob.cs`, `Facilities.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `OutpostStrings.cs`, `MarsOutpostHud.cs`, `OperationsPanelRepairTests.cs`, `OperationsPanelRepairPlayModeTests.cs`, `v3-op10-editmode.xml` (129/129), `v3-op10-playmode.xml` (29/29) |
| OP11 | Kurulabilir batarya ve etkin kapasite | OP10 | Done | `Facilities.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OutpostStrings.cs`, `OperationsBatteryTests.cs`, `OperationsBatteryPlayModeTests.cs`, `v3-op11-editmode.xml` (133/133), `v3-op11-playmode.xml` (30/30) |
| OP12 | İptal, söküm ve maliyet bütünlüğü | OP11 | Done | `Facilities.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `OutpostStrings.cs`, `MarsOutpostHud.cs`, `OperationsDismantleTests.cs`, `OperationsDismantlePlayModeTests.cs`, `v3-op12-editmode.xml` (137/137), `v3-op12-playmode.xml` (31/31) |
| OP13 | Kurulabilir buz su çıkarıcı | OP12 | Done | `Facilities.cs`, `Resources.cs`, `MissionSimulation.cs`, `SaveLoad.cs`, `OutpostStrings.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsWaterExtractorTests.cs`, `OperationsWaterExtractorPlayModeTests.cs`, `v3-op13-editmode.xml` (141/141), `v3-op13-playmode.xml` (32/32) |
| OP14 | Su tesisi görünümü ve anlaşılır telemetri | OP13 | Done | `Resources.cs`, `SaveLoad.cs`, `MissionSimulation.cs`, `OutpostStrings.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OperationsExtractorTelemetryPlayModeTests.cs`, `v3-op14-editmode.xml` (141/141), `v3-op14-playmode.xml` (33/33), `v3-op14-extractor-1440.png` |
| OP15 | Teslim A: kur–bağla–üret–kaydet | OP14 | Done | `OperationsDeliveryAPlayModeTests.cs`, `DELIVERY_A.md`, `v3-op15-editmode.xml` (141/141), `v3-op15-playmode.xml` (35/35), `Build/Linux/MarsOutpost.x86_64`, `v3-op15-open-1440.png` |
| OP16 | Keşif işi ve kalıcı keşif sisi | OP15 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; yarım çalışma docs/hackathon/deferred-op16.patch |
| OP17 | Yerel kaynak taraması | OP16 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP18 | Alternatif regolit fırını | OP17 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP19 | Ek su tankı | OP18 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP20 | Mini harita ve harita odaklama | OP19 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP21 | Kısa rehber, TR/EN ve geri bildirim | OP20 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP22 | Teslim B uzun koşu ve ekonomi | OP21 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP23 | Teslim B Linux ve kullanılabilirlik | OP22 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP24 | Ek sığınak modeli ve dinlenme yerleri | OP23 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP25 | Yeni sığınağın iç görünümü ve uyku | OP24 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP26 | Rover dok, araç ve şarj | OP25 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP27 | Rover rotası, görev ve geri çağırma | OP26 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP28 | Örnek taşıma ve tek seferlik analiz | OP27 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP29 | Rover görünümü ve örnekleme animasyonu | OP28 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP30 | Tam kayıt uyumu ve 100-sol birleşik regresyon | OP29 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP31 | Son Linux, üç ekran ve kullanıcı akışı | OP30 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |
| OP32 | Son kapsam karşılaştırması ve teslim | OP31 | Deferred | Kullanıcı kapsamı OP15 cilasına daralttı; sonraya ertelendi |

## Çalışır teslimler

- Teslim A / OP15: Done — parsel, inşaat, hat, panel, batarya, su çıkarıcı (`DELIVERY_A.md`).
- Teslim B / OP23: Deferred — keşif, tarama, fırın, tank, mini harita, rehber.
- Teslim C / OP29: Deferred — ek sığınak, rover ve örnek analizi.
- Son kapanış / OP32: Deferred — tüm regresyonlar, Linux ve dürüst kapsam raporu.

## Devralınan doğrulama açıkları

- V2 CP04/CP17: 1920×1080 ve 1280×720 normal gerçek Linux açılışı henüz yok.
- Kullanıcının kendi denemesi ve hedef yaşla çocuk playtesti yapılmadı.
- Bu plan hazırlanırken Unity testleri tekrar çalıştırılmadı; oyun kodu değişmedi.

## İlerleme güncelleme kuralı

Her OP öncesi `checkpoints/OP-ID.md` oluştur; kod yazmadan InProgress yap.
Her anlamlı alt iş sonrası raporu yaz. Done yalnız gerçek kabul/kanıtla verilir.
