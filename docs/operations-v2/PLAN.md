# Üs Operasyonu V2 — Hydrogen için özellik planı

Durum: yalnızca plan, 14 Kasım 2026. Kod uygulanmadı.
Başlangıç: Unity 6000.3.28f1; son yerel kanıt 53 EditMode + 11 PlayMode.
Gerçek kaynaklar ve yeniden çalıştırılan testler bu geçmiş kayıttan önceliklidir.
Önce bu belge, FEATURES.md, CHECKPOINTS.md; sonra eski planlar okunur.
Bu isteğin kapsadığı konularda V2 kararı eski sabit olay/iki sol kararını geçersiz kılar.

## 1. Modlar

- **Training:** mevcut sabit olaylı iki solluk görev; eğitim/test referansı korunur.
- **Demo:** mevcut sıkıştırılmış kısa gösterim; V2'nin dakika kurallarını değiştirmez.
- **Outpost Operations:** yeni uzun süreli, rastgele olaylı esas mod. İki sol sonunda
  başarıyla kapanmaz. Kullanıcı bitirene veya güvenlik kaybına kadar sürer.
  Bir sonraki açılış varsayılanı Operations; mod seçimi bunu açıkça gösterir.
- Her modun bağımsız senaryo/önayarı vardır. Training'e yeni rastgele olaylar sızmaz.
- Sol uzunluğu ilk ayarda 720 sn; gündüz 480 sn, gece 240 sn. Ayarlanabilir.

## 2. Zamanın kesin anlamı

Kullanıcının verdiği dakika aralıkları **simülasyon dakikasıdır**. 1x akışta bir
dakika yaklaşık gerçek bir dakikadır. Pause/menü/odak kaybında durur; 8x uykuda
ilerler. Bu önerilen yorum arayüzde “simülasyon süresi” diye yazılmalı; gerçek
duvar saati isteyen bir revizyonda tüm zaman sözleşmesi birlikte değiştirilmelidir.
Tek otorite MissionSimulation.ElapsedSeconds; ikinci DateTime/Invoke saati yok.
Uyku olayları atlamaz; tehlike uyarısı veya arıza gerektiğinde uykuyu kesip pause
talebi oluşturur. Bir karede biriken eski 8x süre, devamdan sonra sıçramaz.

## 3. Olayların değişmez aralıkları

| Bağımsız olay kanalı | İlk/sonraki bekleme | Etki | Aktif süre / kapanış |
|---|---|---|---|
| Radyasyon | 15–20 dk (900–1200 sn) | İç/dış eğitim risk puanı; korunma kararı | Öneri: 60–120 sn, ardından biter |
| Güneş paneli arızası | 20–30 dk (1200–1800 sn) | Sağlam panellerden biri üretimi bırakır | Astronot tamir edene kadar |
| Oksijen üretici arızası | 20–30 dk | Üretim kapasitesi düşer | 2× enerjili düzeltme işi bitene kadar |
| Kum fırtınası | 10–15 dk (600–900 sn) | Güneş üretimi **%80 azalır: ×0,20** | **1–3 dk (60–180 sn)** |
| Su çıkarıcı arızası | 20–25 dk (1200–1500 sn) | Çıkarma hızı **%50 azalır: ×0,50** | **10 bilim puanı** harcanınca |

Kullanıcının belirttiği aralık ve yüzdeler değiştirilemez; diğer süre/maliyetler
FEATURES.md içindeki başlangıç önerileridir ve ayarlardan gelir.

Beş ayrı PRNG akışı (seed + sabit kanal kimliği) kullanılır. Bir kanalı onarmak,
erteleme veya rastgele seçim yapmak diğer kanalın sonraki zamanını değiştirmez.
System.Random/UnityEngine.Random global ortak akışı kullanılmaz. Küçük, açıkça
tanımlı ve test edilmiş deterministik PRNG tercih edilir; nesne GetHashCode'u seed değildir.
Uniform örnekleme; aynı seed + aynı komutlar + aynı sürüm aynı olayları üretir.

İlk olay zamanı t=0'dan örneklenir. Geçici olayda yeni bekleme olay bittikten;
kalıcı arızada onarım tamamlandıktan sonra başlar. Böylece aynı kanal üst üste
arıza biriktirmez. Bu “bekleme aralığı” yorumudur, iki başlangıç arası kesin aralık
değildir; belgede ve testte açıkça korunur. Kanallar arasında eşzamanlı olay mümkündür.
Olaylar sıra ile gelmek zorunda değildir. Rastgele radyasyon/fırtına başlamadan
önerilen 60 sn uyarı gösterilir; uyarı aktif tehlike değildir, ilk başlangıç
yine belirtilen aralıkta kalır. Arızalar oluştuğunda teşhis bildirimi gelir.

## 4. Huzurlu temel döngü ve sürdürülebilirlik

Olay yokken sürekli kırmızı ekran/tehlike sesi kullanılmaz. Kaynak eğilimleri
okunur, ekip normal rutin yapar, oyuncu araştırma/öğün/istasyon işleri seçer.
Operations'ın sağlıklı üssü en az altı sol otomatik yaşam desteğiyle yaşayabilmeli;
arıza yokken kasıtlı kaynak tuzağı kurulmaz. Araştırma, dış görev ve onarımlar hâlâ
enerji/stamina tercihi yaratır. Uzun süreli test en az 100 sol kapsar.

Mevcut kodda **su geri kazanımı var, su çıkarma yok**. Yeni çıkarıcı ayrı modüldür;
geri kazanıma isim değiştirip istek tamamlandı denemez. Gıda stokları mevcut iki
sola göre sınırlı; Operations için günlük öğünün sürdürülebilir temini ayrıca
tanımlanır (FEATURES.md). Yeni ekonomi Training/Demo değerlerini sessizce değiştirmez.

Bilim ekonomisi: ResearchProgress toplam kazanılmış araştırmadır; ayrı
ScienceBalance harcanabilir bakiyedir. Araştırma ile kazanılır, su düzeltmesi 10
harcar. Bakiye eksiye düşmez. Toplam araştırmayı düşürmek veya Dünya görevlerinden
bilim kazandırmak yasaktır. Safety loss devam eder; bilim hedefi Operations'ı bitirmez.

## 5. Dünya komutları

Sol 2,4,6… tamamlandığında (Sol 3,5,7… başlangıcında) **bir yeni komut ulaşır**.
İlk açılışta komut yok. Bu varış zamanıdır; eski Ask Earth Advice gecikmesi buraya
yeniden eklenmez. Eski isteğe bağlı tavsiye ayrı özellik olarak korunur.
Yedi komut türü FEATURES.md içinde tanımlı. Hepsinin bilim ödülü **0**.
Seed'li karıştırılmış torba: yedi tür bitmeden tekrar yok. Emri bitirmemek sonraki
periyodu durdurmaz. Bekleyen emirler kaybolmaz; liste sayfalanır, ilk sürümde
aynı anda tek emir aktif çalışır. Son tarih uyarısı öneri olarak iki soldur;
gecikme otomatik oyun kaybı/puan cezası değildir. Kaynak kazancı da verilmez.

## 6. Mimari ve kayıt

Simulation katmanı Unity'den bağımsız kalır. Yeni tipler ihtiyaca göre küçük
dosyalara ayrılır: OperationsConfig, EventScheduler, EquipmentState,
WaterExtractorState, ScienceWallet, EarthOrderState, snapshot DTO.
Bu isimler rehberdir; büyük framework/DI/backend eklemeyin.
HUD veya WorldView iş tamamlama, RNG, maliyet veya üretim hesabı yapamaz.
Başlangıç/bitiş/tamir/sol/komut varış sınırları tek integrasyon döngüsünde işlenir;
bir büyük Tick ile küçük Tick'ler aynı sonuca ulaşır. Eşzamanlı sınır sırası:
geçici olay bitişi → yeni olay/arıza → modül akışları/görevler → maliyet/onarım
tamamlama → güvenlik → sol raporu/komut varışı. Her etki bir kez uygulanır.

Geliştirici checkpoint'i ile **oyuncu kaydı ayrı şeylerdir**. Kodlama limiti için
zorunlu olan CHECKPOINTS protokolüdür. Uzun operasyon için oyuncu kaydı da ayrı
CP16'da uygulanır: sürümlü JSON, atomik temp→replace, bozuk kayıt kontrolü.
Saat, PRNG durumları, sonraki zamanlar, aktif olaylar/ekipman, bilim, görevler,
öğün, ekip ve Dünya emirleri korunur. Makine kapalıyken çevrimdışı ilerleme yok.
Yükleme sonrası oyun pause'da açılır; render nesneleri kaydedilmez.

## 7. Test ve tamamlanma

Her özellik ayrı küçük teslimdir. EditMode mekanik; PlayMode akış/kamera/UI;
gerçek Linux çıktısı uyku, hareket ve efekt kalitesini doğrular. Sadece
Renderer.enabled kontrolü “görsel doğru” kanıtı değildir.
1440×900 (kullanıcının ekranı), 1920×1080 ve 1280×720 hedeflenir; başlatma ve
kaydedilmiş ayarlarla tekrar açma denenir. Üs/panel kadraj dışı kalmamalı,
arayüz küçülüp okunamaz olmamalı. Ayar menüsü korunur.
Hiçbir test, build veya görsel inceleme yapılmış gibi raporlanmaz.
NASA kaynakları bilimsel ilişki içindir; rastgele süreler/bozulma katsayıları
oyun tasarımıdır. Radyasyon görünür lazer gibi öğretilmez; görsel şematik gösterimdir.

## 8. Bu planın dışında

Serbest kolonileşme, geniş harita, gerçek rover fiziği, çok oyunculu mod,
fotogerçekçi asset paketi ve gerçek ISRU mühendislik simülasyonu yok.
Su çıkarma, küçük bir enerji→su üretim modülü olarak modellenir; doğrulanmamış
kW/litre/saat veya NASA sahasına ait buz rezervi iddiası yapılmaz.
