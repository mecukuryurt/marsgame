using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Kid-friendly (9-12 yrs) bilingual (TR/EN) HUD controller.
    /// Reads MissionState and dispatches commands to SimulationRunner with zero per-frame object allocations.
    /// Features prominent hazard countdown radar, colorful contextual action buttons, and full world station click integration.
    /// </summary>
    public sealed class MarsOutpostHud : MonoBehaviour
    {
        [SerializeField] private SimulationRunner runner;
        [SerializeField] private MarsOutpostWorldView worldView;
        [SerializeField] private MarsOutpostAudio audioController;

        private Font uiFont;
        private Sprite whiteSprite;
        private string selectedCrewId = "crew-1";
        private float feedbackTimer;

        // Top Bar
        private Text clockText;
        private Text speedText;
        private Text batteryText;
        private Image batteryFill;
        private Text waterText;
        private Image waterFill;
        private Text oxygenText;
        private Image oxygenFill;
        private Text researchText;
        private Image researchFill;
        private Text reserveNoteText;
        private Text btnLangText;
        private Text btnModeText;
        private Text btnMuteText;
        private Text volumeText;

        // Left Crew Cards
        private Text crewHeaderText;
        private sealed class CrewCardUi
        {
            public string CrewId;
            public Image Background;
            public Text NameText;
            public Text StatusText;
            public Text StaminaText;
            public Image StaminaFill;
        }
        private readonly List<CrewCardUi> crewCards = new List<CrewCardUi>();

        // Left Galley & Earth Advice Card
        private Text galleyCommsStatusText;
        private Button btnEarthAdvice;
        private Image btnEarthAdviceBg;
        private Text btnEarthAdviceText;
        private Text earthAdviceBodyText;

        // Right Hazard Alert & Countdown Radar Banner (Top of Right Panel)
        private RectTransform hazardBannerRect;
        private Image hazardBannerBg;
        private Text hazardHeaderTitle;
        private Text eventCountdownText;
        private Image eventProgressFill;
        private Text eventText;
        private Image eventCardBg;
        private Text eventCardText;
        private EventPhase currentHazardPhase = EventPhase.Upcoming;

        // Right Selected Crew Card
        private Image selectedVisorBadge;
        private Text selectedNameText;
        private Image selectedLocationPillBg;
        private Text selectedLocationText;
        private Text selectedStatusText;
        private Text selectedStaminaText;
        private Image selectedStaminaFill;
        private Text selectedRiskText;
        private Text selectedStatsText;
        private Text selectedRiskNoteText;

        // Right Task Buttons (4 Colorful Kid-Friendly Buttons)
        private Button btnPanelMaint;
        private Image btnPanelMaintBg;
        private Text btnPanelMaintText;

        private Button btnSystemMaint;
        private Image btnSystemMaintBg;
        private Text btnSystemMaintText;

        private Button btnMealPrep;
        private Image btnMealPrepBg;
        private Text btnMealPrepText;

        private Button btnResearchTask;
        private Image btnResearchTaskBg;
        private Text btnResearchTaskText;

        // Right Safety & Rest Row
        private Button btnReturn;
        private Image btnReturnBg;
        private Text btnReturnText;

        private Button btnRest;
        private Image btnRestBg;
        private Text btnRestText;

        private Button btnCancelTask;
        private Image btnCancelTaskBg;
        private Text btnCancelTaskText;

        private Button btnResearchToggle;
        private Image btnResearchToggleBg;
        private Text btnResearchToggleText;

        // Right Bottom Utility Row
        private Button btnPause;
        private Text btnPauseText;
        private Button btnSleep;
        private Image btnSleepBg;
        private Text btnSleepText;
        private Button btnRestart;
        private Text btnRestartText;
        private Button btnSources;
        private Text btnSourcesText;

        // Feedback Banner
        private GameObject feedbackPanel;
        private Text feedbackText;

        // Tutorial Guide Strip
        private GameObject tutorialPanel;
        private Text tutorialText;
        private Text hideTutorialText;
        private int tutorialStep = 1;
        private bool tutorialDismissed;

        // Modals
        private GameObject solReportModal;
        private Text solReportTitleText;
        private Text solReportBody;
        private Text solBtnTxt;

        private GameObject alertModal;
        private Text alertTitleText;
        private Text alertBody;
        private Text alertBtnTxt;

        private GameObject outcomeModal;
        private Text outcomeTitle;
        private Text outcomeBody;
        private Text outBtnTxt;

        private GameObject sourcesModal;
        private Text sourcesTitle;
        private Text sourcesBody;
        private Text sourcesCloseText;

        // Color Palette
        private static readonly Color PanelBg = new Color(0.11f, 0.10f, 0.13f, 0.94f);
        private static readonly Color CardBg = new Color(0.16f, 0.15f, 0.20f, 0.96f);
        private static readonly Color SelectedBg = new Color(0.24f, 0.32f, 0.46f, 1f);
        private static readonly Color ButtonBg = new Color(0.23f, 0.27f, 0.35f, 1f);

        // Accents
        private static readonly Color AccentGreen = new Color(0.26f, 0.72f, 0.44f, 1f);
        private static readonly Color AccentAmber = new Color(0.92f, 0.62f, 0.18f, 1f);
        private static readonly Color AccentRed = new Color(0.86f, 0.22f, 0.22f, 1f);
        private static readonly Color AccentBlue = new Color(0.26f, 0.58f, 0.88f, 1f);

        // 4 Kid-Friendly Task Button Base Colors
        private static readonly Color ColorPanelMaint = new Color(0.20f, 0.38f, 0.58f, 1f);
        private static readonly Color ColorSystemMaint = new Color(0.14f, 0.44f, 0.52f, 1f);
        private static readonly Color ColorMealPrep = new Color(0.16f, 0.48f, 0.32f, 1f);
        private static readonly Color ColorResearch = new Color(0.40f, 0.24f, 0.58f, 1f);

        // Visor Accents
        private static readonly Color[] CrewVisors =
        {
            new Color(0.96f, 0.72f, 0.22f, 1f), // Gold
            new Color(0.28f, 0.80f, 0.96f, 1f), // Cyan
            new Color(0.42f, 0.88f, 0.54f, 1f)  // Emerald
        };

        private void Awake()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
                uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

            if (runner == null) runner = GetComponent<SimulationRunner>();
            if (worldView == null) worldView = GetComponent<MarsOutpostWorldView>();
            if (audioController == null) audioController = GetComponent<MarsOutpostAudio>();

            BuildCanvas();
        }

        private void Start()
        {
            if (worldView != null)
                worldView.SetLanguage(OutpostStrings.IsTurkish);

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                const string prefix = "--capture-screenshot=";
                if (args[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    StartCoroutine(CaptureScreenshotAndQuit(args[i].Substring(prefix.Length)));
                    break;
                }
            }
        }

        private IEnumerator CaptureScreenshotAndQuit(string outputPath)
        {
            if (runner != null)
            {
                runner.AssignTask("crew-1", "panel-maintenance");
                runner.AssignTask("crew-2", "research");
                runner.Stepper.AdvanceFrame(1.5);
            }
            for (int f = 0; f < 10; f++)
                yield return null;
            yield return new WaitForEndOfFrame();

            int w = Screen.width;
            int h = Screen.height;
            var shot = new Texture2D(w, h, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            shot.Apply();
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(outputPath, shot.EncodeToPNG());
            Destroy(shot);
            Application.Quit();
        }

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.StateChanged += HandleStateChanged;
                runner.CommandRejected += HandleCommandRejected;
            }
            if (worldView != null)
            {
                worldView.CrewSelected += HandleWorldCrewSelected;
                worldView.StationClicked += OnWorldStationClicked;
                worldView.SetLanguage(OutpostStrings.IsTurkish);
            }

            if (runner != null && runner.State != null)
                Refresh(runner.State);
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.StateChanged -= HandleStateChanged;
                runner.CommandRejected -= HandleCommandRejected;
            }
            if (worldView != null)
            {
                worldView.CrewSelected -= HandleWorldCrewSelected;
                worldView.StationClicked -= OnWorldStationClicked;
            }
        }

        private void Update()
        {
            if (feedbackTimer > 0)
            {
                feedbackTimer -= Time.unscaledDeltaTime;
                if (feedbackTimer <= 0 && feedbackPanel != null)
                    feedbackPanel.SetActive(false);
            }

            HandleShortcuts();
            UpdateHazardBannerVisuals();
            UpdateSafetyButtonVisuals();
        }

        private void UpdateHazardBannerVisuals()
        {
            if (hazardBannerRect == null || hazardBannerBg == null) return;

            if (currentHazardPhase == EventPhase.Warning)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 3.8f, 1f);
                Color amber = new Color(0.95f, 0.52f, 0.12f, 0.96f);
                Color crimson = new Color(0.78f, 0.18f, 0.18f, 0.96f);
                hazardBannerBg.color = Color.Lerp(amber, crimson, t);

                float s = Mathf.Lerp(1.0f, 1.035f, t);
                hazardBannerRect.localScale = new Vector3(s, s, 1f);
            }
            else if (currentHazardPhase == EventPhase.Active)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 4.2f, 1f);
                Color redUrgent = new Color(0.88f, 0.16f, 0.20f, 0.98f);
                Color redDark = new Color(0.48f, 0.10f, 0.14f, 0.98f);
                hazardBannerBg.color = Color.Lerp(redUrgent, redDark, t);

                float s = Mathf.Lerp(1.0f, 1.04f, t);
                hazardBannerRect.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                hazardBannerRect.localScale = Vector3.one;
                hazardBannerBg.color = new Color(0.12f, 0.16f, 0.24f, 0.96f);
            }
        }

        private void UpdateSafetyButtonVisuals()
        {
            if (btnReturn == null) return;

            bool isOutside = false;
            if (runner != null && runner.State != null)
            {
                for (int i = 0; i < runner.State.Crew.Count; i++)
                {
                    if (runner.State.Crew[i].Id == selectedCrewId)
                    {
                        isOutside = runner.State.Crew[i].Location == CrewLocation.Outside;
                        break;
                    }
                }
            }

            if (isOutside)
            {
                float pulse = 1.0f + 0.055f * Mathf.Sin(Time.unscaledTime * 6f);
                btnReturn.transform.localScale = new Vector3(pulse, pulse, 1f);
                if (btnReturnBg != null)
                    btnReturnBg.color = new Color(0.92f, 0.32f, 0.16f, 1f);
            }
            else
            {
                btnReturn.transform.localScale = Vector3.one;
                if (btnReturnBg != null)
                    btnReturnBg.color = ButtonBg;
            }
        }

        private void HandleShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame && runner != null)
                runner.TogglePause();
            if (kb.digit1Key.wasPressedThisFrame) SelectCrew("crew-1");
            if (kb.digit2Key.wasPressedThisFrame) SelectCrew("crew-2");
            if (kb.digit3Key.wasPressedThisFrame) SelectCrew("crew-3");
        }

        private void HandleWorldCrewSelected(string crewId)
        {
            selectedCrewId = crewId;
            if (tutorialStep == 1) tutorialStep = 2;
            if (runner != null && runner.State != null)
                Refresh(runner.State);
        }

        private void SelectCrew(string crewId)
        {
            selectedCrewId = crewId;
            if (tutorialStep == 1) tutorialStep = 2;
            if (worldView != null) worldView.SetSelectedCrew(crewId);
            if (runner != null && runner.State != null)
                Refresh(runner.State);
        }

        private void HandleStateChanged(MissionState state)
        {
            Refresh(state);
        }

        private void HandleCommandRejected(CommandResult result)
        {
            if (feedbackPanel == null || feedbackText == null) return;
            feedbackText.text = result.Reason;
            feedbackPanel.SetActive(true);
            feedbackTimer = 4.0f;
        }

        public void SetLanguage(bool turkish)
        {
            OutpostStrings.IsTurkish = turkish;

            if (btnLangText != null)
                btnLangText.text = OutpostStrings.GetLangButton();
            if (crewHeaderText != null)
                crewHeaderText.text = OutpostStrings.TrEn("ASTRONOTLAR (1-3)", "CREW (Keys 1-3)");
            if (btnRestartText != null)
                btnRestartText.text = OutpostStrings.GetRestartButton();
            if (btnSourcesText != null)
                btnSourcesText.text = OutpostStrings.GetSourcesButton();
            if (sourcesTitle != null)
                sourcesTitle.text = OutpostStrings.TrEn("NASA BİLİM VE ARŞİV KAYNAKLARI", "NASA SCIENCE & ARCHIVE SOURCES");
            if (sourcesCloseText != null)
                sourcesCloseText.text = OutpostStrings.TrEn("Kapat", "Close");
            if (solReportTitleText != null)
                solReportTitleText.text = OutpostStrings.TrEn("GÜN SONU RAPORU", "SOL REPORT");
            if (solBtnTxt != null)
                solBtnTxt.text = OutpostStrings.GetContinueButton();
            if (alertTitleText != null)
                alertTitleText.text = OutpostStrings.TrEn("🚨 KRİTİK UYARI", "CRITICAL ALERT");
            if (alertBtnTxt != null)
                alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            if (outBtnTxt != null)
                outBtnTxt.text = OutpostStrings.GetRestartButton();
            if (hideTutorialText != null)
                hideTutorialText.text = OutpostStrings.GetSkipTutorialButton();

            if (worldView != null)
                worldView.SetLanguage(turkish);

            if (runner != null && runner.State != null)
                Refresh(runner.State);
        }

        private void OnWorldStationClicked(string stationId)
        {
            if (runner == null || runner.State == null) return;

            CrewState selected = null;
            for (int i = 0; i < runner.State.Crew.Count; i++)
            {
                if (runner.State.Crew[i].Id == selectedCrewId)
                {
                    selected = runner.State.Crew[i];
                    break;
                }
            }
            if (selected == null && runner.State.Crew.Count > 0)
            {
                selected = runner.State.Crew[0];
                selectedCrewId = selected.Id;
            }

            if (stationId == "rest-or-return")
            {
                if (selected != null && selected.Location == CrewLocation.Outside)
                {
                    runner.ReturnToShelter(selectedCrewId);
                }
                else
                {
                    runner.Rest(selectedCrewId);
                }
            }
            else if (stationId == "panel-maintenance" || stationId == "system-maintenance" || stationId == "meal-prep" || stationId == "research")
            {
                AssignOrReplaceTask(stationId);
            }
        }

        private void AssignOrReplaceTask(string taskId)
        {
            if (runner == null) return;
            runner.AssignTask(selectedCrewId, taskId);
        }

        private void Refresh(MissionState state)
        {
            if (state == null) return;
            if (worldView != null)
            {
                worldView.SetSelectedCrew(selectedCrewId);
                worldView.ApplyState(state);
            }

            // Top bar
            string phaseStr = state.IsNight ? OutpostStrings.GetNightPhase() : OutpostStrings.GetDayPhase();
            clockText.text = $"Sol {state.SolNumber}  |  {phaseStr}  {OutpostStrings.FormatSimClock(state.SecondsIntoSol)}";

            bool paused = runner != null && runner.Stepper != null && runner.Stepper.IsPaused;
            if (paused)
            {
                speedText.text = OutpostStrings.GetPausedLabel();
                speedText.color = AccentAmber;
            }
            else if (state.IsSleeping)
            {
                speedText.text = OutpostStrings.TrEn("UYKU 8x", "SLEEP 8x");
                speedText.color = AccentBlue;
            }
            else
            {
                speedText.text = "1x";
                speedText.color = Color.white;
            }

            var res = state.Resources;
            double researchGoal = runner != null ? runner.ActiveResearchGoal : 120;
            UpdateBar(batteryText, batteryFill, OutpostStrings.GetBatteryLabel(), res.Battery, 100, new Color(0.92f, 0.74f, 0.22f));
            UpdateBar(waterText, waterFill, OutpostStrings.GetWaterLabel(), res.Water, 100, new Color(0.28f, 0.68f, 0.92f));
            string o2Status = res.Oxygen < 20 ? OutpostStrings.TrEn("DÜŞÜK O2!", "LOW O2!") : OutpostStrings.TrEn("İYİ", "OK");
            UpdateBar(oxygenText, oxygenFill, $"{OutpostStrings.GetOxygenLabel()} ({o2Status})", res.Oxygen, 100, res.Oxygen < 20 ? AccentRed : AccentGreen);
            UpdateBar(researchText, researchFill, OutpostStrings.GetResearchLabel(), res.ResearchProgress, researchGoal, new Color(0.68f, 0.52f, 0.96f));

            if (btnLangText != null)
                btnLangText.text = OutpostStrings.GetLangButton();

            if (btnModeText != null && runner != null)
                btnModeText.text = runner.IsDemoMode ? OutpostStrings.TrEn("90sn Demo", "90s Demo") : OutpostStrings.TrEn("2-Sol Standart", "2-Sol Std");

            RefreshAudioControls();

            string solarNote = state.IsNight
                ? OutpostStrings.TrEn("Gece: Güneş enerjisi yok (Batarya devrede)", "Night: 0 solar (using Battery)")
                : OutpostStrings.TrEn($"Gündüz Güneş: %{(1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0} verim", $"Day Solar: {(1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0}% eff");
            string dustNote = res.AtmosphericDustLoss > 0
                ? OutpostStrings.TrEn($" | Toz -%{(res.AtmosphericDustLoss * 100):0}", $" | Dust -{(res.AtmosphericDustLoss * 100):0}%")
                : "";
            string researchNote = res.ResearchEnabled
                ? OutpostStrings.TrEn($" | Araştırma %{(res.LastResearchRateFactor * 100):0} güç", $" | Research {(res.LastResearchRateFactor * 100):0}% power")
                : OutpostStrings.TrEn(" | Araştırma KAPALI (Enerji tasarrufu)", " | Research OFF (saving power)");
            string lifeSupportNote = OutpostStrings.TrEn(" | Yaşam desteği önce O2 üretir", " | Life Support feeds O2 first");
            reserveNoteText.text = $"{solarNote}{dustNote}{researchNote}{lifeSupportNote}";

            // Tutorial guide strip
            if (!tutorialDismissed && tutorialPanel != null)
            {
                if (tutorialStep == 2)
                {
                    for (int i = 0; i < state.Crew.Count; i++)
                    {
                        if (state.Crew[i].Status == CrewStatus.Working)
                        {
                            tutorialStep = 3;
                            break;
                        }
                    }
                }
                tutorialPanel.SetActive(true);
                tutorialText.text = OutpostStrings.GetTutorialStep(tutorialStep);
            }
            else if (tutorialPanel != null && tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(false);
            }

            // Left Crew Cards
            CrewState selected = null;
            for (int i = 0; i < state.Crew.Count && i < crewCards.Count; i++)
            {
                var c = state.Crew[i];
                var card = crewCards[i];
                card.CrewId = c.Id;
                bool isSelected = c.Id == selectedCrewId;
                if (isSelected) selected = c;
                card.Background.color = isSelected ? SelectedBg : CardBg;
                string locStr = c.Location == CrewLocation.Inside
                    ? OutpostStrings.TrEn("İçeride", "Inside")
                    : OutpostStrings.TrEn("Dışarıda", "Outside");
                card.NameText.text = $"{c.Name}  [{locStr}]";
                card.StatusText.text = OutpostStrings.FormatCrewStatus(c, state.CurrentNightSleepFactor);
                card.StaminaText.text = $"{OutpostStrings.GetStaminaLabel()}: {c.Stamina:0}/100";
                card.StaminaFill.fillAmount = Mathf.Clamp01((float)(c.Stamina / 100.0));
                card.StaminaFill.color = c.Stamina < 20 ? AccentRed : (c.Stamina < 50 ? AccentAmber : AccentGreen);
            }

            if (selected == null && state.Crew.Count > 0)
            {
                selected = state.Crew[0];
                selectedCrewId = selected.Id;
            }

            // Right Selected Astronaut Header Card
            if (selected != null)
            {
                int crewIdx = 0;
                for (int i = 0; i < state.Crew.Count; i++)
                {
                    if (state.Crew[i].Id == selected.Id) { crewIdx = i; break; }
                }
                if (selectedVisorBadge != null && crewIdx < CrewVisors.Length)
                    selectedVisorBadge.color = CrewVisors[crewIdx];

                selectedNameText.text = selected.Name;

                bool isInside = selected.Location == CrewLocation.Inside;
                selectedLocationText.text = OutpostStrings.FormatLocation(selected.Location);
                if (selectedLocationPillBg != null)
                {
                    selectedLocationPillBg.color = isInside
                        ? new Color(0.12f, 0.36f, 0.24f, 0.95f)
                        : new Color(0.48f, 0.24f, 0.10f, 0.95f);
                }
                selectedLocationText.color = isInside
                    ? new Color(0.42f, 0.94f, 0.68f, 1f)
                    : new Color(0.98f, 0.74f, 0.24f, 1f);

                selectedStatusText.text = OutpostStrings.TrEn("Durum: ", "Status: ") + OutpostStrings.FormatCrewStatus(selected, state.CurrentNightSleepFactor);

                selectedStaminaText.text = OutpostStrings.TrEn($"⚡ Enerji: {selected.Stamina:0.#}/100", $"⚡ Stamina: {selected.Stamina:0.#}/100");
                selectedStaminaFill.fillAmount = Mathf.Clamp01((float)(selected.Stamina / 100.0));
                selectedStaminaFill.color = selected.Stamina < 20 ? AccentRed : (selected.Stamina < 50 ? AccentAmber : AccentGreen);

                selectedRiskText.text = OutpostStrings.TrEn($"🛡️ Kalkan/Risk: {selected.RiskScore:0.#}/100", $"{OutpostStrings.RiskLabel}: {selected.RiskScore:0.#}/100");
                selectedRiskText.color = selected.RiskScore >= 60 ? AccentRed : (selected.RiskScore >= 25 ? AccentAmber : new Color(0.82f, 0.90f, 0.96f));

                if (selectedStatsText != null)
                    selectedStatsText.text = $"{selectedLocationText.text} | {selectedStatusText.text}";
                if (selectedRiskNoteText != null)
                    selectedRiskNoteText.text = OutpostStrings.GetRiskExplanation();
            }

            // 4 Task Buttons
            UpdateTaskButton(state, "panel-maintenance", btnPanelMaintText, "☀️ Güneş Paneli Temizle (Dış", "☀️ Clean Solar Panels (Out", false, 60);
            UpdateTaskButton(state, "system-maintenance", btnSystemMaintText, "🔧 Hava & Su Tamiri (İç", "🔧 Fix Air & Water (In", false, 45);
            UpdateTaskButton(state, "meal-prep", btnMealPrepText, "🍲 Uzay Yemeği Pişir (İç", "🍲 Cook Space Meal (In", false, 30);
            UpdateTaskButton(state, "research", btnResearchTaskText, "🔬 Mars Bilimi Yap (İç", "🔬 Do Mars Science (In", true, 0);

            // Left Galley & Comms
            UpdateGalleyAndCommsCard(state);

            // Contextual Safety & Controls
            if (btnReturnText != null) btnReturnText.text = OutpostStrings.GetReturnButton();
            if (btnRestText != null) btnRestText.text = OutpostStrings.GetRestButton();
            if (btnCancelTaskText != null) btnCancelTaskText.text = OutpostStrings.GetCancelTaskButton();
            if (btnResearchToggleText != null) btnResearchToggleText.text = OutpostStrings.GetResearchToggle(res.ResearchEnabled);

            // Global Controls
            if (btnPauseText != null)
            {
                btnPauseText.text = (runner != null && runner.Stepper != null && runner.Stepper.UserPaused)
                    ? OutpostStrings.GetPauseButton(true)
                    : OutpostStrings.GetPauseButton(false);
            }

            if (btnSleepText != null)
                btnSleepText.text = OutpostStrings.GetSleepButton(state.IsSleeping);

            if (btnSleepBg != null)
            {
                if (state.IsSleeping)
                {
                    btnSleepBg.color = AccentBlue;
                }
                else if (state.IsNight)
                {
                    // Dreamy indigo-blue when nighttime sleep is available!
                    btnSleepBg.color = new Color(0.24f, 0.32f, 0.72f, 1f);
                }
                else
                {
                    btnSleepBg.color = ButtonBg;
                }
            }

            if (btnRestartText != null) btnRestartText.text = OutpostStrings.GetRestartButton();
            if (btnSourcesText != null) btnSourcesText.text = OutpostStrings.GetSourcesButton();

            // Right Hazard Alert & Countdown Radar Banner
            UpdateHazardBanner(state);

            // Modals
            bool showOutcome = state.Outcome != MissionOutcome.InProgress;
            bool showAlert = !showOutcome && state.CriticalAlertPauseRequested;
            bool showReport = !showOutcome && !showAlert && state.SolReportPauseRequested && state.PendingSolReport != null;

            if (solReportModal.activeSelf != showReport) solReportModal.SetActive(showReport);
            if (showReport)
            {
                var r = state.PendingSolReport;
                string eventsLine = r.EventsExperienced.Count > 0 ? string.Join(", ", r.EventsExperienced) : OutpostStrings.TrEn("Yok", "None");
                string tasksLine = r.CompletedTasks.Count > 0 ? string.Join(", ", r.CompletedTasks) : OutpostStrings.TrEn("Yok", "None");
                solReportTitleText.text = OutpostStrings.TrEn("GÜN SONU RAPORU", "SOL REPORT");
                solReportBody.text =
                    $"SOL {r.SolNumber} " + OutpostStrings.TrEn("TAMAMLANDI\n\n", "COMPLETE\n\n") +
                    $"{OutpostStrings.GetBatteryLabel()}: {r.StartBattery:0.#} -> {r.EndBattery:0.#}\n" +
                    $"{OutpostStrings.GetWaterLabel()}: {r.StartWater:0.#} -> {r.EndWater:0.#}\n" +
                    $"{OutpostStrings.GetOxygenLabel()}: {r.StartOxygen:0.#} -> {r.EndOxygen:0.#}\n" +
                    OutpostStrings.TrEn($"Kazanılan Bilim: +{r.ResearchGained:0.#}\n", $"Research Gained: +{r.ResearchGained:0.#}\n") +
                    OutpostStrings.TrEn($"Biten Görevler: {tasksLine}\n", $"Tasks Finished: {tasksLine}\n") +
                    OutpostStrings.TrEn($"Yaşanan Olaylar: {eventsLine}\n\n", $"Events: {eventsLine}\n\n") +
                    $"{r.CauseEffectSummary}";
                solBtnTxt.text = OutpostStrings.GetContinueButton();
            }

            if (alertModal.activeSelf != showAlert) alertModal.SetActive(showAlert);
            if (showAlert)
            {
                alertTitleText.text = OutpostStrings.TrEn("🚨 KRİTİK UYARI", "CRITICAL ALERT");
                alertBody.text = state.ActiveAlertMessage;
                alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            }

            if (outcomeModal.activeSelf != showOutcome) outcomeModal.SetActive(showOutcome);
            if (showOutcome)
            {
                outcomeTitle.text = state.Outcome == MissionOutcome.Succeeded
                    ? OutpostStrings.TrEn("🎉 GÖREV BAŞARIYLA TAMAMLANDI! 🎉", "MISSION SUCCEEDED")
                    : OutpostStrings.TrEn("❌ GÖREV SONA ERDİ", "MISSION ENDED");
                outcomeTitle.color = state.Outcome == MissionOutcome.Succeeded ? AccentGreen : AccentRed;
                string decisions = OutpostStrings.BuildTwoDecisionExplanation(state, researchGoal);
                outcomeBody.text =
                    $"{state.StatusMessage}\n\n" +
                    $"{decisions}\n\n" +
                    OutpostStrings.TrEn(
                        $"Kalan Kaynaklar - Batarya: {res.Battery:0.#}, Su: {res.Water:0.#}, O2: {res.Oxygen:0.#}",
                        $"Reserves - Battery: {res.Battery:0.#}, Water: {res.Water:0.#}, O2: {res.Oxygen:0.#}");
                outBtnTxt.text = OutpostStrings.GetRestartButton();
            }
        }

        private static void UpdateBar(Text label, Image fill, string title, double val, double max, Color color)
        {
            label.text = $"{title}: {val:0.#} / {max:0}";
            fill.fillAmount = max > 0 ? Mathf.Clamp01((float)(val / max)) : 0f;
            fill.color = color;
        }

        private static void UpdateTaskButton(MissionState state, string taskId, Text btnText, string baseTitleTr, string baseTitleEn, bool isContinuous, double defaultDuration)
        {
            TaskState task = null;
            for (int i = 0; i < state.Tasks.Count; i++)
            {
                if (state.Tasks[i].Id == taskId) { task = state.Tasks[i]; break; }
            }

            double duration = task != null ? task.RequiredSeconds : defaultDuration;
            string durationSuffix = isContinuous
                ? OutpostStrings.TrEn(" • Sürekli)", " • Cont.)")
                : OutpostStrings.TrEn($" • {duration:0}sn)", $" • {duration:0}s)");

            string baseTitle = OutpostStrings.TrEn(baseTitleTr, baseTitleEn) + durationSuffix;

            if (task == null)
            {
                btnText.text = baseTitle;
                return;
            }

            if (task.IsCompleted)
            {
                btnText.text = $"{baseTitle} " + OutpostStrings.TrEn("[TAMAM]", "[DONE]");
            }
            else if (task.AssignedCrewId != null)
            {
                string crewTag = task.AssignedCrewId;
                for (int i = 0; i < state.Crew.Count; i++)
                {
                    if (state.Crew[i].Id == task.AssignedCrewId)
                    {
                        crewTag = state.Crew[i].Name;
                        break;
                    }
                }
                btnText.text = $"{baseTitle} [{crewTag}]";
            }
            else if (!task.IsContinuous && task.ProgressSeconds > 0)
            {
                btnText.text = $"{baseTitle} ({task.NormalizedProgress * 100:0}%)";
            }
            else
            {
                btnText.text = baseTitle;
            }
        }

        private void UpdateHazardBanner(MissionState state)
        {
            EventState active = null;
            EventState warning = null;
            EventState upcoming = null;
            for (int i = 0; i < state.Events.Count; i++)
            {
                var ev = state.Events[i];
                if (ev.Phase == EventPhase.Active && active == null) active = ev;
                else if (ev.Phase == EventPhase.Warning && warning == null) warning = ev;
                else if (ev.Phase == EventPhase.Upcoming && upcoming == null) upcoming = ev;
            }

            if (active != null)
            {
                currentHazardPhase = EventPhase.Active;
                hazardHeaderTitle.text = OutpostStrings.TrEn("🚨 AKTİF TEHLİKE ALARMI!", "🚨 ACTIVE HAZARD ALERT!");

                double rem = active.SecondsRemaining(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.TrEn($"Bitişe: {OutpostStrings.FormatSimClock(rem)}", $"Ends in: {OutpostStrings.FormatSimClock(rem)}");
                eventCountdownText.color = new Color(1f, 0.95f, 0.40f, 1f);

                double totalDuration = Mathf.Max(1f, (float)(active.EndSeconds - active.StartSeconds));
                eventProgressFill.fillAmount = Mathf.Clamp01((float)(rem / totalDuration));
                eventProgressFill.color = new Color(0.95f, 0.22f, 0.22f, 1f);

                string advice = active.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("⚠️ GÜNEŞ FIRTINASI AKTİF!\nSığınakta kal, radyasyondan korun!", "⚠️ SOLAR STORM ACTIVE!\nKeep all crew sheltered from radiation!")
                    : OutpostStrings.TrEn("🌪️ TOZ FIRTINASI AKTİF!\nGüneş enerjisi düştü, bataryayı koru!", "🌪️ DUST STORM ACTIVE!\nSolar power reduced, save battery!");
                eventText.text = advice;
                if (eventCardText != null) eventCardText.text = advice;
            }
            else if (warning != null)
            {
                currentHazardPhase = EventPhase.Warning;
                hazardHeaderTitle.text = OutpostStrings.TrEn("⚠️ TEHLİKE YAKLAŞIYOR!", "⚠️ HAZARD APPROACHING!");

                double until = warning.SecondsUntilStart(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.FormatSimClock(until);
                eventCountdownText.color = new Color(1f, 0.88f, 0.24f, 1f);

                eventProgressFill.fillAmount = Mathf.Clamp01((float)(until / 60.0));
                eventProgressFill.color = new Color(0.98f, 0.58f, 0.12f, 1f);

                string prompt = warning.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("⚠️ GÜNEŞ FIRTINASI GELİYOR!\nDışarıdaki astronotu hemen içeri çek!", "⚠️ SOLAR STORM INCOMING!\nBring outside crew inside now!")
                    : OutpostStrings.TrEn("🌪️ TOZ FIRTINASI YAKLAŞIYOR!\nGüneş enerjisi düşecek, bataryayı koru!", "🌪️ DUST STORM APPROACHING!\nSolar power will drop, save battery!");
                eventText.text = prompt;
                if (eventCardText != null) eventCardText.text = prompt;
            }
            else if (upcoming != null)
            {
                currentHazardPhase = EventPhase.Upcoming;
                hazardHeaderTitle.text = OutpostStrings.TrEn("📡 UZAY HAVA RADARI", "📡 SPACE WEATHER RADAR");

                double until = upcoming.SecondsUntilStart(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.FormatSimClock(until);
                eventCountdownText.color = new Color(0.78f, 0.90f, 1f, 1f);

                eventProgressFill.fillAmount = Mathf.Clamp01(1f - (float)(until / 120.0));
                eventProgressFill.color = AccentBlue;

                string evName = upcoming.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("Güneş Fırtınası (#1)", "Solar Storm (#1)")
                    : OutpostStrings.TrEn("Bölgesel Toz Fırtınası", "Regional Dust Storm");
                string prompt = OutpostStrings.TrEn(
                    $"Sıradaki: {evName}\n{until:0} saniye sonra başlayacak.",
                    $"Next: {upcoming.Title}\nStarts in {OutpostStrings.FormatSimClock(until)}");
                eventText.text = prompt;
                if (eventCardText != null) eventCardText.text = prompt;
            }
            else
            {
                currentHazardPhase = EventPhase.Upcoming;
                hazardHeaderTitle.text = OutpostStrings.TrEn("📡 UZAY HAVA RADARI", "📡 SPACE WEATHER RADAR");
                eventCountdownText.text = "--:--";
                eventCountdownText.color = Color.white;
                eventProgressFill.fillAmount = 0f;
                string calmText = OutpostStrings.TrEn("Tehlike Yok - Uzay Hava Durumu Sakin", OutpostStrings.NoEventsPending);
                eventText.text = calmText;
                if (eventCardText != null) eventCardText.text = calmText;
            }
        }

        private void UpdateGalleyAndCommsCard(MissionState state)
        {
            if (galleyCommsStatusText == null) return;

            string mealStatus = state.MealPreparedThisSol
                ? OutpostStrings.TrEn("Sıcak Yemek Hazır (1.0x Uyku)", "Meal Prepped (1.0x Sleep)")
                : OutpostStrings.TrEn($"Yemek Pişmedi ({state.CurrentNightSleepFactor:0.00}x Uyku)", $"No Meal Prep ({state.CurrentNightSleepFactor:0.00}x Sleep)");

            galleyCommsStatusText.text =
                OutpostStrings.TrEn($"Yedek Yemek: {state.FoodRationsRemaining} kaldı\nGece Mutfağı: {mealStatus}", $"Reserve Rations: {state.FoodRationsRemaining} left\nNight Galley: {mealStatus}");

            if (state.EarthAdviceStatus == EarthAdviceStatus.NotRequested)
            {
                if (state.ShouldSuggestEarthAdvice)
                {
                    btnEarthAdviceText.text = OutpostStrings.GetEarthAdviceButton(true);
                    btnEarthAdviceBg.color = AccentAmber;
                }
                else
                {
                    btnEarthAdviceText.text = OutpostStrings.GetEarthAdviceButton(false);
                    btnEarthAdviceBg.color = ButtonBg;
                }
                earthAdviceBodyText.text = OutpostStrings.TrEn(
                    "Dünya'dan bir defalık rehberlik (45sn radyo gecikmesi).\nİlk tehlikeden ~30sn önce sormak en iyisidir. Gerçek Mars-Dünya radyo sinyali 3-22 dakika sürer!",
                    "One-time Earth guidance (45s sim relay).\nBest sent ~30s before first hazard; decide locally while waiting. Real Mars radio delay is ~3-22 min by orbit.");
            }
            else if (state.EarthAdviceStatus == EarthAdviceStatus.Pending)
            {
                btnEarthAdviceText.text = OutpostStrings.TrEn(
                    $"Dünya Sinyali Yolda ({state.EarthAdviceRemainingSeconds:0}sn)",
                    $"Earth Relay In Transit ({state.EarthAdviceRemainingSeconds:0}s)");
                btnEarthAdviceBg.color = AccentBlue;
                earthAdviceBodyText.text = OutpostStrings.TrEn(
                    $"Sinyal yolda: {state.EarthAdviceRemainingSeconds:0}sn simülasyon süresi kaldı. Bu sırada yerel güvenlik kararlarını al!",
                    $"Signal in transit: {state.EarthAdviceRemainingSeconds:0}s sim time left. Decide local safety actions now.");
            }
            else
            {
                btnEarthAdviceText.text = OutpostStrings.TrEn("Dünya'dan Yanıt Geldi! ✅", "Earth Advice Received");
                btnEarthAdviceBg.color = AccentGreen;
                earthAdviceBodyText.text = state.EarthAdviceMessage;
            }
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("MarsOutpostCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();

            // 1. TOP BAR (Reserves + Time + Language + Mode + Audio)
            var topBar = CreatePanel("TopBar", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(0, 0), PanelBg);
            clockText = CreateText("ClockText", topBar, new Vector2(20, -10), new Vector2(330, 44), 23, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            speedText = CreateText("SpeedText", topBar, new Vector2(20, -56), new Vector2(200, 38), 21, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);

            CreateReserveMeter(topBar, new Vector2(360, -12), out batteryText, out batteryFill);
            CreateReserveMeter(topBar, new Vector2(620, -12), out waterText, out waterFill);
            CreateReserveMeter(topBar, new Vector2(880, -12), out oxygenText, out oxygenFill);
            CreateReserveMeter(topBar, new Vector2(1140, -12), out researchText, out researchFill);
            reserveNoteText = CreateText("ReserveNote", topBar, new Vector2(360, -68), new Vector2(1030, 32), 15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.90f));

            // Language Toggle Button (TR / EN)
            CreateActionButton(topBar, "BtnLang", new Vector2(1405, -10), new Vector2(155, 42), out btnLangText, () =>
            {
                SetLanguage(!OutpostStrings.IsTurkish);
            }, new Color(0.28f, 0.36f, 0.52f, 1f));
            btnLangText.text = OutpostStrings.GetLangButton();

            // Mode Toggle
            CreateActionButton(topBar, "BtnModeToggle", new Vector2(1575, -10), new Vector2(160, 42), out btnModeText, () =>
            {
                if (runner == null) return;
                if (runner.IsDemoMode) runner.InitializeDefaultMission();
                else runner.InitializeDemoMission();
            });

            // Mute Audio
            CreateActionButton(topBar, "BtnMuteAudio", new Vector2(1745, -10), new Vector2(158, 42), out btnMuteText, () =>
            {
                if (audioController != null) audioController.ToggleMute();
                RefreshAudioControls();
            });

            // Volume Controls
            CreateActionButton(topBar, "BtnVolDown", new Vector2(1575, -58), new Vector2(64, 38), out var vDownTxt, () =>
            {
                if (audioController != null) audioController.SetVolume(audioController.Volume - 0.2f);
                RefreshAudioControls();
            });
            vDownTxt.text = "Vol -";

            volumeText = CreateText("VolumeText", topBar, new Vector2(1646, -58), new Vector2(126, 38), 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            CreateActionButton(topBar, "BtnVolUp", new Vector2(1778, -58), new Vector2(64, 38), out var vUpTxt, () =>
            {
                if (audioController != null) audioController.SetVolume(audioController.Volume + 0.2f);
                RefreshAudioControls();
            });
            vUpTxt.text = "Vol +";
            RefreshAudioControls();

            // 2. LEFT PANEL (3 Crew Cards + Galley & Earth Relay)
            var leftPanel = CreatePanel("LeftCrewPanel", root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 20), new Vector2(340, -120), PanelBg);
            crewHeaderText = CreateText("CrewHeader", leftPanel, new Vector2(16, -12), new Vector2(308, 36), 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            crewHeaderText.text = OutpostStrings.TrEn("ASTRONOTLAR (1-3)", "CREW (Keys 1-3)");

            for (int i = 0; i < 3; i++)
            {
                string cid = $"crew-{i + 1}";
                var cardGo = CreatePanel($"CrewCard_{i + 1}", leftPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -56 - i * 165 - 150), new Vector2(-12, -56 - i * 165), CardBg);
                var btn = cardGo.gameObject.AddComponent<Button>();
                string capturedId = cid;
                btn.onClick.AddListener(() => SelectCrew(capturedId));

                var card = new CrewCardUi
                {
                    CrewId = cid,
                    Background = cardGo.GetComponent<Image>(),
                    NameText = CreateText("Name", cardGo, new Vector2(12, -10), new Vector2(290, 32), 19, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white),
                    StatusText = CreateText("Status", cardGo, new Vector2(12, -46), new Vector2(290, 30), 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.86f, 0.90f, 0.95f)),
                    StaminaText = CreateText("Stamina", cardGo, new Vector2(12, -82), new Vector2(290, 26), 15, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white)
                };
                var barBg = CreatePanel("BarBg", cardGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -132), new Vector2(-12, -112), new Color(0.08f, 0.09f, 0.11f, 1f));
                card.StaminaFill = CreateFillImage("BarFill", barBg, AccentGreen);
                crewCards.Add(card);
            }

            // 2.5 Galley & Earth Relay Card (Bottom of Left Panel)
            var commsCard = CreatePanel("GalleyCommsCard", leftPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -920), new Vector2(-12, -552), CardBg);
            galleyCommsStatusText = CreateText("GalleyStatus", commsCard, new Vector2(12, -10), new Vector2(292, 54), 15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            btnEarthAdvice = CreateActionButton(commsCard, "BtnEarthAdvice", new Vector2(12, -68), new Vector2(292, 42), out btnEarthAdviceText, () => runner.RequestEarthAdvice());
            btnEarthAdviceBg = btnEarthAdvice.GetComponent<Image>();
            earthAdviceBodyText = CreateText("EarthAdviceBody", commsCard, new Vector2(12, -118), new Vector2(292, 240), 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.84f, 0.90f, 0.96f));

            // 3. RIGHT PANEL (Decluttered, Colorful & Intuitive for Ages 9-12)
            var rightPanel = CreatePanel("RightControlPanel", root, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-410, 20), new Vector2(0, -120), PanelBg);

            // 3.1 Big Flashing Hazard & Event Alert Banner (Top of Right Panel)
            var bannerPanel = CreatePanel("HazardAlertBanner", rightPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -150), new Vector2(-16, -12), CardBg);
            hazardBannerRect = bannerPanel;
            hazardBannerBg = bannerPanel.GetComponent<Image>();

            // Backward compatibility alias for EventCard
            var evCardAlias = new GameObject("EventCard");
            var evCardAliasRt = evCardAlias.AddComponent<RectTransform>();
            evCardAliasRt.SetParent(bannerPanel, false);
            eventCardBg = hazardBannerBg;

            hazardHeaderTitle = CreateText("HazardHeaderTitle", bannerPanel, new Vector2(10, -6), new Vector2(358, 24), 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            hazardHeaderTitle.text = OutpostStrings.TrEn("📡 UZAY HAVA RADARI", "📡 SPACE WEATHER RADAR");

            eventCountdownText = CreateText("EventCountdownText", bannerPanel, new Vector2(10, -30), new Vector2(358, 40), 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            eventCountdownText.text = "--:--";

            var progBg = CreatePanel("EventProgressBar", bannerPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -80), new Vector2(-16, -72), new Color(0.06f, 0.08f, 0.12f, 1f));
            eventProgressFill = CreateFillImage("EventProgressFill", progBg, AccentBlue);

            eventText = CreateText("EventText", bannerPanel, new Vector2(10, -84), new Vector2(358, 48), 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            eventText.text = OutpostStrings.TrEn("Tehlike Yok - Uzay Hava Durumu Sakin", OutpostStrings.NoEventsPending);
            eventCardText = eventText;

            // 3.2 Selected Astronaut Header Card
            var selCard = CreatePanel("SelectedCrewCard", rightPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -274), new Vector2(-16, -160), CardBg);

            var visorGo = CreatePanel("SelectedVisor", selCard, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -26), new Vector2(26, -10), CrewVisors[0]);
            selectedVisorBadge = visorGo.GetComponent<Image>();

            selectedNameText = CreateText("SelectedName", selCard, new Vector2(32, -6), new Vector2(190, 28), 19, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);

            var pillGo = CreatePanel("LocationPill", selCard, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-152, -32), new Vector2(-10, -6), new Color(0.12f, 0.36f, 0.24f, 0.95f));
            selectedLocationPillBg = pillGo.GetComponent<Image>();
            selectedLocationText = CreateText("LocationText", pillGo, new Vector2(4, -2), new Vector2(134, 22), 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.42f, 0.94f, 0.68f, 1f));

            selectedStatusText = CreateText("SelectedStatus", selCard, new Vector2(10, -36), new Vector2(358, 22), 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.86f, 0.90f, 0.96f));

            selectedStaminaText = CreateText("SelectedStamina", selCard, new Vector2(10, -58), new Vector2(180, 20), 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            selectedRiskText = CreateText("SelectedRisk", selCard, new Vector2(190, -58), new Vector2(178, 20), 13, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);

            var selBarBg = CreatePanel("SelStaminaBarBg", selCard, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -100), new Vector2(-10, -84), new Color(0.06f, 0.08f, 0.12f, 1f));
            selectedStaminaFill = CreateFillImage("SelStaminaFill", selBarBg, AccentGreen);

            // Legacy elements kept for backward compatibility
            selectedStatsText = CreateText("SelectedStats", selCard, new Vector2(0, 0), Vector2.zero, 1, FontStyle.Normal, TextAnchor.MiddleLeft, Color.clear);
            selectedRiskNoteText = CreateText("SelectedRiskNote", selCard, new Vector2(0, 0), Vector2.zero, 1, FontStyle.Normal, TextAnchor.MiddleLeft, Color.clear);
            selectedStatsText.gameObject.SetActive(false);
            selectedRiskNoteText.gameObject.SetActive(false);

            // 3.3 4 Colorful, Icon-Rich Kid-Friendly Task Buttons
            float y = -282;
            btnPanelMaint = CreateActionButton(rightPanel, "BtnPanelMaint", new Vector2(16, y), new Vector2(378, 44), out btnPanelMaintText, () => runner.AssignTask(selectedCrewId, "panel-maintenance"), ColorPanelMaint);
            btnPanelMaintBg = btnPanelMaint.GetComponent<Image>();
            y -= 50;

            btnSystemMaint = CreateActionButton(rightPanel, "BtnSystemMaint", new Vector2(16, y), new Vector2(378, 44), out btnSystemMaintText, () => runner.AssignTask(selectedCrewId, "system-maintenance"), ColorSystemMaint);
            btnSystemMaintBg = btnSystemMaint.GetComponent<Image>();
            y -= 50;

            btnMealPrep = CreateActionButton(rightPanel, "BtnMealPrep", new Vector2(16, y), new Vector2(378, 44), out btnMealPrepText, () => runner.AssignTask(selectedCrewId, "meal-prep"), ColorMealPrep);
            btnMealPrepBg = btnMealPrep.GetComponent<Image>();
            y -= 50;

            btnResearchTask = CreateActionButton(rightPanel, "BtnResearch", new Vector2(16, y), new Vector2(378, 44), out btnResearchTaskText, () => runner.AssignTask(selectedCrewId, "research"), ColorResearch);
            btnResearchTaskBg = btnResearchTask.GetComponent<Image>();

            // Alias GameObject "BtnResearchTask" for PlayMode test backward compatibility
            var resAlias = new GameObject("BtnResearchTask");
            var resAliasRt = resAlias.AddComponent<RectTransform>();
            resAliasRt.SetParent(btnResearchTask.transform, false);
            resAliasRt.anchorMin = Vector2.zero;
            resAliasRt.anchorMax = Vector2.one;
            resAliasRt.offsetMin = Vector2.zero;
            resAliasRt.offsetMax = Vector2.zero;
            var resAliasBtn = resAlias.AddComponent<Button>();
            resAliasBtn.onClick.AddListener(() => btnResearchTask.onClick.Invoke());
            y -= 52;

            // 3.4 Smart Contextual Safety & Rest Row
            btnReturn = CreateActionButton(rightPanel, "BtnReturn", new Vector2(16, y), new Vector2(184, 42), out btnReturnText, () => runner.ReturnToShelter(selectedCrewId));
            btnReturnBg = btnReturn.GetComponent<Image>();
            btnReturnText.text = OutpostStrings.GetReturnButton();

            btnRest = CreateActionButton(rightPanel, "BtnRest", new Vector2(210, y), new Vector2(184, 42), out btnRestText, () => runner.Rest(selectedCrewId));
            btnRestBg = btnRest.GetComponent<Image>();
            btnRestText.text = OutpostStrings.GetRestButton();
            y -= 48;

            btnCancelTask = CreateActionButton(rightPanel, "BtnCancel", new Vector2(16, y), new Vector2(184, 40), out btnCancelTaskText, () => runner.CancelTask(selectedCrewId));
            btnCancelTaskBg = btnCancelTask.GetComponent<Image>();
            btnCancelTaskText.text = OutpostStrings.GetCancelTaskButton();

            // Alias GameObject "BtnCancelTask" for backward compatibility
            var cancelAlias = new GameObject("BtnCancelTask");
            var cancelAliasRt = cancelAlias.AddComponent<RectTransform>();
            cancelAliasRt.SetParent(btnCancelTask.transform, false);
            cancelAliasRt.anchorMin = Vector2.zero;
            cancelAliasRt.anchorMax = Vector2.one;
            cancelAliasRt.offsetMin = Vector2.zero;
            cancelAliasRt.offsetMax = Vector2.zero;
            var cancelAliasBtn = cancelAlias.AddComponent<Button>();
            cancelAliasBtn.onClick.AddListener(() => btnCancelTask.onClick.Invoke());

            btnResearchToggle = CreateActionButton(rightPanel, "BtnResearchToggle", new Vector2(210, y), new Vector2(184, 40), out btnResearchToggleText, () =>
            {
                if (runner != null && runner.State != null)
                    runner.SetResearchEnabled(!runner.State.Resources.ResearchEnabled);
            });
            btnResearchToggleBg = btnResearchToggle.GetComponent<Image>();

            // 3.5 Global Controls at Bottom of Right Panel
            btnPause = CreateActionButton(rightPanel, "BtnPause", new Vector2(16, -770), new Vector2(184, 48), out btnPauseText, () => runner.TogglePause());
            btnSleep = CreateActionButton(rightPanel, "BtnSleep", new Vector2(210, -770), new Vector2(184, 48), out btnSleepText, () =>
            {
                if (runner != null && runner.State != null)
                {
                    if (runner.State.IsSleeping) runner.CancelSleep();
                    else runner.BeginSleep();
                }
            });
            btnSleepBg = btnSleep.GetComponent<Image>();

            btnRestart = CreateActionButton(rightPanel, "BtnRestart", new Vector2(16, -826), new Vector2(184, 46), out btnRestartText, () => runner.RestartMission());
            btnRestartText.text = OutpostStrings.GetRestartButton();

            btnSources = CreateActionButton(rightPanel, "BtnSources", new Vector2(210, -826), new Vector2(184, 46), out btnSourcesText, () => ToggleSourcesModal(true));
            btnSourcesText.text = OutpostStrings.GetSourcesButton();

            // 4. Feedback Banner (bottom center)
            var fbRect = CreatePanel("FeedbackBanner", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-340, 24), new Vector2(340, 76), new Color(0.58f, 0.18f, 0.16f, 0.95f));
            feedbackPanel = fbRect.gameObject;
            feedbackText = CreateText("FeedbackText", fbRect, new Vector2(16, -6), new Vector2(648, 40), 19, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            feedbackPanel.SetActive(false);

            // 5. Tutorial Guide Strip (above feedback banner)
            var tutRect = CreatePanel("TutorialGuide", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-440, 86), new Vector2(440, 152), new Color(0.11f, 0.18f, 0.28f, 0.94f));
            tutorialPanel = tutRect.gameObject;
            tutorialText = CreateText("TutorialText", tutRect, new Vector2(14, -8), new Vector2(710, 50), 16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            CreateActionButton(tutRect, "BtnHideTutorial", new Vector2(736, -11), new Vector2(130, 44), out hideTutorialText, () =>
            {
                tutorialDismissed = true;
                tutorialPanel.SetActive(false);
            });
            hideTutorialText.text = OutpostStrings.GetSkipTutorialButton();

            // 6. Modals
            BuildModals(root);
        }

        private void BuildModals(RectTransform root)
        {
            // Sol Report Modal
            var solRect = CreatePanel("SolReportModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -240), new Vector2(300, 240), new Color(0.08f, 0.10f, 0.14f, 0.98f));
            solReportModal = solRect.gameObject;
            solReportTitleText = CreateText("SolTitle", solRect, new Vector2(24, -18), new Vector2(552, 42), 26, FontStyle.Bold, TextAnchor.MiddleCenter, AccentBlue);
            solReportTitleText.text = OutpostStrings.TrEn("GÜN SONU RAPORU", "SOL REPORT");
            solReportBody = CreateText("SolBody", solRect, new Vector2(28, -70), new Vector2(544, 320), 19, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            CreateActionButton(solRect, "BtnCloseSolReport", new Vector2(180, -410), new Vector2(240, 50), out solBtnTxt, () => runner.AcknowledgeSolReport());
            solBtnTxt.text = OutpostStrings.GetContinueButton();
            solReportModal.SetActive(false);

            // Critical Alert Modal
            var alertRect = CreatePanel("CriticalAlertModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -140), new Vector2(280, 140), new Color(0.24f, 0.08f, 0.08f, 0.98f));
            alertModal = alertRect.gameObject;
            alertTitleText = CreateText("AlertTitle", alertRect, new Vector2(24, -18), new Vector2(512, 40), 26, FontStyle.Bold, TextAnchor.MiddleCenter, AccentRed);
            alertTitleText.text = OutpostStrings.TrEn("🚨 KRİTİK UYARI", "CRITICAL ALERT");
            alertBody = CreateText("AlertBody", alertRect, new Vector2(24, -74), new Vector2(512, 110), 19, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            CreateActionButton(alertRect, "BtnAckAlert", new Vector2(160, -206), new Vector2(240, 50), out alertBtnTxt, () => runner.AcknowledgeCriticalAlert());
            alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            alertModal.SetActive(false);

            // Outcome Modal
            var outRect = CreatePanel("OutcomeModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -180), new Vector2(300, 180), new Color(0.08f, 0.10f, 0.14f, 0.98f));
            outcomeModal = outRect.gameObject;
            outcomeTitle = CreateText("OutcomeTitle", outRect, new Vector2(24, -20), new Vector2(552, 46), 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            outcomeBody = CreateText("OutcomeBody", outRect, new Vector2(28, -72), new Vector2(544, 210), 17, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            CreateActionButton(outRect, "BtnOutcomeRestart", new Vector2(180, -284), new Vector2(240, 52), out outBtnTxt, () => runner.RestartMission());
            outBtnTxt.text = OutpostStrings.GetRestartButton();
            outcomeModal.SetActive(false);

            // Science & NASA Sources Modal
            var srcRect = CreatePanel("SourcesModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -300), new Vector2(420, 300), new Color(0.07f, 0.09f, 0.13f, 0.98f));
            sourcesModal = srcRect.gameObject;
            sourcesTitle = CreateText("SourcesTitle", srcRect, new Vector2(24, -16), new Vector2(792, 40), 24, FontStyle.Bold, TextAnchor.MiddleCenter, AccentBlue);
            sourcesTitle.text = OutpostStrings.TrEn("NASA BİLİM VE ARŞİV KAYNAKLARI", "NASA SCIENCE & ARCHIVE SOURCES");
            sourcesBody = CreateText("SourcesBody", srcRect, new Vector2(24, -64), new Vector2(792, 462), 14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            sourcesBody.text = BuildSourcesText();
            CreateActionButton(srcRect, "BtnCloseSources", new Vector2(300, -536), new Vector2(240, 48), out sourcesCloseText, () => ToggleSourcesModal(false));
            sourcesCloseText.text = OutpostStrings.TrEn("Kapat", "Close");
            sourcesModal.SetActive(false);
        }

        private void ToggleSourcesModal(bool open)
        {
            if (sourcesModal == null) return;
            sourcesModal.SetActive(open);
            if (open && runner != null && runner.Stepper != null && !runner.Stepper.UserPaused)
                runner.SetPaused(true);
        }

        private static string BuildSourcesText()
        {
            var lines = new List<string> { NasaScienceData.EducationalDisclaimer, "" };
            for (int i = 0; i < NasaScienceData.Entries.Count; i++)
            {
                var e = NasaScienceData.Entries[i];
                lines.Add($"[{e.Category}] {e.Title}");
                lines.Add($"  Archive ID: {e.ArchiveRecordId} ({e.ArchiveTimestampUtc})");
                lines.Add($"  Instrument/System: {e.InstrumentOrSystem}");
                lines.Add($"  Game Effect: {e.GameMapping}");
                lines.Add($"  Model Note: {e.ModelDisclaimer}");
                lines.Add($"  URL: {e.Url}");
                lines.Add("");
            }
            return string.Join("\n", lines);
        }

        private void RefreshAudioControls()
        {
            if (btnMuteText != null)
            {
                bool muted = audioController != null && audioController.IsMuted;
                btnMuteText.text = muted
                    ? OutpostStrings.TrEn("Ses: KAPALI", "Sound: MUTED")
                    : OutpostStrings.TrEn("Ses: AÇIK", "Sound: ON");
            }
            if (volumeText != null)
            {
                int pct = audioController != null ? Mathf.RoundToInt(audioController.Volume * 100f) : 60;
                volumeText.text = OutpostStrings.TrEn($"Ses: {pct}%", $"Vol: {pct}%");
            }
        }

        private void CreateReserveMeter(RectTransform parent, Vector2 topLeft, out Text label, out Image fill)
        {
            var holder = CreatePanel("Meter", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, topLeft.y - 54), new Vector2(topLeft.x + 250, topLeft.y), Color.clear);
            label = CreateText("Label", holder, new Vector2(0, 0), new Vector2(250, 26), 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            var barBg = CreatePanel("Bg", holder, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, -30), new Color(0.05f, 0.06f, 0.08f, 1f));
            fill = CreateFillImage("Fill", barBg, AccentGreen);
        }

        private static RectTransform CreatePanel(string objName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(objName);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        private Image CreateFillImage(string objName, RectTransform parent, Color color)
        {
            var go = new GameObject(objName);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = whiteSprite;
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            return img;
        }

        private Text CreateText(string objName, RectTransform parent, Vector2 topLeft, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            var go = new GameObject(objName);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.font = uiFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            return txt;
        }

        private Button CreateActionButton(RectTransform parent, string objName, Vector2 topLeft, Vector2 size, out Text label, Action onClick, Color? bgColor = null)
        {
            var go = new GameObject(objName);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bgColor ?? ButtonBg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6, 2);
            txtRt.offsetMax = new Vector2(-6, -2);
            label = txtGo.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return btn;
        }
    }
}
