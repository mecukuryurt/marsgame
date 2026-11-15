ID / Durum / Başlangıç ve son güncelleme: CP15A / Done / 2026-11-15 01:10 — 01:12 (+03:00)
Önkoşullar ve okunan belgeler:
- `docs/operations-v2/checkpoints/CP14.md` (`Done`)
- `docs/operations-v2/FEATURES.md` (Bölüm L — EARTH-01)
- `docs/operations-v2/CHECKPOINTS.md`
- `Assets/MarsOutpost/Runtime/Simulation/EarthOrders.cs`
- `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`
- `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`

Mevcut git durumu ve korunacak kullanıcı değişiklikleri:
- CP00–CP14 `Done`.

Alt işler:
- [x] `EarthOrders.cs`: `EarthOrderStepSpec` yapısı ve `GetStepSpec(kind, stepIndex)` / `GetStepCount(kind)` yardımcısı ekle; `EARTH-01` (`SoilSample`) için Aşama 0 (`Outside`, 60sn, -0.25 stamina/sn) ve Aşama 1 (`Inside`, 60sn, -0.15 stamina/sn) tanımla
- [x] `MissionSimulation.cs` & `Resources.cs`: `AssignEarthOrder` içinde aşamanın `Location` kuralını uygula (dışarıdaki ekip üyesi iç aşamaya anında atanamaz, `ReturnToShelter` gerekir; dış aşamaya atanınca `Outside` olur); `StepResources` içinde aktif aşamanın `StaminaRate` değerini uygula; aşama bitince `CurrentStepIndex` ilerletip son aşamada `Completed` yap (bilim 0, iptal/tekrar ödül veya rapor çoğaltmaz)
- [x] EditMode testi ekle (`Earth01SoilSampleRequiresExteriorReturnThenInteriorAnalysisAndZeroScience`)
- [x] EditMode testlerini çalıştır ve sonucu `CP15A.md` ve `PROGRESS.md`'ye kaydet

Değişen dosyalar ve API:
- `Assets/MarsOutpost/Runtime/Simulation/EarthOrders.cs`: `EarthOrderStepSpec`, `UpfrontWaterPaid`, `GetStepCount`, `GetStepSpec`
- `Assets/MarsOutpost/Runtime/Simulation/MissionSimulation.cs`: `AssignEarthOrder` konum kontrolü
- `Assets/MarsOutpost/Runtime/Simulation/Resources.cs`: `GetCrewStaminaRate` Dünya emri desteği, `ApplyTransitions` çok aşamalı ilerleme
- `Assets/MarsOutpost/Tests/EditMode/EarthOrderInfrastructureTests.cs`: `Earth01SoilSampleRequiresExteriorReturnThenInteriorAnalysisAndZeroScience`

Çalıştırılan komutlar, çıkış kodu, test/build/görüntü yolları:
- `/home/senesatka/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath /run/media/senesatka/storage/projects/marsgame -runTests -testPlatform EditMode -testResults /run/media/senesatka/storage/projects/marsgame/docs/validation/v2-cp15a-editmode.xml -logFile /tmp/v2-cp15a-editmode.log` -> çıkış kodu `0` (`67/67` geçti)

Gerçekte doğrulanan davranış:
- `EARTH-01` dışarıda 60sn (`-0.25/sn` stamina = `-15`), ardından `ReturnToShelter` ile dönüş (dönüş öncesi iç aşama reddi) ve içeride 60sn (`-0.15/sn` stamina = `-9`) ile tamamlanır; bilim `0`, tekrar kabul/iptal reddedilir.

Yapılmayan doğrulama / bilinen sorun:
- Yok.

Son sağlam nokta ve devamda atılacak TAM ilk adım:
- CP15A tamamlandı; CP15B (`EARTH-02` oksijen deneyi) başlatılacak.

Bir sonraki checkpoint: CP15B
