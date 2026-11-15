# V3 özellik sözleşmeleri

Bu sayıların tümü başlangıç **oyun dengesi** önerisidir. V2'nin kullanıcıca
belirlenen olay süre/yüzde/maliyetleri sabittir. Diğer değerler son denge
checkpoint'lerinde gerekçeli ölçümle değiştirilebilir; değişikliği rapora yaz.

## F01 — Grid, arazi ve tahsis

1. GridCoord yalnız int X/Z içerir; eşitlik/hash deterministiktir. Hücreler yarı
   açık aralıklardır; negatif koordinat dönüşümünde truncation yerine floor kullan.
   World↔cell gidiş-dönüş, sınır ve harita dışı kontrolleri gerekir.
2. 16×16 hücrede arazi türü, geçilebilirlik, oyun güneş katsayısı, keşif/tarama
   durumu, tahsis ve kaynak bilgisi bulunur. MapSeed ve sabit stream kimliği
   hava olaylarının beş RNG akışından ayrıdır. Renk/efekt için RNG çekmek veriyi değiştirmez.
3. Başlangıç üs, hava kilidi, eski panel dizisi ve yürüyüş/çıkış koridorlarının
   gerçek footprint'lerini mevcut sahneyle eşleştir. Onların üstüne yapı kurulmaz.
   Habitat merkezi (-2,0,0), hava kilidi ve mevcut solar konumu koddan doğrulanır.
4. Başlangıç tesisleri ve aralarındaki koridor tahsislidir. Yeni tahsis yalnız
   keşfedilmiş, sınır içi, dört komşusundan biri tahsisli hücreye yapılır.
   Tahsis bedeli 2 malzeme; aynı hücreye ikinci tık ödeme yapmaz.
5. İlk A tesliminde üs çevresindeki en az iki uygun buz parseli başlangıçta
   taranmış/açık olur. Başlangıç erişimi tüm seed'lerde garantili ve kaydedilmiştir.
   Sonraki keşif teslimine gelmeden su tesisini denemek mümkün olmalı.
6. Genel map data oyuncuya gerçek NASA yeraltı ölçümü gibi gösterilmez.
   Önerilen SolarFactor 0,65–1; başlangıç panellerinde tam 1 (V2 dengesi korunur).
7. Geçilebilirlik, tesis footprint'i ve kablo katmanı ayrıdır. Bir tesisten en çok
   bir adet; bağlantı hattı aynı hücrede ayrıca bulunabilir. Dört yönlü hatlar
   çaprazdan bağlanmaz. Blueprint de footprint'i rezerve eder.
8. Oynanış sınırı 256 parsel, en çok 48 oyuncu tesisi, toplam en çok 12 panel,
   4 yeni çıkarıcı, 1 ek habitat ve 1 rover. Bunlar config'de sınırdır; yeni framework değil.

## F02 — Malzeme ve yapı kataloğu

Malzeme yeni tek envanterdir: başlangıç 40, kapasite 80, her Sol başında +8'e
kadar rutin senaryo lojistiği. Sol marker'ı saklanır; yükleme/raporu açma ikinci
teslimat oluşturmaz. Bu gerçek günlük Dünya inişi diye anlatılmaz. Kendi maden
ekonomisini bu sürüme ekleme. Bilim ve toplam araştırma bu maliyetlerden etkilenmez.

| Yapı | Footprint | Malzeme | Net iş süresi | İş enerjisi toplam | Çalışınca etkisi |
|---|---|---:|---:|---:|---|
| Bağlantı hattı | 1 kablo katmanı | 1 | 5 sn | 0 | Üs enerji/su bağlantısı |
| Solar panel | 1×1 | 12 | 45 sn | 2 | SolarRate/3 × yerel katsayı |
| Batarya | 1×1 | 16 | 45 sn | 2 | +25 erişilebilir enerji kapasitesi; başlangıç doluluğu 0 |
| Buz su çıkarıcı | 1×1 | 18 | 60 sn | 3 | 0,012 su/sn × verim; 1,5 enerji/su |
| Regolit fırını | 1×1 | 22 | 75 sn | 4 | 0,006 su/sn × verim; 4 enerji/su |
| Su tankı | 1×1 | 10 | 40 sn | 1 | +50 su kapasitesi; su üretmez |
| Ek sığınak | 2×2 | 24 | 90 sn | 5 | 2 ek dinlenme yeri, aşağıdaki korunma kuralı |
| Rover dok + tek araç | 2×1 | 20 | 75 sn | 4 | Tek rover ve şarj noktası; yeni araç boş bataryalı |

Yeni yapı türü ilgili checkpoint doğrulanmadan oyuncu kataloğunda açılmaz.
Eski üç panelli dizi ve başlangıç çıkarıcısı ücretsiz yeniden spawn edilemez.
Tesis sayısı/hattın maliyeti UI'da görünür. Süre "net çalışma"dır; yol süresi eklenir.

## F03 — Önizleme, inşaat ve söküm

1. Önizleme dünya hücresine oturan yarı saydam modeldir; yeşil geçerli/kırmızı
   gerekçeli geçersiz. UI üstüne tık, sürükleyerek kamera hareketi ve Escape oyun
   komutu sayılmaz. Ghost iptali para harcamaz.
2. Yerleştir komutu tüm koşulları doğrular: mod, tahsis, keşif, tür/zemin, kapasite,
   malzeme, footprint, erişilebilir çalışma komşusu, üs dönüş koridoru. Ret atomiktir.
   Kabulde tek ID ve tek malzeme ödemesi; Blueprint kaydedilir, üretim vermez.
3. Bir astronot bir işe atanır. Ulaşılabilir komşu çalışma hücresine gerçekten
   yürür, sonra çalışma ilerler. Net işte stamina -0,20/sn; hareket -0,10/sn.
   Yetersiz iş enerjisinde ilerleme orantılı durur/yavaşlar; ücretsiz tamamlanmaz.
4. Lifecycle: Blueprint → Travelling/Working (görev aşamaları) → Commissioned.
   Astronotu durdurma işi/enerji harcamasını bırakır; inşaat ve ödenen ilerleme
   kalır, tekrar atama kaldığından devam eder. Ekip işi bırakabilir ve sığınağa dönebilir.
5. Her tesis en çok bir kez hizmete girer. Tamamlama/görev geçişi büyük Tick ve
   küçük Tick'te aynıdır. Aynı astronot aynı anda EarthOrder/Research/inşaat yapmaz.
6. Yerleştirilmiş Blueprint'i iptal etmek malzeme iade etmez; bu bilgi yerleştirme
   ve iptal UI'sında açık yazılır. Harcanmış iş enerjisi de geri verilmez.
7. Bitmiş oyuncu tesisini sökmek ilk ödenen malzemenin floor(%50)'sini kapasiteye
   kadar bir defa verir. Başlangıç/korunan tesis sökülemez; arızanın aktif hedefi,
   çalışılan/üzerinde ekip olan tesis ve sığınakta kişi varken söküm reddedilir.
8. Söküm/hat kopması kapasiteyi düşürürse erişilebilir ortak rezerv yeni kapasiteye
   kırpılır; fark EnergySpilled/WaterSpilled olarak kaydedilip UI'da önce gösterilir.
   Sonradan tekrar bağlama rezervi doldurmaz. Gizli kaynak yaratma yok.
9. Söküm, yerleştirme veya iptal bir astronot/roverın üsten geri dönüş yolunu
   kapatamaz. Komut öncesi rota kontrolü yapılır. Oyuncuyu duvarın içinde doğurma yok.

## F04 — Yüzey işleri, yol ve ortak görev sahipliği

1. Basit 4 komşulu A*/BFS; eşit maliyette sabit N/E/S/W sırası. Rota yoksa ret.
   Görsel mesh/Unity NavMesh simülasyon saatinin veya konumunun otoritesi değildir.
2. Yeni SurfaceJob içinde hedef parsel/tesis, ekip ID, rota, segment ilerlemesi,
   Travel/Work/Return aşaması ve kalan net iş saklanır. Konum/rota double değerlerle
   simülasyonda ilerler; önerilen yürüme 2 oyun birimi/sn.
3. Yeni uzak yüzey işinden dönüş rota uzunluğu/hız + hava kilidi geçişine bağlıdır.
   Eski Training/Demo ReturnToShelterSeconds sözleşmesini sessizce değiştirme.
   V2 eski görevleri için mevcut 10sn/4sn dönüş regresyonları korunur.
4. Returning konumuna aynı karede iki sistem yazamaz. Domain kalan zamanı ve
   pozisyonu üretir, WorldView önceki/güncel örnekler arasında interpole eder.
   Eski Returning azaltması ile yeni yol süresi iki kez sayılmaz.
5. Cancel/Replace/Rest/Return/BeginSleep/EarthOrder bütün yeni iş referanslarını
   doğru serbest bırakır. Red mevcut işi bozmaz. Dışarıda dinlenme/sleep ile içe
   ışınlanma yok; dış ekip uyku öncesi dönmelidir. Exhaustion işi bırakır, ilerleme korunur.
6. Yeni işlerin tümü mevcut radyasyon riskini taşır. Uyarı geldi diye otomatik
   yol kestirip güvene ışınlanılmaz; geri çağırma komutu vardır. Uzun mesafe uyarısı
   rota süresine dayanır; farklı NASA radyo süresi iddiası yoktur.
7. Pause/menü/odak/kritik uyarıda seyahat, iş, maliyet ve efekt durur. Uykudaki
   simülasyon ölçeği aynı sınırları işler. 15/30/60 FPS, 1x/8x, iptal/yeniden atama test edilir.

## F05 — Hat ve bağlı tesis

Üs bağlantı portu ve değişmeyen başlangıç altyapısı hazır çekirdek ağdır. Her yeni
tesis, footprint'ine gelen veya dört yönden bitişik bağlantı hattı çekirdek ağa
ulaşıyorsa bağlıdır. Bitmemiş hat ile çapraz temas bağlantı sağlamaz. Hat aynı
oyun altyapısında elektrik ve su taşıyan yalıtımlı hizmet hattıdır; ayrı akışkan
basınç simülasyonu yoktur. Yol görseliyle ağ bağı aynı doğrulanan grid'den gelir.

Bağlantısız soların üssü beslemesi, çıkarıcının tanka su göndermesi ve bataryanın
kapasite eklemesi 0'dır. Yerel izole mikro şebeke yoktur; UI nedenini söyler.
Hat değişiminde ağ deterministik yeniden hesaplanır. Başlangıç üç panel ve su
çıkarıcısı V2 rezerv dengesini korumak için her zaman çekirdek ağa bağlıdır.

## F06 — Yeni solar ve arızalar

`Solar = day ? SolarRate × Σ(healthyOnlineConnectedPanel.Weight × SolarFactor)
× (1 - PanelFouling) × (1 - AtmosphericDustLoss) : 0`.

İlk üç panelin Weight'i 1/3 ve SolarFactor'ü 1'dir. Her yeni panel 1/3 ekler;
liste uzayınca hepsini yeniden 1/N yapma ve toplamı 1'e clamp etme. Yarım inşaat
üretmez. Panel sayacı 3 sabitine değil canlı listeye dayanır; starter üçlü aynı
görsel tesisin alt birimleri olabilir, yeni panelin ayrı tesis/ID/konumu vardır.

V2 solar kanalı aynı aralıkta bir uygun sağlıklı hizmette paneli arızalandırır;
hedef `ActiveTargetId` ile sabitlenir. Yeni panel kurmak RNG'yi yeniden başlatmaz.
Yeni hedefin tamiri o paneli düzeltir; bütün listeyi sağlıklı yapan eski döngüyü
kullanma. Tek kanalda ikinci arıza yığılmaz. Tamir durdurulunca doğru hedefin
ilerlemesi kalır. Genel panel temizliği mevcut global fouling bakımını korur;
arıza tamiri, temizleme ve kumun bitişi birbirinin durumunu silmez.

## F07 — Enerji, depolar ve görünür kapasite

1. Temel batarya kapasitesi config'deki 100; +25 sadece bağlı tamamlanmış batarya
   başına. Yeni kapasite boş eklenir. Küresel ortak havuz seçilmiştir; ayrı batarya
   hücreleri arasında fiziksel dengeleme simülasyonu yoktur.
2. Resource integratörünün Limit/Apply/SnapBoundary çağrıları, rapor ve HUD
   artık etkin BatteryCapacity/WaterCapacity kullanır. Stamina/risk 100 kuralları
   değiştirilmez; dosyada bütün 100'leri topluca replace etme.
3. Üretim/kullanım/taşma defteri sınır geçişinde korunur. Kaynak kapasitesi
   büyüdü diye ücretsiz enerji/su verilemez. Hata veya ret depoyu azaltamaz.
4. Güç sırası: O₂ → geri kazanım → başlangıç çıkarıcısı → yeni çıkarıcılar
   (sabit ID sırası) → O₂ onarımı → araştırma → Dünya deneyleri → inşaat/tarama →
   rover şarjı. Mevcut önceliklerde sessiz değişim yapma. Kalan güçte orantılı iş.
5. Aynı t=... anındaki sıra: geçici tehlike bitişi → yeni olay/arıza → rota varışı
   ve mevcut topoloji ile akışlar → iş maliyeti/tamamlama → topoloji/etkin kapasite
   yenileme → güvenlik → Sol raporu/lojistik/Dünya varışı. Tam bu sınır test edilir.

## F08 — Yeraltı suyu ve iki çıkarma yöntemi

1. Başlangıç çıkarıcısı mevcut sınırsız erişilebilir rezerv senaryosudur;
   `legacy-extractor` ile tek kaynaktır. Hem eski scalar hem yeni liste üzerinden
   iki kez üretilemez. Yenileri ayrı sitedir; eski oran korunur.
2. Yeni parsellerde gizli kaynak modeli: Ice / HydratedRegolith / None,
   YieldFactor, RemainingWater (öneri uygun sahada 250–600 oyun su birimi).
   Tür ve stok map seed'i ile oluşturulur, save'de korunur. Derinlik/saflık için
   gerçeğe ait uydurma metre veya yüzde gösterme; erişim/kalite sınıfı yeterli.
3. Buz çıkarıcı yalnız tahsisli/taranmış Ice hücresinde kurulabilir. Regolit fırını
   yalnız taranmış HydratedRegolith hücresinde. Uygun olmayan yerde kesin gerekçeli ret.
4. İnşaat/kurulum tamamlanınca tesis tipi görselinde sondaj/ısınma, çalışma,
   durma ve rezerv bitişi ayrılır. Kurulum enerjisi zaten F02'deki toplamdır;
   aynı commissioning işi için ikinci gizli ücret alma.
5. Fiili üretim bağlı/enabled/sağlıklı durum, enerji, yerel stok ve tank boşluğu
   ile sınırlıdır. Alınan su tam stoktan düşer. Tanka giremeyen su için stok ve
   enerji tüketilmez. Büyük Tick rezerv tükenmesi ve tank dolumunda kırpılır.
6. `WaterExtractorEnabled` ana aç/kapat; tesisin ayrıca Enabled anahtarı olabilir.
   `WaterExtractorFaulted` bütün çıkarma ağına ×0,50 servis düşüşüdür; yeni
   makineler bunu bypass etmez. V2 su arızası 10 bilimle ağı bir kez düzeltir;
   sahadaki su stokunu artırmaz. HUD bu ortak servis durumunu açık söyler.
7. Regolit fırınının daha yüksek enerji/su oranı bir oyun tercihidir; her Mars
   toprağından su çıkacağı veya doğrulanmış gerçek tesis debisi iddiası yoktur.
8. Ek tank yalnız kapasite ekler. Söküm/hat kaybı fazlası F03 defterine gider.
   Geri kazanım ve O₂'nin su maliyeti ayrı korunur; negatif stok yoktur.

## F09 — Keşif, tarama ve mini harita

- Keşif: başlangıçta üs çevresi ve starter koridorlar açık. Oyuncu görünen
  sınırın komşu bilinmeyen hücresine Explore işi yollar. Ekip fiziksel olarak
  ulaştıkça komşu görünür alan kalıcı keşfedilir. Keşif kuyruğu çizmek anında sis kaldırmaz.
- Tarama: keşfedilmiş parselde 30sn dış iş, toplam 2 enerji, stamina -0,20/sn.
  Keşif ve tarama ayrı; tahsis tarama için zorunlu değildir, inşaat için zorunludur.
  Aynı tamamlanan tarama yeniden maliyet/bilim doğurmaz. Tarama bilim vermez.
- Görsel arazi mevcut kalır; bilinmeyen parseller koyu overlay ile işaretlenir.
  Sis HUD/metni kapatmaz; kullanıcıya henüz bilinmeyen stok miktarı gösterilmez.
- Mini haritada üs, ekip, hat, yapılar, rover ve bilinen su yerleri vardır.
  Tıklamak kamera odağını değiştirir; yanlışlıkla yeni tesis komutu vermez.
- Gece/kum görüşü geçici azaltabilir ama kalıcı keşif geçmişini silmez.
  İlk uygulamada kalıcı keşif + aktif görüş ayrımını basit tut; karmaşık gerçek
  ışın görüşü/FOV geometrisi zorunlu değildir. Dört komşulu keşif yeterlidir.

## F10 — Ek sığınak

Bir 2×2 bağlı ve hizmete girmiş modül; iki ek dinlenme anchor'ı ve kontrollü
giriş/çıkış. Üç astronot aynı kalır; ekip kartı/crew ID çoğaltılmaz. Oyuncu dinlenme
yeri seçebilir; dolu anchor'a ikinci ekip atanmaz. Söküm içeride biri varken reddedilir.
Aktif ek sığınak içindeki kişinin akut eğitim risk artışına 0,75 oyun katsayısı
uygulanır; dışarıdaki kişi etkilenmez. Bu gerçek doz veya sertifikalı radyasyon
koruması değildir. Stamina toparlanması V2 oranıdır; gıda/öğün bedava artmaz.
Yeni iç hacim WorldView'in korumalı hacim listesine katılır; radyasyon/toz içeride
çizilmez. Uykunun yatış/kalkış, isim/halka ve pause davranışları yeni anchor'da da geçer.

## F11 — Rover ve örnek döngüsü

1. En fazla bir dok/rover paketi. Tamamlanınca tek kalıcı rover ID'si ve boş
   20 kapasiteli araç bataryası olur. Yeniden yükleme/tamamlama ikinci araç yaratmaz.
   Dok bağlıyken kalan üs gücünden en fazla 0,02 enerji/sn şarj eder; harcanan üs
   enerjisiyle araç enerjisi aynı miktardır. Kapasitede şarj durur.
2. Araç aynı nav grid'inde hareket eder; hız başlangıç 2 oyun birimi/sn, hareket
   tüketimi 0,02 araç enerji/sn. Durunca tüketim 0. Hedef/ev rotası ve kalan şarj
   önizlenir; dönüş bütçesi +%10 pay yoksa görev reddedilir. Yolda rota değiştiyse
   yeniden bütçele. Şarj biterse araç durur; ışınlanarak dönmez veya kendiliğinden dolmaz.
3. Tek kontrol: seç → hedef Explore/Sample → gönder/geri çağır. Simülasyon rota,
   enerji, kargo, aşama ve zaman sahibidir. Kamera takip isteğe bağlıdır.
   Radyo komutuna gerçek Dünya gecikmesi ekleme; yerel üs kontrolü olarak etiketle.
4. Örnek: uygun keşfedilmiş parselde 30sn toplama; bir kargo slotu; ev/doka
   fiziksel dönüş; sonra serbest laboratuvar ve bir astronotla 30sn analiz.
5. `SampleId`, kaynak parseli ve `SampleOrigin` saklanır. **Exploration** örneği
   ilk başarılı analizinde 5 ResearchProgress ve 5 ScienceBalance üretir; aynı
   parselin ikinci incelemesi 0. Ödül kimliği kalıcıdır, iptal/yükleme çoğaltmaz.
6. Dünya toprağı/oksijen deneyinin örneği `EarthOrder` kökenlidir ve her yoldan
   bilim ödülü 0 kalır. Dünya emrinin numunesini rover analizine sokup ödül elde edilemez.
   Yeni analiz de Research/EarthOrder ile laboratuvar kapasitesini paylaşır.
7. Altı tekerlek, yavaş direksiyon ve araziye göre basitleştirilmiş salınım görsel
   animasyondur. Fizik motoru çarpışmaları domain rotasına rastgele yön vermez.
   Kol hareketi/numune toplama kareleri iş süresine bağlanır; pause ve 8x uyumludur.

## F12 — Arayüz, ses ve kabul

İnşa modu ayrı aç/kapatılır. Normal görev seçimi, parsel seçimi ve kamera sürükleme
birbirinin tıklarını tüketmez. Komut öncesi maliyet/konum/bağlantı/çıkış yolu;
komut sonrası durum, net üretim ve atanmış astronot görünür. Yeni yüzey işini
nereden durdurup geri çağıracağı kullanıcıya gösterilir. Her bildirim bir kez
çalabilir; pause'da her Refresh'te ses/uyarı tekrarı yapılmaz.

TR/EN metinler, renk yanında metinsel gerekçe, normal 1440×900 önceliği;
1920×1080/1280×720 de kontrol edilir. Dünya içindeki büyük etiketler panel HUD'ının
altında kesilmez. İnşa/seçim modalı açıkken arkadaki dünyaya tık gitmez. UI içinde
DTO, checkpoint, RNG, test ID veya mühendislik iç uygulama ayrıntısı gösterilmez.

Görsel kanıt: grid seçimi, geçersiz önizleme, astronotla inşaat, dört panel üretimi,
bağlantısız tesis, aktif su tesisinin stok/akışı, ek sığınakta uyku, rover gidiş/dönüş,
ayar menüsü ve normal açılış. Sadece `renderer.enabled=true` görsel kabul değildir.
