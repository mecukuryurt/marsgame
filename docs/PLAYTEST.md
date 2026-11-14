# Mars Outpost — Kullanıcı Denemesi (Playtest) Rehberi

Durum: 14 Kasım 2026 (T07). Bu belge hem tamamlanan otomatik kontrolleri hem de gerçek oyuncularla (7–12 yaş hedef grubu) yapılması önerilen gözlem listesini ayırır.

## 1. Yapılan kontroller (otomatik / teknik)

- **EditMode (42/42 geçti):** Saat, kaynak öncelikleri, stamina tükenmesi/dinlenme, olay sınırları, radyasyon risk puanı, kritik oksijen uyanması, sol raporu ve stepper kuyruk durdurma kuralları doğrulandı.
- **PlayMode (`t06-playmode-results.xml`, başsız):** `MarsOutpost.unity` sahnesinin açıldığı, UI düğmeleriyle araştırma görevi atanabildiği, duraklatma ve yeniden başlatmanın çalıştığı ve sahnenin ikinci kez açılabildiği doğrulandı.
- **Gerçek çocuk/kullanıcı oturumu:** Bu geliştirme döngüsünde insan oyuncularla canlı playtest **henüz yapılmamıştır**; aşağıdaki gözlem formu bir sonraki aşama için hazırdır.

## 2. Önerilen canlı kullanıcı denemesi (yapılacak)

Hedef: 7–12 yaş arası 2–4 katılımcı ile 15–25 dakikalık rehbersiz deneme (gerekirse yalnızca soru sorulduğunda not alarak müdahale).

### Gözlem kontrol listesi

| # | Gözlem sorusu | Beklenen davranış | Başarısız olursa düzeltilecek katman |
|---|---|---|---|
| 1 | Oyuncu öğretici yardımıyla astronot seçip görev verebiliyor mu? | 1/3 → 2/3 adımlarını kendi başına geçer; "Research" veya "Panel Maint." başlatır. | HUD buton etiketleri / görsel vurgulama |
| 2 | Astronotun neden yorulduğunu ve dinlenmesi gerektiğini anlıyor mu? | Stamina 20 altına düşünce veya `Exhausted` görünce "Rest Inside" / gece uykusu seçer. | Stamina uyarısı ve durum açıklaması |
| 3 | 5 dakikalık radyasyon uyarısını ve aktif tehlike kartını fark ediyor mu? | Kehribar `WARNING` kartını görünce dışarıdaki astronotu "Return Inside (10s)" ile çağırır. | Uyarı kartı görünürlüğü ve sesli uyarı (T10) |
| 4 | 10 saniyelik dönüş gecikmesini fark ediyor mu? | Dönüş sırasında hâlâ dışarıda olduğunu ve riskin arttığını gözler. | Dönüş sayacı ve astronot konumu |
| 5 | Gece uykusunda (`8x`) kaynak tüketiminin sürdüğünü ve sol raporunu anlıyor mu? | Sol 1 raporunda başlangıç/bitiş değerlerini ve neden–sonuç özetini okuyabilir. | Sol raporu metin sadeleştirme |
| 6 | Enerji kıtlığında yaşam desteği–araştırma tercihini kurabiliyor mu? | Gerekince "Research Power: OFF" yaparak oksijen üretimini korur. | Üst çubuk neden–sonuç açıklaması |

### Deneme sonrası kayıt şablonu

- **Katılımcı yaşı / tarihi:**
- **Takıldığı ilk ekran veya terim:**
- **İlk radyasyon olayında dışarıda kaldı mı?**
- **Sol 1 raporundan ne anladı?**
- **Arayüzde kısaltılması/değiştirilmesi gereken metinler:**
