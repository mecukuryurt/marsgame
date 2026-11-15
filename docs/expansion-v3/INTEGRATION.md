# Mevcut kodla birleşme ve kayıt sözleşmesi

15 Kasım 2026 kaynak incelemesidir; Hydrogen OP00'da gerçek dosyalarla yeniden
karşılaştırır. Satır numarasına değil tip/metot adına dayan.

## Mevcut noktalar ve gerekli uyarlama

| Dosya / nokta | Bugünkü durum | V3 işi |
|---|---|---|
| Simulation/MissionSimulation.cs / InitializeFromScenario | Operations'da panel-1..3, Weight=1/3 | Starter tesis sahipliği oluştur; başlangıç sayısını koru; sonradan ekleme/silme komutları |
| Resources.cs / ResourceState.Copy, SimulationMutableState.Clone | Tick önce mutable state'i kopyalıyor | Map, tesis, iş, rota, stok, rover ve ödül defterinde deep copy; eski snapshot mutasyona uğramaz |
| MissionSimulation.cs / PublishState, MissionState | Kopyalanmış anlık durum | Yeni alanların immutable/read-only snapshot'ı; HUD gizli jeoloji bilgisini açığa çıkarmasın |
| Resources.cs / AdvanceAll | Kaynak ve olay sınırlarında parçalıyor | Yol varışı, iş bitişi, site tükenmesi, yeni etkin kapasite ve şarj sınırları aynı döngüye katılır |
| Resources.cs / GetPanelHealthFactor | Sağlam Weight toplamı, üst clamp yok | Yerel katsayı/bağlantı ve commissioning dahil; ilk toplam 1 korunur |
| Resources.cs / OnSolarPanelFaultTriggered | Sağlam panel seçiyor; ActiveTargetId kaydediyor | Yalnız hizmette/uygun panel adayları; aynı kanalın RNG ve bekleme aralığı korunur |
| Resources.cs / SolarPanelRepair tamamlama | Bütün panellerin IsFaulted değerini false yapıyor | Yalnız doğrulanmış hedefi onar; silinmiş/yanlış hedefte maliyetsiz ret/uygun temizleme |
| Resources.cs / çıkarma akışı | Tek WaterExtractionRate ve scalar fault/enabled | Başlangıç çıkarıcısı + ayrı siteler; global anahtar/servis düşüşü; çift sayım yok |
| Resources.cs / Limit, Apply, SnapBoundary | Config'deki sabit kapasite kullanılıyor | Etkin bağlantılı kapasiteye geç; depo küçülmesindeki taşmayı deftere kaydet |
| MissionSimulation.cs / Assign, Replace, ReleaseTask, Rest, Return, BeginSleep | TaskState/EarthOrder sahipliği | Yeni SurfaceJob/analiz/tesis işi ekle; bütün komutlarda karşılıklı dışlama/atomik ret |
| SimulationRunner.cs | UI sarmalayıcıları ve StateChanged/CommandRejected | Yeni komutların hepsi buradan; HUD doğrudan mutable Simulation state yazamaz |
| WorldView / ComputeTargetPosition, Returning ve sleepAnchors | Sabit istasyon konumları; Returning tek rota sahibi; üç yatak | Stable tesis ID→konum; yeni domain rotasının render interpolasyonu; ek habitat anchor/hacim listesi |
| WorldView / BuildTerrainMesh | x[-36,36], z[-28,44], prosedürel yükseklik | 16×16 grid bu alanın içinde; ikinci terrain veya farklı domain/view grid'i yaratma |
| MarsOutpostCameraController | Sınırlı pan/zoom, menu guard | Harita sınırlarına göre pan; üs resetini koru; inşa/mini harita tık ayrımı |
| MarsOutpostHud / RefreshEquipmentUi | healthy/3 sabiti ve global onarım düğmeleri | Canlı toplamlar, seçili tesis hedefi, gerçek üretim/kapasite, malzeme ve işler |
| MarsOutpostAudio | StateChanged farklarını izliyor | Yeni olay/görev bir kez; yüklemede baseline sync; ses ayarlarını ezme |
| SaveLoad.cs / ValidateSnapshot | Panels.Count != 3 reddi, Weight<=1 | Starter minimum/sahiplik/ID sınırına göre değişken liste doğrulama; toplamı 1 yapma |
| SaveLoad.cs / serializer/ReadConfig | Şema 2, v1 uyarlaması; config/event/report korunumu | Şema 3 + revision, yeni veriler, v1/v2 göç; mevcut düzeltmeleri kaybetme |
| MarsOutpostSceneBuilder.BuildLinuxDesktop | BuildScene çağırıp sahneyi yeniden üretir | Kullanıcı sahnesini koru; sırf binary için -buildLinux64Player kullan |

Yeni dosyaları Simulation ve Presentation altına konularına göre eklemek
uygundur; tüm genişlemeyi mevcut 1000+ satırlı tek sınıfa yığma. Simülasyon
asmdef'i Unity bağımlılığı almamalı. Enum'a değer eklerken eski sayıları değiştirme.

## Başlangıç sahipliği ve tek hesap kaynağı

- `starter-solar` tesisi eski üç panel ID'sine sahiptir; görselde eski üçlü dizi
  veya alt paneller kullanılır. Bir panelin iki sahibi veya sahipsiz aktif panel olamaz.
- `legacy-extractor` mevcut akışın tek sahibidir. Yeni site listesine taşırken
  eski rate'i ayrıca ekleyip toplamı ikiye katlama. Eski anahtarlar global servis içindir.
- `core-habitat`, çekirdek port ve güvenli koridorlar korunan altyapıdır.
- State/Panels listesi ve tesis ilişkisi aynı komutun atomik sonucudur. Görsel
  instantiate/Destroy olayı üretim ekleyemez, ödül veremez, RNG çekemez.
- Tesis/iş/örnek ID'leri artan kalıcı sayaçtan gelir; Guid/DateTime/global Random
  ile simülasyon kararını nondeterministik yapma. Save/load next-ID'yi korur.

## MIGRATION — uygulanacak geçiş politikası

Şimdiki şema v2 dosyası ve v1 okuma davranışı OP00'da fixture olarak korunur.
Yeni kayıt: `schemaVersion=3`, `expansionRevision` pozitif tam sayı. Kullanılan
revision ve eklenen alanlar her kalıcı model checkpoint'inde bu bölüme eklenir.
İleri revision desteklenmiyorsa gerekçeli ret; eksik alanları sessizce temiz oyuna çevirme.

### Revision geçmişi
- **Revision 1 (OP02):** `materials`, `lastMaterialSupplySol`, `nextEntityId`, `map` (`mapSeed`, `width`, `height`, `cellSize`, `originX`, `originZ`, `cells`). Legacy v1/v2 göçünde `Materials = InitialMaterials (40)`, `LastMaterialSupplySol = current Sol`, `NextEntityId = 1`, `Map = ParcelMap.Generate(DeriveMapSeed(OperationsSeed))`; mevcut V2 kaynak/olay/kanal/Sol raporu/Dünya emri korunur. Panel doğrulaması `panel-1..3` zorunluluğu ve `<= 12` üst sınırı kullanır.
- **Revision 2 (OP04):** `crew` (`activeSurfaceJobId`, `surfacePosX`, `surfacePosZ`, `hasSurfacePosition`) ve `surfaceJobs` (`jobId`, `kind`, `targetId`, `targetX/Z`, `workX/Z`, `assignedCrewId`, `phase`, `travelSpeed`, `workRequiredSeconds`, `workProgressSeconds`, `workStaminaRate`, `workEnergyTotal`, `energyConsumed`, `waypoints`, `legTotalDistance`, `legDistanceTraveled`, `airlockRemainingSeconds`). Rev 1 ve legacy kayıtlardan göçte boş `surfaceJobs` listesi başlatılır; hayali aktif iş üretilmez.
- **Revision 3 (OP06):** `map.cells[].ubid` (`UtilityBlueprintId`) ve `facilities` (`facilityId`, `kind`, `originX/Z`, `rotation`, `lifecycleStatus`, `workProgressSeconds`, `workRequiredSeconds`, `energySpentTotal`, `energyRequiredTotal`, `paidMaterials`, `isProtectedStarter`, `isConnectedToCore`, `isProductionApplied`, `footprint`, `linkedPanelIds`). Rev 1/2 ve legacy kayıtlardan göçte starter tesisler (`core-habitat`, `starter-solar`, `legacy-extractor`) başlatılır; bitmemiş `Blueprint` tesisleri 0 üretim kuralıyla yüklenir.

1. Legacy kayıt önce mevcut V2 decoder semantiğiyle okunur. Elapsed/config,
   timeCompensation, kaynaklar, arıza hedefi/RNG, görevler, Sol raporu, bilim,
   günlük öğün ve Dünya emirleri korunur. Göçte `Tick` çalıştırılmaz.
2. MapSeed eski OperationsSeed'den ayrı sabit map stream'iyle türetilir; eski
   beş kanalın sayısı/state'i/tarihi hiç değişmez. Default procedural map, starter
   footprint/tahsis/tesis ilişkisi kurulur. Mevcut panel-1..3 ID'si değişmez.
3. Yeni malzeme başlangıç ödeneği bir defa 40 verilir; `lastMaterialSupplySol`
   mevcut Sol'a kurulur. Eski kaydı her açış yeni dosyada ikinci ödenek değildir;
   migrator'ın tekrar çalışması state'i değiştirmemeli. Göç kaynak üretmez.
4. Mevcut başlangıç Battery/Water kapasitesi ve rezervleri aynen taşınır. Yeni
   batarya/tank/saha/rover boş listeleri oluşturulur; hayali dolu depo yaratılmaz.
5. Yeni state alanları etkinleştiği checkpoint'te aynı revision geçişi + serializer
   + decoder + Validate + Clone + Snapshot + Restart + test eklenir.
   Varsayılanlar sonraki kontrolsüz UI Awake içinde üretilmez.
6. Yüklemede tüm referanslar doğrulanır: eşsiz ID; footprint çakışması; harita
   sınırı; tesis-panel/site sahibi; iş-ekip/rota sahibi; geçmiş malzeme işareti;
   süre/kaynak sonluluğu ve nonnegative sınırlar; tek rover/kargo; ödül anahtarları.
7. Kayıt tmp→replace atomik kalır. Başarısız göç/yükleme aktif simülasyonu
   değiştirmez. Yüklenen oyun pause'da, eski 8x kuyruk boş ve offline atlama 0'dır.
8. Sadece varlık sayısını kaydetmek yetmez: blueprint maliyeti/iade bayrağı,
   tamamlanma uygulanmış bayrağı, iş hedefi/rotası, kalan deposit, topoloji,
   tarama/keşif, sample origin/award ledger ve sonraki ID de saklanır.

OP30'da migration matrisini gerçek fixture yolları ve sonuçlarıyla doldur:

| Girdi | Beklenen |
|---|---|
| v1 legacy | Mevcut v1 sınırlamalarını dürüstçe koru; kanaldan kalan tehlike kurulur |
| v2 V2 review | Tüm V2 veri + ilk map/starter; eski üç panel/çıkarıcı 1 kere |
| v3 önceki revision | Yalnız yeni alanın deterministik geçişi; ekonomi reset yok |
| v3 güncel | Round-trip ve gelecek komut replay eşdeğer |
| Bilinmeyen sürüm/bozuk referans/NaN | Açıklamalı ret; mevcut oyun aynı |

## Entegrasyon test grupları

- Eski 91/17 tabanı korunur. Sabit 3 gibi eski bir beklenti değişecekse nedenini
  açıkla; eski starter davranışı için ayrıca test tut. Testi silerek yeşil yapma.
- Atomik ret: malzeme yetersiz, yanlış arazi, meşgul ekip, hat yok, rota yok,
  sınır dışı, çift tık, aktif hedef sökümü. State/RNG/bakiye farkı 0.
- Akışlar: 3→4 panel gücü, gece 0, arızalı 0, toz ×0,20; O₂/global su arızası;
  bağlı/bağlantısız; dolu tank; sıfır enerji; tükenen site; taşma defteri.
- İş: seyahat sırasında iş yok; iptal/yeniden ata; yorgunluk; dönüş; uyku/uyandırma;
  aynı laboratuvar/anchor çakışması; büyük/küçük Tick ve pause.
- Save: blueprint/travel/work/repair/survey/site tükenme/rover kargo/analiz/uyku/
  radyasyon+kum aktifken yükleme ve aynı komutlarla ileri karşılaştırma.
- RNG: map, seçim, görsel ve hat işleri V2'nin diğer kanallarını yeniden çekmez.
- Görsel: UI tık filtresi; frame interpolasyon; bina taşma/rotasyon; yeni iç
  hacimde efekt yok; malzeme/enerji nedenleri okunur; source sayıları UI'ya sızmaz.

## Çalıştırma rehberi

Kurulu Unity: `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity`.
Proje: `/run/media/senesatka/storage/projects/marsgame`.

EditMode: `-batchmode -nographics -projectPath <proje> -runTests -testPlatform
EditMode -testResults <proje>/docs/validation/v3-opXX-editmode.xml
-logFile /tmp/mars-v3-opXX-editmode.log`.

PlayMode: aynı test komutunda `-testPlatform PlayMode`; grafik kontrolü için
mevcut erişilebilir ekranı kullan, gerektiğinde -nographics kaldır. Başka Unity
aynı projeyi açık tutuyorsa lock dosyasını silme; çalışan editörü/diğer ajanı
doğrula ve çakışan koşu açma. Lisans/IPC engelinde başarısızlığı raporla.

Linux: `-batchmode -nographics -quit -projectPath <proje> -buildLinux64Player
<proje>/Build/Linux/MarsOutpost.x86_64 -logFile /tmp/mars-v3-opXX-build.log`.
Normal açılışta çözünürlük zorlamadan kontrol et; zorlanmış görüntüyü normal
açılış kanıtı diye yazma. Kaydedilmiş ayar ve yeniden açılış ayrı doğrulanır.
