# Hackathon sürümü — 15 Kasım 2026

Mevcut OP15 oyunu iyileştirildi. OP16–32 ertelendi; yarım keşif değişikliği
etkin oyundan ayrıldı ve deferred-op16.patch olarak korundu.

## Değişen deneyim

- Başlangıç solar modeli ayrılmış hücresine oturtuldu; hat/port ve yeni panel
  bağlantısı, üretim ve kayıtla doğrulandı. Habitat çok hücreli tek tesis olarak korunur.
- İnşaatta R dönüşü kamera resetini tetiklemez. Grid zeminden seçilir;
  önündeki yapı/ghost yanlış hücre seçtirmez. UI ve kamera drag girişi filtrelenir.
- İnşa ve Sistemler ayrıntıları istekle açılır; Araçlar save/load/emirleri toplar.
  Başarılı yerleşim sonrası parsel/ekip ataması görünür; daha az kalıcı panel vardır.
- Özgün sentez rüzgâr/uğultu ve sakin pad müziği; master/ambiyans/müzik/SFX
  ayrı kaydedilir. Harici şarkı/loop indirilmedi. Ses üretim kaydı
  ../expansion-v3/ART_AUDIO_PROVENANCE.md içindedir.
- Ayar satır aralıkları düzenlendi; dört ses denetimi ilk görünümde erişilir.

## Gerçek kanıt

- EditMode: 142/142, failed=0, skipped=0; ../validation/hackathon-editmode.xml.
- PlayMode: 36/36, failed=0, skipped=0; ../validation/hackathon-playmode.xml.
- Son ayar yerleşimi, tam PlayMode koşusundan sonra yalnız görsel olarak değişti;
  son Linux derlemesinde derlendi ve ekran görüntüsünde incelendi.
- Linux build: Unity 6000.3.28f1, exit 0. Mevcut kullanıcı sahnesi yeniden üretilmedi.
- Gerçek 1440×900 ekranlar: ../validation/hackathon-normal-1440.png,
  hackathon-build-1440.png, hackathon-systems-1440.png, hackathon-settings-1440.png.
  Ana ajan bu dört görüntüyü inceledi.

## Açılacak oyun ve paket

`Build/Linux/MarsOutpost.x86_64` doğrudan açılır. Taşınabilir yerel paket:
`Build/Release/MarsOutpost-Hackathon-Linux.tar.gz` (41.191.880 bayt).
SHA256: `8fc23bc42c252e31269837f9d061ac7c267d378d06cf4dd404e92a178324e504`.
Arşivde executable ve UnityPlayer.so bulunduğu kontrol edildi. Build/ gitignore
kapsamındadır; kaynak push'u oyun paketini GitHub release olarak yayımlamaz.

## Dürüst sınırlar

Stil prosedürel prototip olarak kalır; kapsamlı yeni asset/model paketi yoktur.
Hava kilidi/astronot dünya etiketleri bazen çakışır; inşa kartının yazıları küçüktür.
Gerçek ses dinlemesi ve fiziksel 1920×1080/1280×720 açılışları yapılmadı.
Hedef yaşla playtest yapılmadı. UI drag filtresi uygulandı, fakat tüm fare
sürüklemesini uçtan uca simüle eden test eklenmedi. Bunlar test geçti diye gizlenmez.

## Commit kayıtları

- 9d2282f: devralınan V2 + Hydrogen OP00–15 baseline.
- f95aac3: starter ağ regresyonu, 142 EditMode kanıtı, kapsam/OP16 ertelemesi.
- a4115cb: parsel girişi, bağlam HUD'ı, özgün ses ve ekran kanıtları.
- Son belge/paket teslimi ana ajan tarafından ayrıca commit/push edilir; force-push yoktur.
