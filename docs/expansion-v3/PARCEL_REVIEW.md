# Parsel hizalama incelemesi

15 Kasım 2026 — Kullanıcı tahsisin çalıştığını, başlangıç habitatı ve güneş dizisinin hücreleri kestiğini, dolayısıyla yeni kablo/panel konumlarının anlaşılmadığını bildirdi. R ile yapı dönüşü aynı anda kamera sıfırlıyordu.

Mevcut grid 4 birim, origin (-32,-24). Habitat pad x[-5.25,1.25], z[-3.05,3.05]; airlock ile birlikte korumalı habitat ve koridor hücreleri içinde kalır; birden çok parsel kaplar. Uyku ve eski kayıt koordinatlarını korumak için habitat bu korumalı çok hücreli yapı olarak kalır. Başlangıç güneş modeli (4.65,0.25) çevresinde dört hücreyi keserken domain tek hücre (9,6) ayırıyor: görsel bu hücreye taşınmalı ve sığdırılmalı. Çalışma hücresi (9,5), hava kilidi (8,5) ve mevcut domain ağ portları korunacak. Yeni konum eski kayıtların crew/rota verilerini değiştirmez.

Domain regresyonu `StarterSolarWorkApronConnectsNewLineAndAdjacentPanel`: başlangıç çalışma alanında hat, komşu parselde panel, bağlılık, üretim ve save/load sınanıyor. OP16 temizliğinden sonra `docs/validation/hackathon-editmode.xml` içinde 142/142 EditMode testi geçti. Normal tıklama akışı ve görsel hizalama UX uygulaması ve doğrulamasına bağlıdır; tamamlandı iddiası değildir.
