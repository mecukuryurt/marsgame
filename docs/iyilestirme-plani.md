# Mars Outpost — Hata Analiz Raporu ve İyileştirme Planı

> Tarihsel analiz: burada önerilen poz, MSAA ve ikon çalışmaları uygulanmıştır.
> Güncel renderer/zemin/ayar menüsü ve gerçek test/build kanıtı RELEASE_NOTES.md
> ve VALIDATION.md içindedir. Aşağıdaki eski satır numaraları güncel referans değildir.


Ekran görüntüsü ve proje dosyaları (`Assets/Settings/UniversalRP.asset`, `ProjectSettings/QualitySettings.asset`, `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostWorldView.cs`, `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs`) incelendi. Tespit edilen **4 ana sorunun kök nedenleri** ve **çözüm planı** aşağıdadır.

---

## 1. Astronotların Yarı Yarıya / Parça Parça Görünmesi Sorunu

### Kök Neden Bulguları
- Ekran görüntülerinde merkezde (`IdleSlots`) veya dışarıda (`[GUNES PANELLERI]`) duran astronotların tüm vücudu (`Helmet`, `Torso`, `PLSS`, `LeftLeg`, `RightLeg`) eksiksiz görünürken, iç istasyonlarda (`[O2 & SU]` ve `[BILIM LAB]`, `z = 0.72`) **`Working`** durumundaki astronotların gövde/bacak parçaları (`Torso`, `PLSS`, `Legs`) veya isim etiketi eksik görünüyor.
- `GetTerrainHeight` (`MarsOutpostWorldView.cs:380-390`) üs içinde `0f` döndürüyor ve `HabitatDeck` `y = 0.01` yüksekliğinde, yani zemin astronotun gövdesini (`y = 0.52`) fiziksel olarak örtmüyor.
- Kodda iki şüpheli nokta tespit edildi:
  1. **Rotasyon / Poz Sızıntısı (`MarsOutpostWorldView.cs:1564, 1572`):** `Sleeping` durumu `VisualPivot.localRotation = Quaternion.Euler(-85f, 90f, 0f)` uyguluyor; ardından `Working` durumu `Quaternion.Euler(8f, vis.VisualPivot.localEulerAngles.y, 0f)` ile `localEulerAngles.y` geri okuyarak geçiş yapıyor. Euler geri-okuması (gimbal lock) geçişlerde beklenmedik eksen sapmalarına yol açabiliyor.
  2. **Çoklu Parça & Materyal Ayrışması (`MarsOutpostWorldView.cs:1203, 1241-1305`):** Her astronot 20+ ayrı `CreatePrimitive` alt nesnesinden oluşuyor; `TorsoRenderer` kopyalanmış ayrı bir `new Material(suitMat)` kullanırken kollar/bacaklar paylaşılan `suitMat`, kask `helmetMat`, sırt çantası `plssMat` kullanıyor.

### Çözüm
1. **Çalışma Zamanı Tanı Doğrulaması (PlayMode Test):** İlk adımda, `[O2 & SU]` ve `[BILIM LAB]` istasyonlarında `Working` durumundaki astronotların tüm alt `MeshRenderer` bileşenlerinin `isVisible`, `bounds` ve `VisualPivot` rotasyon değerlerini PlayMode testinde otomatik loglayıp kesin kök nedeni %100 doğrulamak.
2. **Düzgün Çalışma Pozu & Sağlam Rig:** `Working` ve `Idle` durumlarında `localEulerAngles` geri okuması kaldırılarak temiz `Quaternion.Euler(8f, 0f, 0f)` (istasyona dönük) kullanılacak; parçalar arası materyal ve ölçek tutarlılığı garanti altına alınacak.

---

## 2. Ekran Kalitesinin Çok Düşük Görünmesi Sorunu

### Kök Neden Bulguları
1. **Kenar Yumuşatma (MSAA) Kapalı:**
   - `Assets/Settings/UniversalRP.asset` (`line 28`) içinde `m_MSAA: 1` (MSAA kapalı).
   - `ProjectSettings/QualitySettings.asset` (`line 7`) içinde `m_CurrentQuality: 5` ve kalite seviyelerinde `antiAliasing: 0`.
   - Sonuç: Duvarlar, güneş panelleri, kasklar ve tüm 3D kenarlar testere dişi (aliased) görünüyor.
2. **Zemin Dokusunun 72m x 72m Alana Tek Seferde Gerilmesi:**
   - `Assets/MarsOutpost/Art/mars_terrain.png` (`1920x1080`, dikeyde `v = 0.02..0.62` ≈ `648px`) tüm **`72m x 72m`** (`[-36..36, -28..44]`) arazi ağına tek `1x1` UV ile gerilmiş durumda (`MarsOutpostWorldView.cs:466-467`).
   - Bu da metre başına yalnızca **~9–15 piksel** çözünürlük anlamına geliyor ve zeminin aşırı bulanık/çamurlu görünmesine yol açıyor.

### Çözüm
1. **4x MSAA Aktifleştirme:** `UniversalRP.asset` içinde `m_MSAA: 4` ve `QualitySettings.asset` içinde `antiAliasing: 4` açılacak.
2. **Zemin Dokusu Keskinleştirme:** `MarsTerrainMesh` üzerine yüksek frekanslı Mars regoliti detay dokusu (tekrarlı UV tiling / prosedürel kaya-kum mikro dokusu) eklenerek yakın ve orta mesafe zemin netleştirilecek.

---

## 3. Yandaki Paneldeki Yazıların Kötü Görünmesi Sorunu

### Kök Neden Bulguları
- `Assets/MarsOutpost/Runtime/Presentation/MarsOutpostHud.cs` (`lines 1149-1180`) içinde `1920x1080` referans `CanvasScaler` üzerinde Unity `LegacyRuntime.ttf` ile çok küçük punto değerleri kullanılıyor:
  - `LocationText` = `10pt`, `Stamina` = `10pt`, `Status` = `11pt`, `GalleyStatus` = `11pt`, `Name` = `13pt`, alt butonlar = `12pt` (`line 1619`).
  - Oyun `1360x765` pencere boyutunda çalıştığında (`~0.708x` ölçek) `10–11pt` yazılar ekranda yalnızca **`~7–8 piksel`** yüksekliğinde çiziliyor; bu da Linux üzerinde bulanık ve okunaksız harf kenarlarına neden oluyor.

### Çözüm
1. **Okunabilir Minimum Punto Standardı (`>= 14pt`):** Tüm yan panel ve dock yazıları en az `14–16pt` (Astronot adı `16pt`, durum/enerji `14pt`, buton yazıları `14pt`) seviyesine çıkarılacak.
2. **Temiz Kart Düzeni & Hafif Gölge:** Sol panel genişliği `224px` → `256px` yapılarak sıkışıklık giderilecek ve metinlere okunabilirlik için hafif `Shadow` eklenecek.

---

## 4. Karmaşık Arayüzün Sadeleştirilmesi ve Butonlara Simge Eklenmesi

### Kök Neden Bulguları
- `CreateDockButton` (`MarsOutpostHud.cs:1592-1626`) yalnızca `Text` bileşeni oluşturuyor; `[SOLAR]`, `[O2/SU]`, `[MUTFAK]`, `[BILIM]` gibi köşeli parantezli metinler butonları kalabalık ve karmaşık gösteriyor.
- *(Not: Linux Unity `LegacyRuntime.ttf` 4-baytlık Unicode emojileri `□` olarak çizdiği için emoji yerine gerçek 2D PNG ikon sprite'ları kullanılmalıdır.)*

### Çözüm
1. **Özel 2D PNG Buton İkonları (`Assets/MarsOutpost/Resources/UI/icons/`):**
   - `icon_solar.png` (Güneş / Solar Panel)
   - `icon_o2.png` (O2 Molekülü / Hava & Su Damlası)
   - `icon_food.png` (Yemek / Yaprak-Tabak — Mutfak)
   - `icon_lab.png` (Deney Tüpü / Erlenmayer — Bilim Lab)
   - `icon_home.png` (Sığınak / Dönüş)
   - `icon_rest.png` (Yatak / Dinlen)
   - `icon_stop.png` (Durdur / İptal)
   - `icon_sleep.png` (Ay & Yıldız — Gece Uykusu)
   - `icon_tech.png` (Araştırma Ağacı)
2. **Sadeleştirilmiş İkonlu Butonlar:** Her dock butonunun soluna net `28x28` simge `Image` bileşeni yerleştirilecek, `[SOLAR]`/`[MUTFAK]` gibi köşeli parantez etiketleri kaldırılıp kısa Türkçe isimler (`Solar`, `O2 & Su`, `Mutfak`, `Bilim`, `Sığınağa Dön`, `Dinlen`, `Durdur`, `Gece Uyu`) kullanılacak.
3. **Panel Sadeleştirme:** Sol kartlardaki gereksiz tekrar metinleri azaltılıp sadece `İsim + Konum + Enerji Çubuğu` odaklı temiz görünüm sağlanacak.

---

## Uygulama & Doğrulama Adımları
1. PlayMode tanı kontrolü ile iç istasyondaki astronot görünüm hatasının kesin kaynağını doğrulayıp düzeltmek.
2. `4x MSAA` + yüksek detaylı Mars zemin dokusunu uygulamak.
3. `>= 14pt` net tipografi, sadeleştirilmiş sol panel ve özel 2D PNG ikonlu dock butonlarını eklemek.
4. EditMode & PlayMode testlerini çalıştırıp `Build/Linux/MarsOutpost.x86_64` Linux build'ini güncellemek.
