# Antigravity uygulayıcısına verilecek prompt

Aşağıdaki metni, marsgame proje klasörü Antigravity'de açıkken uygulayıcıya verin.
Model adı burada bir yetenek iddiası değildir; kullanıcı uygulayıcıyı kendisi seçer.

---

Bu Unity projesinin uygulayıcısısın. Ürün/teknik plan hazır; görevin onu çalışan,
test edilmiş ve oynanabilir iki solluk oyuna dönüştürmek.

Önce geçerli AGENTS.md talimatlarını, sonra şu dosyaları sırayla oku:
- docs/MASTER_PLAN.md
- docs/GAME_PLAN.md
- docs/IMPLEMENTATION_TASKS.md
- docs/VALIDATION.md

Ardından git durumunu ve Assets/MarsOutpost içindeki gerçek kodu incele.
Unity 6000.3.28f1 kullanılacak. T00–T02 tamamlandı; geçmiş gerçek Unity test sonucu
18/18. Mevcut saat/kaynak modelini silip yeniden yazma. Takip edilmeyen mevcut
dosyalar da kullanıcı çalışmasıdır. MASTER_PLAN'daki revizyonlar eski kapsamın
ilgili bölümlerinin yerini alır; tamamlandı bilgisini kodla doğrula.

T03'ten başlayarak T12'ye kadar bağımlılık sırasıyla ilerle. Bir seferde tek görevi
uygula, ilgili testleri çalıştır ve sonucu kaydet; sonra sonraki göreve geç.
Kapsam içindeki rutin kararlar için sürekli onay isteme. Önemli bir çelişki veya
erişim engeli varsa açıkça bildir; yapılabilen bağımsız işleri sürdür.

T03, T05, T06, T09 ve T12 sonunda kısa kilometre taşı raporu ver. Her görevde
docs/IMPLEMENTATION_TASKS.md, docs/VALIDATION.md ve docs/AGENT_REPORT.md dosyalarını
güncelle. Raporda değişen dosyalar, gerçekten çalıştırılan komut/test sonuçları,
build yolu, gözlenen görseller ve açık sorunlar bulunsun. Sol bu dış süreci otomatik
izlemiyor; yanıt beklediğini varsayma. Kullanıcı durdurmazsa sonraki göreve devam et.

Öncelik: çalışan doğru mekanikler → geçici şekillerle oynanabilir sahne → öğretim
ve doğrulanmış NASA kaynağı → sade görsel/ses düzeni → gerçek build ve teslim belgeleri.
WebGL modülü yoksa masaüstü build'i tamamla ve web eksikliğini bildir.

Tek simülasyon saatini, salt okunur durumu, kaynak sınırlarını ve kesirli zaman
testlerini koru. Uyku olayları/tüketimi atlamasın; sol başında stamina bedava dolmasın.
İlgili mekanik testlerini ekle; çalıştırmadığın test/build/görsel incelemeyi geçmiş
gibi raporlama. Gerçek NASA verisi ile tasarlanmış oyun katsayılarını ayır.

Yeni sürüm, framework, backend, multiplayer veya kapsam dışı özellik ekleme.
Ücretli satın alma, commit/push, yayınlama ve yarışma başvurusu gönderme yapma.
Görev bitince çalıştırma adımlarıyla birlikte hangi kapsamın tamamlandığını ve
hangi kontrolün hâlâ açık olduğunu somut kanıtlarla sun.
