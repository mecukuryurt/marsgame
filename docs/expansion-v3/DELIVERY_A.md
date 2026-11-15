# Teslim A Doğrulama ve Kabul Raporu (OP00–OP15)

- **Durum:** Done (OP15 Teslim A Kalite Kapısı Geçildi)
- **Kapsam:** Operasyon modunun çekirdek kur–bağla–üret–kaydet döngüsü (sonraki keşif/rover koduna bağımlı olmadan kendi başına oynanabilir ve kayıt/yüklemeden devam edebilir).

## Tamamlanan Özellikler ve Mimari (OP00–OP15)
- **Parsel Haritası ve Ekonomi (`OP01`–`OP03`):** Deterministik `16×16` parsel ızgarası, `MarsOutpost.Simulation.asmdef` saf C# sınırı (`noEngineReferences: true`), tahsis maliyeti (`2 Malzeme`), malzeme stok/kapasite ve ilk kayıt göçü, 3B parsel seçimi.
- **Yüzey Rotası, İş Aşamaları ve İnşaat (`OP04`–`OP07`):** A* / 4-yönlü yüzey rotası, astronot fiziksel yürüyüşü (`Travel -> Work -> Return -> Airlock`), geri çağırma ve kesinti güvenliği, yapı kataloğu + 3B ghost önizleme (`R` dönüş, geçersiz gerekçeler), atomik blueprint yerleştirme ve hizmete alma.
- **Ağ, Yeni Güneş Paneli, Batarya ve Söküm Bütünlüğü (`OP08`–`OP12`):** 4-yönlü çekirdek kablo ağı (`UtilityNetwork`), 4+ kurulabilir güneş paneli (`SolarProduction`) ve tekil panel arıza/tamir hedefleme, kurulabilir batarya (`Battery`, `+25` etkin kapasite katkısı ve taşma defteri), blueprint iptali (`0` malzeme iadesi, harcanan enerji kaybı) ve hizmetteki tesis sökümü (`%50` kapasite sınırlı malzeme iadesi).
- **Kurulabilir Buz Su Çıkarıcı, Görsel Durumlar ve Telemetri (`OP13`–`OP14`):** `WaterExtractor` kataloğa açılışı, taranmış `Ice` yerleşimi, sonlu `RemainingWater` rezervi, `legacy-extractor` + saha sitelerinin deterministik ID sırasıyla tek sayımı, ana/saha enabled anahtarı, global `×0.50` servis arızası (`10` bilim onarımı), `ExpansionRevision = 4` Save/Load; 3B blueprint/aktif/duraklama/tükenme durumları, Pause/8x uyumlu sondaj dönüş animasyonu, seçili tesis ETA (`rate == 0` -> `∞ (Durdu)`), ana su kartında Çıkarma vs Geri Kazanım ayrımı, tek tık `Tesise Odak` ve `Yüzey Ekibini Geri Çağır`.
- **Teslim A Uçtan Uca HUD + Regresyon (`OP15`):** Yeni oyundan HUD düğmeleriyle tahsis -> kablo hattı -> 4. güneş paneli (yarıda kayıt/yükleme) -> batarya -> buz su çıkarıcı -> söküm akışı ve Training / 90sn Demo mod regresyonları.

## Gerçek Doğrulama Sayıları ve Çıkış Kodları
1. **EditMode Test Paketi (`docs/validation/v3-op15-editmode.xml`):**
   - Komut: `Unity -batchmode -nographics -runTests -testPlatform EditMode`
   - Çıkış Kodu: `0`
   - Sonuç: **141/141 Passed** (`failed=0`, `skipped=0`)
2. **PlayMode Test Paketi (`docs/validation/v3-op15-playmode.xml`):**
   - Komut: `Unity -batchmode -nographics -runTests -testPlatform PlayMode`
   - Çıkış Kodu: `0`
   - Sonuç: **35/35 Passed** (`failed=0`, `skipped=0`)
3. **Linux Standalone Derlemesi:**
   - Komut: `Unity -batchmode -nographics -quit -buildLinux64Player Build/Linux/MarsOutpost.x86_64`
   - Çıkış Kodu: `0` (`Build/Linux/MarsOutpost.x86_64`)
4. **Görsel Kanıtlar (`1440×900`):**
   - `docs/validation/v3-op14-extractor-1440.png` (Çıkış Kodu `0` — aktif seçili su çıkarıcı & telemetri)
   - `docs/validation/v3-op15-open-1440.png` (Çıkış Kodu `0` — normal `1440×900` açılış görünümü)

## Kalan Açıklar (Dürüst Durum)
- Normal `1440×900` açılış görüntüsünde (`v3-op15-open-1440.png`) TopBar'daki `[Grid:]` düğme metninin sağ kenarı dar kalmaktadır ve hava kilidinden çıkan astronotun dünya etiketi kısa süreli `[HAVA KİLİDİ]` yazısıyla çakışabilmektedir.
- `1920×1080` ve `1280×720` Linux ekran görüntüleri ile gerçek kullanıcı/hedef yaş playtesti henüz yapılmamıştır (Teslim B / Son kapanış kapsamındadır).
