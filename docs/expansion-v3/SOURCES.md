# V3 bilimsel bağlam ve oyun tasarımı ayrımı

15 Kasım 2026'da aşağıdaki üç birincil sayfa açılıp ilgili açıklamalar kontrol
edildi. Kaynaklar kavramsal ilişkiyi destekler; V3'ün fiyat, saniye, derinlik,
oyun hücresi veya debi değerlerini doğrulamaz. Doğrudan uzun alıntı yapılmadı.

| Kaynak | Desteklediği sınırlı ilişki | Oyunda kullanım |
|---|---|---|
| [NASA SWIM haritası](https://science.nasa.gov/resource/swim-map-shows-subsurface-water-ice-on-mars/) | Görev verileriyle yeraltı buzuna dair bölgesel haritalama, su kaynaklarına göre yer seçimi | "Önce araştır, sonra su tesisi yerleştir" eğitim ilişkisi. Sentetik grid, gerçek SWIM pikseli veya metre ölçekli sondaj ölçümü değildir. |
| [NASA NTRS — ISRU Soil Water Extraction: Thermal Challenges, 20190002020](https://ntrs.nasa.gov/citations/20190002020) | Regolitten su kazanımında ısıtma enerjisi, ısı transferi ve yoğuşturma önemli sistem konularıdır | Buz çıkarıcı/regolit fırını arasında enerji–verim tercihi. 1,5 ve 4 enerji/su oyuna aittir; kaynak bu oranları vermiyor. |
| [NASA Perseverance Rover Components](https://science.nasa.gov/mission/mars-2020-perseverance/rover-components/) | Altı tekerlek ve rocker-bogie düzeni arazide hareket için kullanılır | Altı tekerlekli görsel ve kontrollü rota. Oyunun hızı, şarjı ve basit eklem hareketleri gerçek Perseverance performansı değildir. |

## Oyuncuya kısa açıklama

TR: "Harita, yeraltı kaynakları ve üretim sayıları eğitim için tasarlanmış oyun
modelidir. NASA kaynakları su arama, kaynak kullanımı ve rover kavramlarını açıklar."

EN: "Terrain, resource deposits and production rates are educational game models.
NASA references explain water prospecting, resource use and rover concepts."

İlgili kartta kısa açıklama ve Kaynaklar bağlantısı yeterlidir; oyuncuyu teknik
belge listesi okumaya zorunlu bırakma. Özgün NASA görseli eklenecekse kendi kaynak
sayfasındaki kredi/kullanım koşulları korunur. Bir görselin varlığını gerçek veri
entegrasyonu olarak sunma. Gerçek veriyi indirme/import etme bu V3 kapsamı değildir.

## Eski yol haritasından otomatik taşınmayacaklar

- 12 kW panel, 150 kWh batarya, 2–15 m buz ve sabit sondaj/ısı sıcaklıkları.
- Her regolit parselinde su garantisi, gerçek Mars'ta işletilmiş RodWell iddiası.
- Gerçek HiRISE/CTX'den üretildiği söylenen ama prosedürel olan yükseklik haritası.
- Mars topoğrafyasını gerçek güvenilirlikte çözen rover fiziği veya doz hesabı.

Bu konularda daha ayrıntılı bilimsel özellik istenirse yeni kaynak doğrulaması
ve ayrı tasarım gerekir. V2'nin DONKI/ECLSS referansları mevcut SCIENCE_SOURCES
dosyasında tutulur; yeni sentetik olayları o tarihsel ölçümlerle karıştırma.
