# Checkpoint raporu

Her OP başlamadan `OP00.md`, `OP01.md` vb. dosya oluştur. PLAN/CHECKPOINTS
çalışma protokolüne uy. Bu şablon bir test/build yapıldığı anlamına gelmez.

```text
ID / Başlık:
Durum: Pending | InProgress | NeedsVerification | Done | Blocked
Başlangıç / Son güncelleme (saat dilimiyle):
Okunan talimatlar ve FEATURES maddeleri:
Gerçek başlangıç durumu / korunacak kullanıcı değişiklikleri:
Aktif başka yazar / Unity lock durumu:

Alt işler:
[ ] 1.
[ ] 2.
[ ] 3.

Değişen dosyalar ve API:
Yeni state için Clone / Snapshot / Save / Load / Restart kontrolü:
Kayıt şeması / expansionRevision ve göç:
Komutun kabul/ret ve maliyet kuralları:

Çalıştırılan komutlar / exit code / XML / test sayıları:
Build / normal açılış / görüntü yolları:
Gerçekte doğrulanan davranış:
Yapılmayan doğrulama / başarısızlık / bilinen kalan:

Son derlenebilir sağlam nokta:
Devamda atılacak TAM ilk adım (dosya + metot + yapılacak değişiklik):
Sonraki OP / bağımlılık / dış doğrulama açıkları:
```

Blocked yalnız somut bir dış engel varsa ve neyin engellendiği açıklanabiliyorsa
kullanılır. Derleme/test hatası çözülecek InProgress iştir; otomatik olarak kullanıcıya
devredilmez. NeedsVerification başarısız/çalıştırılmamış görsel kabulü gizlemez.
