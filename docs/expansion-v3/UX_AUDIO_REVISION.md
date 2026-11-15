# V3 kullanıcı düzeltmesi — parsel, sade arayüz ve ses

15 Kasım 2026. Kullanıcı OP15 sonrası parsel sisteminin sorunlu olduğunu ve
arayüzün karmaşık/boğucu olduğunu bildirdi. Ayrıca Mars atmosferi veren ambiyans
ve telif sorunu yaratmayacak müzik istedi; OP32'ye kadar Terra Medium ile
checkpoint ve commit kullanarak ilerlemeyi yetkilendirdi.

## Öncelik ve çalışma alanı

Önce OP15'in gerçek kaynağını ve 141/35 test iddiasını kontrol et. Kullanıcı henüz
kesin tıklama/yerleştirme adımı vermedi; olası hatayı kanıtlamadan düzelttim deme.
UX01 parsel/arayüz, UX02 özgün ses, UX03 grafik doğrulama ayrı teslimlerdir.
Domain düzeltmeleri PARCEL_REVIEW raporunda ve ilgili OP raporunda kaydedilir.
Bunlar OP16–32'yi iptal etmez; yeni özellikler aşağıdaki sade arayüze eklenir.

## UX01 — Kalıcı düğme yığını yerine bağlama göre arayüz

1. Normal görünümde zaman, pause, temel rezervler ve seçili astronot yeterlidir.
   Ekip kartları kısa olabilir; ayrıntı seçince açılır. Teknik üretim oranları,
   bütün tamirler, katalog ve kamera kılavuzu sürekli görünmez.
2. Üstte anlaşılır birkaç giriş: Üs / İnşa / Görevler / Ayarlar. Tek etkin
   bağlam çekmecesi: yeni açılan panel eskisini kapatır. Kapat/Escape her zaman açık.
   Dünya emirleri ve sistem bakımı aynı anda katman katman açık kalmaz.
3. İnşa açıkça seçilen bir moddur: önce parsel seç, uygun eylemi/maliyeti gör,
   sonra taslak yerleştir ve ekip ata. Geçersiz zeminde neden metni; görünmeyen
   alanı veya HUD arkasını tıklama komut vermesin. Kamera sürükleme seçim değildir.
4. Seçili parsel vurgusu ve gerçek konum eşleşmeli; grid yüzeyde okunur fakat
   habitatı/etiketleri örtmez. Ghost collider'ı terrain raycast'ini kapatmasın.
   R dönüş tuşuyla kameranın R kısayolu çakışmasın. Escape önce ghost/modu kapatsın.
5. Mevcut otomasyon/test button ID'lerini mümkün olduğunca koru; gizlenmiş
   panel için test açma eylemini uygular. Eski test beklentisini körlemesine silme.
6. 1440×900 öncelikli; ölçeklenen metin, scroll ve tek çekmece. Yeni minimap/rover
   ayrıntıları sonradan aynı bağlam düzenine uyar; yeni kalıcı sağ panel yığını eklenmez.

Kabul: yeni oyuncu normal ekranda kataloğa/tamir listesine boğulmaz; parsel seçme,
önizleme, yerleştirme, tahsis, iptal ve kamera drag için PlayMode kanıtı vardır.

## UX02 — Özgün ambiyans ve müzik

1. Dosya indirmek yerine projeye özgün prosedürel ses üret: düşük seviyeli
   filtrelenmiş rüzgâr + iç mekân uğultusu; sakin uzun pad/notalar ve seyrek motif.
   Var olan şarkı/melodi taklidi yok; dış telifli kaynak veya lisansı belirsiz loop yok.
2. En az iki ayrı loop AudioSource (ambiyans/müzik) ve mevcut SFX; master/müzik/
   ambiyans/SFX düzeyleri, mute ve kalıcı kullanıcı tercihi. Master iki kez çarpılmaz.
3. Kısa fade giriş/çıkış, dikişsiz loop; pause ve ayar açıp kapamak klibi yeniden
   başlatmaz. Odak kaybında düşük seviyeye fade; kritik uyarı SFX anlaşılır kalır.
   Sesler düşük başlangıç seviyesinde; kullanıcı müziği tamamen kapatabilir.
4. Özgün üretim yöntemini ART_AUDIO_PROVENANCE.md içinde kaydet. Bu hukuki
   lisans sertifikası iddiası değildir; özgün sentez ve dış asset kullanılmama kaydıdır.
5. Sahne yeniden açılırken çift kaynak/çoğalan loop, üretilmiş clip sızıntısı ve
   her frame yeni tampon yok. Testler ses donanımı olmasa da mixer state/loop/
   ayar kalıcılığını doğrular; gerçek dinleme yapılmadıysa rapor bunu söyler.

## UX03 — Görsel ve akış kabulü

Gerçek Linux çıktısında normal, inşa, seçili parsel, görevler, sistemler ve
ayar ekranları incelenir. Rüzgâr ve müzik gerçekten dinlenebiliyorsa seviye/loop
kontrolü yapılır; yalnız AudioSource.isPlaying testini dinleme kanıtı sayma.

## Commit ve limit protokolü

Mevcut checkout'ta V2 + Hydrogen OP00–15 kodları commit edilmemiştir. Önce
bunun devralınan baseline olduğu açıkça belirtilen bir commit oluşturulur;
sonraki düzeltme/OP commit'leri bunun üstüne gelir. Assets, Packages,
ProjectSettings ve docs dışında kişisel IDE/üretim klasörlerini topluca ekleme.
Her doğrulanan küçük teslimde açık dosya listesiyle commit; push yok. Çalışan
başka ajanın dosyalarını kendi commit'ine alma. Git izin engelini açık raporla.
Limitten önce checkpoint raporunda tam dosya/metot/sonraki adım bırak;
yarım iş Done olmaz. Başarısız testler gizlenmez.
