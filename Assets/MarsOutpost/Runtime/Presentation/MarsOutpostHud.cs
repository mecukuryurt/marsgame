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
    /// Sleek, compact, translucent sci-fi HUD controller (>80% open 3D center viewport).
    /// Built on a 1920x1080 reference CanvasScaler with 9-slice sliced sci-fi titanium/glass sprites.
    /// Reads MissionState and dispatches commands to SimulationRunner with zero per-frame object allocations.
    /// Features:
    /// - Slim Top Telemetry Strip (66px) with clock, temp, compact meters, and 3D camera/dome utilities.
    /// - Compact Left Crew Panel (224px) with floating astronaut capsules and concise galley footer.
    /// - Top-Right Tactical Radar / Hazard Alert Banner (278px) pulsing on storm events.
    /// - Bottom-Center Horizontal Command Dock (76px) containing all selected-crew actions and base toggles.
    /// - Center 3D viewport remains 100% open with zero giant side walls.
    /// </summary>
    public sealed class MarsOutpostHud : MonoBehaviour
    {
        [SerializeField] private SimulationRunner runner;
        [SerializeField] private MarsOutpostWorldView worldView;
        [SerializeField] private MarsOutpostAudio audioController;
        [SerializeField] private MarsOutpostCameraController cameraController;

        private Font uiFont;
        private Sprite whiteSprite;
        private Sprite panelFrameSprite;
        private Sprite alertFrameSprite;
        private Sprite buttonPillSprite;
        private Sprite barBgSprite;
        private Sprite barFillSprite;

        // Dock 2D PNG Icons
        private Sprite iconSolar;
        private Sprite iconO2;
        private Sprite iconFood;
        private Sprite iconLab;
        private Sprite iconHome;
        private Sprite iconRest;
        private Sprite iconStop;
        private Sprite iconSleep;
        private Sprite iconTech;

        private string selectedCrewId = "crew-1";
        private float feedbackTimer;
        private bool isScreenshotMode;

        // Center Pause Overlay
        private GameObject pauseOverlayPanel;
        private Text pauseTitleText;
        private Text pauseSubText;
        private Button btnResumeOverlay;
        private Text btnResumeOverlayText;

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

        // Top Bar Utility Buttons
        private Button btnCamReset;
        private Text btnCamResetText;
        private Button btnDomeRoof;
        private Text btnDomeRoofText;
        private Text btnLangText;
        private Text btnModeText;
        private Button btnPause;
        private Text btnPauseText;
        private Button btnSources;
        private Text btnSourcesText;
        private Text btnMuteText;
        private Text volumeText;
        private Text cameraHintText;

        // Left Crew Cards
        private Text crewHeaderText;
        private sealed class CrewCardUi
        {
            public string CrewId;
            public Image Background;
            public Image VisorBadge;
            public Text NameText;
            public Image LocationPillBg;
            public Text LocationText;
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

        // Top-Right Tactical Radar / Event Banner
        private RectTransform hazardBannerRect;
        private Image hazardBannerBg;
        private Text hazardHeaderTitle;
        private Text eventCountdownText;
        private Image eventProgressFill;
        private Text eventText;
        private Image eventCardBg;
        private Text eventCardText;
        private EventPhase currentHazardPhase = EventPhase.Upcoming;

        // Bottom Command Dock: Selected Astronaut Strip
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

        // Bottom Command Dock: Selected Crew Actions
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

        private Button btnReturn;
        private Image btnReturnBg;
        private Text btnReturnText;

        private Button btnRest;
        private Image btnRestBg;
        private Text btnRestText;

        private Button btnCancelTask;
        private Image btnCancelTaskBg;
        private Text btnCancelTaskText;

        // Bottom Command Dock: Base Toggles
        private Button btnSleep;
        private Image btnSleepBg;
        private Text btnSleepText;

        private Button btnResearchToggle;
        private Image btnResearchToggleBg;
        private Text btnResearchToggleText;

        private Button btnRestart;
        private Text btnRestartText;

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
        private static readonly Color PanelBg = new Color(0.04f, 0.07f, 0.12f, 0.90f);
        private static readonly Color CardBg = new Color(0.08f, 0.12f, 0.18f, 0.92f);
        private static readonly Color SelectedBg = new Color(0.16f, 0.28f, 0.44f, 0.96f);
        private static readonly Color ButtonBg = new Color(0.12f, 0.18f, 0.28f, 0.95f);

        // Accents
        private static readonly Color AccentGreen = new Color(0.26f, 0.76f, 0.48f, 1f);
        private static readonly Color AccentAmber = new Color(0.95f, 0.64f, 0.18f, 1f);
        private static readonly Color AccentRed = new Color(0.88f, 0.22f, 0.22f, 1f);
        private static readonly Color AccentBlue = new Color(0.24f, 0.62f, 0.92f, 1f);
        private static readonly Color AccentCyan = new Color(0.22f, 0.74f, 0.95f, 1f);

        // Task Button Base Colors
        private static readonly Color ColorPanelMaint = new Color(0.16f, 0.32f, 0.52f, 1f);
        private static readonly Color ColorSystemMaint = new Color(0.12f, 0.40f, 0.48f, 1f);
        private static readonly Color ColorMealPrep = new Color(0.14f, 0.44f, 0.30f, 1f);
        private static readonly Color ColorResearch = new Color(0.36f, 0.22f, 0.54f, 1f);

        // Crew Visor Accents
        private static readonly Color[] CrewVisors =
        {
            new Color(0.96f, 0.72f, 0.22f, 1f), // Gold (Astro 1)
            new Color(0.28f, 0.80f, 0.96f, 1f), // Cyan (Astro 2)
            new Color(0.42f, 0.88f, 0.54f, 1f)  // Emerald (Astro 3)
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

            // Load sleek 9-slice sci-fi sprites with procedural fallback
            panelFrameSprite = LoadSlicedSprite("UI/hud_panel_frame", new Vector4(18, 18, 18, 18), new Color(0.04f, 0.07f, 0.12f, 0.92f));
            alertFrameSprite = LoadSlicedSprite("UI/hud_alert_frame", new Vector4(18, 18, 18, 18), new Color(0.12f, 0.05f, 0.07f, 0.92f));
            buttonPillSprite = LoadSlicedSprite("UI/hud_button_pill", new Vector4(16, 16, 16, 16), new Color(0.12f, 0.18f, 0.28f, 0.95f));
            barBgSprite = LoadSlicedSprite("UI/hud_bar_bg", new Vector4(6, 6, 6, 6), new Color(0.03f, 0.05f, 0.07f, 0.98f));
            barFillSprite = LoadSlicedSprite("UI/hud_bar_fill", new Vector4(4, 4, 4, 4), Color.white);

            iconSolar = LoadIconSprite("UI/icons/icon_solar");
            iconO2 = LoadIconSprite("UI/icons/icon_o2");
            iconFood = LoadIconSprite("UI/icons/icon_food");
            iconLab = LoadIconSprite("UI/icons/icon_lab");
            iconHome = LoadIconSprite("UI/icons/icon_home");
            iconRest = LoadIconSprite("UI/icons/icon_rest");
            iconStop = LoadIconSprite("UI/icons/icon_stop");
            iconSleep = LoadIconSprite("UI/icons/icon_sleep");
            iconTech = LoadIconSprite("UI/icons/icon_tech");

            if (runner == null) runner = GetComponent<SimulationRunner>();
            if (worldView == null) worldView = GetComponent<MarsOutpostWorldView>();
            if (audioController == null) audioController = GetComponent<MarsOutpostAudio>();
            if (cameraController == null) cameraController = FindFirstObjectByType<MarsOutpostCameraController>();

            BuildCanvas();
        }

        private Sprite LoadSlicedSprite(string resourcePath, Vector4 border, Color fallbackColor)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            }

            var fallbackTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = fallbackColor;
            fallbackTex.SetPixels(pixels);
            fallbackTex.Apply();
            return Sprite.Create(fallbackTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite LoadIconSprite(string resourcePath)
        {
            var sp = Resources.Load<Sprite>(resourcePath);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void Start()
        {
            if (worldView != null)
                worldView.SetLanguage(OutpostStrings.IsTurkish);

            if (cameraController == null)
                cameraController = FindFirstObjectByType<MarsOutpostCameraController>();

            UpdateDomeRoofButtonVisual();

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                const string prefix = "--capture-screenshot=";
                if (args[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    isScreenshotMode = true;
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
                worldView.DomeRoofChanged += OnDomeRoofChanged;
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
                worldView.DomeRoofChanged -= OnDomeRoofChanged;
            }
        }

        private void OnDomeRoofChanged(bool visible)
        {
            UpdateDomeRoofButtonVisual();
        }

        private void UpdateDomeRoofButtonVisual()
        {
            if (btnDomeRoofText == null) return;
            bool roofVisible = worldView != null && worldView.IsDomeRoofVisible;
            btnDomeRoofText.text = OutpostStrings.GetDomeRoofButton(roofVisible);
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
            UpdatePauseOverlay(runner != null ? runner.State : null);
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
                hazardBannerBg.color = PanelBg;
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
            if (kb.rKey.wasPressedThisFrame)
            {
                if (cameraController == null) cameraController = FindFirstObjectByType<MarsOutpostCameraController>();
                if (cameraController != null) cameraController.ResetCameraView();
            }
            if (kb.tKey.wasPressedThisFrame && worldView != null)
            {
                worldView.ToggleDomeRoof();
                UpdateDomeRoofButtonVisual();
            }
        }

        private void HandleWorldCrewSelected(string crewId)
        {
            selectedCrewId = crewId;
            if (tutorialStep == 1) tutorialStep = 2;
            if (runner != null && runner.State != null)
                Refresh(runner.State);
        }

        public void SelectCrew(string crewId)
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
            if (btnCamResetText != null)
                btnCamResetText.text = OutpostStrings.GetCameraResetButton();
            if (cameraHintText != null)
                cameraHintText.text = OutpostStrings.GetCameraNavigationHint();
            UpdateDomeRoofButtonVisual();

            if (sourcesTitle != null)
                sourcesTitle.text = OutpostStrings.TrEn("NASA BİLİM VE ARŞİV KAYNAKLARI", "NASA SCIENCE & ARCHIVE SOURCES");
            if (sourcesCloseText != null)
                sourcesCloseText.text = OutpostStrings.TrEn("Kapat", "Close");
            if (solReportTitleText != null)
                solReportTitleText.text = OutpostStrings.TrEn("GÜN SONU RAPORU", "SOL REPORT");
            if (solBtnTxt != null)
                solBtnTxt.text = OutpostStrings.GetContinueButton();
            if (alertTitleText != null)
                alertTitleText.text = OutpostStrings.TrEn("[!] KRİTİK UYARI", "CRITICAL ALERT");
            if (alertBtnTxt != null)
                alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            if (outBtnTxt != null)
                outBtnTxt.text = OutpostStrings.GetRestartButton();
            if (hideTutorialText != null)
                hideTutorialText.text = OutpostStrings.GetSkipTutorialButton();

            if (pauseTitleText != null)
                pauseTitleText.text = OutpostStrings.TrEn("[!] OYUN DURAKLATILDI", "[!] GAME PAUSED");
            if (pauseSubText != null)
                pauseSubText.text = OutpostStrings.TrEn("Devam etmek için butona tıkla veya Space tuşuna bas", "Click button or press Space to resume");
            if (btnResumeOverlayText != null)
                btnResumeOverlayText.text = OutpostStrings.TrEn("► DEVAM ET (Space / Tıkla)", "► RESUME (Space / Click)");

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

        public void AssignOrReplaceTask(string taskId)
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

            if (selected != null && selected.Status == CrewStatus.Working && selected.ActiveTaskId != null && selected.ActiveTaskId != taskId)
            {
                runner.CancelTask(selectedCrewId);
            }

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
            string tempStr = state.IsNight ? "-95C" : "-63C";
            clockText.text = $"Sol {state.SolNumber} | {phaseStr} {OutpostStrings.FormatSimClock(state.SecondsIntoSol)} | Mars: {tempStr}";

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
            string o2Status = res.Oxygen < 20 ? OutpostStrings.TrEn("DÜŞÜK!", "LOW!") : OutpostStrings.TrEn("İYİ", "OK");
            UpdateBar(oxygenText, oxygenFill, $"{OutpostStrings.GetOxygenLabel()} ({o2Status})", res.Oxygen, 100, res.Oxygen < 20 ? AccentRed : AccentGreen);
            UpdateBar(researchText, researchFill, OutpostStrings.GetResearchLabel(), res.ResearchProgress, researchGoal, new Color(0.68f, 0.52f, 0.96f));

            if (btnLangText != null)
                btnLangText.text = OutpostStrings.GetLangButton();

            if (btnModeText != null && runner != null)
                btnModeText.text = runner.IsDemoMode ? OutpostStrings.TrEn("90sn Demo", "90s Demo") : OutpostStrings.TrEn("2-Sol Standart", "2-Sol Std");

            if (btnCamResetText != null)
                btnCamResetText.text = OutpostStrings.GetCameraResetButton();

            UpdateDomeRoofButtonVisual();
            RefreshAudioControls();

            string solarNote = state.IsNight
                ? OutpostStrings.TrEn("Gece: Güneş Yok (Batarya)", "Night: 0 Solar (Battery)")
                : OutpostStrings.TrEn($"Güneş: %{(1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0}", $"Solar: {(1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0}%");
            string dustNote = res.AtmosphericDustLoss > 0
                ? OutpostStrings.TrEn($" | Toz -%{(res.AtmosphericDustLoss * 100):0}", $" | Dust -{(res.AtmosphericDustLoss * 100):0}%")
                : "";
            string researchNote = res.ResearchEnabled
                ? OutpostStrings.TrEn($" | Lab %{(res.LastResearchRateFactor * 100):0}", $" | Lab {(res.LastResearchRateFactor * 100):0}%")
                : OutpostStrings.TrEn(" | Lab KAPALI", " | Lab OFF");
            string modeTag = runner != null && runner.IsDemoMode ? " | Demo" : " | 2-Sol";
            reserveNoteText.text = $"{solarNote}{dustNote}{researchNote}{modeTag}";

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

                bool isInside = c.Location == CrewLocation.Inside;
                card.LocationText.text = isInside ? OutpostStrings.TrEn("İÇERİDE", "INSIDE") : OutpostStrings.TrEn("DIŞARIDA", "OUTSIDE");
                card.LocationPillBg.color = isInside ? new Color(0.12f, 0.36f, 0.22f, 0.95f) : new Color(0.48f, 0.22f, 0.10f, 0.95f);
                card.LocationText.color = isInside ? new Color(0.42f, 0.94f, 0.68f, 1f) : new Color(0.98f, 0.74f, 0.24f, 1f);

                card.NameText.text = c.Name;
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

            // Selected Crew Info Strip in Command Dock
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
                selectedStaminaText.text = OutpostStrings.TrEn($"Enerji: {selected.Stamina:0.#}/100", $"Stamina: {selected.Stamina:0.#}/100");
                selectedStaminaFill.fillAmount = Mathf.Clamp01((float)(selected.Stamina / 100.0));
                selectedStaminaFill.color = selected.Stamina < 20 ? AccentRed : (selected.Stamina < 50 ? AccentAmber : AccentGreen);

                selectedRiskText.text = OutpostStrings.TrEn($"Kalkan/Risk: {selected.RiskScore:0.#}/100", $"{OutpostStrings.RiskLabel}: {selected.RiskScore:0.#}/100");
                selectedRiskText.color = selected.RiskScore >= 60 ? AccentRed : (selected.RiskScore >= 25 ? AccentAmber : new Color(0.82f, 0.90f, 0.96f));

                if (selectedStatsText != null)
                    selectedStatsText.text = $"{selectedLocationText.text} | {selectedStatusText.text}";
                if (selectedRiskNoteText != null)
                    selectedRiskNoteText.text = OutpostStrings.GetRiskExplanation();
            }

            // Selected-crew action capsules (clean icon + short Turkish/English names)
            UpdateTaskButton(state, "panel-maintenance", btnPanelMaintText, "Solar", "Solar", false, 60);
            UpdateTaskButton(state, "system-maintenance", btnSystemMaintText, "O2 & Su", "O2 & Water", false, 45);
            UpdateTaskButton(state, "meal-prep", btnMealPrepText, "Mutfak", "Kitchen", false, 30);
            UpdateTaskButton(state, "research", btnResearchTaskText, "Bilim", "Science", true, 0);

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
                    btnSleepBg.color = new Color(0.24f, 0.32f, 0.72f, 1f);
                }
                else
                {
                    btnSleepBg.color = ButtonBg;
                }
            }

            if (btnRestartText != null) btnRestartText.text = OutpostStrings.GetRestartButton();
            if (btnSourcesText != null) btnSourcesText.text = OutpostStrings.GetSourcesButton();

            // Top-Right Hazard Banner
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
                alertTitleText.text = OutpostStrings.TrEn("[!] KRİTİK UYARI", "CRITICAL ALERT");
                alertBody.text = state.ActiveAlertMessage;
                alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            }

            if (outcomeModal.activeSelf != showOutcome) outcomeModal.SetActive(showOutcome);
            if (showOutcome)
            {
                outcomeTitle.text = state.Outcome == MissionOutcome.Succeeded
                    ? OutpostStrings.TrEn("★ GÖREV BAŞARIYLA TAMAMLANDI! ★", "★ MISSION SUCCEEDED ★")
                    : OutpostStrings.TrEn("[!] GÖREV SONA ERDİ", "[!] MISSION ENDED");
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

            UpdatePauseOverlay(state);
        }

        private static void UpdateBar(Text label, Image fill, string title, double val, double max, Color color)
        {
            label.text = $"{title}: {val:0.#}/{max:0}";
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
                ? OutpostStrings.TrEn(" (Sür)", " (Cont)")
                : OutpostStrings.TrEn($" ({duration:0}s)", $" ({duration:0}s)");

            string baseTitle = OutpostStrings.TrEn(baseTitleTr, baseTitleEn) + durationSuffix;

            if (task == null)
            {
                btnText.text = baseTitle;
                return;
            }

            if (task.IsCompleted)
            {
                btnText.text = $"{baseTitle}\n" + OutpostStrings.TrEn("[TAMAM]", "[DONE]");
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
                btnText.text = $"{baseTitle}\n[{crewTag}]";
            }
            else if (!task.IsContinuous && task.ProgressSeconds > 0)
            {
                btnText.text = $"{baseTitle}\n({task.NormalizedProgress * 100:0}%)";
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
                hazardHeaderTitle.text = OutpostStrings.TrEn("[!] AKTİF TEHLİKE!", "[!] ACTIVE HAZARD!");

                double rem = active.SecondsRemaining(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.TrEn($"Kalan: {OutpostStrings.FormatSimClock(rem)}", $"Ends: {OutpostStrings.FormatSimClock(rem)}");
                eventCountdownText.color = new Color(1f, 0.95f, 0.40f, 1f);

                double totalDuration = Mathf.Max(1f, (float)(active.EndSeconds - active.StartSeconds));
                eventProgressFill.fillAmount = Mathf.Clamp01((float)(rem / totalDuration));
                eventProgressFill.color = new Color(0.95f, 0.22f, 0.22f, 1f);

                string advice = active.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("[!] GÜNEŞ FIRTINASI AKTİF!\nSığınakta kal!", "[!] SOLAR STORM ACTIVE!\nStay sheltered!")
                    : OutpostStrings.TrEn("[!] TOZ FIRTINASI AKTİF!\nGüneş enerjisi düştü!", "[!] DUST STORM ACTIVE!\nSolar power reduced!");
                eventText.text = advice;
                if (eventCardText != null) eventCardText.text = advice;
            }
            else if (warning != null)
            {
                currentHazardPhase = EventPhase.Warning;
                hazardHeaderTitle.text = OutpostStrings.TrEn("[!] TEHLİKE GELİYOR!", "[!] HAZARD INCOMING!");

                double until = warning.SecondsUntilStart(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.FormatSimClock(until);
                eventCountdownText.color = new Color(1f, 0.88f, 0.24f, 1f);

                eventProgressFill.fillAmount = Mathf.Clamp01((float)(until / 60.0));
                eventProgressFill.color = new Color(0.98f, 0.58f, 0.12f, 1f);

                string prompt = warning.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("[!] GÜNEŞ FIRTINASI GELİYOR!\nAstronotları içeri çek!", "[!] SOLAR STORM COMING!\nRecall outside crew!")
                    : OutpostStrings.TrEn("[!] TOZ FIRTINASI YAKLAŞIYOR!\nBataryayı koru!", "[!] DUST STORM NEAR!\nSave battery power!");
                eventText.text = prompt;
                if (eventCardText != null) eventCardText.text = prompt;
            }
            else if (upcoming != null)
            {
                currentHazardPhase = EventPhase.Upcoming;
                hazardHeaderTitle.text = OutpostStrings.TrEn(">> UZAY HAVA RADARI", ">> SPACE WEATHER RADAR");

                double until = upcoming.SecondsUntilStart(state.ElapsedSeconds);
                eventCountdownText.text = OutpostStrings.FormatSimClock(until);
                eventCountdownText.color = new Color(0.78f, 0.90f, 1f, 1f);

                eventProgressFill.fillAmount = Mathf.Clamp01(1f - (float)(until / 120.0));
                eventProgressFill.color = AccentBlue;

                string evName = upcoming.Kind == EventKind.Radiation
                    ? OutpostStrings.TrEn("Güneş Fırtınası", "Solar Storm")
                    : OutpostStrings.TrEn("Toz Fırtınası", "Dust Storm");
                string prompt = OutpostStrings.TrEn(
                    $"Sıradaki: {evName} ({until:0}s)",
                    $"Next: {upcoming.Title} ({OutpostStrings.FormatSimClock(until)})");
                eventText.text = prompt;
                if (eventCardText != null) eventCardText.text = prompt;
            }
            else
            {
                currentHazardPhase = EventPhase.Upcoming;
                hazardHeaderTitle.text = OutpostStrings.TrEn(">> UZAY HAVA RADARI", ">> SPACE WEATHER RADAR");
                eventCountdownText.text = "--:--";
                eventCountdownText.color = Color.white;
                eventProgressFill.fillAmount = 0f;
                string calmText = OutpostStrings.TrEn("Tehlike Yok - Hava Sakin", OutpostStrings.NoEventsPending);
                eventText.text = calmText;
                if (eventCardText != null) eventCardText.text = calmText;
            }
        }

        private void UpdateGalleyAndCommsCard(MissionState state)
        {
            if (galleyCommsStatusText == null) return;

            string mealStatus = state.MealPreparedThisSol
                ? OutpostStrings.TrEn("Yemek Pişti (1.0x)", "Meal Prepped (1.0x)")
                : OutpostStrings.TrEn($"Pişmedi ({state.CurrentNightSleepFactor:0.00}x)", $"No Meal ({state.CurrentNightSleepFactor:0.00}x)");

            galleyCommsStatusText.text = OutpostStrings.TrEn($"Yedek Yemek: {state.FoodRationsRemaining} | Mutfak: {mealStatus}", $"Rations: {state.FoodRationsRemaining} | Kitchen: {mealStatus}");

            if (state.EarthAdviceStatus == EarthAdviceStatus.NotRequested)
            {
                if (state.ShouldSuggestEarthAdvice)
                {
                    btnEarthAdviceText.text = OutpostStrings.TrEn("[!] Dünya'ya Hemen Sor!", "Ask Earth NOW!");
                    btnEarthAdviceBg.color = AccentAmber;
                }
                else
                {
                    btnEarthAdviceText.text = OutpostStrings.TrEn(">> Dünya'ya Sor (45s)", "Ask Earth Advice (45s)");
                    btnEarthAdviceBg.color = ButtonBg;
                }
                earthAdviceBodyText.text = OutpostStrings.TrEn(
                    "Dünya ile radyo sinyali 45sn simülasyon gecikmeli (Gerçek Mars: 3-22 dk).",
                    "Earth radio relay has 45s sim delay (Real Mars: 3-22 min).");
            }
            else if (state.EarthAdviceStatus == EarthAdviceStatus.Pending)
            {
                btnEarthAdviceText.text = OutpostStrings.TrEn(
                    $"Sinyal Yolda ({state.EarthAdviceRemainingSeconds:0}s)",
                    $"Relay In Transit ({state.EarthAdviceRemainingSeconds:0}s)");
                btnEarthAdviceBg.color = AccentBlue;
                earthAdviceBodyText.text = OutpostStrings.TrEn(
                    $"Sinyal yolda: {state.EarthAdviceRemainingSeconds:0}s kaldı. Yerel kararları al!",
                    $"Signal in transit: {state.EarthAdviceRemainingSeconds:0}s left. Decide locally!");
            }
            else
            {
                btnEarthAdviceText.text = OutpostStrings.TrEn("Dünya Yanıtı Geldi [OK]", "Earth Advice [OK]");
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

            // -------------------------------------------------------------
            // 1. TOP BAR (Height = 66px): Reserves, Time, Temp, Camera, Audio
            // -------------------------------------------------------------
            var topBar = CreateSlicedPanel("TopBar", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -66), new Vector2(0, 0), PanelBg, panelFrameSprite);

            // Left: Clock, Sol, Mars Temp
            clockText = CreateText("ClockText", topBar, new Vector2(16, -6), new Vector2(320, 26), 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            speedText = CreateText("SpeedText", topBar, new Vector2(16, -34), new Vector2(130, 24), 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            speedText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Center: Compact Resource Bars
            CreateCompactReserveMeter(topBar, new Vector2(340, -6), 140, out batteryText, out batteryFill);
            CreateCompactReserveMeter(topBar, new Vector2(490, -6), 140, out waterText, out waterFill);
            CreateCompactReserveMeter(topBar, new Vector2(640, -6), 150, out oxygenText, out oxygenFill);
            CreateCompactReserveMeter(topBar, new Vector2(800, -6), 150, out researchText, out researchFill);
            reserveNoteText = CreateText("ReserveNote", topBar, new Vector2(340, -38), new Vector2(610, 22), 12, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.94f));

            // Right: Utility Buttons Row 1 (y = -6, height = 26px)
            CreatePillButton(topBar, "BtnCamReset", new Vector2(964, -6), new Vector2(92, 26), out btnCamResetText, () =>
            {
                if (cameraController == null) cameraController = FindFirstObjectByType<MarsOutpostCameraController>();
                if (cameraController != null) cameraController.ResetCameraView();
            }, new Color(0.20f, 0.36f, 0.54f, 1f));
            btnCamResetText.text = OutpostStrings.GetCameraResetButton();

            CreatePillButton(topBar, "BtnDomeRoof", new Vector2(1062, -6), new Vector2(96, 26), out btnDomeRoofText, () =>
            {
                if (worldView != null)
                {
                    worldView.ToggleDomeRoof();
                    UpdateDomeRoofButtonVisual();
                }
            }, new Color(0.20f, 0.40f, 0.46f, 1f));
            UpdateDomeRoofButtonVisual();

            CreatePillButton(topBar, "BtnModeToggle", new Vector2(1164, -6), new Vector2(100, 26), out btnModeText, () =>
            {
                if (runner == null) return;
                if (runner.IsDemoMode) runner.InitializeDefaultMission();
                else runner.InitializeDemoMission();
            });

            CreatePillButton(topBar, "BtnLang", new Vector2(1270, -6), new Vector2(74, 26), out btnLangText, () =>
            {
                SetLanguage(!OutpostStrings.IsTurkish);
            }, new Color(0.26f, 0.34f, 0.48f, 1f));
            btnLangText.text = OutpostStrings.GetLangButton();

            CreatePillButton(topBar, "BtnPause", new Vector2(1350, -6), new Vector2(78, 26), out btnPauseText, () => runner.TogglePause());
            CreatePillButton(topBar, "BtnSources", new Vector2(1434, -6), new Vector2(96, 26), out btnSourcesText, () => ToggleSourcesModal(true));
            btnSourcesText.text = OutpostStrings.GetSourcesButton();

            // Right: Audio & Navigation Row 2 (y = -36, height = 24px)
            CreatePillButton(topBar, "BtnMuteAudio", new Vector2(964, -36), new Vector2(88, 24), out btnMuteText, () =>
            {
                if (audioController != null) audioController.ToggleMute();
                RefreshAudioControls();
            });

            CreatePillButton(topBar, "BtnVolDown", new Vector2(1058, -36), new Vector2(46, 24), out var vDownTxt, () =>
            {
                if (audioController != null) audioController.SetVolume(audioController.Volume - 0.2f);
                RefreshAudioControls();
            });
            vDownTxt.text = "Vol-";

            volumeText = CreateText("VolumeText", topBar, new Vector2(1108, -36), new Vector2(68, 24), 12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            CreatePillButton(topBar, "BtnVolUp", new Vector2(1180, -36), new Vector2(46, 24), out var vUpTxt, () =>
            {
                if (audioController != null) audioController.SetVolume(audioController.Volume + 0.2f);
                RefreshAudioControls();
            });
            vUpTxt.text = "Vol+";

            cameraHintText = CreateText("CameraNavHint", topBar, new Vector2(1236, -37), new Vector2(670, 22), 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.72f, 0.82f, 0.92f));
            cameraHintText.text = OutpostStrings.GetCameraNavigationHint();
            RefreshAudioControls();

            // -------------------------------------------------------------
            // 2. LEFT PANEL (Width = 256px, between y = -74 and y = +88)
            // -------------------------------------------------------------
            var leftPanel = CreateSlicedPanel("LeftCrewPanel", root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 88), new Vector2(268, -74), PanelBg, panelFrameSprite);

            crewHeaderText = CreateText("CrewHeader", leftPanel, new Vector2(10, -8), new Vector2(236, 24), 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            crewHeaderText.text = OutpostStrings.TrEn("ASTRONOTLAR (1-3)", "CREW (Keys 1-3)");

            // 3 Translucent Floating Crew Cards (Clean Name + Location + Status + Energy Bar)
            for (int i = 0; i < 3; i++)
            {
                string cid = $"crew-{i + 1}";
                float cardY = -36 - i * 114;
                var cardGo = CreateSlicedPanel($"CrewCard_{i + 1}", leftPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(6, cardY - 108), new Vector2(-6, cardY), CardBg, panelFrameSprite);
                var btn = cardGo.gameObject.AddComponent<Button>();
                string capturedId = cid;
                btn.onClick.AddListener(() => SelectCrew(capturedId));

                // Colored visor accent
                var visor = CreatePanel("VisorAccent", cardGo, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -22), new Vector2(20, -10), CrewVisors[i]);

                // Astronaut Name (16pt Bold)
                var nameTxt = CreateText("Name", cardGo, new Vector2(26, -6), new Vector2(114, 22), 16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);

                // Location Pill (14pt Bold)
                var locPill = CreateSlicedPanel("LocationPill", cardGo, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-98, -28), new Vector2(-6, -6), new Color(0.12f, 0.36f, 0.22f, 0.95f), buttonPillSprite);
                var locTxt = CreateText("LocationText", locPill, new Vector2(2, -2), new Vector2(88, 18), 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.42f, 0.94f, 0.68f, 1f));

                // Concise Status + Task (14pt)
                var statusTxt = CreateText("Status", cardGo, new Vector2(8, -32), new Vector2(228, 26), 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 0.98f));

                // Energy Bar (14pt Bold)
                var stamTxt = CreateText("Stamina", cardGo, new Vector2(8, -60), new Vector2(228, 20), 14, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
                var barBg = CreateSlicedPanel("BarBg", cardGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -94), new Vector2(-8, -82), new Color(0.06f, 0.08f, 0.12f, 1f), barBgSprite);
                var stamFill = CreateFillImage("BarFill", barBg, AccentGreen);

                var card = new CrewCardUi
                {
                    CrewId = cid,
                    Background = cardGo.GetComponent<Image>(),
                    VisorBadge = visor.GetComponent<Image>(),
                    NameText = nameTxt,
                    LocationPillBg = locPill.GetComponent<Image>(),
                    LocationText = locTxt,
                    StatusText = statusTxt,
                    StaminaText = stamTxt,
                    StaminaFill = stamFill
                };
                crewCards.Add(card);
            }

            // Sleek Compact Galley & Earth Relay Footer Card
            var commsCard = CreateSlicedPanel("GalleyCommsCard", leftPanel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(6, 6), new Vector2(-6, 72), CardBg, panelFrameSprite);
            galleyCommsStatusText = CreateText("GalleyStatus", commsCard, new Vector2(6, -4), new Vector2(232, 22), 14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            btnEarthAdvice = CreatePillButton(commsCard, "BtnEarthAdvice", new Vector2(6, -30), new Vector2(232, 30), out btnEarthAdviceText, () => runner.RequestEarthAdvice(), ButtonBg);
            btnEarthAdviceText.fontSize = 14;
            btnEarthAdviceBg = btnEarthAdvice.GetComponent<Image>();

            // Earth advice body text (expandable / accessible for tests)
            earthAdviceBodyText = CreateText("EarthAdviceBody", commsCard, new Vector2(6, -30), new Vector2(232, 30), 1, FontStyle.Normal, TextAnchor.UpperLeft, Color.clear);
            earthAdviceBodyText.gameObject.SetActive(false);

            // -------------------------------------------------------------
            // 3. TOP-RIGHT TACTICAL RADAR / EVENT BANNER (Width = 278px)
            // -------------------------------------------------------------
            var bannerPanel = CreateSlicedPanel("HazardAlertBanner", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-290, -210), new Vector2(-12, -74), CardBg, alertFrameSprite);
            hazardBannerRect = bannerPanel;
            hazardBannerBg = bannerPanel.GetComponent<Image>();

            // Backward compatibility alias for EventCard
            var evCardAlias = new GameObject("EventCard");
            var evCardAliasRt = evCardAlias.AddComponent<RectTransform>();
            evCardAliasRt.SetParent(bannerPanel, false);
            eventCardBg = hazardBannerBg;

            hazardHeaderTitle = CreateText("HazardHeaderTitle", bannerPanel, new Vector2(8, -6), new Vector2(262, 22), 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            hazardHeaderTitle.text = OutpostStrings.TrEn(">> UZAY HAVA RADARI", ">> SPACE WEATHER RADAR");

            eventCountdownText = CreateText("EventCountdownText", bannerPanel, new Vector2(8, -26), new Vector2(262, 34), 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            eventCountdownText.text = "--:--";

            var progBg = CreateSlicedPanel("EventProgressBar", bannerPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -68), new Vector2(-12, -60), new Color(0.06f, 0.08f, 0.12f, 1f), barBgSprite);
            eventProgressFill = CreateFillImage("EventProgressFill", progBg, AccentBlue);

            eventText = CreateText("EventText", bannerPanel, new Vector2(8, -72), new Vector2(262, 56), 11, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            eventText.text = OutpostStrings.TrEn("Tehlike Yok - Hava Sakin", OutpostStrings.NoEventsPending);
            eventCardText = eventText;

            // -------------------------------------------------------------
            // 4. BOTTOM-CENTER HORIZONTAL COMMAND DOCK (Height = 84px)
            // -------------------------------------------------------------
            var commandDock = CreateSlicedPanel("BottomCommandDock", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-700, 10), new Vector2(700, 94), PanelBg, panelFrameSprite);

            // 4.1 Top Status Strip in Dock (Selected Astronaut Summary & Shortcuts)
            var selVisorGo = CreatePanel("SelectedVisor", commandDock, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -18), new Vector2(24, -6), CrewVisors[0]);
            selectedVisorBadge = selVisorGo.GetComponent<Image>();

            selectedNameText = CreateText("SelectedName", commandDock, new Vector2(28, -4), new Vector2(118, 20), 14, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);

            var pillGo = CreateSlicedPanel("LocationPill", commandDock, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -22), new Vector2(250, -3), new Color(0.12f, 0.36f, 0.22f, 0.95f), buttonPillSprite);
            selectedLocationPillBg = pillGo.GetComponent<Image>();
            selectedLocationText = CreateText("LocationText", pillGo, new Vector2(2, -1), new Vector2(96, 17), 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.42f, 0.94f, 0.68f, 1f));

            selectedStatusText = CreateText("SelectedStatus", commandDock, new Vector2(256, -4), new Vector2(280, 20), 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 0.98f));

            selectedStaminaText = CreateText("SelectedStamina", commandDock, new Vector2(542, -4), new Vector2(148, 20), 14, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            var selBarBg = CreateSlicedPanel("SelStaminaBarBg", commandDock, new Vector2(0, 1), new Vector2(0, 1), new Vector2(694, -17), new Vector2(784, -7), new Color(0.06f, 0.08f, 0.12f, 1f), barBgSprite);
            selectedStaminaFill = CreateFillImage("SelStaminaFill", selBarBg, AccentGreen);

            selectedRiskText = CreateText("SelectedRisk", commandDock, new Vector2(794, -4), new Vector2(170, 20), 14, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);

            // Backward compatibility hidden labels
            selectedStatsText = CreateText("SelectedStats", commandDock, Vector2.zero, Vector2.zero, 1, FontStyle.Normal, TextAnchor.MiddleLeft, Color.clear);
            selectedRiskNoteText = CreateText("SelectedRiskNote", commandDock, Vector2.zero, Vector2.zero, 1, FontStyle.Normal, TextAnchor.MiddleLeft, Color.clear);
            selectedStatsText.gameObject.SetActive(false);
            selectedRiskNoteText.gameObject.SetActive(false);

            // 4.2 Horizontal Action & Control Buttons (y = -26, height = 50px)
            float btnX = 12;

            btnPanelMaint = CreateDockButton(commandDock, "BtnPanelMaint", new Vector2(btnX, -26), new Vector2(124, 50), out btnPanelMaintText, () => AssignOrReplaceTask("panel-maintenance"), ColorPanelMaint, iconSolar);
            btnPanelMaintBg = btnPanelMaint.GetComponent<Image>();
            btnX += 128;

            btnSystemMaint = CreateDockButton(commandDock, "BtnSystemMaint", new Vector2(btnX, -26), new Vector2(134, 50), out btnSystemMaintText, () => AssignOrReplaceTask("system-maintenance"), ColorSystemMaint, iconO2);
            btnSystemMaintBg = btnSystemMaint.GetComponent<Image>();
            btnX += 138;

            btnMealPrep = CreateDockButton(commandDock, "BtnMealPrep", new Vector2(btnX, -26), new Vector2(124, 50), out btnMealPrepText, () => AssignOrReplaceTask("meal-prep"), ColorMealPrep, iconFood);
            btnMealPrepBg = btnMealPrep.GetComponent<Image>();
            btnX += 128;

            btnResearchTask = CreateDockButton(commandDock, "BtnResearch", new Vector2(btnX, -26), new Vector2(120, 50), out btnResearchTaskText, () => AssignOrReplaceTask("research"), ColorResearch, iconLab);
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
            btnX += 124;

            btnReturn = CreateDockButton(commandDock, "BtnReturn", new Vector2(btnX, -26), new Vector2(154, 50), out btnReturnText, () => runner.ReturnToShelter(selectedCrewId), null, iconHome);
            btnReturnBg = btnReturn.GetComponent<Image>();
            btnReturnText.text = OutpostStrings.GetReturnButton();
            btnX += 158;

            btnRest = CreateDockButton(commandDock, "BtnRest", new Vector2(btnX, -26), new Vector2(120, 50), out btnRestText, () => runner.Rest(selectedCrewId), null, iconRest);
            btnRestBg = btnRest.GetComponent<Image>();
            btnRestText.text = OutpostStrings.GetRestButton();
            btnX += 124;

            btnCancelTask = CreateDockButton(commandDock, "BtnCancel", new Vector2(btnX, -26), new Vector2(120, 50), out btnCancelTaskText, () => runner.CancelTask(selectedCrewId), null, iconStop);
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
            btnX += 126;

            // Divider Line
            CreatePanel("DockDivider", commandDock, new Vector2(0, 1), new Vector2(0, 1), new Vector2(btnX + 4, -76), new Vector2(btnX + 6, -26), new Color(0.24f, 0.36f, 0.52f, 0.8f));
            btnX += 14;

            // Base Toggles
            btnSleep = CreateDockButton(commandDock, "BtnSleep", new Vector2(btnX, -26), new Vector2(156, 50), out btnSleepText, () =>
            {
                if (runner != null && runner.State != null)
                {
                    if (runner.State.IsSleeping) runner.CancelSleep();
                    else runner.BeginSleep();
                }
            }, null, iconSleep);
            btnSleepBg = btnSleep.GetComponent<Image>();
            btnX += 160;

            btnResearchToggle = CreateDockButton(commandDock, "BtnResearchToggle", new Vector2(btnX, -26), new Vector2(136, 50), out btnResearchToggleText, () =>
            {
                if (runner != null && runner.State != null)
                    runner.SetResearchEnabled(!runner.State.Resources.ResearchEnabled);
            }, null, iconTech);
            btnResearchToggleBg = btnResearchToggle.GetComponent<Image>();
            btnX += 140;

            btnRestart = CreateDockButton(commandDock, "BtnRestart", new Vector2(btnX, -26), new Vector2(136, 50), out btnRestartText, () => runner.RestartMission());
            btnRestartText.text = OutpostStrings.GetRestartButton();

            // -------------------------------------------------------------
            // 5. Feedback Banner & Tutorial Guide Strip
            // -------------------------------------------------------------
            var fbRect = CreateSlicedPanel("FeedbackBanner", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-340, 102), new Vector2(340, 148), new Color(0.58f, 0.18f, 0.16f, 0.95f), alertFrameSprite);
            feedbackPanel = fbRect.gameObject;
            feedbackText = CreateText("FeedbackText", fbRect, new Vector2(16, -4), new Vector2(648, 38), 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            feedbackPanel.SetActive(false);

            var tutRect = CreateSlicedPanel("TutorialGuide", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-440, 102), new Vector2(440, 158), new Color(0.08f, 0.16f, 0.26f, 0.95f), panelFrameSprite);
            tutorialPanel = tutRect.gameObject;
            tutorialText = CreateText("TutorialText", tutRect, new Vector2(14, -6), new Vector2(710, 46), 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            CreatePillButton(tutRect, "BtnHideTutorial", new Vector2(736, -9), new Vector2(130, 40), out hideTutorialText, () =>
            {
                tutorialDismissed = true;
                tutorialPanel.SetActive(false);
            });
            hideTutorialText.text = OutpostStrings.GetSkipTutorialButton();

            // -------------------------------------------------------------
            // 6. Center Pause Overlay
            // -------------------------------------------------------------
            var pauseRect = CreateSlicedPanel("PauseOverlayPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -110), new Vector2(280, 110), new Color(0.08f, 0.10f, 0.14f, 0.96f), panelFrameSprite);
            pauseOverlayPanel = pauseRect.gameObject;

            var pAlias1 = new GameObject("PauseBannerPanel");
            pAlias1.transform.SetParent(pauseRect, false);
            var pAlias2 = new GameObject("PauseOverlay");
            pAlias2.transform.SetParent(pauseRect, false);

            pauseTitleText = CreateText("PauseTitle", pauseRect, new Vector2(16, -14), new Vector2(528, 38), 24, FontStyle.Bold, TextAnchor.MiddleCenter, AccentAmber);
            pauseTitleText.text = OutpostStrings.TrEn("[!] OYUN DURAKLATILDI", "[!] GAME PAUSED");

            pauseSubText = CreateText("PauseSub", pauseRect, new Vector2(16, -56), new Vector2(528, 30), 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 0.95f));
            pauseSubText.text = OutpostStrings.TrEn("Devam etmek için butona tıkla veya Space tuşuna bas", "Click button or press Space to resume");

            btnResumeOverlay = CreatePillButton(pauseRect, "BtnResumeOverlay", new Vector2(120, -114), new Vector2(320, 56), out btnResumeOverlayText, () =>
            {
                if (runner != null) runner.SetPaused(false);
                UpdatePauseOverlay(runner != null ? runner.State : null);
            }, AccentGreen);
            btnResumeOverlayText.fontSize = 18;
            btnResumeOverlayText.text = OutpostStrings.TrEn("► DEVAM ET (Space / Tıkla)", "► RESUME (Space / Click)");
            pauseOverlayPanel.SetActive(false);

            // -------------------------------------------------------------
            // 7. Modals
            // -------------------------------------------------------------
            BuildModals(root);
        }

        private void BuildModals(RectTransform root)
        {
            // Sol Report Modal
            var solRect = CreateSlicedPanel("SolReportModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -240), new Vector2(300, 240), new Color(0.08f, 0.10f, 0.14f, 0.98f), panelFrameSprite);
            solReportModal = solRect.gameObject;
            solReportTitleText = CreateText("SolTitle", solRect, new Vector2(24, -18), new Vector2(552, 42), 26, FontStyle.Bold, TextAnchor.MiddleCenter, AccentBlue);
            solReportTitleText.text = OutpostStrings.TrEn("GÜN SONU RAPORU", "SOL REPORT");
            solReportBody = CreateText("SolBody", solRect, new Vector2(28, -70), new Vector2(544, 320), 19, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            CreatePillButton(solRect, "BtnCloseSolReport", new Vector2(180, -410), new Vector2(240, 50), out solBtnTxt, () => runner.AcknowledgeSolReport());
            solBtnTxt.text = OutpostStrings.GetContinueButton();
            solReportModal.SetActive(false);

            // Critical Alert Modal
            var alertRect = CreateSlicedPanel("CriticalAlertModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -140), new Vector2(280, 140), new Color(0.24f, 0.08f, 0.08f, 0.98f), alertFrameSprite);
            alertModal = alertRect.gameObject;
            alertTitleText = CreateText("AlertTitle", alertRect, new Vector2(24, -18), new Vector2(512, 40), 26, FontStyle.Bold, TextAnchor.MiddleCenter, AccentRed);
            alertTitleText.text = OutpostStrings.TrEn("[!] KRİTİK UYARI", "CRITICAL ALERT");
            alertBody = CreateText("AlertBody", alertRect, new Vector2(24, -74), new Vector2(512, 110), 19, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            CreatePillButton(alertRect, "BtnAckAlert", new Vector2(160, -206), new Vector2(240, 50), out alertBtnTxt, () => runner.AcknowledgeCriticalAlert());
            alertBtnTxt.text = OutpostStrings.GetAcknowledgeButton();
            alertModal.SetActive(false);

            // Outcome Modal
            var outRect = CreateSlicedPanel("OutcomeModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -180), new Vector2(300, 180), new Color(0.08f, 0.10f, 0.14f, 0.98f), panelFrameSprite);
            outcomeModal = outRect.gameObject;
            outcomeTitle = CreateText("OutcomeTitle", outRect, new Vector2(24, -20), new Vector2(552, 46), 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            outcomeBody = CreateText("OutcomeBody", outRect, new Vector2(28, -72), new Vector2(544, 210), 17, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            CreatePillButton(outRect, "BtnOutcomeRestart", new Vector2(180, -284), new Vector2(240, 52), out outBtnTxt, () => runner.RestartMission());
            outBtnTxt.text = OutpostStrings.GetRestartButton();
            outcomeModal.SetActive(false);

            // Science & NASA Sources Modal
            var srcRect = CreateSlicedPanel("SourcesModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -300), new Vector2(420, 300), new Color(0.07f, 0.09f, 0.13f, 0.98f), panelFrameSprite);
            sourcesModal = srcRect.gameObject;
            sourcesTitle = CreateText("SourcesTitle", srcRect, new Vector2(24, -16), new Vector2(792, 40), 24, FontStyle.Bold, TextAnchor.MiddleCenter, AccentBlue);
            sourcesTitle.text = OutpostStrings.TrEn("NASA BİLİM VE ARŞİV KAYNAKLARI", "NASA SCIENCE & ARCHIVE SOURCES");
            sourcesBody = CreateText("SourcesBody", srcRect, new Vector2(24, -64), new Vector2(792, 462), 14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            sourcesBody.text = BuildSourcesText();
            CreatePillButton(srcRect, "BtnCloseSources", new Vector2(300, -536), new Vector2(240, 48), out sourcesCloseText, () => ToggleSourcesModal(false));
            sourcesCloseText.text = OutpostStrings.TrEn("Kapat", "Close");
            sourcesModal.SetActive(false);
        }

        private void ToggleSourcesModal(bool open)
        {
            if (sourcesModal == null) return;
            sourcesModal.SetActive(open);
            if (open && runner != null && runner.Stepper != null && !runner.Stepper.UserPaused)
                runner.SetPaused(true);
            UpdatePauseOverlay(runner != null ? runner.State : null);
        }

        private void UpdatePauseOverlay(MissionState state)
        {
            if (pauseOverlayPanel == null) return;

            bool isPaused = runner != null && runner.Stepper != null && runner.Stepper.IsPaused;
            bool showOutcome = state != null && state.Outcome != MissionOutcome.InProgress;
            bool showAlert = !showOutcome && state != null && state.CriticalAlertPauseRequested;
            bool showReport = !showOutcome && !showAlert && state != null && state.SolReportPauseRequested && state.PendingSolReport != null;
            bool showSources = sourcesModal != null && sourcesModal.activeSelf;

            bool shouldShow = isPaused && !showOutcome && !showAlert && !showReport && !showSources && !isScreenshotMode;
            if (pauseOverlayPanel.activeSelf != shouldShow)
            {
                pauseOverlayPanel.SetActive(shouldShow);
            }
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
                    ? OutpostStrings.TrEn("Ses: KAPALI", "Sound: OFF")
                    : OutpostStrings.TrEn("Ses: AÇIK", "Sound: ON");
            }
            if (volumeText != null)
            {
                int pct = audioController != null ? Mathf.RoundToInt(audioController.Volume * 100f) : 60;
                volumeText.text = OutpostStrings.TrEn($"Ses: {pct}%", $"Vol: {pct}%");
            }
        }

        private void CreateCompactReserveMeter(RectTransform parent, Vector2 topLeft, float width, out Text label, out Image fill)
        {
            var holder = CreatePanel("Meter", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, topLeft.y - 32), new Vector2(topLeft.x + width, topLeft.y), Color.clear);
            label = CreateText("Label", holder, new Vector2(0, 0), new Vector2(width, 18), 12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            var barBg = CreateSlicedPanel("Bg", holder, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -28), new Vector2(0, -20), new Color(0.05f, 0.06f, 0.08f, 1f), barBgSprite);
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

        private RectTransform CreateSlicedPanel(string objName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, Sprite sprite = null)
        {
            var go = new GameObject(objName);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : panelFrameSprite;
            img.type = Image.Type.Sliced;
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
            img.sprite = barFillSprite != null ? barFillSprite : whiteSprite;
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
            if (color.a > 0.05f)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
                shadow.effectDistance = new Vector2(1f, -1f);
            }
            return txt;
        }

        private Button CreatePillButton(RectTransform parent, string objName, Vector2 topLeft, Vector2 size, out Text label, Action onClick, Color? bgColor = null)
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
            img.sprite = buttonPillSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor ?? ButtonBg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(4, 1);
            txtRt.offsetMax = new Vector2(-4, -1);
            label = txtGo.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var pillShadow = txtGo.AddComponent<Shadow>();
            pillShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            pillShadow.effectDistance = new Vector2(1f, -1f);
            return btn;
        }

        private Button CreateDockButton(RectTransform parent, string objName, Vector2 topLeft, Vector2 size, out Text label, Action onClick, Color? bgColor = null, Sprite icon = null)
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
            img.sprite = buttonPillSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor ?? ButtonBg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                var iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.SetParent(rt, false);
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.pivot = new Vector2(0, 0.5f);
                iconRt.anchoredPosition = new Vector2(8, 0);
                iconRt.sizeDelta = new Vector2(28, 28);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = icon != null ? new Vector2(40, 2) : new Vector2(6, 2);
            txtRt.offsetMax = new Vector2(-6, -2);
            label = txtGo.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = icon != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var dockShadow = txtGo.AddComponent<Shadow>();
            dockShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            dockShadow.effectDistance = new Vector2(1f, -1f);
            return btn;
        }
    }
}
