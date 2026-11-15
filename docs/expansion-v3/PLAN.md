# Koloni Genişlemesi V3 — Hydrogen uygulama planı

Tarih: 15 Kasım 2026. Durum: **Plan hazır; OP00–OP32 uygulanmadı.**
Unity: 6000.3.28f1. Bu belge bir sonraki geliştirmeyi tarif eder; mevcut
Operations V2'nin tamamlanma raporu değildir.

## Okuma sırası ve karar kaynağı

1. Geçerli AGENTS.md talimatları.
2. Bu PLAN, FEATURES, INTEGRATION, CHECKPOINTS ve PROGRESS.
3. Son `checkpoints/OP-ID.md`, ardından ilgili gerçek kaynaklar.
4. `../operations-v2/REVIEW.md`, `../RELEASE_NOTES.md`.
5. Bilimsel açıklama için SOURCES; eski FUTURE_ROADMAP yalnız tarihsel vizyondur.

Bu V3 paketi kapsadığı genişleme konularında eski FUTURE_ROADMAP içindeki
uygulanmamış sayıları, örnek Unity bağımlı veri tiplerini ve faz sırasını değiştirir.
Kullanıcının mevcut V2 olay aralıkları, yüzdeleri ve Dünya görevi bilim ödülü 0
kuralı korunur. Oyun kodu bu plan hazırlanırken değiştirilmedi.

## 1. Hedef oynanış

Oyuncu üs çevresinde bir parsel seçer, araziyi inceler, alanı tahsis eder,
bağlantı hattı döşer ve bir astronotu yapıyı kurmaya gönderir. Yeni panel enerji
üretimini, batarya depoyu, su tesisi kullanılabilir suyu gerçekten artırır.
İleride keşif/tarama ile daha iyi su sahası bulur, regolit fırını kurar, ek sığınak
yapar ve bir rover ile örnek getirip laboratuvarda inceler.

İlk açılışta mevcut üç astronotlu üs çalışır. Oyuncunun ilk su kuyusunu yapmadan
hayatta kalabilmesi mevcut başlangıç çıkarıcısıyla sağlanır. Yeni özellikler
Operations'a eklenir; Training ve Demo kendi senaryolarını korur.

## 2. Üç çalışır teslim ve son kapanış

| Teslim | Checkpoint | Oyuncunun yapabildiği |
|---|---|---|
| A — Kur ve büyüt | OP00–OP15 | Parsel seç/tahsis et; astronotla inşa; hat, panel, batarya, su çıkarıcı kur; gerçek üretim, arıza, söküm ve kayıt |
| B — Keşfet ve su bul | OP16–OP23 | Keşif sisi, yerel tarama, sonlu buz sahaları, alternatif regolit fırını, ek su tankı, mini harita ve kısa rehber |
| C — Sığınak ve rover | OP24–OP29 | Ek dinlenme/sığınak modülü, bir rover/dok, rota, enerji, örnek taşıma ve analiz |
| Kapanış | OP30–OP32 | Göç/yükleme, 100-sol bütünleşme, Linux/ekranlar, belge ve açık iş raporu |

Her teslim tek başına açılıp oynanabilir ve kaydedilebilir. OP15 ve OP23 kalite
kapılarıdır: başarısız bağımlılık düzeltilmeden sonraki teslim başlatılmaz.
Yalnız erişilemeyen ekran/manuel kullanıcı testi eksikse NeedsVerification
olarak açık taşınır; mekanik kabulü sağlanmış bağımsız işe devam edilir.
Başarılı kapı rutin kullanıcı onayı gerektirmez. Kullanıcı ayrıca dur demezse
Hydrogen OP32'ye kadar tek checkpoint sırasıyla devam eder.

## 3. Kapsam kararları

- Harita: mevcut prosedürel zeminin içinde 16×16 parsel; hücre kenarı 4 **oyun
  birimi**, başlangıç origin önerisi x=-32, z=-24. Bunlar Mars saha ölçümü değildir.
  Mevcut zemin x[-36,36], z[-28,44] aralığındadır; gereksiz ikinci terrain yaratma.
- Parsel veri katmanı, tahsis, yapı ve bağlantı ayrı şeylerdir. Dört yönde komşuluk.
- Tek yeni harcanabilir kaynak: **Malzeme**. Bilim malzemenin yerine geçmez.
- İlk yapı kataloğu: bağlantı hattı, panel, batarya, buz çıkarıcı; sonraki teslimde
  regolit fırını/su tankı, son teslimde sığınak ve rover dok/araç paketi.
- Yeni tesisler tamamlanıp üssün hattına bağlanmadan etki üretmez.
- Yeni tesislerin konumu, üretimi, görevleri ve ilerlemesi gerçek simülasyon
  verisidir. Sadece sahneye model koymak tamamlanma değildir.
- Harita üretimi, arazi verimi, fiyatlar ve süreler oyun tasarımıdır. SWIM'den
  gerçek 4 birimlik yeraltı haritası türetilmiş veya gerçek bir konum modellenmiş denmez.
- Rover ilk sürümde tek araç ve kontrollü rota izler. Altı tekerlekli görünüm ve
  basitleştirilmiş rocker-bogie animasyonu vardır; gerçek araç dinamiği çözülmez.
- Ek habitat ek dinlenme alanı ve oyun içi korunma sağlar; ekip sayısı bu sürümde
  **3 kalır**. Sera, yeni astronot üretimi ve yeni gıda ekonomisi eklenmez.

## 4. Korunacak mevcut davranış

- V2'nin beş bağımsız olay kanalı aynı bekleme/süre/yüzde sözleşmesiyle çalışır.
- İlk üç sağlıklı panelin toplam gücü yine `ResourceConfig.SolarRate` olur.
- Başlangıç su çıkarıcısının 0,012 su/sn, 0,015 enerji/sn varsayılan akışı korunur.
- ResearchProgress toplamı ile ScienceBalance bakiyesi ayrı kalır. Dünya
  emirleri ve onların örnekleri hiçbir yeni analiz yolundan bilim üretemez.
- Tek simülasyon saati; pause, focus, menü, uyku ve rapor davranışları korunur.
- Uyku anchor'ları, akıcı ileri dönüş, habitatta efekt dışlama, ayarlar ve mevcut
  Linux renderer düzeltmeleri korunur.
- Kayıt/yükleme tehlikeyi, siparişi, inşaatı veya maliyeti sıfırlamaz. Yükleme pause'dadır.

## 5. Mimari sınırları

`MarsOutpost.Simulation` halen `noEngineReferences: true` kullanır. GridCoord,
konum/rota ve parsel modellerinde UnityEngine, Vector2Int, Transform, Physics,
MonoBehaviour veya Time kullanılmaz. Basit int/double tipleri yeterlidir.

Simülasyon: durum + komut + zaman/akış hesabı + atomik maliyet + görev sahipliği.
Presentation: seçim, hayalet önizleme, modeller, UI, ses ve kareler arası interpolasyon.
Mevcut büyük dosyaları bir anda yeniden yazma; yeni konu tiplerini küçük dosyalara
ayır, mevcut integratöre açık bağlantılar ekle. DI/back-end/ECS/framework kurma.

İsim önerileri: `ParcelMap`, `ParcelState`, `FacilityState`, `ConstructionJob`,
`SurfaceJob`, `UtilityNetwork`, `ExtractionSiteState`, `RoverState`, `SampleState`.
İsimleri projeye uyarlayabilirsin; davranış/kayıt sözleşmelerini sessizce değiştirme.

## 6. Kayıt ve kesinti

OP02'den başlayarak her yeni etkin durum alanı aynı checkpoint'te kaydedilip
yüklenir. Kayıt işi son checkpoint'e ertelenmez. Mevcut şema 2'dir; V3 için şema
3 ve `expansionRevision` kullan. Yeni kalıcı sözleşme eklendikçe revision'ı artır,
gerçek geçişini ve varsayılanlarını MIGRATION bölümüne kaydet (INTEGRATION).
Şema v1/v2 okuma ve V2 durumları korunur; geçersiz kayıt mevcut oyunu değiştirmez.

Kodlama limiti için PROGRESS + checkpoint raporu otoritedir. Oyuncu save'i
geliştirici checkpoint'inin yerine geçmez. Her kesintide derlenebilir son nokta,
yarım alt iş, test sonucu ve devamın tam ilk adımı yazılır.

## 7. Başlangıç kanıtı ve bilinen açıklar

Son bağımsız V2 koşusu: 91 EditMode + 17 PlayMode başarılı. OP00 bunları yeniden
çalıştırır; bu belge yeni test başarısı iddiası değildir. V2 CP04/CP17 için
1920×1080 ve 1280×720 gerçek normal Linux açılışları hâlâ doğrulanmayı bekler.
1440×900 gerçek açılış/ayar/uyku görüntüleri mevcuttur. Bu açıkları V3 tamamlandı
diye sessizce kapatma; OP31 sonunda gerçek kanıtla durumlarını güncelle.

## 8. Bu sürümde uygulanmayacak ileri işler

Gerçek HiRISE/CTX yükseklik verisi/GIS işleme, canlı SWIM API'si, gerçek fiziksel
sondaj/ısı akışı/kimyasal arıtma, Dünya–Mars kargo uçuşu, serbest habitat iç tasarımı,
oynanabilir çoklu rover/çok oyuncu, tam rocker-bogie fizik simülasyonu ve devasa
koloni haritası ileriki sürümlerdir. Eski yol haritasındaki kW/kWh, derinlik,
sıcaklık ve "her yerde su garantisi" ifadeleri bu plana taşınmaz.

## 9. Başarı ölçütü

Yeni oyuncu kısa rehberle alan tahsis edip bir panel ve çıkarıcı kurabilir;
üretim değişimini ve bağlantısız durumun nedenini görür. Bilim/malzeme/enerji/su
eksiye düşmez; inşaat/örnek/onarım çift uygulanmaz. Tehlikede ekip geri çağrılabilir.
Yarım işte kaydet/yükle aynı sonuçla devam eder. Gerçek Linux normal açılışları ve
eksik kalan kullanıcı denemeleri dürüstçe raporlanır. Checkpoint'lerin tamamı
kanıtlanmadan "gelecek yol haritası bitti" denmez.
