# Mars Outpost — Operations V2 ve bağımsız son kontrol

15 Kasım 2026. Varsayılan mod artık uzun süreli Operations; Training ve 90sn
Demo korunur. Beş bağımsız rastgele kanal, üç panelin arızası, O₂/su onarımları,
harcanabilir bilim, su çıkarıcı, iki solda bir Dünya emri/yedi görev ve oyuncu
kaydı uygulanmıştır.

Bağımsız kontrolde aktif tehlike ve rapor kaybı, eksik tamir düğmeleri, istasyon
çakışmaları, O₂ deneyi kapasite hesabı ve uyku uyarıları düzeltildi. Kaydetme
şeması v2'dir. **91 EditMode + 17 PlayMode testi geçti.** Linux çıktı güncellendi.
Ayrıntılar ve sınırlamalar: `operations-v2/REVIEW.md`. 1440×900 normal açılış ve
uyku görseli incelendi; diğer ekran boyutları/kullanıcı denemesi hâlâ doğrulanmalı.

---

## Önceki sürüm — denge, görüntü kalitesi ve ayar menüsü

14 Kasım 2026. Terra oyun mantığını ve görselleri, Luna ayar menüsünü uyguladı;
ana ajan birleştirme, inceleme, menü düzeltmeleri ve gerçek Unity doğrulamasını yaptı.

## Değişiklikler

- Standart görevde hedef yine 120. Başlangıç bataryası 50, araştırma enerji oranı
  0,09, ilerleme oranı 0,15. Tek araştırmacıyla beklemek 100 araştırmada hedefi
  kaçırır; hazırlıklı uyku ve bakım/ekip rotasyonu iki farklı kazanma planıdır.
  Demo ve özel konfigürasyon testleri korunur.
- Atomik ReplaceTask komutu: reddedilen görev değişimi mevcut işi iptal etmez.
- 3D dünya artık Renderer2D yerine yeni MarsRenderer3D Universal Renderer kullanır.
  Eski renderer korunur; yumuşak gölgeler ve High'da tam çözünürlük/4x MSAA vardır.
  URP Lit shader'ı Linux build'inde tutulur.
- Gerilmiş manzara dokusu yerine mat ve ince taneli prosedürel Mars toprağı.
  Bu görsel, gerçek NASA topoğrafya verisi olarak sunulmaz.
- Görsel yürüyüş pause'da durur; dönüş konumu simülasyon dönüş sayacına bağlıdır.
- Sağ üst Ayarlar/Settings ve Escape menüsü: ses/mute, Low/Medium/High grafik,
  çözünürlük, tam ekran/pencere, Uygula/İptal/Varsayılan. PlayerPrefs ile kalıcı
  tercihler. Menü pause'u ve önceki kullanıcı/odak pause durumunu korur.
- Grafik ayarları, kaydedilmiş URP varlığı yerine runtime kopyasına uygulanır.
- Ana ses düzeyi iki kez çarpılmıyor; üretilen ses klipleri temizleniyor.
- README güncellendi; eski inceleme tarihsel olarak işaretlendi. NASA olay sırası
  ile farklı eğitim zaman sıkıştırmaları ayrıldı. Gelecek yol haritası hâlâ öneridir.

## Kontroller

Unity 6000.3.28f1 gerçek EditMode **53/53**, PlayMode **11/11** başarılı.
Kanıt: `validation/current-editmode-results.xml`, `validation/current-playmode-results.xml`.
Testler iki kazanma planı, pasif kayıp, güç tercihi, atomik atama, renderer,
pause/hareket/dönüş, menü pause'u, iptal, dropdown açılışı ve kayıt yüklemeyi kapsar.
Linux çıktı: `Build/Linux/MarsOutpost.x86_64`.
Son derleme ve grafik çıktısı sonucu VALIDATION.md güncel revizyon kaydındadır.

## Açık işler

Hedef yaşla gerçek kullanıcı denemesi yapılmadı. WebGL modülü/dağıtımı, lisans
seçimi ve gerçek video/slayt artefaktı açık. Yeni rover/keşif/inşaat eklenmedi.
Bu çalışma mevcut prosedürel 3D stilin doğru çizilmesini ve netleştirilmesini sağlar;
kapsamlı yeni karakter/çevre modellemesi yapılmış gibi sunulmaz.
