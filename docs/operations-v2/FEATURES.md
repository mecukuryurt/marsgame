# Özellik sözleşmeleri ve adımlar

PLAN.md ile birlikte okunur. Sayısal kullanıcı gereksinimleri sabittir; aşağıdaki
ek süre ve maliyetler başlangıç tasarımı önerisidir ve config'de tutulur.

## A. Uyku/yatak görseli

Görüntüde astronot yatağın uzun ekseni yerine enine yatıyor. Kodda yataklar z
ekseninde 1,25 uzunluk/0,58 genişlikte; uyku pozu sabit (-85,90,0) dönüşü kullanıyor.
Bu, kesin fiziksel tanı değil, görselle doğrulanacak güçlü kod bulgusudur.

1. Her yatağa SleepAnchor (baş/ayak yönü, yatak üst düzlemi) ekle; kişiye sabit yatak ata.
2. Model pivotunu yatak anchor'ına göre dönüştür; dünya koordinatına sabit quaternion ezberleme.
3. Yatış/kalkış 0,5–1 sn görsel geçiş olsun. Dinlenme ile uyku pozları tutarlı,
   bacak/baş yatak dışında ve komşu yatakta görünmesin. Fiziksel kol/başta küçük
   doğal taşma kabul edilebilir; gövde yatak yüzeyinde olmalı.
4. Seçim halkası zemin düzleminde kalır, yatay modelle dönmez; isim etiketi baş üstüne taşınır.
5. Uyanma/işe geçiş yürüyüş ve çalışma dönüşünü temiz kurar; eski uyku yaw/pitch'i sızmaz.
6. Üç kişi birlikte uyku, ayrı dinlenme, 8x, pause ve peş peşe durum geçişini gerçek
   kamerada kontrol et. Bounds/anchor testine ek olarak ekran görüntüsü gerekir.

## B. Takılarak/geri geri dönüş

Mevcut PlaceOnReturnPath yalnızca State yenilenince konumu atıyor; normal Tick
0,25 sn olduğundan görsel sıçrama oluşabilir. Update sırasında Returning için
hareket deltası 0; yön ise eski sıradaki waypoint'e göre hesaplanabiliyor.

1. Tek rota sahibi belirle. Dönüş başında hava kilidinden geçen tam yol, segment
   uzunlukları ve başlangıç/bitiş zamanı bir kez hesaplanır.
2. Simülasyonun ReturnRemainingSeconds değeri otoritedir. Render karelerinde
   önceki/güncel simülasyon örnekleri arasında kontrollü interpolasyon yap;
   ikinci bir Tick veya geri sayım üretme. Ara değer kritik sınırlardan öteye taşmaz.
3. Yönü eski waypoint'e değil, o anki yol segmentinin teğetine çevir. Ani 180°
   ters dönüş yerine kısa ve sınırlı dönüş; yürüyüş döngüsü hareket mesafesiyle uyumlu olsun.
4. Segment aşılınca kalan mesafe sonraki segmente taşınır. Bir waypoint atlandı
   diye tekrar geriye dönülmez. Geri geri yürüyüş tasarlanmış ayrı durum olmadıkça kullanılmaz.
5. Dönüş bittiğinde doğru iç anchor'a geç; ikinci bağımsız MoveTowards aynı konumu yazmasın.
6. Pause'da hareket/geri sayım donar; 1x/8x ve düşük FPS'de rota aynı kalır.
   Dönüş sırasında yeni iç görev erken başlamaz. Varınca görev atamak pozu bozmaz.
7. Testler: 10 sn dönüş, yarı yol, son kare; hava kilidi geçişi; 15/30/60 FPS;
   ışınlanma/arka yürüyüş yok. Görsel toleranslar testte açık (öneri <0,05m son konum).

## C. İçerideki radyasyon çizgileri ve daha inandırıcı efekt

Mevcut ortak parçacık havuzu rastgele x/z konumları kullanıyor, habitat içini
dışlamıyor; fırtına ve radyasyon else-if ile aynı havuzu paylaşıyor.

1. Radyasyon, kum ve gece efektlerinin durum/havuzlarını ayır. Eşzamanlı iki olay
   birbirinin efektini silmemeli. Havuz sabit kapasiteli, her kare instantiate yok.
2. Radyasyon görünür renkli lazer yağmuru olarak sunulmasın. Tercih: dış ortamda
   hafif, seyrek şematik parçacık/atmosfer vurgusu + sensör/uyarı + az miktarda dış ışık değişimi.
3. Habitatın iç hacmi ve hava kilidi korumalı render hacmi olarak tanımlanır;
   spawn ve hareket bu hacimle kesişiyorsa parçacık dışarı alınır veya yeniden doğar.
   Sadece çatıyı görünür yapmak çözüm değildir; cutaway kamera açıkken de içeri çizgi düşmez.
4. Ekranın tamamını sürekli kırmızıya boyamak yerine dışarıda tehlike ve içeride
   korunma farkını koru; HUD okunur kalsın. Korunak tam sıfır gerçek doz iddiası taşımaz.
5. Kum, dışarıda yer seviyesinde rüzgâr/toz, görüş ve panel verimiyle ilişkilidir;
   basınçlı habitat içinde aynı toz akışı yok. Efektler pause ve hız ölçeğiyle uyumludur.
6. “Şematik eğitim gösterimi” açıklaması ekle. Fotogerçekçilik veya NASA ölçümü denmez.
7. Radyasyon+kum örtüşmesi, cutaway/çatı, iç/dış astronot ve geceyi gerçek Linux'ta kontrol et.

## D. Ekran ve normal açılış

1. 1440×900, 1920×1080 ve 1280×720'de komut satırı zorlaması OLMADAN açılışı dene.
2. Kaydedilmiş geçerli çözünürlük yoksa monitörün doğal desteklenen çözünürlüğünü seç;
   geçersiz/eski kayıtları güvenli varsayılana taşı. Açık kullanıcı tercihini ezme.
3. Kamera ana üs+panelleri ekranın kullanılabilir alanına sığdırmalı; 16:10'u
   16:9 gibi varsayma. Reset Camera aynı uygun kadraja döner; menüde WASD sahneyi kaydırmaz.
4. Yazılar için ekranda gerçek minimum okunabilir boyut hedefle; sadece referans
   canvas fontSize'ına güvenme. Gerekirse küçük ekranda dock iki satır veya kaydırılabilir olur.
5. Ayarı kaydet → kapat → normal açış testi ve güncel görüntüler bırak.

## E. Bilim bakiyesi ve uzun süreli rutin

1. Toplam araştırma ve harcanabilir bilim bakiyesini ayır. Başlangıç bakiyesi 0;
   Operations araştırması 1 araştırma ilerlemesi başına 1 harcanabilir bilim üretir.
   Önayar oranlarıyla kazanım hızı dengelenir; Dünya işleri bilim üretmez.
2. Bir komutla 10 puan harcama: yeterli değilse gerekçeli ret, hiçbir başka durum değişmesin.
3. Operations için günlük rezerv öğününü Sol başında sürdürülebilir lojistik temini
   olarak sağla (öneri en fazla 2 ekip öğünü kapasitesi, günlük +1). Bu fiziksel
   Dünya kargo inişi veya gıda üretimi diye anlatılmaz. Training'in iki öğün stoğu korunur.
4. Operations kazanma hedefi kapalıdır; sağlık/kaynak güvenliği devam eder.

## F. Su çıkarma temeli (arıza önkoşulu)

1. Geri kazanımdan ayrı WaterExtractor modülü ekle: enerji kullanarak dış rezervden
   su tankına üretim; tank dolunca üretim ve maliyet gerektiği kadar azalır.
2. Kaynak oyun senaryosunda varsayılan sınırsız erişilebilir rezerv olarak etiketlenir;
   gerçek Mars sahası/buz ölçümü değildir. Kazı/rover/yeni harita gerekmiyor.
3. İlk öneri: 0,012 su/sn, 0,015 enerji/sn; gerçek birim değil oyun birimi.
   Sağlıklı otomatik operasyonun 6+ sol rezerv dengesine göre config'de ayarla.
4. Enerji önceliği: oksijen → geri kazanım → su çıkarıcı → isteğe bağlı deney/araştırma.
   Sıfır enerjiyle çıkarma yok; yetersiz güçte orantılı üretim. Çıkarıcı aç/kapatılır.
5. Varsayılan 3 kişide su dengesini, kapasite dolumunu ve arızalı yarım hızı test et.

## G. Radyasyon kanalı

1. Bağımsız 900–1200 sn bekleme; başlangıçtan 60 sn önce uyarı.
2. Önerilen 60–120 sn aktif süre, sonra normale dönüş; katsayılar mevcut risk
   modeline dayanır ama yeni sürede iç mekânın makul güvenli kalması yeniden dengelenir.
3. Uykuda uyarı geldiğinde uyandır ve pause talebi ver; kullanıcı görmeden hızla
   ölüm olmaz. Uyarıyı kabul etmenin kendisi olay zamanını sıfırlamaz.
4. Aktif olay bitince yeni 900–1200 sn bekleme çek. NASA canlı tahmini diye sunma.
5. Operations'da mevcut sonsuza kadar biriken risk, güvenli içeride kalan ekibi
   sonunda kaçınılmaz kayba sürüklememeli. İndeksi **akut eğitim risk göstergesi**
   olarak tanımla: aktif radyasyon yokken içeride önerilen 0,03 puan/sn normale
   dönüş, alt sınır 0. Aktifken veya dışarıdayken bu düşüş yok. Bu gerçek biyolojik
   dozun silinmesi/tedavisi değildir. Training risk modeli değişmez; uzun dönem
   toplam dış maruziyet istenirse ayrı istatistiktir, bu sürümde yeni sağlık sistemi yok.

## H. Tek güneş panelinin arızası

1. PanelUnitState listesi: ilk Operations kurulumunda 3 panel, sabit kimlik/sağlık.
   Sağlıklı toplam üretim eski toplam SolarRate'e eşittir; panel sayısıyla 3 kat artmaz.
2. 1200–1800 sn'de sağlam panellerden biri eşit olasılıkla arızalanır; sadece o
   panelin katkısı 0 olur. Aynı kanal onarım bekler; başka paneli arızalandırmaz.
3. Arızalı hedefe dış tamir görevi: öneri 60 sn, −0,35 stamina/sn. İptalde ilerleme
   korunur, tamamlanınca panel bir kez düzelir. Temizleme ile tamir ayrı işlerdir.
4. Tamir bitince sonraki arıza aralığı örneklenir. Fırtınada tamir fırtınayı bitirmez.
5. Üretim: sağlam panel katkıları toplamı × kirlenme etkisi × kum katsayısı.

## I. Oksijen üreticisinin arızası

1. 1200–1800 sn'de üretim kapasitesi önerilen ×0,50 olur; tüketim değişmez.
2. “Düzeltmek için 2 kat enerji” burada **normal bakım işinin iki kat enerji
   maliyeti** olarak yorumlanır. Normal bakım için öneri 10 oyun enerji birimi,
   bu düzeltme için 20 birim/40 sn. Bu yeni bir tasarım varsayımıdır; her saniye
   normal üretimi 2× tüketimli yapmakla karıştırılmamalı.
3. İç düzeltme görevi bir astronot, öneri −0,20 stamina/sn; enerji iş ilerledikçe
   ödenir. Yetersiz enerji işi yavaşlatır/durdurur, ücretsiz tamamlanmaz.
4. Tamamlanınca kapasite normalleşir; sonraki arıza saati o andan başlar.
5. Elektroliz su/enerji sınırı korunur; üretim düşmesi kaynak kırpma hilesiyle uygulanmaz.

## J. Kum fırtınası

1. Bağımsız 600–900 sn bekleme, önerilen 60 sn ön uyarı.
2. Süre uniform 60–180 sn. Aktifken güneş üretimi ×0,20, yani %80 kayıp.
3. Gece solar 0 kalır; arızalı panel 0 kalır. Kum etkisi bitince başka bir
   arızayı veya kirlenmeyi yanlışlıkla temizleme.
4. Radyasyonla örtüşebilir; her ikisinin etkisi ve uyarısı ayrı görünür.
5. Bitişte yeni bekleme çek; aynı fırtınayı tek kanal içinde tekrar yığma.

## K. Su çıkarıcı düşüşü

1. Bağımsız 1200–1500 sn'de extractor üretim kapasitesine ×0,50 uygula.
2. Geri kazanım oranını değiştirme. Durum “su çıkarıcı” adı ve nedeni ile görünür.
3. “Düzelt — 10 bilim” komutu yeterli bakiyede tam 10 düşer ve arızayı bir kez kaldırır.
   Arızasız cihaz, yetersiz bakiye veya aynı çifte tıklama ikinci ödeme üretmez.
4. İptal/yeniden açış/yükleme ücretsiz sıfırlama sağlamaz. Tamir sonrası yeni saat çekilir.

## L. Dünya komut sistemi ve yedi görev

Her görev bilim ödülü **0**. Liste durumu: Received → Accepted → InProgress →
Completed veya Cancelled. İptal tamamlanmış sayılmaz; harcanmış kaynaklar bedava
geri verilmez. İstasyon kapasitesi, stamina ve tehlike kuralları ortak sistemden gelir.
Bir Dünya emri rastgele ekipman arızasını sessizce onaramaz.

| Kimlik | Görev | Adımlar ve başlangıç önerisi |
|---|---|---|
| EARTH-01 (kullanıcı) | Toprak örneği al ve incele | Dışarıda 60 sn toplama → iç laboratuvarda 60 sn analiz → rapor. Stamina −0,25/−0,15 sn; bilim 0. |
| EARTH-02 (kullanıcı) | Oksijenle özel deney | İç laboratuvarda 60 sn gerçek çalışma boyunca fiilen üretilen oksijenin %10'u deneye yönelir; tanka kalan %90 gider. Üretim 0 ise deney ilerlemez. Bilim 0. |
| EARTH-03 (yeni) | Dış radyasyon sensörünü kalibre et | Dışarıda 60 sn ölçüm/kalibrasyon → iç terminalde 20 sn rapor; oyuncu aktif radyasyonda dışarı gitmek zorunda değil. Bilim 0. |
| EARTH-04 (yeni) | Habitatın ısı yalıtımını kontrol et | Dış yüzeyde 60 sn tarama → iç terminalde 30 sn kayıt; toplam 3 enerji oyun birimi, mevcut yıpranmayı onarmaz. Bilim 0. |
| EARTH-05 (yeni) | Su kalitesi numunesini analiz et | İç sistemden 2 su birimi örnek → laboratuvarda 60 sn, toplam 2 enerji birimi → rapor; tankı/arıza durumunu iyileştirmez. Bilim 0. |
| EARTH-06 (yeni) | Haberleşme antenini hizala | Dışarıda 60 sn ayar, toplam 5 enerji → 30 sn bağlantı testi → rapor. Gerçek gezegenler arası ışık süresini değiştirmez. Bilim 0. |
| EARTH-07 (yeni) | Acil durum stoklarını denetle | İçeride 60 sn sayım → 30 sn acil durum prosedür provası → rapor; öğün stoğunu artırmaz veya tüketmez. Bilim 0. |

1. Önce periyodik varış/torba/kuyruk altyapısını tek sahte test göreviyle doğrula.
2. Sonra her görev satırını AYRI checkpoint olarak uygula; hepsini tek kod üretimine yığma.
3. Dış görev dönüşü ve sonraki iç istasyon açık aşamalardır; dışarıdan içerde
   anında çalışılmaz. İptal, atama reddi ve kayıt/yükleme aşama ilerlemesini korur.
4. Oksijen deneyi üretim bölüşümüdür; tanktan ayrıca %10 çekerek çift maliyet yaratma.
5. Başarılı her görev rapor/komut geçmişinde görünür; bilim veya gizli kaynak ödülü yoktur.
6. UI metinleri TR/EN; varış sesi tek kez, pause sırasında spam yok.
