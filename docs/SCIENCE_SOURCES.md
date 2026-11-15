# Mars Outpost — Bilimsel Kaynaklar ve NASA Arşiv İzlenebilirliği

Kaynak doğrulama tarihi: 14 Kasım 2026 (T08). V2 oyun eşleşmesi: 15 Kasım 2026.

Operations modundaki rastgele beklemeler, arıza olasılıkları, %80 solar kaybı,
%50 modül kapasitesi ve 10 bilim maliyeti oyun tasarımıdır; NASA canlı tahmini
veya Mars'ta ölçülmüş arıza sıklığı değildir. Aşağıdaki sabit olay zamanları
Training moduna aittir. Operations güç önceliği yaşam desteği → geri kazanım →
su çıkarma → onarım → araştırma/deneydir. Su çıkarıcı, sınırsız erişilebilir rezerv
varsayımıyla oyun birimlerinde modellenir; yerel buz sahası verisi iddiası yoktur.
Korunak içindeki akut risk indeksinin normale dönüşü gerçek biyolojik dozun
silinmesi veya radyasyon tedavisi olarak yorumlanmamalıdır.

## 1. Doğrudan doğrulanan NASA arşiv kayıtları ve oyun eşleşmesi

Bu çalışmada `https://api.nasa.gov/DONKI/SEP?startDate=2024-05-01&endDate=2024-05-15&api_key=DEMO_KEY` uç noktası doğrudan sorgulanarak gerçek NASA DONKI (Space Weather Database Of Notifications, Knowledge, Information) Güneş Enerjik Parçacık (SEP) kayıtları incelenmiştir:

| Kaynak / Arşiv Kimliği | Doğrulanan Tarih (UTC) | Araç / Sistem | Oyun Mekaniğine Etkisi | Tasarlanmış Model Ayrımı |
|---|---|---|---|---|
| `2024-05-09T13:59:00-SEP-001`<br>`https://ccmc.gsfc.nasa.gov/DONKI/view/SEP/30609/-1` | `2024-05-09T13:59Z`<br>(İlişkili parlama: `2024-05-09T08:45:00-FLR-001`, uyarı `20240509-AL-004`) | ACE/EPAM (REleASE 15.8–39.8 MeV) | Sol 1 birinci radyasyon olayı (`rad-1`). Parlama→SEP ön süresi (~5 saat), oyunda `t=60` uyarısı (`300 sn` = 5 simülasyon dakikası önceden) ve `[360, 420)` aktif penceresi olarak sıkıştırılır. | DONKI kaydı Dünya/heliosfer yakını gözlemidir; Mars yüzeyinde ölçülmüş doz değildir. İç (`0.15/sn`) ve dış (`1.20/sn`) risk puanı tamamen eğitim amaçlı oyun katsayılarıdır. |
| `2024-05-10T13:35:00-SEP-001`<br>`https://ccmc.gsfc.nasa.gov/DONKI/view/SEP/30677/-1` | `2024-05-10T13:35Z`<br>(İlişkili CME: `2024-05-08T05:36:00-CME-001`) | GOES-P: SEISS (>10 MeV) | İlk olaydan ~23,6 saat sonraki ikinci dalga, Sol 1 gecesindeki `rad-2` (`t=600` uyarı, `[660, 720)` aktif) olayını belirler ve gece uykusunda tehlike yönetimini öğretir. | Olayların tarihsel sırası örnek alınır. Olay aralığı ve uyarı süresi farklı eğitim sıkıştırmalarıyla tasarlanmıştır; tek bir oransal zaman ölçeği veya Mars yüzey dozu hesabı iddiası yoktur. |
| **NASA ISS ECLSS**<br>`https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/` | Güncel referans | Water Recovery System (WRS) ve Oxygen Generation System (OGS) | Su + elektrik → elektroliz ile O₂ üretimi; atık sudan su geri kazanımı; kıtlıkta güç önceliği (`Yaşam Desteği → Su Geri Kazanımı → Araştırma`). | Gerçek ISS su geri kazanımı ~%90 kütle dengesiyle çalışır; oyundaki `0–100` rezerv birimleri ve saniyelik oranlar 7–12 yaş eğitimi için sadeleştirilmiş katsayılardır. |
| **2018 Mars Küresel Toz Fırtınası Bağlamı**<br>`https://science.nasa.gov/mission/mars-exploration-rovers/` | Haziran 2018 arşivi (Opportunity & Curiosity) | Güneş panelli Opportunity güç kaybı ve Curiosity Gale Krateri atmosferik opaklık (`tau`) artışı | Sol 2 `dust-1` (`t=750` uyarı, `[780, 960)` aktif) olayının güneş üretimini düşürmesi; panel üzerindeki fiziksel kirlenmeden (`PanelFouling`) ayrı tutulması. | Oyunda kullanılan `0.50` güneş azalma katsayısı ve `180 sn` süre, ham `tau` eğrisi değil eğitim amaçlı tasarlanmış oyun parametresidir. |

## 2. Yarışma kapsamı değerlendirmesi (gıda ve iletişim gecikmesi)

`MASTER_PLAN.md` bölüm 4 revizyonu uyarınca, Space Apps Junior Astronaut Mission Trainer hedeflerini daha eksiksiz kapsamak için iki küçük, açıklanabilir mekanik T09'da eklenir:
- **Gıda hazırlama:** Beşinci kaynak barı açılmadan, her sol için tek ekip öğünü stoku ve gece toparlanma katsayısı (`1.0` vs `0.75`).
- **İletişim gecikmesi:** Sabit 45 sn gecikmeli kural tabanlı Dünya tavsiyesi mesaj kartı (gerçek Mars tek yön ışık süresi 3–22 dk aralığının sıkıştırılmış eğitim gösterimi; harici LLM/API kullanılmaz).
Tarım simülasyonu, bitki büyütme, serbest bina yerleştirme ve çok oyunculu mod bu sürümün dışındadır.
