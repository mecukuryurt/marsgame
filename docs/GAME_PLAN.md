# Mars Outpost — temel oyun planı

Tarih: 14 Kasım 2026. Durum: T02 kaynak simülasyonu uygulandı; oynanabilir oyun henüz yok.

Güncel uygulama iki solluk 3D/2.5D oyundur. Bu dosyanın ilk prototip kararları
tarihsel tasarım referansıdır; güncel durum `RELEASE_NOTES.md` içindedir.

Bu dosya tasarımın kaynağıdır. Uygulama sırası ve ajanlara verilecek görevler
`IMPLEMENTATION_TASKS.md` içindedir. Sonraki değişikliklerde iki dosyayı birlikte güncelleyin.
Güncel tam oyun kapsamı ve dış uygulayıcı devri `MASTER_PLAN.md` içindedir.
Oradaki açık revizyonlar, aşağıdaki ilk sürüm erteleme kararlarının yerini alır:
gıda/iletişim T09'a, görsel/ses T10'a, nihai build T11'e, teslim anlatımı T12'ye alınmıştır.

## 1. Hedef ve mevcut proje

7–12 yaş grubuna, Mars üssündeki enerji ve yaşam desteği kararlarının sonuçlarını
oynayarak öğretmek. Oyuncu tek kişidir; “ekip” oyuncunun yönettiği astronotlardır.
İlk sürümde çok oyunculu sistem bulunmaz.

Proje kullanıcının talebiyle kurulu Unity 6000.3.28f1 sürümüne yükseltildi;
URP 2D ve Input System korunuyor. Editor açılışı, paket çözümleme, C# derleme ve
18 EditMode testi doğrulandı. T01 saat, T02 kaynak simülasyonunu içerir.
SampleScene mevcut; oynanabilir sahne ve hedef platform build'i henüz hazırlanmadı.

İlk hedef: geçici şekillerle gösterilen, gerçek zamanlı çalışan, iki sol süren tek
oynanabilir senaryo. Ekip bilimi anlayıp kararlarının sonucunu görebilmeli.
Yedi solluk kampanya bu temel doğrulandıktan sonra değerlendirilir.

## 2. Kararlar ve varsayımlar

### Ekipten gelen kararlar

- 2D Mars üssü yaklaşımı korunacak.
- Ana akış tur bazlı olmayacak; olaylara hazırlık gerçek zamanlı olacak.
- Örneğin “radyasyon olayına 5 dakika kaldı” geri sayımı bulunacak.
- Uyku, sol geçişiyle bağlantılı olacak.
- Astronotlar yaptıkları işe göre stamina kaybedebilecek.
- Şimdilik öncelik temel mantık ve plan; aşağıdakiler geliştirme varsayımlarıdır.

### İlk uygulama için önerilen varsayımlar

- Üstten görünümlü sabit üs; üç astronot, tek harita ve görev noktaları.
- Karakter seçimi ve görev düğmeleriyle yönetim; WASD hareketi ve yol bulma sonraya.
- Normal hız 1x; duraklatma serbest; toplu uykuda hız 8x.
- Bir sol 720 simülasyon saniyesi: normal hızda 12 gerçek dakika.
- İlk 480 saniye gündüz, son 240 saniye gece; bu oranlar oyun sadeleştirmesidir.
- Oyun süresi gerçek Mars solunun uzunluğunu temsil eden bir zaman sıkıştırmasıdır.
- Sol takvimle ilerler. “Ekibi uyut” gecenin kalanını hızlandırarak yeni sola ulaştırır.
  Oyuncu uyutmazsa da takvim ilerler; böylece zaman durmaz ve yorgunluk devam eder.
- Gecenin başlangıcından önce toplu uyku yok; bir astronot her zaman kısa dinlenebilir.
- Bu süreler, ekip sayısı ve tüm denge değerleri ayarlardan değiştirilebilir.

Takvimin yalnızca uyku düğmesiyle değişmesi istenirse ayrıca tasarım revizyonu
yapılmalı; ilk uygulayıcı bunu sessizce farklı yorumlamamalı.

## 3. Oyuncunun temel döngüsü

1. Kaynakları, ekip durumunu ve yaklaşan olayı gör.
2. Bir astronotu göreve veya dinlenmeye ata; otomatik sistemlerin önceliğini seç.
3. Görev sürerken zaman, kaynaklar ve stamina birlikte ilerlesin.
4. Uyarıya göre işi ertele, üssü koru veya ekibi içeri çağır.
5. Gece ekibi uyut; kaynaklar ve olaylar işlemeye devam etsin.
6. Yeni sol raporunda kararın nedenini ve sonucunu gör; sonraki sol için düzenle.

Öğretilecek ilk ilişki: düşük güneş üretimi → bataryaya bağımlılık → oksijen
üretimi ile araştırma arasında enerji önceliği. Radyasyon, dış görev ile korunma
arasında ikinci bir seçim oluşturur. Bu iki olay farklı fiziksel süreçlerdir.

## 4. Zaman, duraklatma ve olaylar

### Tek saat

Tüm mekanikler `MissionSimulation` içindeki monoton `ElapsedSeconds` değerini
kullanır. Sol 1, t=0'da başlar; `SolNumber = floor(ElapsedSeconds / 720) + 1`.
Gece ve diğer süreler yapılandırmadan hesaplanır; 720 koda gömülmez.

- `Tick(simulationSeconds)` temel giriş noktasıdır.
- Unity tarafında tek runner kare süresini toplar ve 0,25 saniyelik adımlara böler.
- Hız, runner'ın topladığı süreyi çarpar; sistemler ayrı ayrı hız uygulamaz.
- Duraklatmada saat, görev, tüketim, stamina ve olay geri sayımı birlikte durur.
- Sekme/uygulama odağı kaybolunca oyun durur; geri dönünce kullanıcı devam ettirir.
- Gerçek bilgisayar saatiyle arka planda ilerleme veya kaçırılan süre telafisi yoktur.
- Büyük zaman adımları olay ve sol sınırlarında bölünür. Başlangıç/bitiş olayları
  zaman sırasıyla ve bir kez işlenir; hızlı uyku bunları atlayamaz.

### İlk senaryodaki olaylar

| Olay | Başlangıç ayarı | Etki |
|---|---|---|
| Radyasyon uyarısı | t=60 | “5 simülasyon dakikası kaldı” bilgisi |
| Radyasyon olayı | t=360, süre 60 sn | Dışarıdaki ekipte artan oyun risk puanı |
| İkinci radyasyon olayı | t=660, süre 60 sn; uyarı t=600 | Gece/uyku sırasında da işlenme kontrolü |
| Toz olayı | t=780, süre 180 sn; uyarı t=750 | Gündüz güneş üretimine ayarlanabilir azalma |

Gösterilen geri sayım simülasyon süresidir. Hızlı uykuda daha hızlı azalır; duraklatmada
durur. Tüm olaylar sabit, tekrarlanabilir bir senaryo dosyasından gelir.
İlk mekanik prototipindeki olay tarihleri ve sayılar tasarlanmış oyun verisidir;
NASA olayı veya tahmini gibi gösterilmez.

Bir tick içindeki sıra: zaman sınırındaki olaylar → otomatik kaynak işlemleri →
ekip ve görev ilerlemesi → görev tamamlama etkileri → kritik durum kontrolü →
sol raporu → görev sonu. Sınırda biten bir görev yeni güne bir kez yansır.

## 5. Kaynaklar ve otomatik sistemler

Ana arayüzde batarya/enerji, su ve oksijen rezervi görünür. Bunlar ilk sürümde
0–100 arası oyun birimleridir; yüzde, kWh, litre veya gerçek basınç gibi sunulmaz.

- Güneş paneli gündüz bataryayı doldurur; gece üretmez. Toz üretimi düşürür.
- Yaşam desteği su ve elektrik kullanarak oksijen rezervini artırır.
- Üç astronot uyurken de oksijen ve su tüketir; tüketim sayıları ayarlanabilirdir.
- Su geri kazanımı yalnızca biriken atık sudan su üretir; yoktan su yaratmaz.
- Araştırma elektrik tüketir ve görev ilerlemesi kazandırır.
- İşleyen otomatik modül için başında sürekli astronot beklemesi gerekmez.
- Enerji kıtsa öncelik sırası yaşam desteği → su geri kazanımı → araştırmadır.
  Oyuncu araştırmayı kapatabilir; oksijen üretimi için enerji ayırmanın sonucunu görür.
- Bir modül yalnızca kendisine gerçekten ayrılan enerji ve mevcut girdiler kadar
  üretir; sıfır enerjiyle üretim veya negatife düşen rezerv oluşmaz.
- Kapasite sınırları uygulanır, karşılanamayan tüketim raporlanır. Eksik girdide
  durma süresi de tick içinde hesaba katılır; kaynakları sonda kırpmak yeterli değildir.
- Elektriğin bitmesi tek başına anında kayıp değildir; rezervler ve durmuş sistemler
  oyuncuya toparlanma fırsatı verir.

Denge ayarları ilk önce otomatik sistemler açıkken iki solun tamamlanabildiği bir
başlangıç sağlar. Gerçek görev verimi ve tüketim oranları henüz iddia edilmez.

## 6. Ekip, görevler ve stamina

Her astronotun kalıcı kimliği, adı, stamina değeri (0–100), konumu (iç/dış),
durumu ve varsa aktif görevi bulunur. İlk sürümde uzmanlık bonusu yoktur.

Durumlar: Idle, Working, Resting, Returning, Sheltered, Sleeping, Exhausted.
Bir astronot aynı anda tek durumda ve tek görevde olabilir.

| İş | Önerilen süre | Stamina | Sonuç |
|---|---|---|---|
| Dış panel bakımı | 60 sn | −0,35/sn | Panel kirlenme kaybını azaltır; atmosferdeki tozu silmez |
| İç sistem bakımı | 45 sn | −0,20/sn | Görevde tanımlı ekipman bakımını tamamlar |
| Araştırma | Kesilebilir sürekli iş | −0,15/sn | Enerji mevcutsa araştırma ilerlemesi |
| İçeride dinlenme | İstendiğinde kesilir | +0,20/sn | Stamina toparlar; iş üretmez |
| Gece uykusu | Kalan gece süresi | +0,40/sn | Stamina toparlar; kaynak tüketimi sürer |
| Üsse dönüş | 10 sn | −0,10/sn | Dönüş tamamlanana kadar dışarıda sayılır |

Bu değerler başlangıç önerisidir, gerçek insan fizyolojisi değildir. Gündüz boşta
durmak stamina doldurmaz; görev, dinlenme ve uyku anlamlı seçimler olur.

- Stamina sol başında sıfırlanmaz veya otomatik 100 olmaz.
- Stamina 20 altındaysa uyarı; 0'da görev durur. Astronot kaybolmaz/ölmez.
- Dışarıda tükenen astronotun güvenli dönüş komutu kullanılabilir; dönüş 0 stamina ile
  de tamamlanır. Bitkinlik, oyuncuyu kurtarılamayan bir duruma sokmaz.
- Tamamlanan tek seferlik görev ödülü bir kez verilir. Görev ilerlemesi görev
  örneğinde tutulur; iptal/yeniden atama ödül çoğaltamaz.
- Bir görev noktası aynı anda tek astronota aittir; döndüğünde veya işi bıraktığında
  serbest kalır. Görev durması ve yeniden başlamasının kuralları arayüzde anlaşılırdır.
- Dışarı çıkma ilk sürümde mantıksal geçiş ve basit görsel yer değişimidir;
  “İçeri çağır” anında korunma sağlamaz, dönüş süresi işler.

## 7. Uyku ve sol geçişi

1. Gece başladıysa oyuncu “Ekibi uyut” komutunu verebilir.
2. Dışarıda veya dönüşte biri varsa önce içeri çağırması istenir. İçerideki aktif
   işler durdurularak ekip Sleeping durumuna alınır.
3. Oyun 8x ilerler. Otomatik yaşam desteği, kaynak tüketimi ve olaylar aynı kurallarla
   çalışır; ayrı bir gece hesabı kullanılmaz.
4. Ekibin tamamı korunaklı alandadır. Radyasyon olayı yine başlar/biter ve raporlanır;
   uyku korunak dışında kalmaya karşı sihirli bağışıklık sağlamaz.
5. Kritik oksijen uyarısında (ilk ayar: 20 birim altına düşüş) uyku iptal edilir,
   hız 1x olur ve simülasyon duraklar. Oyuncu açıklamayı görüp devam eder.
   Uyarı eşik geçişinde bir kez tetiklenir; her karede tekrar etmez.
6. Sol sınırında hız 1x'e döner, gece uykusu biter ve kısa rapor açılır. Rapor açıkken
   oyun durur. Rapordan sonra ekip Idle veya stamina 0 ise Exhausted olur.
7. Oyuncu uykuyu iptal edebilir; yalnızca geçen süre kadar toparlanma korunur.

Sol raporu: başlangıç/bitiş rezervleri, çalışılan işler, stamina değişimi, yaşanan
olaylar ve bir kısa neden–sonuç açıklaması. Araştırma ilerlemesi ertesi güne taşınır.

## 8. Radyasyon, başarısızlık ve görev sonu

- Korunak, zaten var olan sabit bir üs bölmesidir. İlk sürümde kalkan inşa sistemi yok.
- Radyasyon aktifken içeride maruziyet azalır, dışarıda artar; tam risk katsayıları
  ayarlardan gelir. “İçerisi tamamen radyasyonsuzdur” bilimsel iddiası yapılmaz.
- Gösterge “eğitim amaçlı risk puanı”dır; gerçek doz veya sağlık tahmini değildir.
- Ekip risk sınırı aşılırsa görev güvenlik nedeniyle sonlandırılır; ölüm sahnesi yok.
- Oksijen rezervi sıfırda 20 simülasyon saniyesi kalırsa kurtarma/yeniden deneme ekranı
  açılır. Arada oksijen sağlanırsa kesintisiz eksiklik sayacı sıfırlanır.
- Sol 2 tamamlandığında, ekip güvenliyse ve yapılandırılmış araştırma hedefi
  karşılandıysa başarı; hedef karşılanmadıysa açıklamalı yeniden deneme sonucu.
- Başarı/kayıp durumunda tüm simülasyon donar. Son kritik kontrol başarıdan önce yapılır.
- Yeniden başlatma saati, olayları, kaynakları, ekip durumlarını ve tek seferlik etkileri
  ilk senaryo durumuna getirir; yinelenen abonelik veya olay kalmaz.

## 9. İlk sahne ve arayüz

- Sabit üs çizimi, üç seçilebilir astronot, panel, korunak ve araştırma noktası.
- Üstte sol/gündüz–gece, üç rezerv ve simülasyon hızı.
- Seçili astronotta stamina, mevcut iş, görev/dinlen/dön komutları.
- Yaklaşan olay ve geri sayımı; olay aktifken farklı ve açık durum mesajı.
- Duraklat/devam, gece uyku ve yeniden başlatma düğmeleri.
- Renk yanında yazı/ikon; okunaklı metin; eğitim uyarısında düşünme zamanı.
- Geçici şekiller ve basit hareket yeterli. İllüstrasyon ve animasyon sonraya.
- İlk UI dili İngilizce; metinler tek yerde toplanır, Türkçe kolayca eklenebilir.

## 10. Küçük teknik yapı

Önerilen kök: `Assets/MarsOutpost/`. Unity'nin mevcut paketleri korunur.

| Katman | Sorumluluk |
|---|---|
| Runtime/Simulation | Unity sahnesinden bağımsız C# durum ve zaman mantığı |
| Runtime/Presentation | Tek SimulationRunner, HUD ve sahne görselleri |
| Editor | Gerekirse yalnızca proje sahnesini kuran küçük araç |
| Data | Senaryo ve denge ayarları; kaynak açıklamaları |
| Scenes | MarsOutpost prototip sahnesi |
| Tests/EditMode | Zaman, uyku, kaynak ve görev sınırı testleri |

Temel sözleşme: `MissionSimulation(config, scenario)`; `Tick(double seconds)`;
`AssignTask(crewId, taskId)`, `Rest(crewId)`, `ReturnToShelter(crewId)`,
`BeginSleep()`, `CancelSleep()`, `SetResearchEnabled(bool)` ve `Restart()`.
Komutlar başarı/ret ve okunabilir neden döndürür. `State` UI tarafından okunur;
UI rezervleri veya ekip durumlarını doğrudan değiştiremez.

T01'de sabitlenen sözleşme:

- `MissionConfig(double solSeconds = 720, double daySeconds = 480, ResourceConfig resources = null)`
- `MissionScenario(IEnumerable<CrewState> crew = null)`; varsayılan üç astronot.
- `MissionSimulation(MissionConfig config, MissionScenario scenario)`
- `MissionState State { get; }`: değiştirilemeyen anlık durum; `ElapsedSeconds`,
  `SolNumber`, `SecondsIntoSol`, `IsNight`, `Crew`, `Tasks`, `Events`, `Resources`.
- `void Tick(double seconds)` ve `void Restart()`.
- `CommandResult AssignTask(string crewId, string taskId)`, `Rest(string crewId)`,
  `ReturnToShelter(string crewId)`, `BeginSleep()`, `CancelSleep()`,
  `SetResearchEnabled(bool enabled)`; sonuçta `Success` ve `Reason` bulunur.

T02 ile `ResourceConfig` üzerinden kapasite, başlangıç rezervleri ve saniyelik
oranlar ayarlanır. Değerler sonlu ve 0–1.000.000 aralığında olmalıdır; başlangıç
rezervi kapasiteyi aşamaz. `State.Resources` batarya, su, oksijen, atık su,
karşılanamayan su/oksijen tüketimini ve araştırma isteğini salt okunur sunar.
Tüketim geçen simülasyon süresine bağlıdır; ileride uyku da aynı Tick yolunu kullanır.

Üretim, gündüz/gece ve depo boşalma/dolma sınırlarında yeniden hesaplanır.
Kaynak karşılaştırmalarında test toleransı 1e-7 oyun birimidir. Patolojik büyüklükte
bir Tick için 100.000 kaynak geçişi sınırı vardır; aşılırsa açık hata döner ve
durum değişmez. Runner zamanı küçük adımlara bölmelidir; süre sessizce atlanmaz.

Saat, yeniden başlatma ve kaynaklar uygulanmıştır. Ekip yalnızca kimlik/ad taşır;
stamina ve görev davranışı T03'te eklenecektir. TaskState/EventState davranışları
sonraki görevler için ayrılmıştır; koleksiyonları boştur. `SetResearchEnabled` artık
araştırma isteğini değiştirir ve başarılı sonuç döndürür; T03'te astronot ataması
eklenene kadar araştırma enerji tüketmez veya ilerleme kazandırmaz. Diğer görev/uyku
komutları açıklamalı ret verir. Uyku, pause/odak runner'ı ve olay işleme henüz
uygulanmamıştır. Sonraki görevler bu davranışları aynı sözleşmeye ekler.

Harici DI, genel event bus, ağ altyapısı veya her
iş için ayrı yönetici sınıfları kurulmaz. Zaman, ekip, kaynak ve olayların tek sahibi
simülasyondur. Özel assembly kullanılırsa EditMode test assembly referansları da kurulur.

## 11. Aşamalar ve bitiş tanımı

1. Saat ve duraklatma çalışır.
2. Enerji–su–oksijen birbirini etkiler.
3. Astronot görev yapar, yorulur ve dinlenir.
4. Radyasyon uyarısı ve korunma çalışır.
5. Uyku aynı simülasyonu hızlandırır ve sol raporu üretir.
6. Bu döngü gerçek bir sahnede düğmelerle oynanır ve yeniden başlatılır.
7. Sonra NASA kaynakları ve öğretici eklenir; sunulabilir sürüm doğrulanır.

Her aşama küçük ve doğrulanmış teslimdir. İlk test edilebilir sahne için tüm
görselleri, NASA API bağlantısını veya kampanyayı beklemeyin.

## 12. NASA verisi ve bilimsel kapsam

Temel mantık ilk önce açıkça etiketlenmiş tasarım verileriyle çalışır. Yarışmaya
sunulabilir sürüm için T08'de en az bir doğrulanmış NASA kaynağı kararları anlamlı
biçimde etkiler; sadece görsel arka plan eklemek yeterli kabul edilmez.

- ECLSS: su–enerji–oksijen ilişkisini açıklamak için bilimsel dayanak.
- DONKI: tarih ve kimliği doğrulanmış arşiv olayından eğitim senaryosu üretmek.
- Gerçek kayıt, oyunun zamana sıkıştırması ve tasarlanmış risk katsayıları ayrı açıklanır.
- DONKI kaydından doğrudan Mars yüzeyindeki astronot dozu hesaplanmış gibi davranılmaz.
- Arşiv veri canlı veri olarak sunulmaz; kaynak, tarih ve dönüşüm açıklaması eklenir.

Kaynaklar:
- https://www.spaceappschallenge.org/2026/challenges/junior-astronaut-mission-trainer/
- https://www.spaceappschallenge.org/resources/judging-awards-guide/
- https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/
- https://ccmc.gsfc.nasa.gov/tools/DONKI/

## 13. İlk sürüm dışında kalanlar

Serbest bina yerleştirme, gelişmiş yol bulma, doğrudan karakter kontrolü, multiplayer,
karmaşık envanter, tarım simülasyonu, iletişim gecikmesi sistemi, beceri ağacı,
canlı veri zorunluluğu ve kayıt/yükleme ertelenir. Gıda ile iletişim gecikmesi görev
hedeflerinin parçasıdır; T08'de yarışma kapsamı açısından yeniden değerlendirilir.

Sonraki revizyonlar: erken uyuma, vardiyalar, bireysel uyku, görev uzmanlıkları,
sera/gıda, gecikmeli Dünya tavsiyesi, daha uzun kampanya ve alternatif senaryolar.

## 14. Ekipçe daha sonra kararlaştırılabilecekler

- Oyuncu astronotu doğrudan mı hareket ettirecek, görev mi atayacak?
- Sol ve gündüz/gece uzunluğu oyun denemesinden sonra nasıl değişmeli?
- Uyku yalnızca gece mi, gündüz de mümkün mü?
- İlk hedef tarayıcı mı masaüstü mü? Tarayıcı tercih edilir; build desteği doğrulanmalı.
- Hikâye, karakter adları, görsel stil ve hedeflenen oturum süresi.

Bunlar planı hazırlamaya engel değildir. İlk uygulama yukarıdaki varsayımlarla ilerler;
ekip kararı gelirse plan güncellenir.
