# Doğrulama kaydı

## Güncel durum — Unity 6000.3.28f1 / T02

Kullanıcının yeni talebiyle proje kurulu **6000.3.28f1 (8f082d2fe476)** sürümüne
geçirildi. İlk import, paket çözümleme ve C# derleme başarılı; gerçek Unity
EditMode Test Runner sonucu **18 test, 18 başarılı, 0 başarısız, 0 atlanan**.
Kalıcı sonuç: `docs/validation/t02-editmode-results.xml`.
Hedef platform build'i ve oynanabilir sahne bu aşamanın kapsamında değildir.
Aşağıdaki 6000.3.23f1 engeli önceki incelemenin tarihsel kaydıdır; artık geçerli
proje sürümü 6000.3.28f1'dir. Ayrıntılı geçiş ve T02 kanıtları dosyanın sonundadır.

## T00 — Unity ortamı (14 Kasım 2026)

- `ProjectSettings/ProjectVersion.txt`: proje Editor sürümü **6000.3.23f1**,
  revizyon `09d2ecc7fb28`. Standart Hub dizininde yalnızca
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity` bulundu;
  gerekli sürüm aranan konumlarda bulunamadı. Bu, diğer tüm olası kurulum
  konumlarının kesin olarak dışlandığı anlamına gelmez.
- PATH üzerindeki `/home/senesatka/.local/bin/unity` bir ELF yürütülebilir
  dosyadır (`file` ve `readlink -f`); kendisi 6000.3.23f1 Editor kurulumu
  olarak doğrulanmadı veya çalıştırılmadı. `~/.config/UnityHub/Settings` boş
  bir JSON nesnesi; `~/.local/share` altında beş düzeye kadar yapılan Editor
  ve sürüm dizini aramasında da gerekli sürüm bulunmadı.
- `ps -eo pid,comm,args | rg -i '[u]nity|[l]icense'` ve
  `ps -eo pid,comm | rg -i 'unity|license'` hiçbir eşleşme döndürmedi.
  Başka bir Editor işlemi gözlenmedi; yine de sürüm uyuşmadığı için mevcut
  projeyi farklı Editor sürümüyle açma girişiminde bulunulmadı.
- Projede `Packages/manifest.json` ve `Packages/packages-lock.json` mevcut;
  `Library/PackageCache`, `Library/ScriptAssemblies` ve proje `Logs` dizini
  yok. Kilit dosyasının varlığı paketlerin indirilebildiğini kanıtlamaz.
  Paket çözümlemesi, başlangıç derlemesi, lisans ve Editor'de açılış **henüz
  doğrulanmadı**. Bu kontrol için bu çalışmaya ait Unity logu oluşmadı.
- Yüklü **6000.3.28f1** sürümünün `Editor/Data/PlaybackEngines` dizininde
  `AndroidPlayer`, `LinuxStandaloneSupport` ve `WindowsStandaloneSupport`
  var; WebGL dizini yok. Bu yalnızca dosya varlığı kontrolüdür, build veya
  Android araç zinciri doğrulaması değildir; gerekli 6000.3.23f1 sürümünün
  modülleri de doğrulanmadı. Web hedefi için uygun sürüme WebGL desteği gerekir.
- `Assets/Scenes/SampleScene.unity` ve meta dosyası mevcut ve değiştirilmedi.

Yapılan diğer inceleme komutları: `cat ProjectSettings/ProjectVersion.txt
Packages/manifest.json`, `ls -d /opt/unity* /opt/Unity*
/home/senesatka/Unity/Hub/Editor/* /opt/unityhub*`, `find /opt /usr/local
/home/senesatka/Unity /run/media/senesatka/storage -maxdepth 6 \( -name Unity
-o -name 6000.3.23f1 -o -name UnityHub.AppImage \) -print`, `find Library
-maxdepth 2 -type d`, ve ilgili PlaybackEngines dizini için `ls -la`.
Mevcut genel Editor logları `~/.config/unity3d/Editor.log` ve
`~/.config/unity3d/Editor-prev.log` bu çalışma tarafından üretilmedi ve bu
projeye ait derleme kanıtı olarak kullanılmadı.

Sonraki doğrulama: 6000.3.23f1 ve hedef modüller kullanılabilir olduğunda,
önce başka bir Editor'ün projeyi açmadığını kontrol edip projeyi o sürümle
açın; paket çözümlemesini, derlemeyi ve hedef build'i gerçek Editor loguyla
doğrulayın. Kaynak kodu üzerindeki bağımsız çalışmalar bu açık doğrulama ile
devam edebilir.

## T01 — Temel simülasyon saati (14 Kasım 2026)

`Assets/MarsOutpost/Runtime/Simulation/` saf C# saat, yapılandırma, senaryo ve
salt okunur durum sözleşmesini; `Assets/MarsOutpost/Tests/EditMode/` yedi EditMode
testini içerir. Sonraki görevlerin komutları şimdilik açıklamalı ret döndürür.

Bağımsız kontrol: 6000.3.28f1 kurulumundaki `Editor/Data/MonoBleedingEdge/bin/mono`
ile `lib/mono/msbuild/Current/bin/Roslyn/csc.exe` çalıştırılarak önce simülasyon,
sonra test derlemesi oluşturuldu. Testler aynı kurulumun
`Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll`
dosyasına bağlandı. `nunit-console` betiği eksik `lib/mono/4.0/nunit-console.exe`
nedeniyle çalışmadı; bunun yerine `/tmp/marsgame-t01/Runner.cs` içindeki küçük
reflection sürücüsü `[Test]` metotlarını ve NUnit assertion'larını çalıştırdı:
**7 test, 0 hata**. Derlenmiş geçici dosyalar `/tmp/marsgame-t01/` altındadır.
Komut biçimi: `mono csc.exe -target:library -out:<dll> <kaynaklar>`; test için
ayrıca `-r:<simülasyon dll> -r:<nunit.framework.dll>`; sonrasında
`mono /tmp/marsgame-t01/Runner.exe`.

Bu sonuç Unity Editor derlemesi veya Unity Test Runner sonucu değildir. Gerekli
6000.3.23f1 ile paket çözümlemesi ve EditMode testleri hâlâ doğrulanmalıdır;
proje başka bir Unity sürümüyle açılmadı.

### Koordinatör incelemesi

Sol kaynak kodunu ve testleri inceledi; mevcut kaynaklardan simülasyon ve test
DLL'lerini yeniden derleyerek aynı yedi testi tekrar çalıştırdı: **7/7 geçti**.
Komut ve çıktılar `/tmp/marsgame-t01/coordinator-verification.log` dosyasındadır;
geçici dizin kalıcı bir CI kaydı değildir. Assembly JSON dosyaları ve yeni varlıkların
meta dosyaları/GUID tutarlılığı da kontrol edildi. Mevcut sahneler, paketler ve
ProjectSettings değiştirilmedi. T02 veya sonraki görevler başlatılmadı.

## Kurulu sürüme geçiş ve T02 (14 Kasım 2026)

### Editor yükseltmesi

`/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -version` çıktısı
`6000.3.28f1`. Editor logunda revizyon `8f082d2fe476` doğrulandı ve
ProjectVersion.txt bu sürüme geçirildi. İlk sandbox çalışması lisans IPC/veritabanı
erişiminde takıldı ve durduruldu; izinli çalışmada lisans bağlantısı başarılı oldu.

İlk import komutu:

```bash
/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /run/media/senesatka/storage/projects/marsgame \
  -logFile /tmp/marsgame-unity-upgrade-escalated.log
```

Süreç çıkış kodu 0; log `Exiting batchmode successfully now!` ile sonlandı.
Paketler çözümlendi, Library önbelleği oluşturuldu ve scriptler derlendi.
Unity geçiş sırasında Linux SDK/toolchain 1.1.1 paketlerini ekledi, transitif Burst
ve Collections çözümlemesini güncelledi; manifest/kilit dosyaları bu gerçek Editor
çözümlemesini içerir. PlayerSettings varsayılan Android ikon alanları ve ShaderGraph
ayarına yeni varsayılan alan da Editor tarafından yazıldı. Mevcut sahneler korunmuştur.

### Kaynak modeli ve inceleme

Terra Medium, ayarlanabilir kapasite/başlangıç/rate değerleri, gündüz güneş üretimi,
batarya, su–enerji ile oksijen üretimi, atık sudan geri kazanım, ekip tüketimi ve
karşılanamayan tüketim toplamlarını ekledi. Öncelik yaşam desteği → geri kazanım;
araştırma isteği değiştirilebilir, fakat T03'te ekip ataması gelene kadar araştırma
enerji tüketmez veya ilerleme üretmez. Uyku henüz uygulanmadı; tüketim Tick'e bağlıdır.

Sol ilk 17 testi bağımsız olarak yeniden derleyip doğruladı. Ek 50 ayarlı
zaman bölme karşılaştırması kesirli sınırda hata buldu. Terra, süreyi ayrı ayrı
azaltmak yerine tek hedef zamana ilerleme ve temsil edilemeyecek kadar küçük
kaynak sınırlarını normalleştirme ile düzeltti; bu karşılaştırma kalıcı regresyon
testine eklendi. Güncel suite 7 saat + 11 kaynak testinden oluşur.

### Gerçek Unity EditMode sonucu

```bash
/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /run/media/senesatka/storage/projects/marsgame \
  -runTests -testPlatform EditMode \
  -testResults /tmp/marsgame-t02-unity-results.xml \
  -logFile /tmp/marsgame-t02-unity-tests.log
```

Süreç çıkış kodu 0. XML: `result=Passed`, `total=18`, `passed=18`, `failed=0`,
`skipped=0`. Testler 14 Kasım 2026, Türkiye saatiyle 14:44'te tamamlandı.
XML sonucu `docs/validation/t02-editmode-results.xml` içine kopyalandı;
tam Editor logları `/tmp` içinde geçicidir. Kaynak karşılaştırma toleransı 1e-7 oyun
birimidir; 50 ayarlı regresyon bir büyük Tick ile 1.000 küçük Tick'i karşılaştırır.

WebGL modülü hâlâ kurulu değil; Android/Linux/Windows hedefleri için bu çalışmada
build alınmadı. T03 ve sonraki görevler başlatılmadı.

## T03 — Ekip, görev atama ve stamina (14 Kasım 2026)

- Değişenler: `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`,
  `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`,
  `Assets/MarsOutpost/Tests/EditMode/CrewAndTaskSimulationTests.cs`.
- Uygulananlar: `CrewState` stamina/konum/durum/dönüş sayacı; `TaskState` tek seferlik
  ve sürekli görevler (`panel-maintenance`, `system-maintenance`, `research`);
  `AssignTask`, `CancelTask`, `Rest`, `ReturnToShelter` komutları ve red nedenleri;
  aynı alt-adım entegrasyon döngüsünde stamina, 10s dönüş ve görev tamamlanma sınırları;
  araştırmanın ekip + kalan enerjiyle orantılı ilerlemesi.
- Komut:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /tmp/marsgame-t03-results.xml -logFile /tmp/marsgame-t03-unity.log`
- Sonuç: Çıkış kodu 0; **26/26 test geçti** (7 saat + 11 kaynak + 8 ekip/görev/stamina).
  Kalıcı XML: `docs/validation/t03-editmode-results.xml`.

## T04 — Olaylar, korunak ve görev sonucu (14 Kasım 2026)

- Değişenler: `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`,
  `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`,
  `Assets/MarsOutpost/Tests/EditMode/ResourceSimulationTests.cs` (`OverflowAndWorkLimitFailAtomically` sınırsız takvimle sınır testini koruyacak şekilde güncellendi),
  `Assets/MarsOutpost/Tests/EditMode/EventAndOutcomeSimulationTests.cs`.
- Komut:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /tmp/marsgame-t04-results.xml -logFile /tmp/marsgame-t04-unity.log`
- Sonuç: Çıkış kodu 0; **32/32 test geçti** (+6 olay/risk/sonuç testi).
  Kalıcı XML: `docs/validation/t04-editmode-results.xml`.

## T05 — Gece uykusu ve sol raporu (14 Kasım 2026)

- Değişenler: `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`,
  `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`,
  `Assets/MarsOutpost/Tests/EditMode/SleepAndSolReportTests.cs`.
- Komut:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /tmp/marsgame-t05-results.xml -logFile /tmp/marsgame-t05-unity.log`
- Sonuç: Çıkış kodu 0; **38/38 test geçti** (+6 uyku/kritik O2/sol raporu testi).
  Kalıcı XML: `docs/validation/t05-editmode-results.xml`.

## T06 — Oynanabilir Unity sahnesi (14 Kasım 2026)

- Değişenler / eklenenler:
  - `Assets/MarsOutpost/Runtime/Presentation/SimulationStepper.cs`, `SimulationRunner.cs`, `MarsOutpostWorldView.cs`, `MarsOutpostHud.cs`, `OutpostStrings.cs`, `MarsOutpost.Presentation.asmdef`
  - `Assets/MarsOutpost/Editor/MarsOutpostSceneBuilder.cs`, `MarsOutpost.Editor.asmdef`
  - `Assets/MarsOutpost/Scenes/MarsOutpost.unity` (SampleScene.unity korundu)
  - `Assets/MarsOutpost/Tests/EditMode/SimulationStepperTests.cs`
  - `Assets/MarsOutpost/Tests/PlayMode/MarsOutpostScenePlayTests.cs`, `MarsOutpost.Tests.PlayMode.asmdef`
- Sahne oluşturma komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -quit -projectPath /run/media/senesatka/storage/projects/marsgame -executeMethod MarsOutpost.Editor.MarsOutpostSceneBuilder.BuildSceneFromCommandLine -logFile /tmp/marsgame-t06-scene.log` (çıkış 0).
- EditMode komutu:
  `...-runTests -testPlatform EditMode -testResults /tmp/marsgame-t06-editmode-results.xml` → **42/42 geçti** (`docs/validation/t06-editmode-results.xml`).
- PlayMode komutu:
  `...-runTests -testPlatform PlayMode -testResults /tmp/marsgame-t06-playmode-results.xml` → **1/1 geçti** (`docs/validation/t06-playmode-results.xml`, sahne yükleme, UI görev atama, duraklatma, yeniden başlatma ve ikinci açılış). Not: `-nographics` başsız doğrulama yapılmıştır; görsel piksel/yerleşim kontrolü T10/T11'de gerçek grafik/ekran çıktısıyla yapılacaktır.

## T07 — Metin, gösterge ve kullanıcı denemesi (14 Kasım 2026)

- Değişenler / eklenenler: `Assets/MarsOutpost/Runtime/Presentation/OutpostStrings.cs`,
  `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs`, `docs/PLAYTEST.md`.
- PlayMode komutu:
  `...-runTests -testPlatform PlayMode -testResults /tmp/marsgame-t07-playmode-results.xml` → **1/1 geçti** (`docs/validation/t07-playmode-results.xml`).
- Canlı kullanıcı denemesi yapılmadı (`docs/PLAYTEST.md` içinde yapılan ve önerilen denemeler ayrı yazıldı).

## T08 — NASA kaynağı ve bilimsel şeffaflık (14 Kasım 2026)

- Değişenler / eklenenler:
  - `Assets/MarsOutpost/Runtime/Simulation/NasaScienceData.cs`
  - `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs` (`EventState.SourceRecordId`, `SourceNote`)
  - `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs` (`Sources` butonu ve modal pencere)
  - `Assets/MarsOutpost/Tests/EditMode/EventAndOutcomeSimulationTests.cs` (`DefaultScenarioEventsTraceToVerifiedNasaScienceEntries`)
  - `docs/SCIENCE_SOURCES.md`
- Doğrulanan NASA kayıtları:
  - `https://api.nasa.gov/DONKI/SEP?startDate=2024-05-01&endDate=2024-05-15&api_key=DEMO_KEY` üzerinden `2024-05-09T13:59:00-SEP-001` (ACE/EPAM, bağlı FLR `2024-05-09T08:45:00-FLR-001`) ve `2024-05-10T13:35:00-SEP-001` (GOES-P SEISS >10 MeV, bağlı CME `2024-05-08T05:36:00-CME-001`) doğrulandı.
  - NASA ECLSS (`Environmental Control and Life Support Systems`) ve NASA Mars Exploration Rovers 2018 küresel toz fırtınası referansları eklendi; senaryo zamanlaması ve katsayıların eğitim/oyun tasarımı olduğu ayrıldı.
- EditMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t08-editmode-results.xml -logFile /tmp/marsgame-t08-editmode.log` → **43/43 geçti** (`docs/validation/t08-editmode-results.xml`).
- PlayMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform PlayMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t08-playmode-results.xml -logFile /tmp/marsgame-t08-playmode.log` → **1/1 geçti** (`docs/validation/t08-playmode-results.xml`).

## T09 — Gıda hazırlama ve 45 sn gecikmeli Dünya tavsiyesi (14 Kasım 2026)

- Değişenler / eklenenler:
  - `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs` (`TaskKind.MealPreparation`, `EarthAdviceStatus`, `RequestEarthAdvice`, öğün ve iletişim yapılandırma/durum alanları)
  - `Assets/MarsOutpost/Runtime/Simulation/Resources.cs` (gece başına 1 kez `SettleNightMealIfDue`, kilitli `NightSleepRecoveryFactor`, 45 sn `CheckEarthAdviceDelivery` ve kural tabanlı `BuildEarthAdviceMessage`)
  - `Assets/MarsOutpost/Runtime/Presentation/SimulationRunner.cs`, `OutpostStrings.cs`, `MarsOutpostHud.cs` (yeni üst bar eklenmeden `Prep Meal` görevi ve sol panel altı `Galley & Earth Relay` kartı)
  - `Assets/MarsOutpost/Tests/EditMode/SleepAndSolReportTests.cs` (`0.75x` hazırlıksız uyku beklentisiyle hizalandı)
  - `Assets/MarsOutpost/Tests/EditMode/FoodAndCommsSimulationTests.cs` (4 yeni test: öğün hazırlığı/maliyet/reset, `0.75x` kilitli gece katsayısı ve yeniden uyku engeli, 45 sn gecikme + anti-spam + pause, uykuda ilerleme + geçmiş olay zaman damgası + restart)
- EditMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t09-editmode-results.xml -logFile /tmp/marsgame-t09-editmode.log` → **47/47 geçti** (`docs/validation/t09-editmode-results.xml`).
- PlayMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform PlayMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t09-playmode-results.xml -logFile /tmp/marsgame-t09-playmode.log` → **1/1 geçti** (`docs/validation/t09-playmode-results.xml`).

## T10 & T11 — Görsel/ses düzeni, Linux masaüstü build, çözünürlük ve uçtan uca doğrulama (14 Kasım 2026)

- Değişenler / eklenenler:
  - `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostAudio.cs` (ücretli/harici asset olmadan çalışma anı `AudioClip.Create` prosedürel SFX + Mute/Volume kontrolü)
  - `Assets/MarsOutpost/Runtime/Presentation/SimulationRunner.cs` (`InitializeDemoMission()` 2x45 sn demo önayarı, aynı `MissionSimulation` çekirdeği)
  - `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs` (`GALLEY`, hava kilidi, sıcak Mars zemin ayrıntıları, kompakt etiketler, astronot vizörleri)
  - `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs`, `OutpostStrings.cs` (`Mode` seçici, ses kontrolleri, görsel yerleşim düzeltmeleri ve doğrulanmış çözünürlük ekran görüntüsü desteği `--capture-screenshot`)
  - `Assets/MarsOutpost/Editor/MarsOutpostSceneBuilder.cs` (`BuildLinuxDesktop` eklendi)
  - `Assets/MarsOutpost/Tests/PlayMode/MarsOutpostScenePlayTests.cs` (`AudioSourcesFoodCommsAndNinetySecondDemoCompleteEndToEnd` eklendi)
- Masaüstü build komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -quit -projectPath /run/media/senesatka/storage/projects/marsgame -executeMethod MarsOutpost.Editor.MarsOutpostSceneBuilder.BuildLinuxDesktop -logFile /tmp/marsgame-t11-final-build.log` → çıkış kodu 0; çıktı: `Build/Linux/MarsOutpost.x86_64`.
- Gerçek grafik çalıştırma ve çözünürlük ekran görüntüsü kontrolü (`DISPLAY=:0`):
  - `DISPLAY=:0 .../Build/Linux/MarsOutpost.x86_64 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --capture-screenshot=docs/validation/screenshot-1280x720.png` → çıkış 0 (`docs/validation/screenshot-1280x720.png`).
  - `DISPLAY=:0 .../Build/Linux/MarsOutpost.x86_64 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 --capture-screenshot=docs/validation/screenshot-1920x1080.png` → çıkış 0 (`docs/validation/screenshot-1920x1080.png`).
  - İlk görsel incelemede bulunan istasyon etiket çakışması, `EventCard`/`Stop Task` üst üste binmesi ve `Research Power: ON` kırpması düzeltilerek build yeniden alınıp iki çözünürlük de temiz doğrulandı.
- Final EditMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t11-editmode-results.xml -logFile /tmp/marsgame-t11-editmode.log` → **47/47 geçti** (`docs/validation/t11-editmode-results.xml`).
- Final PlayMode komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform PlayMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/t11-playmode-results.xml -logFile /tmp/marsgame-t11-playmode.log` → **2/2 geçti** (`docs/validation/t11-playmode-results.xml`).
- Eksik modül / açık kalem: Unity `6000.3.28f1` kurulumunda WebGL PlaybackEngine bulunmadığı için WebGL build alınamadı.

## T12 — Teslim belgeleri ve yarışma anlatımı (14 Kasım 2026)

- Oluşturulanlar: `README.md`, `docs/DEMO_SCRIPT.md` (7 slayt taslağı ve 30 sn video senaryosu; video henüz üretilmedi), `docs/PROJECT_SUBMISSION_EN.md`.
- `spaceappschallenge.org` güncel demo sınırları (en fazla 30 sn harici linkli video ve İngilizce altyazı, veya en fazla 7 slayt İngilizce PDF) çevrimiçi doğrulandı. Başvuru gönderimi/yayınlama yapılmadı.

## Özel 2D Sprite Asset Güncellemesi & Son Yeniden Doğrulama (14 Kasım 2026)

- Eklenen / güncellenenler (dış/ücretli üçüncü taraf asset yok):
  - `Assets/MarsOutpost/Art/` altında proje içi üretilmiş 7 özel 2D PNG sprite ve `.meta` dosyası: `mars_terrain.png`, `habitat_shelter.png`, `station_lifesupport.png`, `station_galley.png`, `station_lab.png`, `solar_array.png`, `astronaut_suit.png`.
  - `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs`: 7 `[SerializeField] private Sprite` alanı, gündüz/gece zemin tonlaması, güneş paneli kirlenme tonlaması, astronot kıyafet/vizör/seçim halkası çizimi ve prosedürel yedek korunumu.
  - `Assets/MarsOutpost/Editor/MarsOutpostSceneBuilder.cs`: `LoadSpriteAsset(string assetPath)` (`TextureImporterType.Sprite`, `Single`, `100 PPU`, `Bilinear`, `alphaIsTransparency = true`) ile `MarsOutpost.unity` içine 7 sprite bağlandı.
- Yeniden build komutu:
  `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -quit -projectPath /run/media/senesatka/storage/projects/marsgame -executeMethod MarsOutpost.Editor.MarsOutpostSceneBuilder.BuildLinuxDesktop -logFile /tmp/marsgame-art-build.log` → çıkış kodu 0; `Assets/MarsOutpost/Art/*.png.meta` oluşturuldu ve `Build/Linux/MarsOutpost.x86_64` güncellendi.
- Güncellenmiş gerçek ekran görüntüsü kontrolü (`DISPLAY=:0`):
  - `docs/validation/screenshot-1280x720.png` (çıkış 0) ve `docs/validation/screenshot-1920x1080.png` (çıkış 0) görsel olarak incelendi; zemin, habitat gövdesi, `O2 / SYS`, `GALLEY`, `LAB`, `SOLAR ARRAY` ve astronot kıyafet/etiket yerleşimleri iki çözünürlükte de çakışmasız ve net doğrulandı.
- Son düzenleme sonrası final EditMode & PlayMode testleri:
  - EditMode (`-testPlatform EditMode -testResults docs/validation/t11-editmode-results.xml`) → **47/47 geçti** (`1.26s`).
  - PlayMode (`-testPlatform PlayMode -testResults docs/validation/t11-playmode-results.xml`) → **2/2 geçti** (`3.71s`).
