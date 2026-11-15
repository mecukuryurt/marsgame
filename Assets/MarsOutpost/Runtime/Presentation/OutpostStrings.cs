using System;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Centralized bilingual (TR / EN) UI strings; structured for kid-friendly (9-12 yrs) clarity.
    /// All original const string fields are preserved for backward compatibility.
    /// </summary>
    public static class OutpostStrings
    {
        public static bool IsTurkish = true;

        public static string TrEn(string tr, string en) => IsTurkish ? tr : en;

        // Backward compatibility constants
        public const string GameTitle = "MARS OUTPOST";
        public const string BatteryLabel = "Battery";
        public const string WaterLabel = "Water";
        public const string OxygenLabel = "Oxygen";
        public const string ResearchLabel = "Research";
        public const string RiskLabel = "Training Risk Score";
        public const string RiskExplanation = "Training index (0-100), not a real radiation dose. Inside habitat reduces exposure.";
        public const string StaminaLabel = "Stamina";
        public const string DayPhase = "DAY";
        public const string NightPhase = "NIGHT";
        public const string PausedLabel = "PAUSED";
        public const string PauseButton = "Pause";
        public const string ResumeButton = "Resume";
        public const string SleepButton = "Sleep Crew (8x)";
        public const string WakeButton = "Cancel Sleep";
        public const string EarthAdviceButton = "Ask Earth Advice (45s)";
        public const string RestartButton = "Restart";
        public const string RestButton = "Rest Inside";
        public const string ReturnButton = "Return (10s)";
        public const string CancelTaskButton = "Stop Task";
        public const string ResearchToggleOn = "Research: ON";
        public const string ResearchToggleOff = "Research: OFF";
        public const string ContinueButton = "Continue";
        public const string AcknowledgeButton = "Acknowledge";
        public const string SkipTutorialButton = "Hide Guide";
        public const string NoEventsPending = "No upcoming hazards scheduled.";

        public const string TutorialStep1 = "GUIDE (1/3): Click an astronaut on the left or map (or press 1, 2, 3) to select them.";
        public const string TutorialStep2 = "GUIDE (2/3): Assign a task on the right (try Research Lab or Panel Maint.) and watch Stamina & Battery.";
        public const string TutorialStep3 = "GUIDE (3/3): Watch the Hazard card! Recall outside crew inside before a Solar Storm begins, and sleep at night to recover.";

        // Kid-friendly dynamic labels
        public static string GetBatteryLabel() => TrEn("Batarya", BatteryLabel);
        public static string GetWaterLabel() => TrEn("Su", WaterLabel);
        public static string GetOxygenLabel() => TrEn("Oksijen", OxygenLabel);
        public static string GetResearchLabel() => TrEn("Bilim", ResearchLabel);
        public static string GetStaminaLabel() => TrEn("Enerji (Stamina)", StaminaLabel);
        public static string GetRiskLabel() => TrEn("Kalkan / Risk", RiskLabel);
        public static string GetRiskExplanation() => TrEn("Sığınakta kalmak radyasyondan korur!", RiskExplanation);
        public static string GetDayPhase() => TrEn("GÜNDÜZ", DayPhase);
        public static string GetNightPhase() => TrEn("GECE", NightPhase);
        public static string GetPausedLabel() => TrEn("DURAKLATILDI", PausedLabel);

        public static string GetPauseButton(bool paused) =>
            paused ? TrEn("Devam Et", ResumeButton) : TrEn("Duraklat", PauseButton);

        public static string GetSleepButton(bool isSleeping) =>
            isSleeping ? TrEn("Uykuyu Bitir", WakeButton) : TrEn("Gece Uykusu", SleepButton);

        public static string GetEarthAdviceButton(bool urgent) =>
            urgent ? TrEn("[!] Dünya'ya Hemen Sor!", "Ask Earth NOW (Hazard <30s)") : TrEn(">> Dünya'ya Danış (45sn)", EarthAdviceButton);

        public static string GetRestButton() => TrEn("Dinlen", "Rest");
        public static string GetReturnButton() => TrEn("Sığınağa Dön", "Return");
        public static string GetCancelTaskButton() => TrEn("Durdur", "Stop");

        public static string GetResearchToggle(bool enabled) =>
            enabled ? TrEn("Lab: AÇIK", "Lab: ON") : TrEn("Lab: KAPALI", "Lab: OFF");

        public static string GetRestartButton() => TrEn("Yeniden Başlat", "Restart");
        public static string GetContinueButton() => TrEn("Devam Et", ContinueButton);
        public static string GetAcknowledgeButton() => TrEn("Anladım", AcknowledgeButton);
        public static string GetSkipTutorialButton() => TrEn("Rehberi Gizle", SkipTutorialButton);
        public static string GetSourcesButton() => TrEn("[NASA] Bilim Kaynakları", "NASA Sources");
        public static string GetLangButton() => TrEn("[TR / EN]", "[EN / TR]");
        public static string GetCameraResetButton() => TrEn("[3D] Kamera", "[3D] Reset Cam");
        public static string GetDomeRoofButton(bool isRoofVisible) =>
            isRoofVisible ? TrEn("[3D] Kubbe: Açık", "[3D] Dome: Closed") : TrEn("[3D] Kubbe: Kesit", "[3D] Dome: Cutaway");
        public static string GetCameraNavigationHint() =>
            TrEn("Tekerlek: Yakınlaştır | Sürükle / WASD: Gez | R: Sıfırla",
                 "Wheel: Zoom | Drag / WASD: Pan | R: Reset");

        public static string GetTutorialStep(int step)
        {
            switch (step)
            {
                case 1:
                    return TrEn("REHBER (1/3): Soldan veya haritadan bir astronot seç (veya 1, 2, 3 tuşlarına bas)!", TutorialStep1);
                case 2:
                    return TrEn("REHBER (2/3): Sağdaki renkli görev butonlarına tıkla, Enerji ve Bataryayı takip et!", TutorialStep2);
                default:
                    return TrEn("REHBER (3/3): Üstteki Tehlike Sayacına dikkat et! Güneş Fırtınası gelmeden içeri çekil ve gece uyu!", TutorialStep3);
            }
        }

        public static string FormatSimClock(double secondsIntoSol)
        {
            int total = (int)secondsIntoSol;
            int min = total / 60;
            int sec = total % 60;
            return $"{min:00}:{sec:00}";
        }

        public static string FormatCrewStatus(CrewState crew, double sleepFactor = 1.0)
        {
            switch (crew.Status)
            {
                case CrewStatus.Idle: return TrEn("Bekliyor (Boşta)", "Idle");
                case CrewStatus.Working: return TrEn("Görevde (Çalışıyor)", "Working");
                case CrewStatus.Resting: return TrEn("Dinleniyor (+0.20/sn)", "Resting (+0.20/s)");
                case CrewStatus.Returning: return TrEn($"Sığınağa Dönüyor ({crew.ReturnRemainingSeconds:0.#}sn)", $"Returning ({crew.ReturnRemainingSeconds:0.#}s)");
                case CrewStatus.Sheltered: return TrEn("Sığınakta Güvende", "Sheltered");
                case CrewStatus.Sleeping: return TrEn($"Uyuyor (+{0.40 * sleepFactor:0.00}/sn)", $"Sleeping (+{0.40 * sleepFactor:0.00}/s)");
                case CrewStatus.Exhausted: return TrEn("Yorgun Düşmüş (Dinlenmeli!)", "Exhausted (Needs Rest)");
                default: return crew.Status.ToString();
            }
        }

        public static string FormatLocation(CrewLocation location) =>
            location == CrewLocation.Inside
                ? TrEn("[GÜVENDE] Sığınakta", "[SAFE] Inside Shelter")
                : TrEn("[!] Mars Yüzeyinde (Dışarıda)", "[!] Outside on Mars Surface");

        public static string BuildTwoDecisionExplanation(MissionState state, double researchGoal)
        {
            double maxRisk = 0;
            for (int i = 0; i < state.Crew.Count; i++)
                if (state.Crew[i].RiskScore > maxRisk) maxRisk = state.Crew[i].RiskScore;

            int completedTasks = 0;
            for (int i = 0; i < state.Tasks.Count; i++)
                if (!state.Tasks[i].IsContinuous && state.Tasks[i].IsCompleted) completedTasks++;

            string decision1;
            if (maxRisk < 25)
            {
                decision1 = TrEn(
                    $"1. Tehlike Zamanlaması: Mürettebat radyasyon anında sığınakta kaldı (en yüksek risk: {maxRisk:0.#}/100). Harika!",
                    $"1. Hazard Timing: Crew stayed sheltered during radiation windows (peak risk {maxRisk:0.#}/100).");
            }
            else
            {
                decision1 = TrEn(
                    $"1. Tehlike Zamanlaması: Dışarıda kalındığı için risk {maxRisk:0.#}/100 değerine çıktı; uyarı başladığında hemen içeri çek!",
                    $"1. Hazard Timing: Outside exposure raised peak risk to {maxRisk:0.#}/100; recall crew when warnings start.");
            }

            string decision2;
            if (state.Resources.Oxygen >= 20)
            {
                decision2 = TrEn(
                    $"2. Enerji ve Yaşam Desteği: Oksijen korundu ({state.Resources.Oxygen:0.#}/100), {completedTasks} bakım görevi tamamlandı ve {state.Resources.ResearchProgress:0.#}/{researchGoal:0.#} bilim puanına ulaşıldı.",
                    $"2. Power & Life Support: Kept O2 healthy ({state.Resources.Oxygen:0.#}/100), finished {completedTasks} maintenance task(s), and reached {state.Resources.ResearchProgress:0.#}/{researchGoal:0.#} research.");
            }
            else
            {
                decision2 = TrEn(
                    $"2. Enerji ve Yaşam Desteği: Oksijen rezervi çok düştü ({state.Resources.Oxygen:0.#}/100); batarya azaldığında Lab gücünü kapat veya panelleri temizle.",
                    $"2. Power & Life Support: O2 reserve fell low ({state.Resources.Oxygen:0.#}/100); turn off Research Power or clean panels when battery is scarce.");
            }

            return decision1 + "\n" + decision2;
        }

        public static string GetGridToggleButton(bool visible) =>
            visible ? TrEn("Grid: AÇIK", "Grid: ON") : TrEn("Grid: KAPALI", "Grid: OFF");

        public static string FormatParcelTerrain(ParcelTerrainKind terrain)
        {
            switch (terrain)
            {
                case ParcelTerrainKind.Plain: return TrEn("Düzlük", "Plain");
                case ParcelTerrainKind.Dune: return TrEn("Kumul", "Dune");
                case ParcelTerrainKind.Crater: return TrEn("Krater", "Crater");
                case ParcelTerrainKind.Ridge: return TrEn("Eğimli Sırt", "Ridge");
                case ParcelTerrainKind.OutpostPad: return TrEn("Üs Platformu", "Outpost Pad");
                default: return terrain.ToString();
            }
        }

        public static string FormatParcelResource(ParcelState cell)
        {
            if (cell == null || !cell.IsExplored)
                return TrEn("Bilinmiyor (Keşfedilmedi)", "Unknown (Unexplored)");
            if (!cell.IsScanned)
                return TrEn("Bilinmiyor (Taranmadı)", "Unknown (Unscanned)");

            switch (cell.ResourceKind)
            {
                case ParcelResourceKind.Ice: return TrEn("Yüzey Buzu (Uygun)", "Surface Ice (Suitable)");
                case ParcelResourceKind.HydratedRegolith: return TrEn("Nemli Regolit", "Hydrated Regolith");
                case ParcelResourceKind.None: return TrEn("Yok / Çorak", "None / Barren");
                default: return cell.ResourceKind.ToString();
            }
        }

        public static string FormatClaimFailureReason(ParcelMap map, MissionState state, GridCoord coord)
        {
            if (state == null || map == null || state.Mode != MissionMode.Operations)
                return TrEn("Yalnız Operations modunda kullanılabilir.", "Only available in Operations mode.");
            if (!map.TryGetCell(coord, out var cell))
                return TrEn("Parsel harita sınırları dışında.", "Parcel is out of bounds.");
            if (cell.IsClaimed)
                return TrEn("Bu parsel zaten tahsis edildi.", "Parcel is already claimed.");
            if (!cell.IsExplored)
                return TrEn("Önce bu parseli keşfetmelisin.", "Explore this parcel first.");
            if (!map.HasClaimedNeighbor(coord))
                return TrEn("Tahsisli en az bir komşu parsel gerekli.", "Requires at least one claimed neighbor.");
            if (state.Materials < MissionSimulation.ClaimParcelCost)
                return TrEn(
                    $"Yetersiz malzeme ({state.Materials}/{MissionSimulation.ClaimParcelCost} gerekli).",
                    $"Insufficient materials ({state.Materials}/{MissionSimulation.ClaimParcelCost} required).");
            return string.Empty;
        }

        public static string FormatSurfaceJobSummary(SurfaceJobState job)
        {
            if (job == null) return string.Empty;
            string phaseStr;
            switch (job.Phase)
            {
                case SurfaceJobPhase.Travel:
                    phaseStr = TrEn($"Yolda → {job.TargetCoord} ({job.GetRemainingLegSeconds():0.#} sn)", $"Walking → {job.TargetCoord} ({job.GetRemainingLegSeconds():0.#} s)");
                    break;
                case SurfaceJobPhase.Work:
                    double remWork = System.Math.Max(0, job.WorkRequiredSeconds - job.WorkProgressSeconds);
                    phaseStr = TrEn($"Saha İşi {job.TargetCoord} ({remWork:0.#} sn)", $"Surface Work {job.TargetCoord} ({remWork:0.#} s)");
                    break;
                case SurfaceJobPhase.Return:
                    phaseStr = TrEn($"Hava Kilidine Dönüş ({job.GetRemainingLegSeconds():0.#} sn)", $"Returning to Airlock ({job.GetRemainingLegSeconds():0.#} s)");
                    break;
                default:
                    phaseStr = TrEn($"Tamamlandı {job.TargetCoord}", $"Completed {job.TargetCoord}");
                    break;
            }
            return phaseStr;
        }

        public static string FormatFacilityName(FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.UtilityLine: return TrEn("Bağlantı Hattı", "Utility Line");
                case FacilityKind.SolarPanel: return TrEn("Güneş Paneli", "Solar Panel");
                case FacilityKind.Battery: return TrEn("Batarya Modülü", "Battery Module");
                case FacilityKind.WaterExtractor: return TrEn("Buz Çıkarıcı", "Ice Extractor");
                case FacilityKind.RegolithFurnace: return TrEn("Regolit Fırını", "Regolith Furnace");
                case FacilityKind.WaterTank: return TrEn("Su Tankı", "Water Tank");
                case FacilityKind.ExtraShelter: return TrEn("Ek Sığınak", "Extra Shelter");
                case FacilityKind.RoverDock: return TrEn("Rover İstasyonu", "Rover Dock");
                default: return kind.ToString();
            }
        }

        public static string FormatFacilityLifecycle(FacilityLifecycleStatus status)
        {
            return status == FacilityLifecycleStatus.Commissioned
                ? TrEn("Aktif (Devrede)", "Commissioned")
                : TrEn("Taslak", "Blueprint");
        }

        public static string FormatFacilityConnectionStatus(FacilityState fac, ParcelMap map, GridCoord parcelCoord)
        {
            if (fac != null)
            {
                if (fac.LifecycleStatus == FacilityLifecycleStatus.Commissioned)
                {
                    return fac.IsConnectedToCore
                        ? TrEn("Bağlı (Çekirdek Ağ)", "Connected (Core Network)")
                        : TrEn("Bağlantı Yok (Merkeze 4-yönlü hat gerekli)", "Not Connected (Needs 4-way line to core)");
                }
                bool reach = UtilityNetwork.IsCoordConnectedPreview(map, parcelCoord);
                return reach
                    ? TrEn("Hat Erişimi Hazır", "Core Line Adjacent")
                    : TrEn("Bağlantı Yok (Hat gerekli)", "Not Connected (Needs line)");
            }

            bool cellReach = UtilityNetwork.IsCoordConnectedPreview(map, parcelCoord);
            return cellReach
                ? TrEn("Ağ Erişimi Var", "In Network Reach")
                : TrEn("Bağlantı Yok", "Not Connected");
        }

        public static string FormatSolarFacilityContribution(MissionState state, FacilityState fac, double solarRate)
        {
            if (state == null || fac == null || (fac.Kind != FacilityKind.SolarPanel && fac.Kind != FacilityKind.StarterSolarArray))
                return "";

            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                return TrEn("Üretim: 0 (Taslak)", "Output: 0 (Blueprint)");

            double current = SolarProduction.ComputeFacilitySolarOutput(solarRate, state, fac, out double baseDay);
            if (!fac.IsConnectedToCore)
                return TrEn("Üretim: 0 (Ağ Bağlantısı Yok)", "Output: 0 (No Core Link)");

            int faulted = 0;
            int owned = 0;
            var panels = state.Resources?.Panels;
            if (panels != null)
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    var p = panels[i];
                    bool owns = p.FacilityId == fac.FacilityId ||
                        (fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(p.Id));
                    if (owns)
                    {
                        owned++;
                        if (p.IsFaulted) faulted++;
                    }
                }
            }

            if (owned > 0 && faulted == owned)
                return TrEn("Üretim: 0 (Arızalı)", "Output: 0 (Faulted)");
            if (state.IsNight)
                return TrEn($"Üretim: 0 / Gece (Gündüz taban: +{baseDay:0.###}/sn)", $"Output: 0 / Night (Day base: +{baseDay:0.###}/s)");
            return TrEn($"Üretim: +{current:0.###}/sn (Taban: +{baseDay:0.###}/sn)", $"Output: +{current:0.###}/s (Base: +{baseDay:0.###}/s)");
        }

        public static string FormatActivePanelFaultSummary(MissionState state)
        {
            if (state?.Resources?.Panels == null)
                return "";

            string channelTargetId = null;
            if (state.ChannelSchedules != null)
            {
                for (int i = 0; i < state.ChannelSchedules.Count; i++)
                {
                    var ch = state.ChannelSchedules[i];
                    if (ch.ChannelId == OperationsChannelId.SolarPanel && ch.IsActive)
                    {
                        channelTargetId = ch.ActiveTargetId;
                        break;
                    }
                }
            }

            PanelUnitState faulted = null;
            for (int i = 0; i < state.Resources.Panels.Count; i++)
            {
                var p = state.Resources.Panels[i];
                if (!p.IsFaulted) continue;
                if (faulted == null || p.Id == channelTargetId)
                    faulted = p;
            }
            if (faulted == null)
                return "";

            var owner = SolarProduction.FindOwningFacility(faulted, state.Facilities);
            string loc = owner != null ? $" ({owner.Origin.X},{owner.Origin.Z})" : "";
            return TrEn($" | Hedef: {faulted.Id}{loc}", $" | Target: {faulted.Id}{loc}");
        }

        public static string FormatBatteryCapacityContribution(MissionState state, FacilityState fac, double baseBatteryCap)
        {
            if (state == null || fac == null || fac.Kind != FacilityKind.Battery)
                return "";

            double delta = FacilityCatalog.GetSpec(FacilityKind.Battery).BatteryCapacityDelta;
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                return TrEn($"Kapasite: +0 (Aktif olunca +{delta:0})", $"Cap: +0 (+{delta:0} when built)");

            if (!fac.IsConnectedToCore)
                return TrEn("Kapasite: +0 (Bağlantı Yok)", "Cap: +0 (Unlinked)");

            double lossPreview = ColonyStorage.PreviewBatteryLossIfFacilityDisconnected(
                new ResourceConfig(batteryCapacity: baseBatteryCap),
                state,
                fac);
            string previewNote = lossPreview > 0
                ? TrEn($" | Koparsa taşma kaybı: -{lossPreview:0.#}", $" | Unlink loss: -{lossPreview:0.#}")
                : "";
            return TrEn($"Kapasite: +{delta:0} (Etkin: {state.EffectiveBatteryCapacity:0}){previewNote}",
                        $"Cap: +{delta:0} (Total: {state.EffectiveBatteryCapacity:0}){previewNote}");
        }

        public static string FormatCancelBlueprintNotice(FacilityState bpFac)
        {
            if (bpFac == null || bpFac.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                return "";
            return bpFac.EnergySpentTotal > 0
                ? TrEn($"İptal: 0 malz. iade (harcanan {bpFac.EnergySpentTotal:0.#} enerji yanar)",
                       $"Cancel: 0 mat refund ({bpFac.EnergySpentTotal:0.#} spent energy lost)")
                : TrEn("İptal: 0 malz. iade (ödenen malzeme yanar)",
                       "Cancel: 0 mat refund (paid materials forfeited)");
        }

        public static string FormatDismantlePreview(MissionState state, FacilityState fac, ResourceConfig resConfig, CommandResult dismantleCheck)
        {
            if (state == null || fac == null || fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned || fac.IsProtectedStarter)
                return "";

            FacilityPlacementValidator.PreviewDismantleRefund(
                fac, state.Materials, state.MaterialCapacity, out int applied, out int raw, out int cappedLoss);
            FacilityPlacementValidator.PreviewStorageSpillOnDismantle(
                resConfig ?? new ResourceConfig(), state, fac, out double eSpill, out double wSpill);

            string capNote = cappedLoss > 0
                ? TrEn($" (kapasite dolu, {raw}→{applied})", $" (at cap, {raw}→{applied})")
                : "";
            string spillNote = "";
            if (eSpill > 0)
                spillNote += TrEn($" | Enerji taşması: -{eSpill:0.#}", $" | Energy spill: -{eSpill:0.#}");
            if (wSpill > 0)
                spillNote += TrEn($" | Su taşması: -{wSpill:0.#}", $" | Water spill: -{wSpill:0.#}");

            if (dismantleCheck != null && !dismantleCheck.Success)
                return TrEn($"Söküm engelli ({dismantleCheck.Reason})", $"Dismantle blocked ({dismantleCheck.Reason})");

            return TrEn($"Söküm iadesi: +{applied} malz.{capNote}{spillNote}",
                        $"Dismantle refund: +{applied} mat{capNote}{spillNote}");
        }

        public static string FormatExtractorStopReason(MissionState state, FacilityState fac, ParcelState cell)
        {
            if (state == null || fac == null)
                return "";
            double rem = cell != null && cell.IsScanned ? Math.Max(0, cell.RemainingWater) : -1;
            if (rem == 0)
                return TrEn("Stok Tükendi", "Reserve Depleted");
            if (state.Resources != null && !state.Resources.WaterExtractorEnabled)
                return TrEn("Ana Anahtar KAPALI", "Master Switch OFF");
            if (!fac.IsEnabled)
                return TrEn("Tesis KAPALI", "Site Switch OFF");
            if (!fac.IsConnectedToCore)
                return TrEn("Bağlantı Yok", "Unlinked to Core");
            if (state.Resources != null && state.Resources.Water >= 100.0 - 1e-6)
                return TrEn("Su Tankı Dolu", "Water Tank Full");
            if (fac.LastWaterExtractionRate <= 1e-6)
                return TrEn("Yetersiz Enerji", "No Power");
            return TrEn("Aktif", "Active");
        }

        public static string FormatExtractorSiteSummary(MissionState state, FacilityState fac, ParcelState cell)
        {
            if (state == null || fac == null || !FacilityCatalog.TryGetSpec(fac.Kind, out var spec) || spec.WaterProductionRate <= 0)
                return "";
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                return "";

            bool knownStock = cell != null && cell.IsScanned;
            double rem = knownStock ? Math.Max(0, cell.RemainingWater) : 0;
            bool faulted = state.Resources != null && state.Resources.WaterExtractorFaulted;
            bool masterOn = state.Resources == null || state.Resources.WaterExtractorEnabled;

            string connStr = fac.IsConnectedToCore ? TrEn("Bağlı", "Linked") : TrEn("Bağsız", "Unlinked");
            string siteStr = fac.IsEnabled ? TrEn("Saha AÇIK", "Site ON") : TrEn("Saha KAPALI", "Site OFF");
            string globalSvc = !masterOn
                ? TrEn("Ana KAPALI", "Master OFF")
                : (faulted ? TrEn("Servis ×0.50 Arıza", "Service ×0.50 Fault") : TrEn("Servis %100", "Service 100%"));
            string stopOrActive = FormatExtractorStopReason(state, fac, cell);

            string etaStr;
            if (!knownStock)
            {
                etaStr = TrEn("Bilinmiyor (Tarama Gerekli)", "Unknown (Scan Needed)");
            }
            else if (rem <= 1e-6)
            {
                etaStr = TrEn("Tükendi", "Depleted");
            }
            else if (fac.LastWaterExtractionRate <= 1e-6)
            {
                etaStr = TrEn("∞ (Durdu)", "∞ (Paused)");
            }
            else
            {
                double sec = rem / fac.LastWaterExtractionRate;
                double sols = sec / 720.0;
                etaStr = sec < 300.0
                    ? TrEn($"{sec:0} sn (~{sols:0.##} sol)", $"{sec:0}s (~{sols:0.##} sol)")
                    : TrEn($"{sols:0.#} sol ({sec:0} sn)", $"{sols:0.#} sol ({sec:0}s)");
            }

            string stockLine = knownStock
                ? TrEn($"Bilinen Stok: {rem:0.#} | Tahmini Bitiş: {etaStr}", $"Known Stock: {rem:0.#} | ETA: {etaStr}")
                : TrEn($"Bilinen Stok: Bilinmiyor | Tahmini Bitiş: {etaStr}", $"Known Stock: Unknown | ETA: {etaStr}");

            return TrEn(
                $"Çıkarıcı ({fac.Origin.X},{fac.Origin.Z}) | {connStr} | {siteStr} | {globalSvc}\nDurum: {stopOrActive} | +{fac.LastWaterExtractionRate:0.000} su/sn | -{fac.LastEnergyDrawRate:0.000} en/sn\n{stockLine}",
                $"Extractor ({fac.Origin.X},{fac.Origin.Z}) | {connStr} | {siteStr} | {globalSvc}\nState: {stopOrActive} | +{fac.LastWaterExtractionRate:0.000} H2O/s | -{fac.LastEnergyDrawRate:0.000} en/s\n{stockLine}");
        }

        public static string FormatWaterBreakdownSummary(MissionState state)
        {
            if (state?.Resources == null)
                return "";
            var r = state.Resources;
            return TrEn(
                $"Çık: +{r.LastWaterExtractionRate:0.000}/s (Ana {r.LastLegacyWaterExtractionRate:0.000}+Saha {r.LastSiteWaterExtractionRate:0.000}) | GK: +{r.LastRecycledWaterRate:0.000}/s\nToplam -> Çıkarma: {r.TotalExtractedWater:0.#} | Geri Kaz.: {r.TotalRecycledWater:0.#}",
                $"Ext: +{r.LastWaterExtractionRate:0.000}/s (Base {r.LastLegacyWaterExtractionRate:0.000}+Site {r.LastSiteWaterExtractionRate:0.000}) | Rec: +{r.LastRecycledWaterRate:0.000}/s\nTotals -> Extracted: {r.TotalExtractedWater:0.#} | Recycled: {r.TotalRecycledWater:0.#}");
        }

        public static string FormatBlueprintFailureReason(CommandResult result, FacilityKind kind, int cost, int materials)
        {
            if (result.Success)
                return TrEn($"Geçerli ({cost} malzeme, Stok: {materials})", $"Valid ({cost} mat, Stock: {materials})");

            string raw = result.Reason ?? string.Empty;
            if (raw.IndexOf("explored", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Önce bu parseli keşfetmelisin.", "Explore this parcel first.");
            if (raw.IndexOf("claimed", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Önce bu parseli tahsis etmelisin.", "Claim this parcel before building.");
            if (raw.IndexOf("starter", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Başlangıç tesisi üzerine kurulamaz.", "Cannot build on starter facility.");
            if (raw.IndexOf("already has a utility line", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Bu parselde zaten bağlantı hattı/taslağı var.", "Utility line or blueprint already present.");
            if (raw.IndexOf("already contains a facility", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Parselde zaten yapı veya taslak var.", "Parcel already has a structure or blueprint.");
            if (raw.IndexOf("not buildable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("cannot support a utility line", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Zemin inşaat için uygun değil.", "Terrain is not buildable.");
            if (raw.IndexOf("Insufficient materials", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn($"Yetersiz malzeme ({materials}/{cost}).", $"Insufficient materials ({materials}/{cost}).");
            if (raw.IndexOf("standing there", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Astronot bu parseldeyken kurulamaz.", "Cannot place while crew is standing there.");
            if (raw.IndexOf("return corridor", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Dışarıdaki ekibin dönüş koridoru kapanır.", "Would block outside crew return corridor.");
            if (raw.IndexOf("adjacent work cell", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("No reachable path", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Hava kilidinden iş erişim koridoru yok.", "No reachable work corridor from airlock.");
            if (raw.IndexOf("out of map bounds", StringComparison.OrdinalIgnoreCase) >= 0)
                return TrEn("Harita sınırları dışında.", "Out of map bounds.");
            return raw;
        }
    }
}
