# V2 uygulama ilerlemesi

Current: Bağımsız son kontrol tamamlandı; CP04/CP17 görsel kabulü NeedsVerification
Next: 1920×1080 / 1280×720 gerçek Linux açılışları ve kullanıcı denemesi
Son tamamlanan V2 checkpoint: CP17
Durum: V2 mekanikleri uygulandı. Bağımsız kontrolde 91/91 EditMode ve 17/17 PlayMode geçti; CP04/CP17 için iki ekran boyutunda gerçek açılış kanıtı eksik. Ayrıntılar REVIEW.md içinde.

Bu dosya uygulayıcı tarafından her küçük teslimde güncellenir. Gerçek kod ve
kanıtlarla karşılaştırılır; yalnız rapora dayanarak Done yazılmaz.

| Checkpoint | Durum | Rapor / kanıt / kalan iş |
|---|---|---|
| CP00 | Done | `BASELINE.md`, `checkpoints/CP00.md`; EditMode 53/53, PlayMode 10/11 (`v2-cp00-playmode.xml` içinde 1 mevcut ses düzeyi hatası) |
| CP01 | Done | `checkpoints/CP01.md`; `SleepAnchor_0..2`, 0.65s geçiş, EditMode 53/53, `CrewMovementVisualTests` 2/2, `v2-cp01-sleep.png` |
| CP02 | Done | `checkpoints/CP02.md`; tek sahip `ReturnPath`, interpolasyon ve teğet dönüş, EditMode 53/53, `CrewMovementVisualTests` 3/3 |
| CP03 | Done | `checkpoints/CP03.md`; ayrı efekt havuzları, `IsInsideProtectedVolume`, EditMode 53/53, `CrewMovementVisualTests` 4/4, `v2-cp03-hazards.png` |
| CP04 | NeedsVerification | `checkpoints/CP04.md`; 16:10 kadraj, doğal çözünürlük, menü WASD kilidi, EditMode 53/53, `SettingsMenuTests` 5/5, `v2-cp04-normal-launch.png` (1440×900) |
| CP05 | Done | `checkpoints/CP05.md`; `MissionMode.Operations` (süresiz, 0 olay), `CycleMode()`, EditMode 54/54 (`v2-cp05-editmode.xml`), `CrewMovementVisualTests` 4/4 (`v2-cp05-playmode.xml`) |
| CP06 | Done | `checkpoints/CP06.md`; `ScienceBalance`, atomik `SpendScience`, günlük rezerv öğün lojistiği, EditMode 55/55 (`v2-cp06-editmode.xml`) |
| CP07 | Done | `checkpoints/CP07.md`; `WaterExtractor`, enerji önceliği, 6/100 sol denge, EditMode 56/56 (`v2-cp07-editmode.xml`) |
| CP08 | Done | `checkpoints/CP08.md`; `OperationsChannels.cs`, 5 bağımsız PRNG kanalı, sınır kırpması, EditMode 59/59 (`v2-cp08-editmode.xml`) |
| CP09 | Done | `checkpoints/CP09.md`; Radyasyon kanalı (900–1200sn, 60sn uyarı, 60–120sn aktif, `0.03/sn` normale dönüş), EditMode 60/60 (`v2-cp09-editmode.xml`) |
| CP10 | Done | `checkpoints/CP10.md`; 3 panel (`PanelUnitState`), `SolarPanelRepair` dış tamir, iptalde ilerleme korunumu, EditMode 61/61 (`v2-cp10-editmode.xml`) |
| CP11 | Done | `checkpoints/CP11.md`; O₂ arızası (`×0.50` kapasite), 2× enerjili (`20/40sn`) iç tamir, EditMode 62/62 (`v2-cp11-editmode.xml`) |
| CP12 | Done | `checkpoints/CP12.md`; Kum fırtınası (600–900sn, 60–180sn, `×0.20` solar), EditMode 63/63 (`v2-cp12-editmode.xml`) |
| CP13 | Done | `checkpoints/CP13.md`; Su çıkarıcı arızası (`×0.50`), atomik 10 bilim onarım komutu, EditMode 64/64 (`v2-cp13-editmode.xml`) |
| CP14 | Done | `checkpoints/CP14.md`; `EarthOrders.cs`, Sol 3/5/7 varışı, seeded torba, HUD modal, EditMode 66/66 (`v2-cp14-editmode.xml`), PlayMode 9/9 (`v2-cp14-playmode.xml`) |
| CP15A | Done | `checkpoints/CP15A.md`; `EARTH-01` dış 60sn -> dönüş -> iç 60sn analiz, bilim 0, EditMode 67/67 (`v2-cp15a-editmode.xml`) |
| CP15B | Done | `checkpoints/CP15B.md`; `EARTH-02` %10 O₂ yönlendirme, 0 üretimde durma, bilim 0, EditMode 68/68 (`v2-cp15b-editmode.xml`) |
| CP15C | Done | `checkpoints/CP15C.md`; `EARTH-03` dış 60sn -> dönüş -> iç 20sn rapor, bilim 0, EditMode 69/69 (`v2-cp15c-editmode.xml`) |
| CP15D | Done | `checkpoints/CP15D.md`; `EARTH-04` dış 60sn -> dönüş -> iç 30sn, toplam 3 enerji, `SystemWear` onarmaz, bilim 0, EditMode 70/70 (`v2-cp15d-editmode.xml`) |
| CP15E | Done | `checkpoints/CP15E.md`; `EARTH-05` başlangıçta 2 su + 60sn 2 enerji analiz, yetersiz suda ret, bilim 0, EditMode 71/71 (`v2-cp15e-editmode.xml`) |
| CP15F | Done | `checkpoints/CP15F.md`; `EARTH-06` dış 60sn (5 enerji) -> dönüş -> iç 30sn bağlantı testi, takvim/gecikme değişmez, bilim 0, EditMode 72/72 (`v2-cp15f-editmode.xml`) |
| CP15G | Done | `checkpoints/CP15G.md`; `EARTH-07` iki iç adım (60sn sayım -> 30sn tatbikat), tayın/bilim değişmez, 7 tip torba doğrulandı, EditMode 73/73 (`v2-cp15g-editmode.xml`) |
| CP16 | Done | `checkpoints/CP16.md`; `SaveLoad.cs` (ilk v1; son kontrolde v2, aktif olay/rapor/config korunumu), atomik dosya yazımı, duraklatılmış yükleme, HUD Kaydet/Yükle, EditMode 75/75 (`v2-cp16-editmode.xml`) |
| CP17 | NeedsVerification | `checkpoints/CP17.md`; 100-sol multi-seed soak (`101/202/303`), EditMode 78/78 (`v2-cp17-editmode.xml`), PlayMode 14/15 (`v2-cp17-playmode.xml`; 1 mevcut CP00 ses hatası), Linux build (`Build/Linux/MarsOutpost.x86_64`) ve temiz başlatma doğrulandı |

## 15 Kasım bağımsız son kontrol

`REVIEW.md` kaynak/kanıt karşılaştırmasını, düzeltmeleri, komutları ve kalanları içerir.
Güncel kanıtlar: `validation/v2-review-editmode.xml` (**91/91**),
`validation/v2-review-playmode.xml` (**17/17**; eski ses testi de geçti),
`validation/v2-review-normal-launch.png`, `validation/v2-review-settings.png`,
`validation/v2-review-sleep.png`.
Buradaki validation yolları `docs/validation/` dizinine aittir.
Linux çıktı: `Build/Linux/MarsOutpost.x86_64`.
CP04/CP17'nin önceki Done raporları 1920×1080/1280×720 normal açılış kanıtı
sağlamıyordu; bu kontroller yapıldı gibi işaretlenmedi. Mevcut fiziksel ekran
1440×900'dür. Kullanıcı/çocuk playtesti yapılmadı.
