# Hackathon teslim planı — mevcut oyunu iyileştirme

15 Kasım 2026. Kullanıcı son kararı: OP15'e kadarki oyunu görsel/menü/deneyim
bakımından toparla; Terra Medium uygulasın, doğrulanmış son hâl commit ve push
edilsin. OP16–32'nin yeni keşif/rover/tesis genişlemeleri bu teslimin dışındadır.

## H00 — Çalışır tabanı sabitle

Devralınan V2 + Hydrogen OP00–15 baseline: 9d2282f. Yarım OP16 yalnız onu yazan
ajan tarafından ayrı patch/rapora alınır; kullanıcı çalışması veya OP15 kayıtları
silinmez. Yeni schema revision gerektirmeyen yarım özellik arayüzden gizlenip
bitmiş sayılmaz. Etkin teslim yalnız mevcut işlevleri içerir.

## H01 — Parsel, hat ve panel güvenilirliği

- Kullanıcının tahsis sorunu yok; bozuk yerleşim/R/hat akışını çöz.
- R inşaatta yalnız döndürür; kamera reseti aynı karede çalışmaz.
- Starter solar tek ayrılmış (9,6) hücresine görsel olarak sığar; erişim (9,5)
  ve port, ghost, tamamlanmış tesis, kablo ve simülasyon koordinatları eşleşir.
- Habitat çok hücreli tek tesis olarak açık gösterilir; yatak, hava kilidi ve
  mevcut kayıtları bozacak rastgele ölçekleme/taşıma yapılmaz.
- UI üstü tık/drag dünya komutu değildir; yapılar grid seçiminde zemin yerine
  yanlış hücre hesaplatmaz. Hattı yerleştir → ekip kurar → paneli bağla → güç
  artar akışı, yanlış yer retleri ve save/load test edilir.

## H02 — Sakin ve anlaşılır arayüz

Normal ekranda kısa zaman/rezerv bilgisi, ekip seçimi ve seçili kişinin eylemi
öne çıkar. Tamir kataloğu, inşa kataloğu, dünya emirleri, kamera ipuçları ve
telemetri sürekli yığılmaz. İnşa/Sistemler/Görevler gerektiğinde açılan tek
bağlam panelidir. Escape/geri/kapat nettir; bir panel diğerinin üstüne birikmez.

İnşa akışı: modu aç → parsel seç → uygun yapı/maliyet → önizleme → yerleştir →
ekip ata. Tahsis, Blueprint ve tamamlanmış yapının farkı kısa metinle görünür.
Mevcut button ID'leri korunabilir; testler yeni açma eylemini uygular.
TR/EN etiketler ve aynı düğme/başlık hiyerarşisi; 1440×900'de okunur yazı,
yeterli boşluk, kısa kaynak etiketleri. Kritik uyarı görünür ama sakin zamanda
sürekli kırmızı baskı yok. Normal açılışta kamera/etiket/HUD taşması incelenir.

## H03 — Özgün Mars ambiyansı ve sakin müzik

Dış parça/loop indirmek yerine özgün deterministik sentez: duyulabilir bantta
rüzgâr, düşük habitat uğultusu, sakin pad/notalar. Dünya dış ortamında gerçek
Mars mikrofon kaydı iddiası yok. Master, müzik, ambiyans, efekt ayarları ayrı,
kalıcı ve tamamen kapatılabilir. Fade/loop dikişi, kaynak/clip yaşam döngüsü,
yüklemede yeniden başlama/çoğalma ve uyarıların anlaşılır kalması kontrol edilir.
Üretim yöntemi/provenance kaydı tutulur. Dinleme yapılmadıysa test isPlaying
sonucu dinleme doğrulaması olarak sunulmaz.

## H04 — Görsel ve oynanış kontrolü

Mevcut prosedürel 3D stil korunur; asset paketi/rover/yeniden fizik motoru yok.
Boyut/yerleşim, kontrast, metin/etiket, ışık dengesi ve arayüz boşluğu iyileştirilir.
Gerçek Linux görüntüleri: normal, inşa/grid, parsel/hat/panel, sistemler, ayarlar.
Uyku ve dönüş, habitat içinde efekt dışlama ve pause/focus davranışları korunur.
Varsayılan Operations huzurlu akar; mevcut 90sn Demo sunum için erişilebilir kalır.

## H05 — Son doğrulama, commit ve push

1. Güncel kaynakta EditMode ve PlayMode; başarısızlıklar düzeltilir.
2. Kullanıcı sahnesini yeniden üretmeyen Linux build; normal açılış + settings
   sonrası yeniden açılış. 1440×900 öncelikli, erişilemeyen ekranlar açıkça yazılır.
3. README ve RELEASE raporu: gerçek kapsam, kullanıcının açacağı binary yolu,
   test/build kanıtları, bilinen açıklar. OP16–32 tamamlandı denmez.
4. Her doğrulanan küçük teslim açık dosya listesiyle commit edilir. .vscode,
   Library, Temp, üretim/cache dosyaları topluca eklenmez. Başka ajanın dosyası
   yanlış commit'e girmez. Kullanıcı açıkça push yetkisi verdi; uzak dal farkı
   kontrol edilir, force-push yok. Son commit ve push sonucu raporlanır.

## İş ve kesinti sahipliği

terra_v3_completion: H00 temiz ayrım, H01 domain/şebeke ve EditMode.
terra_ui_audio: H01 presentation, H02–H04, PlayMode ve ekran çıktıları.
Ana ajan: plan, kontrol, son birleştirme ve push. Aynı projede iki Unity koşusu yok.
Her ajan tam sonraki adımı dosyaya yazar; bitmemiş değişiklik Done değildir.
Mevcut V3 UX raporları ve PARCEL_REVIEW geçmiş kanıt olarak korunur.
