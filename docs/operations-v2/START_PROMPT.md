# Hydrogen başlangıç ve devam promptları

## Başlangıç

```text
Bu açık marsgame projesinde Üs Operasyonu V2 planını uygula.
Önce geçerli AGENTS.md talimatlarını, sonra sırayla şu belgeleri oku:
docs/operations-v2/PLAN.md
docs/operations-v2/FEATURES.md
docs/operations-v2/CHECKPOINTS.md
docs/operations-v2/PROGRESS.md
Ardından mevcut kaynakları ve docs/RELEASE_NOTES.md dosyasını incele.

Bu V2 belgeleri kapsadıkları konularda eski MASTER_PLAN ve Antigravity promptunun
yerini alır. Mevcut kodu sıfırdan yazma; git'teki takip edilmeyen dosyalar dahil
kullanıcı çalışmasını koru. Unity sürümü 6000.3.28f1 olarak kalacak.

CP00'dan başla; bir seferde tek checkpoint uygula. Her checkpoint başlamadan
checkpoints/CP-ID.md raporuna küçük alt işleri ve dosya sınırlarını yaz;
her derleme/test sonrası ilerlemeyi kaydet. Bitişte gerçek kanıtla PROGRESS'i
güncelle, sonra sıradaki checkpoint'e geç. Özellikle CP15A–CP15G ayrı teslimlerdir.
Kullanıcı durdurmadıkça kapsam içindeki rutin işler için sürekli izin isteme.

Kontekst veya kullanım limiti yaklaşınca yeni büyük işe başlama. Çalışan kodu,
yarım kalan alt işi, test durumunu ve devamda atılacak tam ilk adımı checkpoint
dosyasına kaydet. Sonraki oturumun sohbet geçmişini bildiğini varsayma.
Yalnız plan önermekle durma; uygun olan kodu gerçekten uygula ve doğrula.

Önce uyku/yatak, takılarak geri yürüme, iç mekânda radyasyon efektini ve 1440×900
normal açılışı düzelt; sonra sürekli Operations modu, bilim/su çıkarma altyapısı,
beş bağımsız rastgele olay ve her iki sol sonunda yedi tür Dünya komutu gelir.
Süre/yüzde/10 bilim maliyetlerini değiştirme. Dünya görevleri bilim kazandırmaz.
PLAN'daki belirsizlik yorumlarını sessizce farklı uygulama; gerekirse tek somut soru sor.

Tek simülasyon saati, bağımsız seed'li kanallar, atomik maliyet/onarım, kaynak
korunumu, mevcut Training/Demo ve ayar menüsü korunmalı. Gerekli testleri ekle;
çalıştırmadığın test/build/görsel kontrolü başarılı diye raporlama. Üretim sayıları
oyun tasarımıdır; NASA ölçümü veya görünür gerçek radyasyon olarak anlatma.
Commit/push, ücretli satın alma, yayınlama veya yarışma başvurusu gönderme.
Son raporda tamamlanan CP'leri, kanıt yollarını ve açık kalan işleri listele.
```

## Limit sonrası devam

```text
Üs Operasyonu V2 uygulamasına kaldığın yerden devam et.
docs/operations-v2/PLAN.md, FEATURES.md, CHECKPOINTS.md, PROGRESS.md ve son
checkpoints/CP-ID.md kaydını oku. Git durumunu ve gerçek kodu karşılaştır.
InProgress veya NeedsVerification işi varsa önce onu tamamla/doğrula; yalnız
Done satırlarına bakıp yarım işi atlama. Tamamlanmış özellikleri yeniden yazma.
İlgili doğrulamayı çalıştırıp checkpoint kaydını güncelledikten sonra sıradaki
tek checkpoint'e geç; aynı protokolle ilerlemeyi sürdür.
```
