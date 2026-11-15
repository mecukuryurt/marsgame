# V2 bağımsız son kontrol — 15 Kasım 2026

Durum: Bağımsız kontrol ve kapsam içi düzeltmeler tamamlandı. Mekanik testler yeşil;
CP04/CP17 diğer iki ekran boyutu için NeedsVerification.
Antigravity CP00–CP17'yi Done olarak raporladı. Kontrolde kaynakların ve CP17
kanıtlarının bir saatten eski olduğu, Unity kilidi bulunmadığı görüldü. Başka
çalışanın aktif dosya yazdığına dair kanıt yoktu. Mevcut kullanıcı değişiklikleri
korundu; commit/push yapılmadı.

## Düzeltilenler

- [x] Aktif/uyarıdaki radyasyon ve kum EventState'leri kayıt dosyasından kayboluyordu.
  Yükleme sonrasında kalan tehlike süresi, etkisi ve PRNG akışı artık korunuyor.
- [x] Bekleyen Sol raporu ve rapor geçmişi kayboluyor, rapor pause'u görünmez
  kalabiliyordu. Raporlar geri yükleniyor; Runner yükleme sonrası ayrıca pause'da açılıyor.
- [x] Kayıtlı özel Sol uzunluğu ve kaynak oranları sessizce varsayılana dönüyordu.
  Görev/kaynak konfigürasyonu v2 kaydında korunuyor. Negatif, taşmış veya NaN/Infinity
  rezervler reddediliyor. Görev ödülünün uygulanmış olma bayrağı da korunuyor.
- [x] Şema v1 kayıtları okunabiliyor: eksik dinamik tehlikeler kanaldan kalan süreyle,
  tekrar RNG çekmeden kuruluyor. V1'de hiç yazılmamış rapor geçmişi kurtarılamaz;
  görünmez rapor pause'u kaldırılır. Yeni kayıtlar şema v2 kullanır.
- [x] Panel/O₂/su arıza onarımları simülasyonda vardı, HUD'da komut yoktu.
  Sağ sistem kartına panel tamiri, O₂ tamiri, 10 bilim karşılığı su düzeltmesi ve
  çıkarıcı aç/kapat eklendi. Solar yüzde artık arızalı panel katkısını da içeriyor.
- [x] Kaydet/Yükle/Dünya Emirleri düğmeleri kaynak alt yazısının üstündeydi;
  sağ araç sırasındaki boşluğa taşındı. Araştırma toplamı ve bilim bakiyesi farklı
  adlarla gösteriliyor. Güneş paneli dünya etiketi HUD altında kalmaması için kısaltıldı.
- [x] Dünya emri komutları Runner bildirimlerini atlıyordu; reddedilen atama
  görünmüyordu. Artık HUD geri bildirimi ve CommandRejected akışı kullanılıyor.
- [x] Dünya görevleri WorldView hedeflerinde tanınmıyordu. Dış görevler dış hedefe,
  iç aşamalar laboratuvar/yaşam desteği/mutfak istasyonuna gider. Emir ekranı
  aşama sayısını, konumu ve dış işten sonra sığınağa dönüp yeniden atama ihtiyacını gösterir.
- [x] Dünya işleriyle normal işler aynı istasyona eşzamanlı atanabiliyordu.
  Laboratuvar, yaşam desteği ve mutfak rezervasyonu iki yönde kontrol ediliyor.
- [x] O₂ deneyinde tank doluyken üretim sınırı %10 yönlendirmeyi hesaba katmıyor,
  kapasite sınırında gereksiz dalgalanma oluşuyordu. Net tanka giren pay artık sınır
  ve sıfır rezervde tüketim hesabında kullanılıyor; %10 iki kez düşülmüyor.
- [x] Kum uyarısı ve ekipman arızaları uykuyu kesmiyordu. Uyuyan ekip uyanır,
  kritik pause isteği oluşur; uyanık rutin akış otomatik duraklatılmaz.
- [x] Yanyana yataklarda üç isim birbirine giriyordu. Yatan kişilerden yalnız
  seçilinin adı çizilir; tüm isimler ekip kartlarında kalır.
- [x] Eski ses PlayMode testi kullanıcı sesinin hep 0,6 olacağını varsayıyordu.
  Artık düğmenin göreli değişimini test eder, önceki ses/mute değerlerini geri koyar.
- [x] 100-sol testi kayıt yükledikten sonra eski simülasyonda çalışmayı sürdürüyordu.
  İkinci 50 sol gerçekten yüklenen kopyadan devam ediyor. İki yanlış bakım görev ID'si düzeltildi.
- [x] README Operations akışını, modları, onarımları ve kaydı anlatacak şekilde
  güncellendi. NASA kaynaklarındaki Training zamanları ve V2 tasarım sayıları ayrıldı.

## Gerçek doğrulama

Unity: `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity`.
Proje: `/run/media/senesatka/storage/projects/marsgame`.

1. Mevcut EditMode testleri bağımsız koşuda **78/78** geçti (çıkış 0).
2. Yeni regresyonlarla düzeltme öncesi **78 geçti / 7 başarısız**:
   `docs/validation/v2-review-regressions-before.xml`, çıkış 2.
   Dört uyarı/aktif tehlike yükleme vakası, Sol raporu, geçersiz rezerv ve dolu
   tankta O₂ deneyi sorunu böyle doğrulandı.
3. Son EditMode: **91/91**, çıkış **0**.
   `docs/validation/v2-review-editmode.xml`, 00:37:15–00:37:17 UTC.
   Komut: Unity `-batchmode -nographics -projectPath <proje> -runTests
   -testPlatform EditMode -testResults <proje>/docs/validation/v2-review-editmode.xml
   -logFile /tmp/mars-v2-review-editmode.log`.
4. Son PlayMode: **17/17**, çıkış **0**.
   `docs/validation/v2-review-playmode.xml`, 00:38:09–00:38:33 UTC.
   Komut: `DISPLAY=:0` Unity `-batchmode -projectPath <proje> -runTests
   -testPlatform PlayMode -testResults <proje>/docs/validation/v2-review-playmode.xml
   -logFile /tmp/mars-v2-review-playmode.log`.
   Yeni kontroller tamir düğmeleri/atomik ret, Runner hata bildirimi, yükleme pause'u
   ve görünür Sol raporunu kapsar. Mevcut uyku/15–30–60 FPS dönüş/korumalı iç hacim,
   ayar menüsü ve Training/Demo testleri de geçer.
5. Güncel uyku görseli: `docs/validation/v2-review-sleep.png` (1440×900 kamera
   çıktısı; yatak doğrultuları ve isim çakışması görsel olarak incelendi).
6. Linux: `-batchmode -nographics -quit -projectPath <proje> -buildLinux64Player
   <proje>/Build/Linux/MarsOutpost.x86_64 -logFile /tmp/mars-v2-review-build.log`.
   Bu yol sahneyi yeniden üretmez; mevcut kullanıcı sahnesi korunur.
   Son derleme çıkış **0**; günlük `Exiting batchmode successfully now!` ile bitti.
7. Normal Linux açılışı: `DISPLAY=:0 Build/Linux/MarsOutpost.x86_64
   --capture-screenshot=<proje>/docs/validation/v2-review-normal-launch.png
   -logFile /tmp/mars-v2-review-player.log`, çıkış **0**. Çözünürlük zorlaması yok;
   gerçek ekran **1440×900 @ 75 Hz**. Görüntü incelendi; tamir kartı ve kaynak
   satırları okunur, güneş etiketi HUD'ın altında kalmıyor.
8. İkinci normal açılışta aynı komuta `--capture-settings` eklenerek menü kontrol
   edildi, çıkış **0**. `docs/validation/v2-review-settings.png`; açık menüde oyun
   pause'da, doğal çözünürlük 1440×900 ve kayıtlı ses/grafik değerleri korunuyor.
   İki oyuncu günlüğünde Exception/Error/Shader error kaydı bulunmadı.

Son iki değişiklik yalnız araştırma/bilim ve güneş etiketlerinin metinleridir;
sonrasında Linux yeniden derlendi ve görüntüsü incelendi. Mantık testleri bu
metin değişikliklerinden önceki 91/17 koşusudur. Gerçek Linux görüntüsü için
kullanıcının sahnesi yeniden üretilmedi.

Başlangıç sandbox koşusu lisans IPC erişemediği için durduruldu (130); gerçek
Unity koşuları mevcut lisans servisine erişen onaylı çalıştırma üzerinden yapıldı.
Bir ara derleme ve yeni testteki yanlış HUD nesne adı düzeltildi; nihai sonuçlar
üsttedir. Çalıştırılmayan kontroller geçirilmiş sayılmadı.

## Kalan doğrulama / kapsam

- Fiziksel ekran 1440×900. 1920×1080 ve 1280×720 için gerçek Linux normal açılış
  görüntüsü henüz yok; formül testi bu iki cihaz kontrolünün yerine geçmez.
  Bu nedenle CP04/CP17 görsel kabulü NeedsVerification olarak izlenmeli.
- Yaş grubuyla çocuk playtesti ve kullanıcının kendi açılışında son onay yapılmadı.
- Stil hâlâ prosedürel bir prototiptir; yeni profesyonel karakter/çevre asset'i üretilmedi.
- Yükleme standart Operations senaryosu içindir. V1'in kaydetmediği geçmiş raporlar
  veya varsayılandan farklı eski konfigürasyon değerleri geriye dönük bulunamaz.

## Değişen dosyalar

- Simulation: `SaveLoad.cs`, `MissionSimulation.cs`, `Resources.cs`, `EarthOrders.cs`.
- Presentation: `SimulationRunner.cs`, `MarsOutpostHud.cs`, `MarsOutpostWorldView.cs`.
- Testler: `OperationsSaveLoadTests.cs`, `OperationsChannelSchedulerTests.cs`,
  `EarthOrderInfrastructureTests.cs`, `OperationsLongRunSoakTests.cs`,
  `MarsOutpostScenePlayTests.cs`, `CrewMovementVisualTests.cs`.
- Belgeler: bu rapor, PROGRESS, CP04/CP16/CP17 ek notları, PLAN'daki CP16 referansı,
  README, VALIDATION, RELEASE_NOTES ve SCIENCE_SOURCES.
- Bu kontrol öncesindeki değişiklikler ayrıca çalışma ağacında korunur; git diff'in
  tamamı bu kontrol tarafından yazılmış değildir.

Tek seferlik takip tamamlandı; zamanlanmış tekrar kaldırılır.
