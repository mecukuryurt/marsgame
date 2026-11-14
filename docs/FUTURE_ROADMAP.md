# Mars Outpost — Gelecek Sürüm Yol Haritası (Future Technical Roadmap)
**Doküman Sürümü:** 1.0.0-PROPOSAL  
**Hedef Kitle:** Mühendislik & Tasarım Ekibi, Simülasyon Geliştiricileri  
**Bağlam:** NASA Bilimsel Verilerine ve ISRU (In-Situ Resource Utilization) İlkelerine Dayalı Genişletme Mimarisi  

---

## 1. Yönetici Özeti ve Vizyon

Mars Outpost mevcut sürümünde (v1.0), 3 astronotun yaşam desteğini, enerji dengesini, güneş fırtınası / toz fırtınası tehlikelerini ve bilim araştırmalarını `96x64` çözünürlüklü `32m x 32m` boyutunda ölçeklenmiş 3D Mars üssü habitatında başarıyla simüle etmektedir.

Bu yol haritası, üssün yerel bir sığınaktan gezegen ölçeğinde kendi kendine yetebilen kalıcı bir **Mars Kolonisi ve Araştırma İstasyonu**na dönüşmesini sağlayacak 4 temel genişleme sistemini ve bu sistemlerin arkasındaki NASA bilimsel temellerini tanımlar:

1. **Genişletilmiş Haritada Serbest Dolaşım & Keşif (Extended Basin Exploration & Fog-of-War)**
2. **Parsel Parsel Arazi Sistemi & Modüler Güneş/Yapı İnşası (Grid-Parcel Land Claims & Modular Infrastructure)**
3. **Gerçek NASA Bilimine Dayalı Yer Altından Su Çıkarma (Subsurface Water ISRU: SWIM, RodWell, Regolith Baking)**
4. **3D Mars Rover ile Toprak ve Kaya Örneklemesi (Autonomous Rocker-Bogie Science Rover)**

---

## 2. Sistem 1: Genişletilmiş Haritada Serbest Dolaşım & Keşif

### 2.1 Bilimsel Arka Plan ve Tasarım Hedefleri
Mevcut üs alanı, Gale Krateri veya Jezero Krateri tabanında yer alan korunaklı bir havzada yer almaktadır. Koloninin uzun vadeli hayatta kalması, üs çevresindeki jeolojik formasyonların (eski nehir deltaları, killi katmanlar, bazaltik lav tüpleri ve buzlu krater sırtları) haritalanmasını gerektirir.

### 2.2 Temel Mekanikler
- **64m x 64m (Genişletilebilir 256m x 256m) Krater Havzası:**
  - Yükseklik haritası (Heightmap), NASA MRO (Mars Reconnaissance Orbiter) HiRISE ve CTX topoğrafya modellerinden türetilir.
  - Değişken eğimli yamaçlar (%0-%35 eğim), derin regolitle kaplı batak kumluklar ve bazaltik kaya tarlaları.
- **Dinamik Savaş Sisi (Fog-of-War / Unexplored Shroud):**
  - Başlangıçta yalnızca üssün 15 metre yarıçapındaki habitat çevresi görünürdür.
  - Astronotlar ve Rover keşif yaptıkça görüş menzilleri (Line-of-Sight) doğrultusunda sis kalkar.
  - Gece olduğunda veya toz fırtınası sırasında görüş menzili %60 oranında daralır.
- **Taktik Mini-Harita & HUD Entegrasyonu:**
  - Ekranın sağ üst veya sol alt köşesinde gerçek zamanlı vektörel topografik mini-harita.
  - Üs binaları, enerji hatları, astronot pozisyonları ve tespit edilen su/maden rezervleri filtrelenebilir katmanlar halinde sunulur.
- **Keşif İşaretçileri (Navigational Beacons & Waypoints):**
  - Astronotlar yüzeyde ilerlerken radyo ve lazer telemetri işaretçileri (`NavBeacon`) dikebilir.
  - Bu işaretçiler, Rover'ların otonom seyrüsefer yapabilmesi için yerel GPS benzeri bir transponder ağı oluşturur.

---

## 3. Sistem 2: Parsel Parsel Arazi Sistemi & Modüler Güneş/Yapı İnşası

### 3.1 Parsel Tabanlı Grid Mimarisi
Mars yüzeyi, her biri $4\text{ m} \times 4\text{ m}$ ebadında standart mantıksal parsellere (`MarsParcel`) ayrılır.

```
+---------------+---------------+---------------+
| Parcel (0, 0) | Parcel (1, 0) | Parcel (2, 0) |
| Solar Array A | Power Cable   | Battery Bank  |
+---------------+---------------+---------------+
| Parcel (0, 1) | Parcel (1, 1) | Parcel (2, 1) |
| Regolith Bed  | RodWell Rig   | Storage Tank  |
+---------------+---------------+---------------+
```

### 3.2 Parsel Veri Modeli ve Çevresel Nitelikler
Her parsel kendine has fiziksel ve jeolojik verilere sahiptir:
- **Güneş Verimi Katsayısı ($\eta_{\text{solar}}$):** Krater duvarlarının oluşturduğu gölgeleme açısı ve parsel eğimine bağlı olarak hesaplanan yerel güneş ışınımı ($0.0 - 1.0$).
- **Zemin Eğimi & Taşlılık Oranı:** İnşaat yapılabilmesi için regolitin düzleştirilmesi (`Grading / Bulldozing`) gerekip gerekmediğini belirler.
- **Toz Birikim Hızı:** Rüzgar koridorlarına yakın parsellerde güneş panelleri %25 daha hızlı tozlanır.
- **Yeraltı Buz Derinliği & Mineral Yoğunluğu:** Su çıkarma ve inşaat agregası için jeolojik potansiyel.

### 3.3 Oyuncu Tarafından İnşa Edilebilir Modüler Yapılar
1. **Fotovoltaik Güneş Paneli Dizisi (Modular Solar PV Array):**
   - Parsel başına $12\text{ kW}$ zirve gündüz gücü üretir. Toz fırtınasında verimi düşer.
2. **Kriyojenik Pil Bankası (Regolith-Shielded Battery Bank):**
   - Gece döngüsü ve fırtına dönemleri için $150\text{ kWh}$ enerji depolar. Regolit dolgu ile soğuk Mars gecelerinden yalıtılmıştır.
3. **Güç ve Akışkan Dağıtım Hatları (Power Cables & Utility Conduits):**
   - Parseller arasında enerji ve su akışını sağlayan yüzey üstü yalıtımlı kablo kanalları.
4. **Basınçlı Habitat Genişleme Modülü (Hab Pod Expansion):**
   - Ek astronot yatakhanesi ve hidroponik sera alanı sunar.

---

## 4. Sistem 3: Gerçek NASA Bilimine Dayalı Yer Altından Su Çıkarma (ISRU)

Su, Mars kolonisinin en kritik hammaddesidir. Yalnızca içme suyu ve bitki yetiştirme için değil, aynı zamanda **Sabatier reaktörü** ve su elektrolizi yoluyla **Oksijen ($O_2$)** ve roket yakıtı olan **Sıvı Metan ($CH_4$)** üretiminin temel taşıdır.

Bu sistem, NASA'nın en güncel bilimsel araştırma ve mühendislik prototiplerine dayanarak 3 ardışık aşamada kurgulanmıştır:

### 4.1 Aşama 1 — Buz Haritalaması (NASA SWIM — Subsurface Water Ice Mapping)
- **Bilimsel Dayanak:** NASA Mars Reconnaissance Orbiter (MRO) üzerindeki **SHARAD (Shallow Radar)**, Mars Odyssey üzerindeki **GRS (Gamma Ray Spectrometer)** ve nötron dedektörleri verileri.
- **Oyun Mekaniği:**
  - Astronotlar veya yörünge uydusu, parselleri yüzey altı radarı ile tarar.
  - Her parsel için yeraltı buzunun derinliği ($2\text{ m} - 15\text{ m}$), saf buz tabakası kalınlığı ve zararlı **Perklorat ($ClO_4^-$)** tuz konsantrasyonu tespit edilir.
  - Oyuncu, en az enerjiyle en yüksek saflıkta su çıkarabileceği "tatlı noktaları" (sweet spots) belirler.

### 4.2 Aşama 2 — Rodriguez Kuyusu ("RodWell" / Honeybee Robotics RedWater Yöntemi)
- **Bilimsel Dayanak:** NASA Langley & Honeybee Robotics tarafından Grönland ve Güney Kutbu buz tabakalarında kanıtlanmış, Mars için geliştirilen **RedWater Deep Ice Extraction** teknolojisi.
- **Uygulama Alanı:** Saf ve kalın donmuş buz tabakası içeren parseller.
- **Süreç Adımları:**
  1. **Regolit Delme:** Matkap, $3-5\text{ m}$ kalınlığındaki kuru üst örtü regoliti delerek saf buz katmanına ulaşır.
  2. **Termal Sirkülasyon & Ergitme:** Yüzeyden yalıtımlı çift cidarlı sondaj borusu ile $60-80^\circ\text{C}$ sıcak su veya elektrikli ısıtıcı enjekte edilir.
  3. **Yeraltı Erime Havuzu (Subsurface Meltwater Cavity):** Buz tabakası içinde kendi kendini yalıtan küresel bir sıvı su havuzu oluşur.
  4. **Yüzeye Pompalama & Elektroliz:** Sıvı su yüzey tanklarına çekilir; filtreleme ve deiyonizasyon sonrası Yaşam Desteği ve $O_2$ Elektrolizörüne beslenir.
- **Enerji Maliyeti:** Sürekli $8-15\text{ kW}$ termal/elektriksel güç tüketir; ancak birim zamanda en yüksek debide saf su sağlayan yöntemdir.

### 4.3 Aşama 3 — Hidratlı Regolit Fırınlama (Regolith Baking / NASA Glenn Eğimli Tepsi + Yoğuşturucu)
- **Bilimsel Dayanak:** NASA Glenn Research Center ve KSC tarafından geliştirilen, buz içermeyen ekvatoral ve killi topraklardan su kazanımı yöntemi.
- **Uygulama Alanı:** Kalın buz tabakası bulunmayan, sülfat ve filosilikat (kil) mineralleri içeren standart Mars toprakları (%1-%4 bağlı kristalize su).
- **Süreç Adımları:**
  1. **Toprak Kazıma:** Rover veya astronot, kepçeyle yüzey regolitini toplayarak fırın bunkerına doldurur.
  2. **Yüksek Sıcaklıkta Pişirme:** Fırın veya odaklanmış güneş aynalı eğimli tepsi regoliti $200^\circ\text{C} - 500^\circ\text{C}$ sıcaklığa kadar ısıtır.
  3. **Taşıyıcı Gaz Akışı ($\text{CO}_2$ Sweep Gas):** Mars atmosferinden çekilen $\text{CO}_2$ gazı fırından geçirilerek salınan su buharını sürükler.
  4. **Yoğuşturucu (Condenser):** Soğuk kondansatör peteklerinde su buharı sıvı faza yoğuşturulur; kalan kuru cüruf dışarı atılır.
- **Enerji & Lojistik Özelliği:** Saf buza göre ton başına 5-10 kat daha fazla enerji ister, ancak gezegenin her yerinde su üretim garantisi sunar.

---

## 5. Sistem 4: 3D Mars Rover ile Toprak ve Kaya Örneklemesi

### 5.1 Araç Mimarisi ve Kinematik
- **Rocker-Bogie Süspansiyon Sistemi:** NASA Curiosity ve Perseverance tarzı 6 bağımsız tahrikli tekerlek; engelleri ve kayaları devrilmeden aşabilen pasif diferansiyel salıncak mekanizması.
- **Radyoizotop Termoelektrik Jeneratör (MMRTG) / Genişletilmiş Güneş Paneli:** Geceleri de çalışabilen kesintisiz düşük güç modu.

### 5.2 Görev Döngüsü ve Oynanış
1. **Otonom Rota Çizimi (Autonomous Waypoint Navigation):** Oyuncu haritadan hedef jeolojik parseli seçer; Rover arazi eğimini analiz ederek en güvenli rotayı çizer.
2. **Robotik Kol & Karot Matkabı (Robotic Arm Core Drill):**
   - Hedef kayaya yanaşarak toz temizleme fırçası (Dust Removal Tool) ve ultraviyole spektrometre (SHERLOC benzeri) ile ön analiz yapar.
   - Karot matkabı ile silindirik kaya örneği çıkararak hermetik titanyum numune tüpüne mühürler.
3. **Üsse İntikal & Bilim Analiz Laboratuvarı:**
   - Rover örnek tüpünü Üs Bilim Laboratuvarına teslim eder.
   - Oyuncu anında yüksek miktarda **Bilim Puanı** kazanır ve haritada yeni yeraltı kaynak rezervleri (nadir mineraller, konsantre buz cepleri) görünür hale gelir.

---

## 6. Veri Modeli ve Kod Mimarisi (Data Contracts)

Genişletme sistemlerinin mevcut `SimulationRunner` ve `MissionState` mimarisine kusursuz entegre olabilmesi için tasarlanan C# veri yapıları:

```csharp
namespace MarsOutpost.Expansion
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public enum ParcelTerrainKind
    {
        SmoothBasalt,
        RollingRegolith,
        RockyOutcrop,
        PermafrostCraterRim,
        SoftSandDrift
    }

    public enum ExtractionMethod
    {
        None,
        RodWellIceCavity,
        RegolithThermalBaking
    }

    /// <summary>
    /// Haritadaki her 4x4m inşa edilebilir/keşfedilebilir arazi parselinin durumu.
    /// </summary>
    [Serializable]
    public sealed class MarsParcelState
    {
        public Vector2Int GridCoord;
        public Vector3 WorldCenter;
        public ParcelTerrainKind TerrainType;
        public bool IsExplored;
        public bool HasLineOfSight;

        // Çevresel ve Güneş Verileri
        public float Elevation;
        public float SlopeAngle;
        public float SolarEfficiencyFactor; // 0.0 - 1.0 (gölgeleme katsayısı)
        public float DustAccumulationRate;

        // NASA SWIM Yeraltı Su Verileri
        public bool HasSubsurfaceIce;
        public float IceDepthMeters;          // 2.0m - 15.0m
        public float IceReserveVolumeLiters;  // Rezerv hacmi
        public float PerchlorateSalinityRatio;// Tuzluluk oranı

        // İnşa Edilen Yapı
        public string ConstructedFacilityId; // "solar-array-mk1", "battery-bank", "rodwell-drill"
        public bool IsPowered;
        public float StructuralHealth;
    }

    /// <summary>
    /// Aktif su çıkarma kuyusu veya fırınlama tesisi telemetrisi.
    /// </summary>
    [Serializable]
    public sealed class WaterExtractionSite
    {
        public string SiteId;
        public Vector2Int ParcelCoord;
        public ExtractionMethod Method;
        public bool IsOperating;

        // Operasyonel Veriler
        public float OperatingTemperatureCelsius; // RodWell: 60-80C | Baking: 200-500C
        public float PowerDrawKilowatts;
        public float WaterYieldLitersPerHour;
        public float RemainingIceReserveLiters;
        public float FilterWearPercent;
    }

    /// <summary>
    /// 3D Mars Gezgini (Rover) durum ve seyrüsefer telemetrisi.
    /// </summary>
    [Serializable]
    public sealed class RoverUnitState
    {
        public string RoverId;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public float BatteryLevel;            // 0 - 100
        public float MaximumPayloadCapacityKg;
        public float CurrentPayloadWeightKg;

        public bool IsDrilling;
        public bool IsNavigating;
        public Vector3 CurrentDestination;
        public List<Vector3> ActiveNavigationPath;

        public List<string> CollectedSampleIds;
    }
}
```

---

## 7. Aşamalı Geliştirme Kilometre Taşları (Milestones)

| Faz | Odak Alanı | Çıktılar & Hedefler | Bağımlılıklar |
|---|---|---|---|
| **Faz 1** | **Grid & Topografya Genişletmesi** | $64\text{m} \times 64\text{m}$ havza arazisi, `MarsParcelState` grid haritası, dinamik Fog-of-War ve mini-harita entegrasyonu. | Mevcut `MarsOutpostWorldView` 3D yüzey mesh motoru |
| **Faz 2** | **Modüler İnşaat & Güç Ağı** | Parsel tıklama ile Güneş Paneli Dizisi, Pil Bankası ve Güç Kablosu inşası; parsel bazlı güneş gölgeleme verimi. | Faz 1 grid sistemi, `SimulationRunner` kaynak sistemi |
| **Faz 3** | **NASA ISRU Su Çıkarma İstasyonları** | SWIM haritalama radarı, Rodriguez Kuyusu ("RodWell") animasyonlu sondaj kulesi, fırınlama tesisleri ve yaşam desteği su bağlantısı. | Faz 2 inşaat sistemi, su/enerji kaynak simülasyonu |
| **Faz 4** | **3D Bilim Gezgini (Rover)** | 6 tekerlekli Rocker-Bogie süspansiyonlu 3D Rover modeli, otonom rota çizimi, karot matkap delme sekansı ve laboratuvar numune teslim mekaniği. | Faz 1 seyrüsefer ağı, Lab araştırma mekaniği |

---

## 8. Sonuç

Bu yol haritası, Mars Outpost projesini basit bir sığınak yönetim simülasyonundan çıkararak, NASA'nın Artemis ve Moon-to-Mars mimarilerinde planlanan gerçek uzay mühendisliği ve ISRU operasyonlarını oyuncuya deneyimleten kapsamlı bir bilimsel simülasyona dönüştürecektir.
