# V3 küçük teslimler — OP00–OP32

Her OP ayrı çalışır teslimdir. Bir özelliğin aşağıdaki alt adımları raporda tek
tek işaretlenir. Limit bir OP'nin ortasında biterse kaldığı alt adım saklanır;
sonraki oturum tüm özelliği yeniden üretmez. Başka bir OP'yi aynı anda başlatma.

## Her OP için zorunlu çalışma protokolü

1. PROGRESS, ilgili sözleşme ve gerçek kaynakları oku; başka ajanın aktif dosya
   yazdığını/Unity projeyi açtığını görürsen aynı dosyalarda yarışma.
2. Kodu değiştirmeden `checkpoints/OP-ID.md` oluştur, InProgress yap; 3–6 alt iş,
   dosya sınırı, mevcut sağlam nokta ve ilk yapılacak işi yaz.
3. Küçük değişiklik → derleme/ilgili anlamlı test → rapor güncellemesi. Yeni
   kalıcı state için Clone/Snapshot/Save/Load/Restart aynı teslimin parçasıdır.
4. Kanıtı `docs/validation/v3-opXX-*` altında sakla. Gerçek komut, exit code,
   test sayısı, build/görsel yolu ve doğrulanmayanları yaz. Geçici /tmp logu
   kaybolabileceğinden sonuç özeti/başarısızlık mesajını checkpoint raporuna da koy.
5. Kabul sağlandıysa Done. Başarısız test/derleme varsa InProgress; dış donanım
   veya manuel görüntü eksikse NeedsVerification. Çalıştırılmayan test başarılı sayılmaz.
6. PROGRESS current/next ve tablosunu güncelle; sonra sıradaki tek OP'ye geç.
   Limit yaklaşınca yeni büyük işe başlama. Tahmin edilen kalan token sayısı uydurma.

**Kapı kuralı:** mekanik/derleme/test başarısızlığı bağımlı işi durdurur ve önce
düzeltilir. Sadece erişilemeyen ikinci monitör veya çocuk playtesti gibi dış
doğrulama eksikse bunu NeedsVerification olarak taşı, bağımsız kod işine devam
et; ilgili görsel kabulü Done yapma. Kullanıcı ayrıca istemedikçe izin sorarak
rutin kontrol/uygulamayı kesme. Commit/push/yayınlama yapma.

## OP00 — Yeni başlangıç ve kapsam fotoğrafı

Bağımlılık: yok. Dosyalar: bu paket, V2 REVIEW, gerçek kod, test kanıtları.

1. AGENTS, PLAN, FEATURES, INTEGRATION, mevcut git durumunu oku; takip edilmeyen
   kodları da mevcut kullanıcı çalışması say. Aktif Unity/diğer yazar durumunu kontrol et.
2. V2 91/17 iddiasını mevcut Unity sürümünde bağımsız tekrar koş; değişmişse gerçek sonucu yaz.
3. Eksik grid/inşa/rover sınıflarını ve var olan çıkarıcı/panel sistemini envanterle;
   zaten yapılmış ek iş varsa tekrar üretme, davranışı kanıtla.
4. `BASELINE.md` yaz: sürüm, kaynak/binary yolu, testler, legacy save fixture yolları,
   bilinen V2 ekran doğrulama açıkları ve korunacak değişiklikler.

Kabul: yeni baseline gerçek kanıtlı; uygulama durumları önceden Done yapılmamış.

## OP01 — Parsel veri modeli ve deterministik harita

Bağımlılık: OP00. Sözleşme: F01.

1. Unity bağımsız GridCoord/map config/cell state ve world↔cell dönüşümlerini ekle.
2. Mevcut terrain sınırları içinde tek 16×16 map; starter footprint/çıkış koridorunu
   gerçek konumlarla eşleştir; starter solar üç panelin tek tesis ilişkisini tasarla.
3. Ayrı map stream'inden sabit zemin/verim/kaynak sınıfı ve sonlu rezerv üret;
   ulaşılabilir en az iki başlangıç taranmış Ice hücresi garanti et.
4. Negatif koordinat/sınır/komşuluk/aynı seed testleri; map üretiminin beş olay
   kanalının state/tarihlerini değiştirmediğini kanıtla. Henüz inşaat UI'sı açma.

Kabul: aynı seed aynı harita; noEngineReferences korunur; terrain/view ile tek dönüşüm sözleşmesi.

## OP02 — Tahsis, malzeme ve ilk kayıt göçü

Bağımlılık: OP01. Sözleşme: F01–F02; INTEGRATION/MIGRATION.

1. Map/starter tesislerini Operations mutable state ve snapshot'a bağla; Training/Demo'ya sızdırma.
2. 40/80 malzeme, Sol başına +8 ve lastSupplySol marker'ı; 2 malzemelik atomik ClaimParcel.
3. Şema 3/revision 1; v1/v2→map/starter göçü, panel/çıkarıcı tek sahip, günlük
   malzeme/seed/sayacın round-trip'i. Eski 3-panel kayıt reddini doğru kurala geçir.
4. Harita dışı/tahsissiz komşuluk/yetersiz malzeme/çift tık; pause ve aynı Sol'da
   yükleme; migration idempotence ve eski tehlike/rapor korunumu testleri.

Kabul: iki kez ödeme/tedarik yok; eski oyuna ücretsiz ikinci panel veya çıkarıcı eklenmez.

## OP03 — Grid görünümü ve parsel seçimi

Bağımlılık: OP02. Sözleşme: F01, F12.

1. Operations'a aç/kapat grid overlay, seçili hücre çizgisi ve terrain raycast'i ekle.
2. Seçim kartında tahsis/keşif/zemin/hat durumu, malzeme ve Tahsis Et düğmesi.
3. UI üstü tık, kamera sürükleme, harita dışı ve Escape filtrelerini ayır; mevcut
   astronot/istasyon seçimini koru. Runner bildirimlerini kullan.
4. PlayMode hücre seçimi/atomik ret ve 1440×900 grid görüntüsü.

Kabul: görsel seçilen hücre domain koordinatıyla aynı; gizli jeoloji gösterilmez.

## OP04 — Yüzey rotası ve domain iş aşamaları

Bağımlılık: OP03. Sözleşme: F04.

1. Dört komşulu deterministik nav ve uygun bitişik çalışma hücresi seçimini ekle.
2. SurfaceJob: hedef ID, ekip, rota, konum, Travel/Work/Return, süre/ilerleme.
3. Integratöre rota varış/segment/çalışma sınırlarını ekle; seyahatte iş/maliyet
   tamamlanmasın. Eski Returning sayacının yeni rotayı ikinci kez saymasını önle.
4. Assign/Replace/Cancel/Rest/Return/Sleep/EarthOrder sahipliğine ortak release
   entegrasyonu; reddin mevcut işi koruduğunu ve exhaustion'u test et.
5. Yeni rota state'inin deep copy, save/revision, yükleme ve restart kapsamını tamamla.

Kabul: büyük/küçük Tick eşdeğer; yol yok ret; yol ortası yüklemede konum/ilerleme aynı.

## OP05 — Yüzey yürüyüşü, geri çağırma ve kamera

Bağımlılık: OP04. Sözleşme: F04, F12.

1. Yeni yüzey işi için WorldView'de domain pozisyonunun tek render sahibini kur;
   önceki/güncel örnekler arasında interpolasyon ve yol teğetine yönelme.
2. Yeni uzak işten hava kilidine gerçek rota dönüşü; eski Training/Demo 10/4sn
   testlerini koru. İç görev varıştan önce başlamasın.
3. Seçili iş/rota/kalan yol ve Dur/Geri Çağır UI; kamera map sınırlarına pan yapabilsin.
4. 15/30/60 FPS, pause, 1x/8x ve dış iş→dönüş→iç görev PlayMode kontrolleri.

Kabul: geri geri yürüme, 0,25sn sıçrama ve iki MoveTowards yazarı yok; güncel görsel kanıt.

## OP06 — Yapı kataloğu, hayalet ve atomik blueprint

Bağımlılık: OP05. Sözleşme: F02–F03.

1. Config'de yapı maliyet/footprint/iş tanımları ve kalıcı Facility/Blueprint ID'si.
2. Yerleşim doğrulayıcı: tahsis, zemin, sınır, slot/hat katmanı, erişim/çıkış yolu,
   malzeme ve limitler. Tek komutta maliyet + rezervasyon, ret durumunda fark 0.
3. Ghost/rotasyon/iptal, geçerli-geçersiz renk + metinsel gerekçe ve fiyat UI.
4. Blueprint save/revision; çift tık/same frame iki komut, işgal/koridor ve
   UI tıklarının yerleştirme üretmemesi testleri. Bitmemiş yapı hiçbir üretim vermez.

Kabul: maket yerleştirmek otomatik tamamlanmış tesis veya bedava kaynak yaratmaz.

## OP07 — Astronotla inşa ve hizmete alma

Bağımlılık: OP06. Sözleşme: F03–F04, F07.

1. Blueprint'e tek ekip atama → uygun komşuya Travel → net Work → Commissioned.
2. Enerji/stamina kısmi akışını ve bitiş sınırını integratöre bağla; hayat desteği önce gelir.
3. Pause/cancel/replace/exhaustion/geri çağırmada ilerleme ve ödeme korunur;
   tamamlanma bayrağıyla kapasite/tesis etkisi en çok bir kez uygulanır.
4. İnşaat aşaması/alet hareketi/progress UI; yol/iş/bitim ortası kayıt ve tekrar atama testleri.

Kabul: rota süresi çalışma sayılmaz; enerji yokken iş ücretsiz bitmez; görsel durum kayda bağlıdır.

## OP08 — Bağlantı hattı ve üs ağı

Bağımlılık: OP07. Sözleşme: F05.

1. Aynı inşa akışıyla hat hücrelerini tamamla; çekirdek port/starter hardwire modeli.
2. Dört yönlü graph ve tesis bağlantısı; tamamlanmamış/çapraz/izole ağ bağlantı sayılmaz.
3. Hat ekleme/kaldırmada deterministic topoloji yenile; ilgili state'i kaydet.
4. UI/görsel hat uçları ve Bağlantı Yok gerekçesi; loop, kopma, save ve starter
   kaynak dengesi testleri. Çekirdek dönüş yolunu kapatma.

Kabul: görünür hatla hesaplanan bağlantı aynıdır; henüz bağlantısız tesiste etki 0.

## OP09 — Kurulabilir yeni güneş paneli

Bağımlılık: OP08. Sözleşme: F06.

1. Commissioned panel için tek PanelUnitState, tesis sahibi/konumu ve Weight=1/3.
2. Gün/yerel katsayı/bağlantı/fouling/dust bileşimini ekle; başlangıç üçlünün toplamını koru.
3. Canlı panel sayacı ve seçili panel çıkışı; eski healthy/3 sabitlerini uygun
   toplamlarla değiştir. Her yeni panel ayrı dünya nesnesi/workpoint olsun.
4. 3→4 panel gücü, gece, bağlantısız, yarım blueprint, toz ve aktif arızada
   üretim testleri; dört panelin Linux/PlayMode görseli.

Kabul: dördüncü panel gerçekten SolarRate/3 × yerel katsayı ekler; yeniden 1/N yapılmaz.

## OP10 — Yeni panel hedefi ve tekil tamir

Bağımlılık: OP09. Sözleşme: F06, V2 kanalları.

1. Solar kanal adaylarını hizmette uygun panel listesine bağla; ActiveTargetId'yi sabit tut.
2. Mevcut bütün-panelleri-düzelt döngüsünü tek hedef tamirine geçir; hedefe yürüme,
   iptalde ilerleme ve onarım sonrası kanalı yeniden planlama.
3. Yeni panel/hat kurarken diğer dört kanalın RNG state/tarihinin değişmediğini test et.
4. HUD'da arızalı hedef/seçili panel/onarım geri bildirimi; kum/temizlik/tamir ayrımı ve
   aktif hedef söküm reddi. Hedef ortası save ve çift onarım testi.

Kabul: bir panelin tamiri diğerini temizlemez/onarmaz; V2 süreleri değişmez.

## OP11 — Kurulabilir batarya ve etkin kapasite

Bağımlılık: OP10. Sözleşme: F07.

1. Tamamlanmış bağlı batarya başına +25; yeni banka boş katkıdır, mevcut enerji aynı kalır.
2. Tüm enerji Limit/Apply/SnapBoundary, snapshot, rapor ve UI'da etkin kapasiteyi kullan.
3. Bağlantı kopması/reconnect için taşma defteri ve önizleme; ücretsiz yeniden dolum yok.
4. 100→125 kapasite, gece deşarjı, tam dolum sınırı, kopma, pause ve yükleme testleri.

Kabul: 100 üstü enerji geçerli ve kaydedilebilir; stamina/risk 100 kuralı yanlış değişmemiş.

## OP12 — İptal, söküm ve maliyet bütünlüğü

Bağımlılık: OP11. Sözleşme: F03.

1. Ghost iptali/yerleştirilmiş blueprint iptali/tamamlanmış tesis sökümü ayrı komutlar.
2. Bir defalık %50 malzeme, kapasiteye sığan iade; starter/aktif hedef/dolu sığınak/
   çalışan ekip/rota kapatma engelleri. Topoloji/panel sahipliği atomik güncellensin.
3. Tesis yokken ikinci tık, iptal→yükleme→iptal ve sök→yükle→sök ödül kopyalama testleri.
4. Enerji kaybı/malzeme iadesi UI önizlemesi ve state defteri; başlanmış inşa enerjisi iade edilmez.

Kabul: ödemeler ve iadeler izlenebilir; harita, panel, iş ve ağ referansları kopuk kalmaz.

## OP13 — Kurulabilir buz su çıkarıcı

Bağımlılık: OP12. Sözleşme: F08.

1. Taranmış uygun başlangıç buz parseline çıkarıcı kataloğunu aç; yanlış yer/tür ret.
2. Site sahibi, finite RemainingWater ve gerçek ek çıkarma akışı; legacy-extractor bir kez sayılır.
3. Enerji önceliği, ağ, ana/per-site anahtar, global ×0,50 servis arızası, tank
   boşluğu ve stok tükenmesi sınırlarını birleştir; aynı checkpoint'te kayıt/revision.
4. Sıfır enerji, dolu tank, stok tükenme ve büyük/küçük Tick; 10 bilim onarımı
   su stokunu artırmıyor ve diğer olayları sıfırlamıyor testleri.

Kabul: yeni makine tanka ölçülebilir su ekler; aldığı su sahadan aynı miktarda düşer.

## OP14 — Su tesisi görünümü ve anlaşılır telemetri

Bağımlılık: OP13. Sözleşme: F08, F12.

1. Blueprint/kurulum/aktif/durmuş/tükenmiş görünümü, sondaj başı ve akış/yoğuşturma
   vurgusu; üretim durunca animasyonun nedenini göster.
2. Seçili tesis: yer, bağlantı, enabled, global servis, fiili su/sn, enerji/sn,
   bilinen kalan stok ve tahmini tükenme; oran 0 ise sonsuz süreyi metne düzgün çevir.
3. Ana su kartında toplam çıkarma/geri kazanım ayrımı; bir tık tesis odağı/geri çağırma.
4. Pause/8x, arıza ve yükleme sonrası görünüm PlayMode + gerçek grafik kanıtı.

Kabul: dönen matkap tek başına başarı değildir; gösterilen rate ile domain akışı aynıdır.

## OP15 — Teslim A: kur–bağla–üret–kaydet

Bağımlılık: OP14. Sözleşme: Teslim A kalite kapısı.

1. Yeni oyundan tahsis → hat → dördüncü panel → batarya → su tesisi akışını
   HUD komutlarıyla çalıştır; ücret/yol/enerji değişimlerini raporla.
2. Aynı senaryoda iş yarıda kayıt, açılış, tamir ve söküm; Training/Demo regresyonu.
3. İlgili EditMode + tam PlayMode koşusu, Linux build ve normal 1440×900 açılış görüntüsü.
4. DELIVERY_A.md: gerçekten tamamlananlar, gerçek sayılar/exit code, kalan görsel açıklar.

Kabul: ilk teslim kendi başına oynanır ve save'den devam eder; sonraki keşif koduna bağımlı değildir.

## OP16 — Keşif işi ve kalıcı keşif sisi

Bağımlılık: OP15'in mekanik kabulü. Sözleşme: F09.

1. Explore hedefini yalnız görünür sınırın komşusuna kabul et; uygun nav hedefi ve dönüş.
2. Ekip gerçekten ilerledikçe keşfi aç; görev kuyruğu/tık anında tüm haritayı açma.
3. Kalıcı explored ile geçici görünür overlay'ini ayır; gece/kum geçmişi silmez.
4. Save/revision, büyük Tick, cancel/pause, gizli bilgilerin UI'da görünmemesi testleri.

Kabul: oyuncu fiziksel keşifle inşa alanını genişletir; mevcut A kaydı geçişte silinmez.

## OP17 — Yerel kaynak taraması

Bağımlılık: OP16. Sözleşme: F08–F09, SOURCES.

1. Keşfedilmiş parselde 30sn/2 enerji dış Survey işi; tahsis gerektirmeden doğru yere git.
2. Yalnız bitişte kaynak türü/verim/stok bilgisini aç; aynı tamamlanan tarama tekrar ödeme yapmaz.
3. Parsel kartı ve kaynak haritası; kısa NASA SWIM bağlamı/Kaynaklar bağlantısı,
   sentetik yeraltı haritası ayrımı. Gerçek metre çözünürlüğü iddiası ekleme.
4. Uygun/uygunsuz saha, stok, iptal/yeniden ata ve save testi; tarama bilim ödülü 0.

Kabul: keşif bilgi verir, tarama su tesisinin yerini seçtirir; ikisi farklı işlerdir.

## OP18 — Alternatif regolit fırını

Bağımlılık: OP17. Sözleşme: F08, F02.

1. HydratedRegolith sahasına özel tesis türü ve yüksek enerji/su oranı; Ice ile karıştırma.
2. Aynı inşa/şebeke/site stok/arıza/depo sınırlarını kullan; ikinci paralel su integratörü yazma.
3. Isıtma/yoğuşturma/stop görünümü ve iki yöntemi fiili üretim/maliyetle karşılaştıran kart.
4. Aynı enerji bütçesinde yöntem farkı, finite stok, dolu tank ve save/onarım testleri.

Kabul: alternatif tesis gerçekten farklı kaynakta farklı enerji tercihi oluşturur; her yerde su garantisi yok.

## OP19 — Ek su tankı

Bağımlılık: OP18. Sözleşme: F07–F08.

1. Tank kataloğu ve bağlı tamamlama başına +50 kapasite; başlangıç suyu 0 katkı.
2. O₂/geri kazanım/çıkarıcı Limit/SnapBoundary/HUD/save doğrulamalarında etkin su kapasitesi.
3. Tank/hat sökümünde excess su defteri ve iade; reconnect bedava su üretmez.
4. 100→150, dolum, zero energy, kopma ve kayıttan devam testleri.

Kabul: 100 üstü su gerçek/kalıcı; kapasite eklemek üretim değildir.

## OP20 — Mini harita ve harita odaklama

Bağımlılık: OP19. Sözleşme: F09, F12.

1. Aynı grid'den düşük maliyetli mini harita; üs/hat/tesisi, ekip ve bilinen saha ikonları.
2. Keşif overlay ve seçili hücre; tıklama yalnız odağı/seçimi değiştirir, inşa emri vermez.
3. Kamera viewport/üs reset ve ekran boyutuna uyum; UI pointer filtreleri ortak kalsın.
4. Pause/çok yapı/yer değiştiren ekip/yükleme PlayMode ve ekran görüntüsü.

Kabul: ayrı bir harita gerçeği yok; bilinmeyen rezerv ikonları gösterilmez.

## OP21 — Kısa rehber, TR/EN ve geri bildirim

Bağımlılık: OP20. Sözleşme: F12.

1. Altı kısa adım: parsel seç → tahsis → hat → panel → tara → su tesisi.
2. İlerleme gerçek komut/olaydan gelir; düğmeye sadece tıklamak iş tamamlandı sayılmaz.
3. Ret gerekçeleri, tek seferlik tamamlanma/arıza sesi, stage/assignment/durum
   metinlerini TR/EN tamamla; yeni oyunda/yüklemede ses spam'ini önle.
4. Rehberi gizle/göster ve kayıt/tercih davranışı; düşük ekranda dock/panel taşma testi.

Kabul: kullanıcı teknik doküman okumadan ilk inşaatı yapabilir; ürün UI'sında checkpoint/DTO terimleri yok.

## OP22 — Teslim B uzun koşu ve ekonomi

Bağımlılık: OP21. Sözleşme: F01–F09.

1. Seed 101/202/303 ile 100-sol komut senaryosu; tahsis/inşa/bağlantı/tamir/site tükenme.
2. Sol 50'de gerçekten yüklenen kopyadan kalan 50 solu çalıştır; küçük/büyük Tick replay karşılaştır.
3. Malzeme/üretim/depo/taşma/araştırma dengesi ve RNG bağımsızlığı; maksimum yapı sayısı.
4. Gerekirse yalnız V3 öneri sayılarını ölçümle dengele; V2 olay aralık/yüzde/10 bilim kuralını değiştirme.

Kabul: kaynak kopyalama/NaN yok; sağlıklı başlangıç üssü sürdürülebilir; büyüme anlamlı tercih yaratır.

## OP23 — Teslim B Linux ve kullanılabilirlik

Bağımlılık: OP22. Sözleşme: ikinci kalite kapısı.

1. Gerçek Linux build + normal açılış/ayar sonrası yeniden açılış; önce 1440×900.
2. İnşa, keşif/tarama, fırın ve tankın state'leri için güncel görüntü; UI ve hareketi izle.
3. Erişilebilir diğer çözünürlükleri dene; erişilemeyeni NeedsVerification yaz.
4. DELIVERY_B.md ve PROGRESS; hata varsa bağımlı yeni özelliğe geçmeden düzelt.

Kabul: A+B beraber çalışır; kullanıcı/çocuk denemesi yapılmadıysa açık yazılır.

## OP24 — Ek sığınak modeli ve dinlenme yerleri

Bağımlılık: OP23'ün mekanik kabulü. Sözleşme: F10.

1. 2×2 tesis, geçiş portu ve 2 dinlenme slotu; üç crew ID'si korunur.
2. Slot/CurrentShelterId rezervasyonu; meşgul slot reddi, iptal/uyku/geri dönüş temizliği.
3. Yalnız içerideki kişiye 0,75 akut risk katsayısı; dış kişiye/global stoğa etki yok.
4. Dolu sığınakta söküm reddi, bağlılık kaybı/geri dönüş, save/revision testleri.

Kabul: yeni bina gerçek dinlenme hedefi ve oyun içi korunma farkı verir; ekip/kaynak üretmez.

## OP25 — Yeni sığınağın iç görünümü ve uyku

Bağımlılık: OP24. Sözleşme: F10, V2 görsel düzeltmeleri.

1. Dünya modeli, giriş/dönüş noktası ve iki doğru sleep anchor'ı; mevcut üç anchor korunur.
2. Korunmuş render hacim listesine yeni sığınak ekle; cutaway/çatı ve iç/dış efekt dışlama.
3. Dinlenme yeri UI ve yeni yatakta yatış/kalkış/etiket/halka; tek rota/poz sahibi.
4. Yeni slotta iki kişi, pause/8x, radiation+dust örtüşmesi ve save sonrası PlayMode/görsel kontrol.

Kabul: sığınak yalnız dış kabuk değildir; içeride ışın/toz ve yatak dışında beden yok.

## OP26 — Rover dok, araç ve şarj

Bağımlılık: OP25. Sözleşme: F11.

1. Dok/araç paketi tek inşa; completion ID ve 20 kapasiteli boş rover bataryası.
2. Bağlı doktan kalan güçle 0,02/sn şarj; aynı miktar üs enerjisinden gider; dolumda durur.
3. RoverState/konum/kargo/komut seçimi ve save/revision; ek kopya/ikinci dok retleri.
4. Yetersiz güç, full charge, pause/8x, dolu stoklar ve tekrar yüklemede araç çoğalmıyor testleri.

Kabul: tek rover gerçek ayrı enerji taşır; ücretsiz şarj veya anında ikinci araç yok.

## OP27 — Rover rotası, görev ve geri çağırma

Bağımlılık: OP26. Sözleşme: F11.

1. Ortak nav üzerinde rover Travel/Work/Return; crew task ve rota sahipliğini karıştırma.
2. Hedef+ev rotası enerji önizlemesi ve %10 pay; yetersiz bütçede atomik ret.
3. Hareket sırasında tüketim, yol değişiminde yeniden planlama, durma/geri çağırma,
   şarj bitince yerinde kalma. Araç asla ışınlanarak/eve ücretsiz dönmez.
4. UI/mini harita/takip; rota ortası kayıt, pause ve büyük/küçük Tick testleri.

Kabul: gidip dönme, enerji ve komut sonucu domain'e bağlıdır; çizilen rota otorite değildir.

## OP28 — Örnek taşıma ve tek seferlik analiz

Bağımlılık: OP27. Sözleşme: F11.

1. Parselden 30sn örnekleme, tek kargo ve ev/doka teslim; stabil SampleId/Origin.
2. Laboratuvarda astronotla 30sn analiz, Research/EarthOrder kapasite rezervasyonu.
3. Exploration parselinin ilk analizinde +5 toplam araştırma/+5 bakiye; kalıcı
   awarded-parcel defteri. Dünya emri numunesi her analiz yolunda 0 bilimdir.
4. İptal/aynı örnek/same frame çift komut/kargo ortası save/ödül anı load ve kaynak
   uyuşmazlığı testleri; ücretsiz numune teleportu yok.

Kabul: roverın işi örnek taşımaktır; haritaya tıklamak doğrudan bilim vermez.

## OP29 — Rover görünümü ve örnekleme animasyonu

Bağımlılık: OP28. Sözleşme: F11–F12, SOURCES.

1. Mevcut stile uygun altı tekerlekli model; basit eklem salınımı ve mesafeye bağlı teker dönüşü.
2. Rota teğetine yönelme, örnekleme kolu/dok boşaltma; ilerleme/kol hareketi iş süresine bağlı.
3. Aynı anda crew yürüyüşü + rover + inşa + kum/radyasyonda görünürlük ve kare maliyeti.
4. Pause/8x, 15/30/60 FPS ve ev dönüşü güncel görüntü; gerçek fizik veya fotogerçekçi asset iddiası yok.

Kabul: görsel takılma/geri kayma yok; iş/ödül animasyon event'inden üretilmez.

## OP30 — Tam kayıt uyumu ve 100-sol birleşik regresyon

Bağımlılık: OP29. Sözleşme: INTEGRATION/MIGRATION ve bütün F'ler.

1. Revision matrisini gerçek v1/v2/eski-v3/güncel-v3/bozuk fixture'larla tamamla;
   3+ panel, 100+ rezerv, göç ve idempotence.
2. İnşa/seyahat/survey/onarım/aktif iki tehlike/yeni sığınakta uyku/rover kargo/
   analiz-ödül sınırında round-trip + aynı ileri komut replay.
3. 100-sol multi-seed bütünleşme; yüklenen kopyadan devam; topoloji/ID/ödül/kapasite
   defteri tutarlı; Training/Demo ve ayar menüsü tam regresyon.
4. Maksimum veri sınırında performans; her frame Instantiate veya her tık tüm
   tarihi kopyalayan gereksiz UI üretimi varsa düzelt; kaydı truncate edip veri kaybetme.

Kabul: eski kayıtlar açıklamalı biçimde taşınır, yeni kayıt deterministiktir; hiçbir kırmızı test geçiştirilmez.

## OP31 — Son Linux, üç ekran ve kullanıcı akışı

Bağımlılık: OP30. Sözleşme: F12 ve V2 CP04/CP17 açıkları.

1. Güncel kaynakla Linux build; mevcut sahneyi yeniden üretmeden normal açılış.
2. 1440×900, 1920×1080, 1280×720; kaydedilmiş ayar → kapat → normal yeniden açış.
   Forced-resolution görüntüsüyle doğal açılış iddiası yapma.
3. Yeni rehberi izleyerek grid→tesis→keşif→rover→analiz; uyku/dönüş/iç efekt/menu
   kontrolleri ve güncel görüntüleri sakla. Büyük yazı/HUD çakışmasını düzelt.
4. Kullanıcı/çocuk denemesi yapılabildiyse gözlem kaydı; yapılamadıysa açık yaz.
   Erişilemeyen ekranları ve V2 eski görsel kabulleri NeedsVerification olarak izle.

Kabul: gerçek cihaz kanıtıyla görsel kabul; ölçülmeyen performans/sahte kullanıcı testi yok.

## OP32 — Son kapsam karşılaştırması ve teslim

Bağımlılık: OP31; dış doğrulama eksikleri açık taşınabilir.

1. FEATURES'in her başlığını gerçek kod ve test/görüntüyle eşleştir; eksik uygulamayı tamamla.
2. README, RELEASE_NOTES, VALIDATION, SCIENCE_SOURCES, FUTURE_ROADMAP'ın durum
   özeti ve bu PROGRESS'i son gerçek duruma göre güncelle. Eski raporları silme.
3. DELIVERY_FINAL.md: tamamlanan OP'ler, yeni API/kayıt sürümü, gerçek test/build
   komutları/sayıları, oyuncunun ne yapabildiği ve açık kalanlar.
4. Tüm çalışma ağacını kendi yazdığın sanma; mevcut kullanıcı değişikliklerini koru.
   Commit/push/yayın/başvuru yapmadan teslim et.

Kabul: "tamamlandı" gerçek kapsamı ifade eder. İleri GIS/kimya/araç fiziği
uygulanmış gibi gösterilmez; NeedsVerification'lar Done'a çevrilmez.
