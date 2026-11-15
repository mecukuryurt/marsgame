# Küçük teslimler ve kaldığı yerden devam protokolü

PLAN.md ve FEATURES.md tasarım kaynağıdır. PROGRESS.md durum kaynağıdır.
Her satır AYRI teslim; hepsini bir defada yazıp en sonda test çalıştırma.

## Her checkpoint'in zorunlu adımları

1. Geçerli talimatları, PROGRESS'i, son checkpoint raporunu ve gerçek dosyaları oku.
2. İlgili satırı InProgress yap. Değiştireceğin dosyaları ve 3–6 küçük alt işi
   `checkpoints/CP-ID.md` içine ÖNCEDEN yaz; kesinti olsa bile niyet kaybolmasın.
3. Küçük değişiklik yap; derleme/kritik kontrol sonrası rapora işlenen alt işi ekle.
4. İlgili mekanik testlerini ve gerekli PlayMode/görsel kontrolü uygula.
5. Gerçek komut, çıkış kodu, XML/build/görüntü yolları, API değişikliği ve kalan
   işi rapora yaz. Çalıştırılmayan kontrolü açıkça “yapılmadı” işaretle.
6. Kabul koşulu sağlanırsa Done, değilse InProgress/Blocked yap. Yalnızca kalan
   manuel grafik kontrolü varsa NeedsVerification; Done ile eş anlamlı değildir.
7. PROGRESS içindeki current/next alanlarını güncelle. Bağımlı işi doğrulama
   yapılmadan başlatma. Kullanıcı durdurmazsa sonraki küçük checkpoint'e devam et.

Limit/kontekst bitmeden raporu kaydet. Bütçe kalanını göremiyorsan kesin sayı
uydurma. Büyük yeniden yapılandırmayı sınırda başlatma. Beklenmeyen kesintiden
sonra son Done'dan değil, InProgress dosyasındaki alt işlerden devam et;
yarım kodu tekrar üretmeden derlenebilirliğini kontrol et.
Kaydetmek oyuncu save'i değildir; kod dosyaları + rapor + doğrulama artefaktıdır.
Commit/push yapılmaz; kullanıcı ayrıca isterse sürüm kontrol checkpoint'i oluşturulur.

## Sıralı iş listesi

| ID | İş ve adımlar | Bağımlılık | Kabul / kanıt |
|---|---|---|---|
| CP00 | Kaynak/git durumu → mevcut testleri yeniden çalıştır → BASELINE.md oluştur | Yok | Çalışılan checkout, Unity sürümü, gerçek test sayısı ve kullanıcı değişiklikleri kaydedilmiş |
| CP01 | Yatak anchor'ları → yatış/kalkış → halka/etiket → poz geçişleri | CP00 | FEATURES A; üç kişi uyurken gövde yatakta, uyanınca doğru poz; güncel Linux görüntüsü |
| CP02 | Dönüş rotası → render interpolasyonu → teğete dönme → görev geçişi | CP01 | FEATURES B; 15/30/60 FPS ve 1x/8x/pause; geri yürüme ve 0,25 sn sıçrama yok |
| CP03 | Efekt havuzlarını ayır → korumalı iç hacim → dış şematik radyasyon → örtüşme | CP02 | FEATURES C; içeride ışın/toz yok; kum+radyasyon birlikte görünür; grafik kanıtı |
| CP04 | Normal açılış ekranı → 16:10 kadraj → okunabilir UI → kaydedip yeniden aç | CP03 | FEATURES D; 1440×900 öncelikli, diğer iki çözünürlük; komut satırı zorlaması olmadan doğrulama |
| CP05 | Mod verisi → Operations sürekli akış → Training/Demo izolasyonu | CP04 | 2 sol sonunda Operations kapanmaz; Training/Demo eski sonuçları korunur; yeni rastgelelik henüz kapalı |
| CP06 | Bilim bakiyesi → atomik harcama → günlük öğün rutinini ayır | CP05 | FEATURES E; toplam araştırma düşmez, negatif bakiye yok, sol başında bir kez lojistik |
| CP07 | Su çıkarıcı durumu → enerji/kapasite akışı → sağlıklı üs dengeleme | CP06 | FEATURES F; geri kazanım ayrı; sağlıklı 6 sol ve uzun 100 sol kontrolü, sıfır enerjide üretim yok |
| CP08 | Kanal bazlı seed/PRNG → sonraki zaman → sınır entegrasyonu → günlük | CP07 | Beş bağımsız akış, aynı seed aynı sonuç, bir kanal değişince diğer dört tarih değişmez; bütün aralık sınırları |
| CP09 | Radyasyon seçimi → ön uyarı → aktif süre → yeni bekleme → uyku kesme → akut indeksin normale dönüşü | CP08 | FEATURES G; 900–1200, uyarı/başlangıç/bitiş bir kez; büyük/küçük Tick eşdeğer; içeride beklenen uzun vadeli davranış |
| CP10 | Üç panelin katkısı → tek arıza hedefi → dış tamir → yeniden planlama | CP09 | FEATURES H; sağlam panel sayısına göre güç, aynı kanalda arıza yığılması yok, tamir bir kez |
| CP11 | O₂ kapasite düşüşü → iki kat enerjili iş → güç kıtlığı → normale dönüş | CP10 | FEATURES I; 1200–1800, düşük girdide kısmi iş, harcanan enerji/üretim sayıları doğrulanmış |
| CP12 | Kum süresi/kayıp → panel/fault bileşimi → ayrı efekt → bitiş | CP11 | FEATURES J; 600–900 bekleme, 60–180 süre, üretim ×0,20; diğer arızayı silmez |
| CP13 | Su çıkarıcı düşüşü → 10 bilim komutu → çift tık/ret → yeni saat | CP12 | FEATURES K; çıkarıcı ×0,50, geri kazanım değişmez, tek ödeme, yetersiz bakiyede atomik ret |
| CP14 | Her iki sol varış → seeded torba → kuyruk → aktif emir/rapor UI | CP13 | Sol 3/5/7 başlangıcında bir emir; büyük Tick atlamaz; devam eden görev yeni emri engellemez |
| CP15A | EARTH-01 toprak toplama ve analiz | CP14 | Dış→dönüş→iç analiz; bilim 0, iptal/tekrar ödül veya rapor çoğaltmaz |
| CP15B | EARTH-02 oksijen deneyi | CP15A | Fiili üretimin %10'u deneye; üretim yokken ilerlemez; tanktan çift düşüş ve bilim yok |
| CP15C | EARTH-03 dış sensör kalibrasyonu | CP15B | Ölçüm/rapor aşamaları; radyasyon risk kuralları geçerli; bilim 0 |
| CP15D | EARTH-04 ısı yalıtım kontrolü | CP15C | Tarama/rapor, 3 enerji toplam maliyet, ücretsiz bakım etkisi yok, bilim 0 |
| CP15E | EARTH-05 su kalite analizi | CP15D | 2 su + 2 enerji gerçek maliyet, girdi yokken açıklamalı ret/kısmi ilerleme, bilim 0 |
| CP15F | EARTH-06 anten hizalama | CP15E | Dış iş + test, 5 enerji, komut varış takvimini bozmaz, bilim 0 |
| CP15G | EARTH-07 stok denetimi | CP15F | Envanter/prova, yemek veya bilim üretmez; yedi tür tamamlanmadan torba tekrarı yok |
| CP16 | Sürümlü kayıt DTO → yaz/yükle → tüm rastgele durum → bozuk kayıt | CP15G | Aynı zaman + seed devam eder; arıza/puan/emir korunur; yükleme pause, offline zaman atlaması yok |
| CP17 | 100 sol seed testleri → tüm mod regresyonu → gerçek Linux build → normal kullanıcı açılışı | CP16 | Aşağıdaki bitiş listesi tamam; kullanıcı denemesi yapılmadıysa açık; final rapor |

CP08'e kadar Operations yeni ekonomi için inşa hâlindedir; CP07 tamamlanmadan
“uzun süreli yaşanabilir üs”, CP13 bitmeden “tüm rastgele olaylar hazır” denmez.
Her CP15 satırı ayrı checkpoint dosyası ve ayrı durum güncellemesi üretir.

## CP17 bitiş listesi

- Öngörülen beş olayın her biri çok sayıda seed'de aralığında gerçekleşir.
- Birlikte oluşan radyasyon, kum ve arızalar fiziksel girdileri doğru birleştirir.
- Pause, uyku, sol raporu, menü, focus ve terminal durumda saatler tutarlıdır.
- Bilim, enerji ve su eksiye düşmez; görev iptali/yeniden yükleme kopyalama sağlamaz.
- Kullanıcı aynı olaydan birden fazla uyarı/ödeme/onarım almaz.
- Uyku pozları doğru, dönüş akıcı/önden yürüyüş, iç hacimde efekt yok.
- 1440×900 normal açılış ve settings sonrası yeniden açılışta UI/kadraj düzgün.
- Build gerçekten çalıştırılmış, XML/ekran görüntüsü ve bilinen eksikler kayıtlı.
- README, Sources ve görev listesi güncel; eski sabit olaya ait anlatım Operations'a uygulanmıyor.
- Gerçek çocuk playtesti yapılmadıysa otomatik testle yapılmış sayılmıyor.

## Checkpoint rapor şablonu

`checkpoints/CP-ID.md` dosyası şu alanları içermeli:

```text
ID / Durum / Başlangıç ve son güncelleme:
Önkoşullar ve okunan belgeler:
Mevcut git durumu ve korunacak kullanıcı değişiklikleri:
Alt işler: [ ] / [x]
Değişen dosyalar ve API:
Çalıştırılan komutlar, çıkış kodu, test/build/görüntü yolları:
Gerçekte doğrulanan davranış:
Yapılmayan doğrulama / bilinen sorun:
Son sağlam nokta ve devamda atılacak TAM ilk adım:
Bir sonraki checkpoint:
```
