# Mars Outpost — tamamlanabilir oyun ve dış uygulayıcı planı

Durum: 14 Kasım 2026; planlama teslimi. Bu belge oyun kodunu uygulama talebi değildir.
Uygulayıcı: kullanıcının Antigravity içinde seçtiği Hydrogen. Planlayıcı/inceleyen: Sol.

## 1. Okuma sırası ve mevcut durum

Önce geçerli AGENTS.md talimatlarını, sonra bu belgeyi, GAME_PLAN.md,
IMPLEMENTATION_TASKS.md ve VALIDATION.md dosyalarını okuyun; gerçek kodla karşılaştırın.
Bu belgedeki açık revizyonlar eski planın ilgili kapsam kararlarının yerini alır.
Diğer mekanik kurallar GAME_PLAN.md içinde geçerlidir. Görev durumları yalnızca
IMPLEMENTATION_TASKS.md ilerleme tablosunda tutulur; ikinci bir durum tablosu yaratmayın.

Mevcut proje Unity 6000.3.28f1, URP 2D, Input System kullanır. T00–T02 tamamlandı.
Saf C# saat ve kaynak simülasyonu vardır; üç kişilik başlangıç ekibi kimlik/ad taşır.
18 test gerçek Unity EditMode Test Runner'da geçti. Sonuç dosyası:
`validation/t02-editmode-results.xml`. Bu geçmiş kanıttır; yeni kod ayrıca doğrulanır.
Oynanabilir sahne, stamina, olaylar ve uyku henüz uygulanmadı. Tamamlanan temeli
sıfırdan yazmayın. Git'te çok sayıda mevcut dosya henüz takip edilmiyor olabilir;
bunları gereksiz dosya sayıp silmeyin. Library/Temp gibi önbellekleri teslimata eklemeyin.

## 2. Bitmiş oyunun tanımı

7–12 yaşa yönelik, tek oyunculu, üstten görünümlü 2D Mars üssü yönetimi.
Oyuncu üç astronota iş verir; enerji, su, oksijen ve yaklaşan riskleri yönetir.
İki sol sonunda ekibi güvenli tutup araştırma hedefini tamamlamaya çalışır.
Uyku zamanı hızlandırır; kaynaklar ve olaylar aynı simülasyonla işlemeye devam eder.

Bir sol 720 simülasyon saniyesi, gündüz 480, gece 240 saniye başlangıç ayarıdır.
İlk denemede toplam oturum öğretici dahil yaklaşık 15–25 dakika hedeflenir;
bu bir tasarım hedefidir, ölçülmüş sonuç değildir. Jüriye gösterim için ayrı bir
90 saniyelik tanıtım senaryosu aynı sistemlerle çalışır; olaylar açıkça hızlandırılır.

Teslim: açılan Unity projesi, oynanabilir build, okunabilir kaynak kodu, test kanıtı,
nasıl çalıştırılır belgesi, NASA kaynak açıklamaları ve yarışma demo taslağı.
Çalışan masaüstü build zorunlu ilk hedeftir. WebGL erişimi hedeflenir; mevcut kurulumda
modül eksiktir. Destek varsa web build ve tarayıcı kontrolü de yapılır; yoksa eksik
açıkça raporlanır. Modül kurulumu şartlarının kabulünü kullanıcıya bırakın.

## 3. Sabit ürün kararları

- Karakter kontrolü: tıklayarak seçim ve görev atama. WASD ve yol bulma yok.
- Tek üs, üç astronot, iki sol. Multiplayer, serbest bina yerleştirme ve kampanya yok.
- Bilgi mimarisi: üstte üç rezerv, solda ekip, ortada üs, sağda seçili görev ve olay.
- Astronot: ad, iş, stamina; risk puanı seçildiğinde görünür. Göstergeler yazı ve ikonla desteklenir.
- Kaynakların neden değiştiği araç ipucu veya kısa açıklamada görünür.
- Bir astronot tek iş yapar; bir görev noktası tek kişiye aittir.
- Üsse dönüş 10 simülasyon saniyesidir; varana kadar dış maruziyet sürer.
- Görev bitmesi, tükenme, olay ve sol sınırları büyük Tick içinde atlanmaz.
- Stamina sol başında yenilenmez. Dinlenme/uykuda geçen süreyle toparlanır.
- Duraklatma serbest; otomatik kritik uyarılar düşünme zamanı verir.
- Aktif oyun 1x, toplu uyku 8x. Focus kaybı duraklatır; geri gelişte otomatik devam yok.
- İlk UI İngilizce, tüm metinler merkezî; Türkçe eklenmesi kolay tutulur.
- Depo birimleri oyun birimidir. Oyun puanı, gerçek radyasyon dozu veya sağlık tahmini değildir.

## 4. Kapsam revizyonları

Eski planda gıda ve iletişim gecikmesi ertelenmişti. Görev hedeflerini daha iyi
kapsamak için T09'da küçük ve açıklanabilir iki mekanik olarak teslim kapsamına alınır.
Beş ayrı kaynak barı açılmaz; gıda günlük görev kartı, iletişim mesaj kartıdır.
Tarım simülasyonu, bitki büyütme sistemi, serbest iletişim ağı kurulmaz.

T08 yalnızca NASA kaynağı ve bilimsel izlenebilirliğe odaklanır. Nihai build
doğrulaması T11'e taşınır. Görsel/ses düzenleme T10, demo/teslim belgeleri T12'dir.
T03–T07'nin kabul koşulları IMPLEMENTATION_TASKS.md içinde korunur.

## 5. Uygulama sırası ve kontrol noktaları

### A — T03: görev, stamina ve araştırma

Mevcut komutları çalışır hâle getirin; sürekli araştırma için enerji ve çalışan
astronot birlikte gerekli olsun. Araştırma kalan enerjiyle orantılı ilerlesin;
iş yapılamayan sürede ilerleme/stamina üretmeyin. Stamina 0'da görev durmalı,
dışarıdaki bitkin astronot yine de üsse dönebilmeli. Tek seferlik bakım etkisi
iptal/yeniden atamayla çoğalmamalı. Bakım, panel üzerindeki kirlenmeye etki eder;
atmosferdeki toz fırtınasını ortadan kaldırmaz.

T03 teslim kanıtı: EditMode sonuçları, değişen API listesi, başarısız koşulların
komut ret nedenleri. Bu ilk Sol kontrol noktasıdır.

### B — T04–T05: olaylar, güvenlik, uyku, sol raporu

GAME_PLAN.md içindeki sabit senaryo zamanlarını veriyle tanımlayın. Uyarı,
başlangıç ve bitiş birbirinden ayrıdır. Radyasyon ile toz aynı etkiyi yapmaz.
Radyasyon iç/dış risk puanını; toz güneş verimini etkiler. Oksijenin kesintisiz
sıfırda kalma süresi T04'te takip edilir; geçici eksiklik toparlanırsa sayaç sıfırlanır.

Toplu uyku yalnızca gece ve herkes içerideyken başlar. Komut içerideki işleri
durdurur. Uyarı, tüketim ve üretim aynı saatle sürer. Kritik oksijen uykuyu keser
ve duraklatma talebi üretir. Domain Unity hızını değiştirmez; runner bu talebi uygular.
Sol raporu ayrı bir duraklatma nedenidir; raporu kapatmak başka bir pause nedenini
yanlışlıkla kaldırmamalıdır. Başarısızlık kontrolü başarı kontrolünden önce gelir.

T05 teslim kanıtı: uyku ile normal zaman karşılaştırması, olay sınırı/sol sınırı,
yeniden başlatma ve terminal durum testleri. İkinci Sol kontrol noktası.

### C — T06: ilk oynanabilir sahne

Geçici şekillerle gerçek düğmelerden oynanabilen sahneyi erken teslim edin.
Tek SimulationRunner ve tek durum sahibi; UI yalnızca komut gönderir.
Runner kuyruğundaki adımların ortasında pause/rapor/terminal talebi gelirse o karede
kalan adımları işletmeyin. Hız değişimini bir sonraki uygun adımda değerlendirin;
8x hesaplanmış eski kuyruk rapordan sonra sıçrama yaratmamalı. İşlem sınırı zamanı
sessizce çöpe atmamalı; duraklatılmış gerçek süre sonradan telafi edilmemeli.

Bir Editor aracı sahneyi oluşturacaksa tekrar çalıştırılması çoğaltma yapmamalı;
SampleScene veya kullanıcının yaptığı sahneleri silmemeli. UI için mevcut uGUI
yeterlidir; Input System ile uyumlu EventSystem kullanın. Runtime'da gereksiz
Find veya her karede yeni UI nesnesi oluşturmayın.

T06 teslim kanıtı: Unity Play kontrolü, ekran görüntüsü veya kısa kayıt, tam
kurulum/başlatma adımları. Gerçekten görülmeyen sahne için görsel onay iddia etmeyin.
Bu aşamada kullanıcı ilk kez oynar; üçüncü Sol kontrol noktası.

### D — T07–T09: öğretim, NASA verisi, gıda ve iletişim

T07: en fazla üç kısa öğretici adımı; seç → görev ver → sonucu gör. Ardından ilk
olayla korunmayı öğretin. Sonuç ekranı yalnızca skor değil, iki kararın açıklamasını
göstersin. Kullanıcı denemesi için PLAYTEST.md hazırlayın, yapılan ve önerilen
denemeleri ayırın. Sorunlar önce anlaşılabilirlik ve mekanik düzeyinde düzeltilir.

T08: gerçek NASA arşiv kaydını bizzat doğrulayın; kaynağın kimliğini, tarihini ve
hangi alanının olay türünü veya sırasını etkilediğini belgeleyin. Zaman sıkıştırması
ve tasarlanmış etki katsayılarını ayırın. Tarihsel kayıt, canlı tahmin veya Mars'ta
ölçülmüş doz değildir. ECLSS, su/elektrik/oksijen ilişkisini açıklayan bilimsel
dayanaktır. Veri erişilemezse temsili senaryo açıkça etiketlenir ve T08 eksik kalır;
kayıt/ölçüm uydurulmaz. Kaynaklar menüden ve ilgili açıklamadan ulaşılabilir olsun.

T09 gıda: her sol için hazır stoktan bir ekip öğünü bulunur. Gece bu stok bir kez
azalır; bir astronotun 30 sn iç mekân hazırlık görevi varsa ekip toparlanması normal,
yoksa o gecenin uyku toparlanması 0,75 katsayılı olur. Hazırlık −0,10 stamina/sn
tüketir. Öğün/katsayı tüm gece için tek kez belirlenir; uykuya tekrar girerek
çoğaltılamaz. Açlık ölümü ve ek sağlık sistemi yok. Gerçek zamanlı bitki üretimi
iddiası yok; “yedek öğün hazırlama” olarak anlatılır. İki sol için yeterli hazır stok
başlangıçta vardır. Bu değerler oyun dengesi önerileridir.

T09 iletişim: oyuncu en fazla bir tavsiye isteği gönderebilir; cevap 45 simülasyon
saniyesi sonra gelir. Mesajda gerçek Mars iletişim sürelerinin konuma bağlı olduğu,
oyun beklemesinin sıkıştırılmış olduğu yazılır. İlk istek olaya 30 sn kala
önerilir; oyuncu yanıt beklerken kendi kararını vermeyi öğrenir. Mesaj pause'da
durur, uykuda ilerler; tavsiye geçmişteki olaya yönelikse zaman damgasıyla açıklanır.
Tavsiye kural tabanlıdır; harici LLM/API gerektirmez. Aynı isteği tekrarlama spam'i yok.

T09 kanıtı: tek öğün/sol, hazırlık maliyeti, kısmi uykuda katsayı, gecikmiş mesaj,
pause/sleep/restart durumları için testler. Dördüncü Sol kontrol noktası.

### E — T10–T12: görsel bütünlük, build ve teslim

T10: tek stil, sıcak Mars zemini ve koyu okunaklı paneller; yeşil/kehribar/kırmızı
durumları metinle destekleyin. Üç karakter, panel, habitat ve korunak kolay ayırt
edilsin. Görsel hareket mantıksal konumu izlesin, ikinci bir görev/zaman hesabı kurmasın.
Basit ikon/şekiller kabul edilir; harici görsel için lisans ve kaynak kaydı tutun.
Sesler: görev tamamlandı, uyarı, hata; sessize alma ve ses düzeyi ayarı. Ücretli
asset/servis satın almayın. Çok sayıda görsel üretimi ana işi bloke etmesin.

T11: gerçek masaüstü build alıp başlatın. WebGL modülü varsa ayrıca tarayıcı build'i
doğrulayın; yoksa masaüstü teslimi sürdürüp eksikliği yazın. 1280×720 ve 1920×1080
arayüzünü kontrol edin. Oyunun başlangıcından sonuna iki solluk bir oturum ve 90 sn
demo senaryosu oynansın. Pause/focus, sol raporu, uyku, ses, kaynaklar, başarı/kayıp,
restart ve sahnenin ikinci açılışı kontrol edilsin. Tekrarlanan abonelik ve zaman
sıçraması olmamalı. Otomatik testler kullanıcı deneyimi kontrolünün yerine geçmez.

T12: README (Editor sürümü, açılacak sahne, kontroller, build yolu), bilim/kaynak
açıklaması, bilinen sınırlamalar ve İngilizce proje metni. 7 slaytlık taslak veya
30 sn demo video senaryosu hazırlayın; teslim anında güncel yarışma gereksinimlerini
resmî kaynaktan tekrar doğrulayın. Video üretmediyse “video hazır” demeyin.
Lisans seçimini ve dış asset lisanslarını kullanıcıyla netleştirin; açık kod
teslimi için gereken eksikleri yazın. Yayınlama, başvuru gönderme ve hesap işlemleri
bu geliştirme promptunun kapsamında değildir.

Son kontrol: testler yeşil, build oynanmış, bilimsel iddialar izlenebilir,
bilinen eksikler açık. Bu beşinci ve son Sol kontrol noktasıdır.

## 6. Uygulayıcı çalışma protokolü

- Her seferinde tek görev; o görev bitince ilgili test ve kısa sonuç kaydı.
- Açık kapsam içindeki rutin seçimlerde ilerleyin; her küçük iş için izin sormayın.
- Planla çelişen büyük değişiklikte gerekçeyi ve öneriyi bildirin; sessizce ürün değişmesin.
- Küçük proje için yeni framework, backend, ağ sistemi, global event bus kurmayın.
- Mevcut sayısal sınır çözümünü koruyun. T03+ için stamina, görev bitişi ve olay
  sınırlarını aynı integrasyon sırasına ekleyin; zamanı sistem başına ayrı işletmeyin.
- API değişikliği gerekiyorsa kullanım yerlerini ve testleri birlikte güncelleyin.
- İlgili mekanik testleri yazın; test geçsin diye beklentileri zayıflatmayın.
- Test çalışmıyorsa engeli kaydedin ve mümkün olan bağımsız işleri sürdürün.
- Unity sürümü 6000.3.28f1'de kalsın. Sahne, .meta ve assembly referansları tutarlı olsun.
- Her kontrol noktasında docs/IMPLEMENTATION_TASKS.md durumunu ve VALIDATION.md
  kanıtlarını güncelleyin; son durumu docs/AGENT_REPORT.md içine yazın.
- AGENT_REPORT: görev kimliği, değişen dosyalar, davranış, komutlar ve sonuçları,
  build yolu, gözlenen görsel sonuç, bilinen eksikler ve bir sonraki iş.
- Kullanıcı durdurmazsa planın sonraki görevine geçilebilir. Sol incelemesi
  kendiliğinden gerçekleşmez; uygulayıcı Sol'dan yanıt bekliyormuş gibi boşta kalmaz.
- Commit/push, yayınlama veya başvuru gönderme yapmayın. Kullanıcının mevcut işini silmeyin.

## 7. Sol ile kontrolün nasıl yapılacağı

Antigravity süreci bu sohbet tarafından kendiliğinden görülemez veya izlenemez.
Kullanıcı A/B/C/D/E sonunda bu sohbete “kontrol et” yazabilir. Sol aynı yerel
checkout'a erişebiliyorsa dosyaları, farkları ve kanıtları inceler; gerekirse test çalıştırır.
Başka makine/checkout kullanılmışsa erişilebilir kaynak/diff ve rapor gerekir.
Uygulayıcı raporunun “başarılı” demesi tek başına doğrulama değildir.
Kullanıcı yalnızca en sonda dönmek isterse AGENT_REPORT ve test/build kanıtlarıyla
tam inceleme yapılır. Otomatik veya arka planda periyodik kontrol kurulmamıştır.

## 8. İlk sürüm sonrasında

Yedi sol, doğrudan karakter hareketi, sera, bina yerleştirme, uzmanlık, vardiyalar,
kayıt/yükleme ve yeni haritalar ayrı sürüm işidir. Teslim edilen iki solluk oyunun
çalışması bu özelliklere bağlı olmamalı. İlk kullanıcı denemelerinden sonra önceliklendirilir.
