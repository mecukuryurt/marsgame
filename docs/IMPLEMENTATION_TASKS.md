# Uygulama görevleri ve model devri

Tarih: 14 Kasım 2026. Oyun tasarımı: `GAME_PLAN.md`.

Güncel tam teslim kapsamı ve devir düzeni: `MASTER_PLAN.md`.
Antigravity başlangıç promptu: `ANTIGRAVITY_PROMPT.md`.

T00 ortam kontrolü, T01 saat ve T02 kaynak simülasyonu tamamlandı. Kullanıcı talebiyle
Unity 6000.3.28f1 sürümüne geçildi; gerçek Unity EditMode çalıştırmasında 18/18 test geçti.
Bu belge bir sonraki uygulayıcının sohbet geçmişine ihtiyaç duymadan başlayabilmesi
içindir. Kapsamı büyütmeden, küçük teslimlerle ilerleyin.

## 1. Çalışma düzeni

- Sol: planlama ve kullanıcı kontrol istediğinde bağımsız inceleme.
- Antigravity'de kullanıcının seçtiği Hydrogen: T03–T12 uygulaması ve görev doğrulaması.
- T00–T02'deki Terra görev sahipliği tamamlanmış işlerin tarihsel kaydıdır;
  aşağıdaki sonraki görevlerde uygulama sorumluluğu dış uygulayıcıya devredilir.
- Başlangıçta işler sırayla uygulanır. Aynı dosyaları iki ajan eşzamanlı değiştirmez.
- Planlayıcı yalnızca açıklama yazıp çekilmez; uygulamanın kabul koşullarını da kontrol eder.
- Ana modelin adı değişmeden alt ajanda başka model çalıştırılabilir; bu, kullanılan
  ortamın alt ajan/model seçimi desteğine bağlıdır. Bu sohbette ilgili araçlar mevcut.
- Bir plan dosyası kendi başına model seçmez veya alt ajan başlatmaz. Bunu kullanıcı
  seçimi ya da ana ajanın açık model seçerek yaptığı devir belirler.
- Bu sohbetin mevcut işi kalan oyunu planlamak ve dış uygulayıcı promptunu hazırlamaktır.
  Kod uygulaması bu devir promptu Antigravity'de verildiğinde orada sürdürülür.

## 2. Uygulayıcının her görevde yapacağı işler

1. Varsa geçerli AGENTS.md talimatlarını, GAME_PLAN.md ve bu dosyayı oku.
2. Git durumunu ve mevcut kodu incele; daha önce yapılan işi yeniden yazma.
3. Bağımlılıkları ve görevin dosya sınırlarını doğrula. Gerekli bir sözleşme değişikliği
   varsa bağımlı kodları birlikte güncelle veya koordinatöre bildir.
4. Yalnızca seçilen görevi uygula. Mevcut Unity sürümünü ve paketleri keyfi yükseltme.
5. İlgili derleme/test/oynama kontrolünü yap. Çalıştırılamayan kontrolü nedeniyle yaz;
   doğrulanmamış işi test geçti diye raporlama.
6. Bu dosyanın ilerleme tablosuna değişen dosyaları, doğrulama sonucunu ve kalan sorunu ekle.
7. Sonucu koordinatöre veya kullanıcıya sun. Açık talep yoksa commit/push yapma.

## 3. Görevler

### T00 — Unity ortamını doğrula

Sorumlu: Terra. Bağımlılık: yok.

- Editor yolunu ve 6000.3.28f1 sürümünü doğrula (kullanıcının onayladığı güncel sürüm);
  proje başka Editor işleminde açıksa
  ikinci süreçle aynı projeyi açma.
- Projenin paketlerini yükleyip yükleyemediğini ve başlangıç derlemesini kontrol et.
- Hedef platformun build modüllerini kontrol et; ilk raporda eksikleri belirt.
- Mevcut SampleScene'i koru. Bu aşamada mekanik, paket veya proje sürümü değiştirme.
- Sonuçları `docs/VALIDATION.md` dosyasına yaz. Komutları ve log yollarını ekle;
  anahtar, token veya başka hassas ortam değerlerini kaydetme.

Kabul: Editor/proje çalışabilirliği ve engeller somut kanıtla raporlanmış olmalı.
Çalıştırma engelliyse kaynak kodu üzerinde ilerlenebilen görevler devam edebilir;
gerçek Unity doğrulaması açık iş olarak kalır.

### T01 — Durum, yapılandırma ve tek saat

Sorumlu: Terra; Sol imzaları gözden geçirir. Bağımlılık: T00 raporu.
Dosyalar: `Assets/MarsOutpost/Runtime/Simulation/` temel tipleri,
`Assets/MarsOutpost/Tests/EditMode/` ilgili testler/assembly tanımları.

- Namespace: `MarsOutpost`. Configuration, scenario, crew, task, event ve mission
  durumlarının gerekli en küçük veri tiplerini oluştur.
- GAME_PLAN.md içindeki dış komut sözleşmesini sabitle; desteklenmeyen komutlar bu
  aşamada açıklamalı ret verebilir, sahte başarı dönmemeli.
- `MissionSimulation.Tick`, sol ve gece hesabı, senaryo başlangıcı ve Restart ekle.
- Domain mantığında MonoBehaviour, sahne nesnesi, DateTime.Now veya ayrı zamanlayıcı kullanma.
- Testler: sol sınırı, sıfır delta, geçersiz negatif delta, yeniden başlatma,
  tek büyük adım ile küçük adımların aynı takvim sonucunu üretmesi.

Kabul: sabit konfigürasyonda zaman sonuçları tekrarlanabilir; temel testler geçer.

### T02 — Kaynaklar ve otomatik üretim/tüketim

Sorumlu: Terra. Bağımlılık: T01.
Dosyalar: Simulation kaynak mantığı, konfigürasyon ve ilgili EditMode testleri.

- Gündüz paneli, batarya, oksijen üretimi, su/atık su geri kazanımı ve araştırma
  tüketimini ekle. Araştırma için ekip ataması T03'te bağlanır.
- Girdi/kapasite ve enerji önceliklerini uygula; tüm oranları ayarlanabilir tut.
- Modülün girdi bittiği anı hesaba kat; kapasite kırpmasını üretimin yerine kullanma.
- Testler: gece panel üretmez; sıfır girdide üretim olmaz; atık sudan fazla su
  kazanılamaz; yaşam desteği araştırmadan önce enerji alır; uyku da tüketim yapar;
  kaynak tükenme sınırında büyük/küçük adım eşdeğerliği tolerans içinde korunur.

Kabul: sahnesiz bir simülasyonda rezervler makul ve nedensel değişir; negatif değer yok.

### T03 — Ekip, görev atama ve stamina

Sorumlu: Terra. Bağımlılık: T02.
Dosyalar: Simulation ekip/görev mantığı, konfigürasyon ve ilgili testler.

- Üç astronot, tek aktif görev, görev noktası sahipliği, süre/ilerleme ve stamina ekle.
- Dinlenme, içeri dönme ve bitkinlik durumlarını bağla.
- Araştırma ilerlemesini hem aktif astronota hem ayrılmış enerjiye bağla.
- Panel bakımının atmosferik tozu silmesini engelle; panel kirlenmesini ayrı tut.
- Testler: iki görevi aynı kişiye verememe; iki kişinin tek görev noktasını
  paylaşamaması; stamina 0'da işin durması; dönüşün 0 stamina ile tamamlanması;
  iptal/yeniden atamada tek seferlik ödülün çoğalmaması; iş yapılmazken ödül üretilmemesi.

Kabul: “görev yap → yorul → dinlen → devam et” döngüsü sahnesiz çalışır.

### T04 — Olaylar, korunak ve görev sonucu

Sorumlu: Terra. Bağımlılık: T03.
Dosyalar: Simulation olay/sonuç mantığı, senaryo tanımı ve ilgili testler.

- Uyarı/başlangıç/bitiş zamanlarını tek saatten işlet; radyasyon ile tozu ayır.
- İç/dış konuma göre risk puanı, tozda panel üretimi kaybı, oksijen eksikliği süresi
  ve görev başarı/yeniden deneme koşullarını ekle.
- Testler: 5 dakikalık uyarı; duraklatılınca olayın ilerlememesi; 10 saniyelik dönüşte
  dış maruziyetin sürmesi; olayın bir kez başlaması/bitmesi; büyük adımın olayı
  atlamaması; oksijen sağlanınca kesintisiz eksiklik sayacının sıfırlanması;
  terminal durumda saatin donması; yeniden başlatmada olayların temizlenmesi.

Kabul: aynı senaryo aynı kararlarla aynı sonuca ulaşır; risk ve kaynak nedenleri görünürdür.

### T05 — Gece uykusu ve sol raporu

Sorumlu: Terra; Sol özellikle zaman sınırlarını inceler. Bağımlılık: T04.
Dosyalar: Simulation uyku/rapor, ilgili konfigürasyon ve testler.

- Uyku başlangıç koşulları, kesme, kaynak uyarısında uyanma ve sonraki sol raporunu ekle.
- Simulation, uyku/hız önerisini durumunda bildirir; zaman çarpanını uygulayacak tek
  yer T06'daki runner'dır. Domain içinde ikinci bir 8x çarpanı kullanma.
- Sol değişiminde stamina yenileme; yalnızca gerçekten uyunan/dinlenilen süreden hesapla.
- Testler: gündüz uyku reddi; dışarıda ekip varken uyku reddi; gece radyasyonunun
  işlenmesi; uyku iptalinde kısmi toparlanma; düşük oksijende tek uyarı;
  normal hızda geceyi geçirmekle hızlı uykunun aynı simülasyon sonucunu vermesi;
  sol sınırının tek rapor oluşturması; 2. sol sonunda doğru görev sonucu.

Kabul: uyku olayları atlamaz, ücretsiz stamina yaratmaz, yeni sol sınırında durur.

### T06 — Oynanabilir Unity sahnesi

Sorumlu: Terra. Bağımlılık: T05.
Dosyalar: Presentation, Editor, Scenes; gerekli Data varlıkları;
gerekirse EditorBuildSettings ve Input System uyumlu giriş kodu.

- Tek SimulationRunner ve tek sahipli durum kur; delta accumulator ile sabit adımları işlet.
- Kare başına işlem sınırı kullanılırsa kalan simülasyon süresini düşürme; biriktir.
- Duraklatma, odak kaybı ve uyku hızını aynı runner üzerinden yönet.
- Geçici şekillerle üs ve görev noktalarını göster; üç astronot seçimi ve görev
  komutları ekle. Hareket Input System ile uyumlu olsun; yol bulma ekleme.
- UI için mevcut uGUI kullanılabilir; renk yanında metin ve kritik komut geri bildirimi ekle.
- Sahne elle bağlanacaksa kesin adımları yaz; tercihen proje kapsamındaki sahneyi
  kuran tekrar çalıştırılabilir küçük bir Editor menüsü oluştur. Kullanıcı sahnelerini silme.
- Oluşan varlıkların .meta dosyalarını ve sahne referanslarını koru.

Kabul: Unity Play içinde seçim, görev, stamina, kaynak, geri sayım, duraklatma,
uyku, rapor, başarı/kayıp ve yeniden başlatma tek sahnede çalışır.
UI aç/kapa tekrarlarında komut veya abonelik çoğalmaz. Somut derleme kontrolü ve
kısa oynama kontrolü `docs/VALIDATION.md` içine kaydedilir.

### T07 — Metin, gösterge ve kullanıcı denemesi

Sorumlu: Luna, sabit T06 sözleşmelerine göre. Bağımlılık: T06.
Dosyalar: merkezi UI metinleri, HUD sunumu, `docs/PLAYTEST.md`.

- Sade İngilizce görev/uyarı metinleri, risk puanı açıklaması ve kısa ilk kullanım
  öğreticisi ekle; bilimsel iddialar uydurma.
- Model ve zaman mantığını değiştirme. Gerekirse düzeltme ihtiyacını bildir.
- Az sayıda kullanıcıyla deneme için gözlem listesi hazırla: görev verebiliyor mu,
  neden yorulduğunu anlıyor mu, uyarıyı görüyor mu, uyku sırasında tüketimi fark ediyor mu?
- Gerçek deneme yapılmadıysa yapılmış gibi sonuç yazma.

Kabul: metin taşması/okunabilirlik kontrol edilir; arayüz terimleri tutarlıdır.

### T08 — NASA kaynağı ve yarışmaya uygunluk

Sorumlu: Terra; Sol kaynak doğruluğunu inceler. Bağımlılık: T07.
Dosyalar: Data senaryosu/kaynak açıklaması, Sources UI, `docs/SCIENCE_SOURCES.md`.

- Gerçek bir NASA kaydını açıp doğrula; kayıt tarihi/kimliği, kaynak URL'si, alınan
  alanlar ve oyun mekaniğine dönüşümünü yaz. Arşiv olay paketini tercih et; API zorunlu değil.
- Oyun senaryosu zamanı ve tasarlanmış risk katsayısı ile ölçülmüş veriyi ayır.
- Seçilen olayın uygunluğuna göre senaryo ayarlarını güncelle; DONKI verisini doğrudan
  Mars doz ölçümü gibi sunma. Temsili eğitim senaryosu olduğunu açıkça belirt.
- Gıda ve iletişim gecikmesi hedeflerini yeniden değerlendir: küçük görevlerle
  kapsanacak mı, sonraki sürüme mi kalacak? Kalan kapsamı proje açıklamasında dürüstçe yaz.
- Nihai platform build doğrulaması güncel planda T11'e taşınmıştır.

Kabul: NASA kaynağı mekanikte izlenebilir bir etki yapar; kaynaklar erişilebilir;
ölçülmüş veri ile tasarlanmış model ayırt edilir.

### T09 — Gıda hazırlama ve gecikmeli tavsiye

Bağımlılık: T08. MASTER_PLAN.md bölüm D'deki günlük öğün ve gecikmeli mesaj
kurallarını uygula. Kabul: görev maliyetleri, bir kez/sol öğün etkisi, uyku katsayısı,
mesaj gecikmesi ve restart testlerle doğrulanır. Yeni kaynak barı/harici LLM gerekmez.

### T10 — Görsel ve ses düzeni

Bağımlılık: T09. MASTER_PLAN.md bölüm E'deki okunaklı tek stil ve temel sesleri
uygula. Kabul: ekranlar gerçek sahnede incelenir, ses kapatılabilir, asset kaynakları kaydedilir.

### T11 — Build ve uçtan uca kontrol

Bağımlılık: T10. Gerçek masaüstü build, mümkünse WebGL, iki sol ve 90 sn demo
senaryosunu doğrula. Kabul: build yolu, test XML, oynama kanıtları ve açık eksikler kaydedilir.

### T12 — Teslim ve yarışma anlatımı

Bağımlılık: T11. README, kaynak/lisans kaydı, İngilizce proje açıklaması ve demo
taslağını hazırla. Kabul: başka kişi çalıştırabilir, güncel teslim kuralları doğrulanır,
üretilmeyen artefaktlar hazır sayılmaz. Başvuru gönderimi/yayınlama yapılmaz.

## 4. Son inceleme

Sol şu riskleri kontrol eder: iki ayrı saat, UI'dan durum değişimi, uykuda olay
atlama, gece kaynaklarının durması, sol başında bedava stamina, çift görev ödülü,
sıfır girdiden üretim, pause/focus hataları ve yeniden başlatmada kalan abonelikler.
Kontroller yeni sorun göstermedikçe tüm testleri tekrar tekrar genişletmeyin.

## 5. İlerleme kaydı

| Görev | Durum | Kanıt / kalan iş |
|---|---|---|
| T00 | Tamamlandı; hedef build sonraki aşamada | Kurulu 6000.3.28f1'e kullanıcı talebiyle geçildi. Lisans bağlantısı, paket çözümleme, ilk import ve Editor derlemesi geçti. WebGL modülü yok; hedef build çalıştırılmadı. |
| T01 | Uygulandı ve Unity'de doğrulandı | Terra Medium uyguladı, Sol inceledi; mevcut 7 test 6000.3.28f1 EditMode çalıştırmasında geçti. |
| T02 | Uygulandı ve Unity'de doğrulandı | Terra Medium kaynak modeli ve 11 kaynak testi ekledi. Sol incelemesi sonrası kesirli zaman sınırı hatası düzeltildi. Toplam 18/18 Unity EditMode testi geçti; 50 ayarlı zaman bölme regresyonu dahil. Kanıt: `docs/validation/t02-editmode-results.xml`. |
| T03 | Uygulandı ve Unity'de doğrulandı | Ekip durumu (`CrewStatus`, `CrewLocation`), tek görev noktası sahipliği, stamina entegrasyonu, `AssignTask`/`CancelTask`/`Rest`/`ReturnToShelter`, kalan enerjiye orantılı araştırma ve bakım etkileri eklendi. 26/26 Unity EditMode testi geçti (8 yeni T03 testi dahil). Kanıt: `docs/validation/t03-editmode-results.xml`. |
| T04 | Uygulandı ve Unity'de doğrulandı | `EventState` (`Radiation`/`Dust`, uyarı/başlangıç/bitiş sınırları), iç/dış radyasyon risk puanı, tozda güneş kaybı (bakımdan ayrı), kesintisiz oksijen eksikliği sayacı ve toparlanmada sıfırlanması, Sol 2 sonu başarı/kayıp ve terminal dondurma eklendi. 32/32 Unity EditMode testi geçti (`docs/validation/t04-editmode-results.xml`). |
| T05 | Uygulandı ve Unity'de doğrulandı | Gece uykusu (`BeginSleep`/`CancelSleep`), yalnızca gece ve tüm ekip içerideyken başlama, `RecommendedTimeScale` (`8.0`/`1.0`), kritik oksijen uyandırması ve ayrı duraklatma nedenleri (`AcknowledgeSolReport`/`AcknowledgeCriticalAlert`), sol sınırı raporu (`SolReport`) ve ücretsiz stamina üretilmemesi eklendi. 38/38 Unity EditMode testi geçti (`docs/validation/t05-editmode-results.xml`). |
| T06 | Uygulandı ve Unity EditMode + PlayMode'da doğrulandı | `SimulationStepper`, `SimulationRunner`, `MarsOutpostWorldView`, `MarsOutpostHud`, `OutpostStrings` ve `MarsOutpostSceneBuilder` eklendi. `Assets/MarsOutpost/Scenes/MarsOutpost.unity` oluşturuldu (`SampleScene.unity` korundu). 42/42 EditMode (`t06-editmode-results.xml`) ve 1/1 PlayMode (`t06-playmode-results.xml`) geçti; başsız `-nographics` çalıştırma olduğu için görsel ekran görüntüsü incelemesi T10/T11'de gerçek grafik çıktısıyla tamamlanacak. |
| T07 | Uygulandı ve PlayMode'da doğrulandı | 3 adımlı etkileşimli öğretici şerit (`seç → görev ver → uyarılarda korun`), merkezî `OutpostStrings` metinleri, eğitim risk puanı açıklaması, neden–sonuç rezerv özeti, iki kararlı sonuç ekranı ve `docs/PLAYTEST.md` eklendi. PlayMode testi geçti (`docs/validation/t07-playmode-results.xml`). Canlı kullanıcı denemesi önerilen açık iş olarak ayrıldı. |
| T08 | Uygulandı ve Unity EditMode + PlayMode'da doğrulandı | Gerçek NASA DONKI SEP kayıtları (`2024-05-09T13:59:00-SEP-001` / ACE EPAM ve `2024-05-10T13:35:00-SEP-001` / GOES-P SEISS), NASA ECLSS referansı ve 2018 Mars küresel toz fırtınası bağlamı `NasaScienceData.cs`, `MissionSimulation.cs` (`SourceRecordId`/`SourceNote`), HUD Sources modalı ve `docs/SCIENCE_SOURCES.md` içine işlendi; ölçülmüş veri ile tasarlanmış oyun senaryosu ayrıldı. EditMode **43/43** (`t08-editmode-results.xml`) ve PlayMode **1/1** (`t08-playmode-results.xml`) geçti. |
| T09 | Uygulandı ve Unity EditMode + PlayMode'da doğrulandı | Günlük hazır yemek stoğu (`InitialFoodRations=2`, gecede 1 kez düşüş), 30 sn iç mekân öğün hazırlığı (`-0.10/s` stamina) ve hazırlık yoksa `0.75x` gece uyku katsayısı (tekrar uykuya girilerek çoğaltılamaz, Sol 2'de sıfırlanır) ile tek kullanımlık 45 sn sıkıştırılmış gecikmeli Dünya tavsiyesi (pause'da durur, 8x uykuda ilerler, geçmiş olayları zaman damgasıyla açıklar) eklendi; yeni kaynak barı eklenmedi. EditMode **47/47** (`t09-editmode-results.xml`) ve PlayMode **1/1** (`t09-playmode-results.xml`) geçti. |
| T10 | Uygulandı ve Unity + gerçek masaüstü çıktısında doğrulandı | Sıcak Mars paleti, `Assets/MarsOutpost/Art/` altında proje içi üretilmiş 7 özel 2D PNG sprite (`mars_terrain.png`, `habitat_shelter.png`, `station_lifesupport.png`, `station_galley.png`, `station_lab.png`, `solar_array.png`, `astronaut_suit.png` — prosedürel yedekle birlikte `MarsOutpostWorldView.cs` ve `MarsOutpostSceneBuilder.cs` üzerinden bağlı), `GALLEY` ve hava kilidi sahne görselleri, astronot vizör/durum ayrımları, aynı `MissionSimulation` saatini kullanan `90s Demo` modu (`SimulationRunner.InitializeDemoMission`) ve harici ücretli asset kullanmayan prosedürel `MarsOutpostAudio` (görev tamamlandı, uyarı/O2/iletişim, hata sesleri; Mute + Vol -/+) eklendi. İlk grafik ekran görüntüsü incelemesinde yakalanan 5 etiket/panel taşması düzeltildi (`O2 / SYS`, `GALLEY`, `LAB`, `EventCard` üst sınırı, `Return (10s)`, `Research: ON`). |
| T11 | Masaüstü build alındı ve uçtan uca doğrulandı (WebGL eksik) | Özel 2D sprite asset'leri içeri alınıp `Build/Linux/MarsOutpost.x86_64` yeniden derlendi ve X11 `DISPLAY=:0` üzerinde hem `1280x720` (`docs/validation/screenshot-1280x720.png`) hem `1920x1080` (`docs/validation/screenshot-1920x1080.png`) gerçek çözünürlüklerde başlatılıp çakışma/kırpmasız görsel olarak doğrulandı. Son düzenleme sonrası Final EditMode **47/47** (`t11-editmode-results.xml`) ve PlayMode **2/2** (`t11-playmode-results.xml`, tam 90 sn demo akışı, ses, Sources, pause ve restart dahil) geçti. Kurulu Unity `6000.3.28f1` içinde WebGL modülü bulunmadığı için WebGL build alınamadı ve açıkça eksiklik olarak bırakıldı. |
| T12 | Hazır (yarışma gönderimi hariç) | Kök `README.md` (Unity 6000.3.28f1, sahne, kontroller, build/test komutları, lisans notu ve sınırlamalar), `docs/DEMO_SCRIPT.md` (resmî `spaceappschallenge.org` 7 slayt ve 30 sn video kuralları doğrulanarak taslaklar), `docs/PROJECT_SUBMISSION_EN.md` (İngilizce proje açıklaması) ve son `AGENT_REPORT.md` raporu hazırlandı. Video render/yükleme, lisans dosya seçimi, canlı çocuk playtesti ve başvuru gönderimi kullanıcıya bırakıldı. |

## 6. Güncel devir ve kontrol

Antigravity'ye verilecek tam prompt `ANTIGRAVITY_PROMPT.md` içindedir.
Dış uygulayıcı, `MASTER_PLAN.md` sırasıyla T03–T12'yi yürütür.
Kullanıcı T03, T05, T06, T09 veya T12 sonunda bu sohbete kontrol talebi yazabilir.
Sol otomatik izleme yapmaz; aynı checkout'a eriştiğinde raporla yetinmeden kodu
ve kanıtları inceler. Kullanıcı yalnızca en sonda dönebilir.
