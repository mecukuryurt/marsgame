# Hydrogen — V3 başlangıç ve devam promptları

Bu paket plan dosyasıdır; oyunda bu özellikler uygulanmış sayılmaz. Aynı
Antigravity sohbetinde veya yeni sohbette açık marsgame projesiyle kullanılabilir.
Yeni sohbet önceki konuşmayı bilmek zorunda değildir; dosyalar otoritedir.

## İlk başlangıç promptu

```text
Bu açık marsgame Unity projesinde Koloni Genişlemesi V3 planını gerçekten uygula.

Önce geçerli AGENTS.md talimatlarını, sonra şunları oku:
docs/expansion-v3/PLAN.md
docs/expansion-v3/INTEGRATION.md
docs/expansion-v3/PROGRESS.md
docs/expansion-v3/CHECKPOINTS.md içindeki çalışma protokolü ve sıradaki OP
İlgili özellik için docs/expansion-v3/FEATURES.md bölümleri
Bilimsel açıklamalar için docs/expansion-v3/SOURCES.md
Mevcut durum için docs/operations-v2/REVIEW.md

Kodları ve git durumunu da kontrol et. Takip edilmeyen dosyalar dahil mevcut
kullanıcı çalışmasını koru. Unity sürümü 6000.3.28f1 olarak kalacak.
V3 belgeleri genişleme konusunda eski FUTURE_ROADMAP önerilerinin yerini alır.
Eski V2 CP00–CP17 uygulamasını yeniden yazma. Yeni OP00 baseline'ından başla.

Hedef: parsel seçme/tahsis, malzeme, astronotla gerçek inşaat, bağlantı hattı,
yeni panel/batarya/su çıkarıcı; ardından keşif/tarama, regolit fırını/tank,
mini harita, ek sığınak ve tek roverla örnek taşıma/analiz.

OP00–OP32'yi sırayla, bir seferde tek checkpoint uygula. OP15 ve OP23'te çalışan
teslimi doğrula; mekanik testler yeşilse kullanıcı ayrıca durdurmadıkça sonraki
checkpoint'e geç. Tüm işleri tek değişiklikte yazıp en sonda test etme.
Sadece plan/anlatım üretip durma; sıradaki işi kodla ve doğrula.

Her OP'den önce checkpoints/OP-ID.md raporu oluştur; durumu InProgress yap,
3–6 küçük alt iş ve değişecek dosyaları yaz. Her derleme/testten sonra alt işleri,
gerçek komutu, çıkış kodunu, kanıt yolunu ve tam sonraki adımı kaydet.
Kabul sağlanınca Done, dış manuel/ekran doğrulaması eksikse NeedsVerification.
PROGRESS current/next ve tabloyu güncelle. Test başarısızlığını geçiştirme.

Önemli entegrasyonlar: Simulation Unity'den bağımsız kalacak. İlk üç panelin
toplam gücü korunacak; yeni panel gerçekten ek güç verecek. Tek panel tamiri
bütün panel listesini onarmayacak. Eski çıkarıcı iki kez sayılmayacak. Yeni
kalıcı verilerin Clone/Snapshot/Save/Load/Restart işlemleri aynı OP'de tamamlanacak.
Şema v1/v2 göçünü, mevcut aktif tehlike/Sol raporu korunmasını ve pause'da
yüklemeyi bozma. Yeni yapı maketi üretim hesabı yerine geçmez.

V2 beş olayın süre/yüzdelerini ve su onarımının 10 bilim maliyetini koru.
Dünya emirleri ve numuneleri her analiz yolunda 0 bilim vermeli. Yeni rover
keşif örneğinin bilim ödülü ayrı ve tek seferliktir. Malzeme/enerji/su kopyalama yok.
Training/Demo, ayarlar, uyku pozu, geri dönüş ve iç hacimde efekt dışlama korunacak.

Görsel/ekran kontrolünü gerçekten yap; gerçek cihaz yoksa açıkça yaz, bir formül
testini normal Linux açılışı gibi sunma. Mevcut kullanıcı sahnesini yeniden
üretmeden binary derle; INTEGRATION'daki komut yolunu kullan.
Kullanıcıdan rutin adımlar için tekrar tekrar izin isteme. Commit/push,
yayınlama, ücretli paket satın alma veya yarışma başvurusu yapma.

Limit/kontekst yaklaşınca büyük yeni işe başlama. Yarım OP'yi derlenebilir sağlam
noktaya getir; rapora ne kaldığını ve devamın TAM ilk adımını yaz. Limitten sonra
aynı kodu yeniden üretmeden bu dosyalardan devam et. Çalıştırılmayan kontrolü
başarılı sayma; kalan bütçeyi görmüyorsan kesin sayı uydurma.

Son teslimde OP durumlarını, oyuncunun gerçekten yapabildiklerini, test/build
kanıtlarını, kayıt sürümünü ve açık kalanları DELIVERY_FINAL.md içinde ver.
```

## Limitten sonra devam promptu

```text
Marsgame Koloni Genişlemesi V3 uygulamasına kaldığın yerden devam et.
docs/expansion-v3/PROGRESS.md, PLAN.md, INTEGRATION.md ve son
checkpoints/OP-ID.md dosyasını oku. Sıradaki OP'nin CHECKPOINTS adımlarını ve
ilgili FEATURES bölümlerini gerçek kod/git durumuyla karşılaştır.

InProgress OP varsa önce onun kayıtlı tam sonraki adımından devam et; yalnız
Done satırlarına bakıp yarım işi atlama. Yarım dosyayı baştan üretme.
NeedsVerification sadece dış ekran/manuel erişim eksiğiyse açık taşı; mekanik
test başarısızlığı varsa bağımlı işe geçmeden düzelt. Her küçük teslimde rapor
ve PROGRESS'i gerçek kanıtla güncelle; OP32'ye kadar sırayla ilerle.
Eski Operations V2'yi yeniden yazma; kullanıcı değişikliklerini koru.
```

## İsteğe bağlı sınırlı çalışma

Sadece ilk oynanabilir teslim istenirse ilk promptun sonuna şu cümle eklenebilir:

```text
Bu oturumda OP15 doğrulaması sonunda dur; OP16'yı başlatma. PROGRESS'e tam
devam adımını ve açık ekran doğrulamalarını kaydet.
```

Bu sınırlama eklenmezse ana prompt tüm planı sırayla uygulamayı ister. Bir
checkpoint'in uzun sürmesi veya limit nedeniyle bölünmesi önceki tamamlananları
yeniden yaptırmayı gerektirmez. Checkpoint dosyası geliştirici ilerleme kaydıdır;
oyuncu kayıt dosyasıyla aynı şey değildir.
