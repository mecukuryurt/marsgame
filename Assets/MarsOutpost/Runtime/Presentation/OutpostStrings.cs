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
    }
}
