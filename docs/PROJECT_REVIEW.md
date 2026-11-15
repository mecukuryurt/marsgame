# Mars Outpost — bağımsız durum incelemesi

Tarih: 14 Kasım 2026. İncelenen HEAD: f56730c.

Bu rapor düzeltmelerden önceki tarihsel incelemedir. Sonraki çözüm ve doğrulama
kaydı `RELEASE_NOTES.md` içindedir; aşağıdaki sorunları güncel açık iş saymadan
önce o belgeyi kontrol edin.
Bu inceleme oyun kodunu değiştirmez; yeni test sonuçları ve bu rapor eklenmiştir.

## Genel sonuç

Proje teknik bir fikirden oynanabilir dikey kesite ilerlemiş. T03–T12'nin ana kod
çıktıları mevcut. Ancak “yarışmaya hazır / hedef yaş için doğrulanmış ürün” demek
erken: denge, görsel–mantıksal eşleşme, güncel belgeler ve dağıtım kapanışı gerekli.
Yeni kolonileşme özelliklerinden önce mevcut iki solluk oyunu sağlamlaştırmak gerekir.

## İnceleme kapsamı ve kanıt sınırı

- Kök README; docs altındaki planlar, görev tablosu, geliştirme raporu, doğrulama
  kayıtları, gelecek yol haritası, iyileştirme planı, bilim kaynakları, playtest ve
  sunum taslakları incelendi. Eski XML sonuçları ayrıştırıldı.
- Simulation/Presentation/Editor/test yapısı; zaman, kaynak, görev komutları,
  kamera, hareket, HUD, test kapsamı ve bilim verisinin senaryo bağlantısı incelendi.
- Güncel Unity 6000.3.28f1 ile bağımsız yeniden çalıştırma:
  **47/47 EditMode**, **6/6 PlayMode** başarılı.
- Kanıt: `validation/review-editmode-results.xml` ve
  `validation/review-playmode-results.xml`. Tam yeni Editor logları geçici olarak
  `/tmp/marsgame-audit-editmode.log` ve `/tmp/marsgame-audit-playmode.log` içindedir.
- Mevcut Linux build dosyaları bulundu. Bu incelemede yeni standalone build alınmadı,
  güncel oyunun grafik oturumu oynanmadı. Kaydedilmiş 1280×720 ekran görüntüsü
  incelendi; görüntü eski 2D arayüzü gösteriyor, güncel 3D görünüm için kanıt değil.
- NASA URL'lerinin dış kaynaktan yeniden doğrulaması bu yerel kod incelemesinin
  parçası değildi; kaynak belgelerindeki doğrulama iddiaları ayrı değerlendirilmelidir.

## Mevcut yapı

| Alan | Gözlenen durum |
|---|---|
| Çekirdek | Unity bağımsız C#, tek saat, kaynak/ekip/görev/olay/uyku/sonuç modeli |
| İçerik | 3 astronot, iki sol, araştırma, iki bakım işi, öğün, Dünya tavsiyesi |
| Akış | Duraklatma/odak kontrolü, gece hızlandırma, sol raporu, başarı/kayıp, restart |
| Sunum | Artık 3D geometri ve 2.5D kamera; yol noktaları, hareket ve istasyon animasyonları |
| Arayüz | TR/EN, ikonlu alt komut çubuğu, kompakt HUD, ses, kaynak penceresi |
| Testler | 47 çekirdek/stepper ve 6 sahne/arayüz testi güncel kodda geçti |
| Build | Linux çıktısı mevcut; güncel build'in yeniden uçtan uca oynanması gerekli |
| Web | Belgelerde WebGL modülü eksik; bu incelemede web build doğrulanmadı |
| Teslim | README ve İngilizce anlatım/script mevcut; lisans, gerçek video/slayt ve kullanıcı denemesi açık |

## Öncelikli bulgular

### 1. Yüksek — Standart senaryoda temel tercihler kazanmak için gerekmiyor

Geçici saf C# denemesinde SimulationRunner.InitializeDefaultMission ile aynı
ayarlar kullanıldı (panel kirlenmesi 0,20; sistem yıpranması 0,15). Yalnızca crew-1
araştırmaya atandı. 0,25 sn Tick ile iki sol ilerletildi; rapor/uyarılar kabul edildi.
Başka görev, dinlenme, uyku, öğün veya bakım komutu verilmedi.

Sonuç: **Succeeded**, araştırma **666,67 / 120**, batarya **73,68**, su **67,76**,
oksijen **98,00**; araştırmacı stamina **0**, diğerleri **100**, kişi başı risk **18**.
Geçici tekrar üretim kaynağı: `/tmp/marsgame-audit/Balance.cs`.

Bu bir motor hatası değil; eğitim hedefini zayıflatan denge sorunu. Kaynak
önceliği, stamina yönetimi ve gece uykusu anlamlı tercihler olmalı. Çözüm yalnızca
hedefi yükseltmek değil: iki ya da üç kararın işe yaradığı, hazırlıksızlığın görünür
ama toparlanabilir bedeli olduğu senaryolar tasarlamak. Tek bir doğru stratejiye
zorlamadan pasif kazanmayı engelleyen denge kabul testleri eklenmeli.

### 2. Yüksek — Görsel hareket simülasyon zamanından ayrılmış

WorldView.UpdateCrewVisuals, yürümeyi Time.unscaledDeltaTime ile ilerletiyor;
WorldView içinde Stepper.IsPaused/EffectiveTimeScale kontrolü yok. Mantıksal görev
AssignTask anında başlıyor, görsel karakter ise istasyona daha sonra yürüyor.
Dönüşün mantıksal süresi 10 simülasyon saniyesi; görsel rota sabit yürüme hızıyla
tamamlanıyor. Sonuç olarak pause/8x uyku ve gerçek konum/korunak göstergeleri
arasında ayrışma riski var; pause'da görsel yürüyüşün duracağı kodla sağlanmıyor.

Çözüm: gameplay konumu/geçiş süresinin sahibi simülasyon olmalı; sunum o durumu
interpole etmeli. Tam taşıma hemen yapılmayacaksa en azından görsel yürüyüş pause'da
durmalı, dönüş görseli mantıksal dönüş sayacına bağlanmalı. Kamera ve seçim halkası
gibi salt sunum efektlerinin pause'da devam etmesi ayrı bir tasarım kararıdır.

### 3. Orta — Başarısız görev değiştirme eski işi iptal ediyor

MarsOutpostHud.AssignOrReplaceTask önce CancelTask, sonra AssignTask çağırıyor.
Yeni görev dolu/geçersizse atama reddedilirken eski görev zaten bırakılmış oluyor.
Örnek: crew-1 araştırmada, crew-2 öğün hazırlığında; crew-1 için öğün seçmek
araştırmayı iptal edip dolu istasyon hatası üretebilir.
Çözüm: değişimi atomik domain komutuyla veya değişiklik öncesi geçerlilik
kontrolüyle yapın. Reddedilen değişim eski görevi korumalı; regresyon testi eklenmeli.

### 4. Orta — Grafik testleri görüntünün gerçekten doğru olduğunu kanıtlamıyor

WorkingAstronautsAtInteriorStations... testinde Sleeping → Working yorumu var,
fakat gerçek uyku başlatılmıyor. Renderers.isVisible loglanıyor; görünürlük veya
örtülme assertion'ı yok. Aktif MeshRenderer ve materyal varlığı, gövdenin kamera
tarafından görüldüğünü kanıtlamaz. Bazı kamera testleri düğme sonrası sonucu beklemek
yerine SetCameraPose ile elle kuruyor. Altı yeşil test görsel kalite onayı değildir.

Çözüm: gerçek uyku → uyanma → işe gitme zinciri; duraklamada hareket; kamera resetinin
gerçek davranışı ve iki çözünürlükte güncel grafik oturumu. Referans görüntüleri yenileyin.

### 5. Orta — Plan, rapor ve gerçek ürün aynı sürümü anlatmıyor

README/MASTER_PLAN/GAME_PLAN hâlâ 2D ve eski kapsam dili içeriyor. VALIDATION
başlığı T02/18 test; ileride daha yeni sonuçlar bulunuyor. AGENT_REPORT son
PlayMode sayısı 2, kayıtlı t11 XML sayısı 4, mevcut bağımsız koşuda sayı 6.
Mevcut ekran görüntüleri eski arayüzü gösteriyor. iyilestirme-plani.md içindeki
4x MSAA, ikonlar ve çalışma pozu değişiklikleri artık kodda var; hâlâ gelecek
zamanla yazılmış plan bunları açık sorun gibi sunuyor.

Çözüm: tek güncel durum özeti, build/commit kimliği ve son test kanıtı; geçmiş
kayıtları tarihsel bölümde tutun. Görsel iyileştirmelerde “uygulandı” ile “grafik
oturumunda doğrulandı” ayrılmalı. Yeni uygulayıcı eski planı tekrar yürütmemeli.

### 6. Orta — NASA entegrasyonu mevcut, fakat nicel veri etkisi sınırlı

EventState kaynak kimlikleri ve açıklamalar taşıyor; olay zamanları/katsayıları
kodda tasarlanmış sabit değerler. Bu dürüstçe etiketlenmiş bir eğitim senaryosu
olabilir, ancak veriyle hesaplanan simülasyon iddiası güçlendirilmemeli.
SCIENCE_SOURCES ikinci olay için “görece sıra korunmuş” diyor; sıralama ile oransal
zaman aralığı aynı şey değil. 23,6 saatlik olay aralığı 300 oyun saniyesine,
yaklaşık 5 saatlik ön süre de 300 saniyeye bağlanmış; tek zaman ölçeği yok.

Çözüm: doğrulanmış ham arşiv kaydı, alan → oyun dönüşüm tablosu ve tekrarlanabilir
senaryo üretimi; ya da farklı sıkıştırmaların açık eğitim tasarımı olduğunu belirtin.
Gelecek yol haritasındaki buz derinliği, enerji/debi ve küresel su üretimi gibi
nicel iddialar kaynaklandırılmadan oyuna bilimsel gerçek olarak taşınmamalı.

## Teknik bakım

WorldView yaklaşık 2.184, HUD 1.686, Resources 993 ve MissionSimulation 975 satır.
Küçük proje için kapsamlı framework gerekmiyor; fakat sonraki özelliklerden önce
HUD bölümleri, dünya üretimi ve ekip görselleri ayrı sorumluluklara ayrılabilir.
Runtime'da üretilen Material/Mesh/AudioClip nesnelerinin yaşam döngüsü ve sahne tekrar
açılışlarında bellek davranışı ölçülmeli; bu incelemede bellek sızıntısı kanıtlanmadı.
Saf simulation assembly'nin noEngineReferences kuralı korunmalı. FUTURE_ROADMAP
Unity Vector3/Quaternion içeren modeller öneriyor; bunlar domain katmanına doğrudan
taşınırsa mevcut bağımsızlık bozulur. Roadmap bir öneridir, uygulanmış özellik değildir.

## Önerilen sıra

1. Standart iki solluk senaryoda gerçek kararlar ve toparlanabilir riskler yaratın;
   pasif kazanma ve alternatif strateji denemelerini kaydedin.
2. Görsel/simülasyon zamanını ve atomik görev değiştirmeyi düzeltin; ilgili testleri ekleyin.
3. Güncel 3D build'i yeniden alın, iki çözünürlükte tüm görev/uyku/pause akışını oynayın.
4. 7–12 yaş grubuyla kısa deneme yapın: ilk görev, tehlike, yorgunluk ve neden–sonuç
   ilişkisini anlayabiliyorlar mı? Sonuçları uydurmadan PLAYTEST'e kaydedin.
5. Güncel belgeleri, bilim izlenebilirliğini, lisansı, WebGL/dağıtım tercihini ve
   gerçek demo artefaktını kapatın.
6. Bundan sonra keşif, parsel, su çıkarma ve rover'dan yalnızca birini seçip küçük
   bir dikey kesit olarak planlayın. Dördünü birlikte ana ürüne eklemeyin.

Bu sıra bir sonraki geliştirme önerisidir; inceleme sırasında oyun koduna düzeltme
uygulanmadı ve yeni genişleme özellikleri başlatılmadı.
