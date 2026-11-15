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
        private MarsOutpostSettingsMenu settingsMenu;

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

        private GameObject earthOrdersModal;
        private Text btnEarthOrdersText;
        private GameObject btnEarthOrdersObj;
        private GameObject btnSaveOpsObj;
        private Text btnSaveOpsText;
        private GameObject btnLoadOpsObj;
        private Text btnLoadOpsText;
        private Text earthOrdersTitle;
        private Text earthOrdersBody;
        private Text earthOrdersHistoryText;
        private Text btnOrderAcceptText;
        private Text btnOrderAssignText;
        private Text btnOrderCancelText;
        private Text btnOrderPrevText;
        private Text btnOrderNextText;
        private Text btnOrderCloseText;
        private int earthOrderPageIndex;
        private int lastKnownEarthOrderCount;

        private GameObject equipmentPanel;
        private Text equipmentStatusText;
        private Text waterFlowBreakdownText;
        private Button panelRepairButton, oxygenRepairButton, waterRepairButton, extractorToggleButton, btnFocusExtractor, btnRecallSurfaceJob;
        private Text panelRepairText, oxygenRepairText, waterRepairText, extractorToggleText, btnFocusExtractorText, btnRecallSurfaceJobText;
        private int focusExtractorCycleIndex;

        // Expansion V3 (OP03): Grid Toggle, Materials & Parcel Selection Card
        private GameObject btnGridToggleObj;
        private Button btnGridToggle;
        private Image btnGridToggleBg;
        private Text btnGridToggleText;
        private Text materialsText;
        private GameObject parcelCardPanel;
        private Text parcelTitleText;
        private Text parcelDetailsText;
        private Text parcelReasonText;
        private Button btnClaimParcel;
        private Image btnClaimParcelBg;
        private Text btnClaimParcelText;
        private Button btnSurfaceWalk;
        private Image btnSurfaceWalkBg;
        private Text btnSurfaceWalkText;
        private Button btnBuildBlueprint;
        private Image btnBuildBlueprintBg;
        private Text btnBuildBlueprintText;
        private Button btnRepairParcel;
        private Image btnRepairParcelBg;
        private Text btnRepairParcelText;
        private Button btnCancelBlueprint;
        private Image btnCancelBlueprintBg;
        private Text btnCancelBlueprintText;
        private Button btnDismantleFacility;
        private Image btnDismantleFacilityBg;
        private Text btnDismantleFacilityText;
        private Button btnToggleSiteExtractor;
        private Image btnToggleSiteExtractorBg;
        private Text btnToggleSiteExtractorText;
        private string selectedBlueprintId;
        private string selectedFaultFacilityId;
        private string selectedDismantleFacilityId;
        private string selectedExtractorFacilityId;
        private GridCoord? selectedParcel;

        // Operations Build Catalog & Ghost Status Panel (OP06+)
        private GameObject operationsBuildPanel;
        private bool buildDrawerOpen;
        private bool systemsDrawerOpen;
        private bool toolsOpen;
        private GameObject buildEntry, systemsEntry, toolsPanel;
        private Text buildCatalogTitleText;
        private Text buildGhostInfoText;
        private Text buildNoticeText;
        private Button btnBuildLine, btnBuildSolar, btnBuildBattery, btnBuildExtractor, btnRotateGhost, btnCancelGhost;
        private Image btnBuildLineBg, btnBuildSolarBg, btnBuildBatteryBg, btnBuildExtractorBg;
        private Text btnBuildLineText, btnBuildSolarText, btnBuildBatteryText, btnBuildExtractorText, btnRotateGhostText, btnCancelGhostText;

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
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                settingsMenu = canvas.GetComponent<MarsOutpostSettingsMenu>();
                if (settingsMenu == null)
                    settingsMenu = canvas.gameObject.AddComponent<MarsOutpostSettingsMenu>();
                foreach (var name in new[] { "BtnCamReset", "BtnDomeRoof", "BtnModeToggle", "BtnLang" })
                {
                    var button = canvas.transform.Find("TopBar/" + name);
                    if (button != null) settingsMenu.AddUtilityButton(button.GetComponent<Button>());
                }
            }
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
            bool captureOp03Grid = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-op03-grid") >= 0;
            bool captureOp05Surface = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-op05-surface") >= 0;
            bool captureOp06Ghost = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-op06-ghost") >= 0;
            bool captureOp14Extractor = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-op14-extractor") >= 0;
            bool captureSystems = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-systems") >= 0;
            bool captureOrders = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-orders") >= 0;
            if (runner != null)
            {
                if (captureOp03Grid || captureOp05Surface || captureOp06Ghost || captureOp14Extractor || captureSystems || captureOrders)
                    runner.InitializeOperationsMission();

                if (captureOp14Extractor)
                {
                    var map = runner.State.Map;
                    ParcelState iceCell = null;
                    foreach (var cell in map.Cells)
                    {
                        if (cell.IsExplored && cell.IsScanned && cell.IsBuildable &&
                            cell.Protection == StarterProtectionKind.None &&
                            cell.ResourceKind == ParcelResourceKind.Ice &&
                            cell.RemainingWater > 0)
                        {
                            iceCell = cell;
                            break;
                        }
                    }

                    if (iceCell != null)
                    {
                        // Claim and bridge shortest cardinal path from core-connected line to iceCell.
                        var q = new Queue<GridCoord>();
                        var parent = new Dictionary<GridCoord, GridCoord>();
                        foreach (var c in map.Cells)
                        {
                            if (c.IsClaimed || c.Protection != StarterProtectionKind.None)
                            {
                                parent[c.Coord] = c.Coord;
                                q.Enqueue(c.Coord);
                            }
                        }
                        while (q.Count > 0 && !parent.ContainsKey(iceCell.Coord))
                        {
                            var cur = q.Dequeue();
                            foreach (var n in map.Config.GetCardinalNeighbors(cur))
                            {
                                if (!map.TryGetCell(n, out var nCell) || parent.ContainsKey(n) || !nCell.IsExplored)
                                    continue;
                                parent[n] = cur;
                                q.Enqueue(n);
                            }
                        }
                        var claimSeq = new List<GridCoord>();
                        if (parent.ContainsKey(iceCell.Coord))
                        {
                            var step = iceCell.Coord;
                            while (!parent[step].Equals(step))
                            {
                                claimSeq.Add(step);
                                step = parent[step];
                            }
                            claimSeq.Reverse();
                            for (int i = 0; i < claimSeq.Count; i++)
                            {
                                if (!runner.State.Map.GetCell(claimSeq[i]).IsClaimed)
                                    runner.ClaimParcel(claimSeq[i]);
                            }
                        }

                        var connected = UtilityNetwork.ComputeConnectedLineCells(runner.State.Map);
                        var lq = new Queue<GridCoord>();
                        var lparent = new Dictionary<GridCoord, GridCoord>();
                        foreach (var lc in connected) { lparent[lc] = lc; lq.Enqueue(lc); }
                        GridCoord? bridgeEnd = null;
                        while (lq.Count > 0 && !bridgeEnd.HasValue)
                        {
                            var cur = lq.Dequeue();
                            foreach (var n in map.Config.GetCardinalNeighbors(cur))
                            {
                                if (n == iceCell.Coord && cur != iceCell.Coord) { bridgeEnd = cur; break; }
                                if (!map.TryGetCell(n, out var nCell) || lparent.ContainsKey(n) || !nCell.IsClaimed || !nCell.IsBuildable || nCell.HasUtilityLine || !string.IsNullOrEmpty(nCell.UtilityBlueprintId)) continue;
                                lparent[n] = cur;
                                lq.Enqueue(n);
                            }
                        }
                        if (bridgeEnd.HasValue)
                        {
                            var lineSeq = new List<GridCoord>();
                            var cur = bridgeEnd.Value;
                            while (!lparent[cur].Equals(cur)) { lineSeq.Add(cur); cur = lparent[cur]; }
                            lineSeq.Reverse();
                            for (int i = 0; i < lineSeq.Count; i++)
                            {
                                var bp = runner.PlaceBlueprint(FacilityKind.UtilityLine, lineSeq[i]);
                                if (bp.Success)
                                {
                                    runner.AssignBuildBlueprint("crew-1", bp.Reason);
                                    for (int t = 0; t < 30; t++) runner.Stepper.Simulation.Tick(2.0);
                                    runner.ReturnToShelter("crew-1");
                                    for (int t = 0; t < 20; t++) runner.Stepper.Simulation.Tick(2.0);
                                }
                            }
                        }

                        var exBp = runner.PlaceBlueprint(FacilityKind.WaterExtractor, iceCell.Coord);
                        if (exBp.Success)
                        {
                            runner.AssignBuildBlueprint("crew-1", exBp.Reason);
                            for (int t = 0; t < 60; t++) runner.Stepper.Simulation.Tick(2.0);
                            runner.ReturnToShelter("crew-1");
                            for (int t = 0; t < 25; t++) runner.Stepper.Simulation.Tick(2.0);
                        }

                        // Advance active extraction a few seconds so rate/stock/totals are live!
                        runner.AssignTask("crew-2", "research");
                        for (int t = 0; t < 6; t++) runner.Stepper.Simulation.Tick(2.0);

                        if (worldView != null)
                        {
                            worldView.SetGridVisible(true);
                            worldView.ApplyState(runner.State);
                            worldView.SelectParcel(iceCell.Coord);
                            var cam = FindFirstObjectByType<MarsOutpostCameraController>();
                            if (cam != null && worldView.TryGetParcelWorldCenter(iceCell.Coord, out var wp))
                                cam.FocusWorldPoint(wp, immediate: true);
                        }
                    }
                }
                else if (captureOp05Surface)
                {
                    runner.AssignSurfaceWork("crew-1", new GridCoord(11, 8), workRequiredSeconds: 15.0);
                    runner.AssignTask("crew-2", "research");
                    runner.Stepper.Simulation.Tick(4.0);
                }
                else if (captureOp06Ghost)
                {
                    // Claim up to 3 buildable neighbors (spend 6 mats -> 34 left), then place 1 line blueprint (-1 -> 33) + 1 solar blueprint (-12 -> 21) and hover green ghost.
                    int claimedCount = 0;
                    for (int pass = 0; pass < 3 && claimedCount < 3; pass++)
                    {
                        foreach (var cell in runner.State.Map.Cells)
                        {
                            if (claimedCount >= 3) break;
                            if (!cell.IsClaimed && cell.IsExplored && cell.IsBuildable && cell.Protection == StarterProtectionKind.None)
                            {
                                if (runner.ClaimParcel(cell.Coord).Success)
                                    claimedCount++;
                            }
                        }
                    }

                    // Place UtilityLine blueprint on SolarWorkCoord (9,5), SolarPanel blueprint on first other valid claimed cell, and hover Ghost on another.
                    runner.PlaceBlueprint(FacilityKind.UtilityLine, ParcelMap.SolarWorkCoord, FacilityRotation.Deg0);
                    GridCoord? placedSolar = null;
                    GridCoord? ghostHover = null;
                    foreach (var cell in runner.State.Map.Cells)
                    {
                        if (cell.IsClaimed && cell.IsBuildable && cell.Protection == StarterProtectionKind.None && string.IsNullOrEmpty(cell.FacilityId) && cell.Coord != ParcelMap.SolarWorkCoord)
                        {
                            if (!placedSolar.HasValue)
                            {
                                var r = runner.PlaceBlueprint(FacilityKind.SolarPanel, cell.Coord, FacilityRotation.Deg90);
                                if (r.Success) placedSolar = cell.Coord;
                            }
                            else if (!ghostHover.HasValue)
                            {
                                if (runner.ValidateBlueprintPlacement(FacilityKind.SolarPanel, cell.Coord, FacilityRotation.Deg0, out _).Success)
                                    ghostHover = cell.Coord;
                            }
                        }
                    }

                    runner.AssignTask("crew-2", "research");
                    runner.Stepper.AdvanceFrame(0.5);
                    if (worldView != null)
                    {
                        worldView.SetGridVisible(true);
                        worldView.ApplyState(runner.State);
                        worldView.StartBuildMode(FacilityKind.SolarPanel);
                        if (ghostHover.HasValue) worldView.SetHoveredBuildCell(ghostHover.Value);
                    }
                }
                else
                {
                    runner.AssignTask("crew-1", "panel-maintenance");
                    runner.AssignTask("crew-2", "research");
                    runner.Stepper.AdvanceFrame(1.5);
                }
            }
            if ((captureOp03Grid || captureOp05Surface || captureOp06Ghost) && worldView != null)
            {
                worldView.SetGridVisible(true);
                if (captureOp06Ghost)
                {
                    var cam = FindFirstObjectByType<MarsOutpostCameraController>();
                    if (cam != null)
                        cam.FocusWorldPoint(new Vector3(4.0f, 0.2f, -2.0f), immediate: true);
                }
                else
                {
                    worldView.SelectParcel(captureOp05Surface ? new GridCoord(11, 8) : new GridCoord(8, 4));
                    if (captureOp05Surface)
                    {
                        worldView.ApplyState(runner.State);
                        var cam = FindFirstObjectByType<MarsOutpostCameraController>();
                        if (cam != null)
                            cam.FocusWorldPoint(new Vector3(10.0f, 0.2f, 6.0f), immediate: true);
                    }
                }
            }
            for (int f = 0; f < 10; f++)
                yield return null;
            yield return new WaitForEndOfFrame();

            if (captureOp06Ghost)
            {
                buildDrawerOpen = true;
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
            }
            if (captureSystems)
            {
                systemsDrawerOpen = true;
                RefreshEquipmentUi(runner != null ? runner.State : null);
            }
            if (captureOrders) ToggleEarthOrdersModal(true);
            if (captureOp06Ghost || captureSystems || captureOrders)
            {
                yield return null;
                yield return new WaitForEndOfFrame();
            }
            bool captureSettingsUtility = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-settings-utility") >= 0;
            if ((captureSettingsUtility || Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-settings") >= 0) && settingsMenu != null)
            {
                settingsMenu.Open();
                yield return null;
                if (captureSettingsUtility)
                {
                    var scroll = settingsMenu.GetComponentInChildren<ScrollRect>();
                    if (scroll != null) scroll.verticalNormalizedPosition = 0f;
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
            }

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
                worldView.ParcelSelected += OnWorldParcelSelected;
                worldView.GridVisibilityChanged += OnGridVisibilityChanged;
                worldView.BuildModeChanged += OnWorldBuildModeChanged;
                worldView.BuildGhostUpdated += OnWorldBuildGhostUpdated;
                worldView.BlueprintPlacementAttempted += OnBlueprintPlacementAttempted;
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
                worldView.ParcelSelected -= OnWorldParcelSelected;
                worldView.GridVisibilityChanged -= OnGridVisibilityChanged;
                worldView.BuildModeChanged -= OnWorldBuildModeChanged;
                worldView.BuildGhostUpdated -= OnWorldBuildGhostUpdated;
                worldView.BlueprintPlacementAttempted -= OnBlueprintPlacementAttempted;
            }
        }

        private void OnWorldBuildModeChanged(FacilityKind? kind)
        {
            RefreshOperationsBuildUi(runner != null ? runner.State : null);
        }

        private void OnWorldBuildGhostUpdated()
        {
            RefreshOperationsBuildUi(runner != null ? runner.State : null);
        }

        private void OnBlueprintPlacementAttempted(FacilityKind kind, GridCoord coord, FacilityRotation rot, CommandResult res)
        {
            if (res.Success && worldView != null)
            {
                buildDrawerOpen = false;
                worldView.CancelBuildMode();
                worldView.SelectParcel(coord);
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
                RefreshParcelSelectionUi(runner != null ? runner.State : null);
                return;
            }
            if (!res.Success && feedbackPanel != null && feedbackText != null)
            {
                feedbackText.text = OutpostStrings.FormatBlueprintFailureReason(res, kind, FacilityCatalog.GetSpec(kind).MaterialCost, runner?.State?.Materials ?? 0);
                feedbackPanel.SetActive(true);
                feedbackTimer = 4.0f;
            }
            RefreshOperationsBuildUi(runner != null ? runner.State : null);
        }

        private void OnWorldParcelSelected(GridCoord? coord)
        {
            selectedParcel = coord;
            // Clicking a parcel before choosing a ghost opens its inspector for claiming.
            if (coord.HasValue && buildDrawerOpen && (worldView == null || !worldView.ActiveBuildKind.HasValue))
            {
                buildDrawerOpen = false;
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
            }
            if (coord.HasValue && !buildDrawerOpen) systemsDrawerOpen = false;
            RefreshEquipmentUi(runner != null ? runner.State : null);
            RefreshParcelSelectionUi(runner != null ? runner.State : null);
        }

        private void OnGridVisibilityChanged(bool visible)
        {
            UpdateGridToggleButtonVisual(runner != null ? runner.State : null);
        }

        private void OnDomeRoofChanged(bool visible)
        {
            UpdateDomeRoofButtonVisual();
        }

        private void UpdateGridToggleButtonVisual(MissionState state)
        {
            bool isOps = state != null && state.Mode == MissionMode.Operations && state.Map != null;
            if (btnGridToggleObj != null) btnGridToggleObj.SetActive(isOps);
            if (!isOps) return;

            bool gridVisible = worldView != null && worldView.IsGridVisible;
            if (btnGridToggleText != null)
                btnGridToggleText.text = OutpostStrings.GetGridToggleButton(gridVisible);
            if (btnGridToggleBg != null)
                btnGridToggleBg.color = gridVisible ? AccentCyan : new Color(0.20f, 0.34f, 0.44f, 1f);
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

            if (settingsMenu == null || !settingsMenu.IsOpen)
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

            RefreshEarthOrdersUi(runner != null ? runner.State : null);

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
                cameraHintText.text = OutpostStrings.TrEn("WASD: Gez | Tekerlek: Yakınlaştır | R: Sıfırla", "WASD: Pan | Wheel: Zoom | R: Reset");
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

            runner.ReplaceTask(selectedCrewId, taskId);
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
            double batteryMax = state.EffectiveBatteryCapacity > 0 ? state.EffectiveBatteryCapacity : 100;
            UpdateBar(batteryText, batteryFill, OutpostStrings.GetBatteryLabel(), res.Battery, batteryMax, new Color(0.92f, 0.74f, 0.22f));
            UpdateBar(waterText, waterFill, OutpostStrings.GetWaterLabel(), res.Water, 100, new Color(0.28f, 0.68f, 0.92f));
            string o2Status = res.Oxygen < 20 ? OutpostStrings.TrEn("DÜŞÜK!", "LOW!") : OutpostStrings.TrEn("İYİ", "OK");
            UpdateBar(oxygenText, oxygenFill, $"{OutpostStrings.GetOxygenLabel()} ({o2Status})", res.Oxygen, 100, res.Oxygen < 20 ? AccentRed : AccentGreen);
            if (state.Mode == MissionMode.Operations)
            {
                string sciLabel = OutpostStrings.TrEn("Bilim", "Science");
                researchText.text = OutpostStrings.TrEn($"Araş.: {res.ResearchProgress:0.#} | {sciLabel}: {res.ScienceBalance:0.#}",
                    $"Research: {res.ResearchProgress:0.#} | {sciLabel}: {res.ScienceBalance:0.#}");
                if (researchFill != null) researchFill.fillAmount = 1f;
            }
            else
            {
                UpdateBar(researchText, researchFill, OutpostStrings.GetResearchLabel(), res.ResearchProgress, researchGoal, new Color(0.68f, 0.52f, 0.96f));
            }

            RefreshEarthOrdersUi(state);
            RefreshEquipmentUi(state);
            RefreshParcelSelectionUi(state);
            RefreshOperationsBuildUi(state);
            UpdateGridToggleButtonVisual(state);
            if (materialsText != null)
            {
                bool showMats = state.Mode == MissionMode.Operations && state.Map != null;
                materialsText.gameObject.SetActive(showMats);
                if (showMats)
                    materialsText.text = OutpostStrings.TrEn($"Malzeme: {state.Materials}/{state.MaterialCapacity}", $"Materials: {state.Materials}/{state.MaterialCapacity}");
            }

            if (btnLangText != null)
                btnLangText.text = OutpostStrings.GetLangButton();

            if (btnModeText != null && runner != null)
            {
                btnModeText.text = runner.CurrentMode == MissionMode.Operations
                    ? OutpostStrings.TrEn("Operasyon", "Operations")
                    : runner.CurrentMode == MissionMode.Demo
                        ? OutpostStrings.TrEn("90sn Demo", "90s Demo")
                        : OutpostStrings.TrEn("2-Sol Eğitim", "2-Sol Training");
                btnModeText.text += OutpostStrings.TrEn("\n(değiştir: sıfırla)", "\n(change: reset)");
            }

            if (btnCamResetText != null)
                btnCamResetText.text = OutpostStrings.GetCameraResetButton();

            UpdateDomeRoofButtonVisual();
            RefreshAudioControls();

            double panelHealth = SolarProduction.GetPanelHealthFactor(res, state.Facilities);
            string solarNote = state.IsNight
                ? OutpostStrings.TrEn("Gece: Güneş Yok (Batarya)", "Night: 0 Solar (Battery)")
                : OutpostStrings.TrEn($"Güneş: %{panelHealth * (1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0}", $"Solar: {panelHealth * (1 - res.PanelFouling) * (1 - res.AtmosphericDustLoss) * 100:0}%");
            string dustNote = res.AtmosphericDustLoss > 0
                ? OutpostStrings.TrEn($" | Toz -%{(res.AtmosphericDustLoss * 100):0}", $" | Dust -{(res.AtmosphericDustLoss * 100):0}%")
                : "";
            string researchNote = res.ResearchEnabled
                ? OutpostStrings.TrEn($" | Lab %{(res.LastResearchRateFactor * 100):0}", $" | Lab {(res.LastResearchRateFactor * 100):0}%")
                : OutpostStrings.TrEn(" | Lab KAPALI", " | Lab OFF");
            string overflowNote = res.BatteryOverflowLostTotal > 0
                ? OutpostStrings.TrEn($" | Taşma -{res.BatteryOverflowLostTotal:0.#}", $" | Overflow -{res.BatteryOverflowLostTotal:0.#}")
                : "";
            string modeTag = runner == null ? ""
                : runner.CurrentMode == MissionMode.Operations ? OutpostStrings.TrEn(" | Operasyon", " | Operations")
                : runner.CurrentMode == MissionMode.Demo ? " | Demo"
                : OutpostStrings.TrEn(" | Eğitim (2-Sol)", " | Training (2-Sol)");
            reserveNoteText.text = $"{solarNote}{dustNote}{researchNote}{overflowNote}{modeTag}";

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

                SurfaceJobState activeSJob = null;
                if (!string.IsNullOrEmpty(selected.ActiveSurfaceJobId) && state.SurfaceJobs != null)
                {
                    for (int j = 0; j < state.SurfaceJobs.Count; j++)
                    {
                        if (state.SurfaceJobs[j].JobId == selected.ActiveSurfaceJobId)
                        {
                            activeSJob = state.SurfaceJobs[j];
                            break;
                        }
                    }
                }
                if (activeSJob != null)
                {
                    string jobSummary = OutpostStrings.FormatSurfaceJobSummary(activeSJob);
                    selectedStatusText.text = jobSummary;
                    if (selectedStatsText != null)
                        selectedStatsText.text = $"{selectedLocationText.text} | {jobSummary}";
                }
                else
                {
                    if (selectedStatsText != null)
                        selectedStatsText.text = $"{selectedLocationText.text} | {selectedStatusText.text}";
                }
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
                    ? OutpostStrings.TrEn("[!] GÜNEŞ FIRTINASI AKTİF!\nSığınakta kal! (Şematik eğitim gösterimi)", "[!] SOLAR STORM ACTIVE!\nStay sheltered! (Schematic educational view)")
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
            materialsText = CreateText("MaterialsText", topBar, new Vector2(158, -34), new Vector2(176, 24), 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.96f, 0.84f, 0.42f));
            materialsText.gameObject.SetActive(false);

            // Center: Compact Resource Bars
            CreateCompactReserveMeter(topBar, new Vector2(340, -6), 140, out batteryText, out batteryFill);
            CreateCompactReserveMeter(topBar, new Vector2(490, -6), 140, out waterText, out waterFill);
            CreateCompactReserveMeter(topBar, new Vector2(640, -6), 150, out oxygenText, out oxygenFill);
            CreateCompactReserveMeter(topBar, new Vector2(800, -6), 150, out researchText, out researchFill);
            reserveNoteText = CreateText("ReserveNote", topBar, new Vector2(340, -38), new Vector2(610, 22), 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.94f));
            reserveNoteText.enabled = false;

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
                runner.CycleMode();
                if (settingsMenu != null && settingsMenu.IsOpen) runner.SetPaused(true);
            });

            CreatePillButton(topBar, "BtnLang", new Vector2(1270, -6), new Vector2(74, 26), out btnLangText, () =>
            {
                SetLanguage(!OutpostStrings.IsTurkish);
            }, new Color(0.26f, 0.34f, 0.48f, 1f));
            btnLangText.text = OutpostStrings.GetLangButton();

            CreatePillButton(topBar, "BtnPause", new Vector2(1350, -6), new Vector2(78, 26), out btnPauseText, () => runner.TogglePause());
            CreatePillButton(topBar, "BtnSources", new Vector2(1434, -6), new Vector2(96, 26), out btnSourcesText, () => ToggleSourcesModal(true));
            btnSourcesText.text = OutpostStrings.GetSourcesButton();

            btnGridToggle = CreatePillButton(topBar, "BtnGridToggle", new Vector2(1536, -6), new Vector2(96, 26), out btnGridToggleText, () =>
            {
                if (worldView != null) worldView.ToggleGrid();
            }, new Color(0.20f, 0.34f, 0.44f, 1f));
            btnGridToggleObj = btnGridToggle.gameObject;
            btnGridToggleBg = btnGridToggle.GetComponent<Image>();
            btnGridToggleText.text = OutpostStrings.GetGridToggleButton(false);
            btnGridToggleObj.SetActive(false);

            buildEntry = CreatePillButton(topBar, "BtnBuildMode", new Vector2(1536, -36), new Vector2(96, 24), out var buildEntryText, () =>
            {
                buildDrawerOpen = !buildDrawerOpen;
                systemsDrawerOpen = false; toolsOpen = false;
                if (toolsPanel != null) toolsPanel.SetActive(false);
                if (worldView != null)
                {
                    if (buildDrawerOpen) { worldView.SetGridVisible(true); worldView.ClearParcelSelection(); }
                    else worldView.CancelBuildMode();
                }
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
                RefreshEquipmentUi(runner != null ? runner.State : null);
                RefreshParcelSelectionUi(runner != null ? runner.State : null);
            }).gameObject;
            buildEntryText.text = OutpostStrings.TrEn("İnşa", "Build");
            systemsEntry = CreatePillButton(topBar, "BtnSystems", new Vector2(1640, -36), new Vector2(96, 24), out var systemsEntryText, () =>
            {
                systemsDrawerOpen = !systemsDrawerOpen;
                buildDrawerOpen = false; toolsOpen = false;
                if (toolsPanel != null) toolsPanel.SetActive(false);
                if (worldView != null) { worldView.CancelBuildMode(); worldView.ClearParcelSelection(); }
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
                RefreshEquipmentUi(runner != null ? runner.State : null);
            }).gameObject;
            systemsEntryText.text = OutpostStrings.TrEn("Sistemler", "Systems");
            CreatePillButton(topBar, "BtnTools", new Vector2(1744, -36), new Vector2(78, 24), out var toolsText, () =>
            {
                toolsOpen = !toolsOpen;
                if (toolsOpen)
                {
                    buildDrawerOpen = false; systemsDrawerOpen = false;
                    if (worldView != null) { worldView.CancelBuildMode(); worldView.ClearParcelSelection(); }
                    RefreshOperationsBuildUi(runner != null ? runner.State : null);
                    RefreshEquipmentUi(runner != null ? runner.State : null);
                }
                if (toolsPanel != null) toolsPanel.SetActive(toolsOpen);
                RefreshEarthOrdersUi(runner != null ? runner.State : null);
            });
            toolsText.text = OutpostStrings.TrEn("Araçlar", "Tools");

            // Right: Audio & Navigation Row 2 (y = -36, height = 24px)
            btnSaveOpsObj = CreatePillButton(topBar, "BtnSaveOps", new Vector2(1236, -36), new Vector2(58, 24), out btnSaveOpsText, () =>
            {
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, "operations_save.json");
                var res = runner.SaveOperationsToFile(savePath);
                if (res.Success && feedbackPanel != null && feedbackText != null)
                {
                    feedbackText.text = OutpostStrings.TrEn("Operasyon kaydedildi.", "Operations saved.");
                    feedbackPanel.SetActive(true);
                    feedbackTimer = 3.0f;
                }
            }, new Color(0.18f, 0.36f, 0.28f, 1f)).gameObject;
            btnSaveOpsText.text = OutpostStrings.TrEn("Kaydet", "Save");

            btnLoadOpsObj = CreatePillButton(topBar, "BtnLoadOps", new Vector2(1298, -36), new Vector2(58, 24), out btnLoadOpsText, () =>
            {
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, "operations_save.json");
                var res = runner.LoadOperationsFromFile(savePath);
                if (res.Success && feedbackPanel != null && feedbackText != null)
                {
                    feedbackText.text = OutpostStrings.TrEn("Kayıt yüklendi (Duraklatıldı).", "Save loaded (Paused).");
                    feedbackPanel.SetActive(true);
                    feedbackTimer = 3.0f;
                }
            }, new Color(0.24f, 0.30f, 0.44f, 1f)).gameObject;
            btnLoadOpsText.text = OutpostStrings.TrEn("Yükle", "Load");

            btnEarthOrdersObj = CreatePillButton(topBar, "BtnEarthOrders", new Vector2(1360, -36), new Vector2(134, 24), out btnEarthOrdersText, () => ToggleEarthOrdersModal(true), new Color(0.18f, 0.30f, 0.46f, 1f)).gameObject;
            btnEarthOrdersText.text = OutpostStrings.TrEn("Dünya Emirleri (0)", "Earth Orders (0)");

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

            cameraHintText = CreateText("CameraNavHint", topBar, new Vector2(1502, -37), new Vector2(400, 22), 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.72f, 0.82f, 0.92f));
            cameraHintText.text = OutpostStrings.TrEn("WASD: Gez | Tekerlek: Yakınlaştır | R: Sıfırla", "WASD: Pan | Wheel: Zoom | R: Reset");
            cameraHintText.enabled = false;
            RefreshAudioControls();

            // Auxiliary actions live in a single collapsible menu; audio is in Settings.
            var aux = CreateSlicedPanel("ToolsPanel", root, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-300, -330), new Vector2(-12, -68), CardBg, panelFrameSprite);
            toolsPanel = aux.gameObject;
            string[] toolNames = { "BtnSources", "BtnSaveOps", "BtnLoadOps", "BtnEarthOrders" };
            for (int i = 0; i < toolNames.Length; i++)
            {
                var child = topBar.Find(toolNames[i]);
                if (child == null) continue;
                var rect = child.GetComponent<RectTransform>();
                rect.SetParent(aux, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(10 + (i % 2) * 136, -10 - (i / 2) * 48);
                rect.sizeDelta = new Vector2(128, 40);
            }
            foreach (var name in new[] { "BtnMuteAudio", "BtnVolDown", "BtnVolUp", "VolumeText" })
            {
                var legacy = topBar.Find(name);
                if (legacy != null) legacy.gameObject.SetActive(false);
            }
            void PositionHeader(string name, float x, float width)
            {
                var rect = topBar.Find(name).GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(x, -12);
                rect.sizeDelta = new Vector2(width, 38);
            }
            PositionHeader("BtnPause", 1000, 110);
            PositionHeader("BtnBuildMode", 1120, 110);
            PositionHeader("BtnSystems", 1240, 120);
            PositionHeader("BtnTools", 1370, 110);
            toolsPanel.SetActive(false);

            // -------------------------------------------------------------
            // 2. LEFT PANEL (Width = 256px, between y = -74 and y = +104 to clear BottomCommandDock)
            // -------------------------------------------------------------
            var leftPanel = CreateSlicedPanel("LeftCrewPanel", root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 104), new Vector2(268, -74), PanelBg, panelFrameSprite);

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

            var equipmentRect = CreateSlicedPanel("EquipmentPanel", root, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -478), new Vector2(-12, -218), CardBg, panelFrameSprite);
            equipmentPanel = equipmentRect.gameObject;
            equipmentStatusText = CreateText("EquipmentStatus", equipmentRect, new Vector2(8, -6), new Vector2(262, 42),
                13, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            waterFlowBreakdownText = CreateText("WaterFlowBreakdown", equipmentRect, new Vector2(8, -48), new Vector2(262, 34),
                11, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.70f, 0.90f, 1.0f));
            panelRepairButton = CreatePillButton(equipmentRect, "BtnRepairPanel", new Vector2(8, -86), new Vector2(262, 28),
                out panelRepairText, () => AssignOrReplaceTask("panel-repair"), AccentAmber);
            oxygenRepairButton = CreatePillButton(equipmentRect, "BtnRepairOxygen", new Vector2(8, -120), new Vector2(262, 28),
                out oxygenRepairText, () => AssignOrReplaceTask("o2-repair"), AccentAmber);
            waterRepairButton = CreatePillButton(equipmentRect, "BtnRepairWater", new Vector2(8, -154), new Vector2(262, 28),
                out waterRepairText, () => runner.RepairWaterExtractor(), AccentBlue);
            extractorToggleButton = CreatePillButton(equipmentRect, "BtnToggleExtractor", new Vector2(8, -188), new Vector2(146, 30),
                out extractorToggleText, () => runner.SetWaterExtractorEnabled(!runner.State.Resources.WaterExtractorEnabled));
            btnFocusExtractor = CreatePillButton(equipmentRect, "BtnFocusExtractor", new Vector2(160, -188), new Vector2(110, 30),
                out btnFocusExtractorText, FocusPrimaryExtractorSite, new Color(0.18f, 0.36f, 0.50f, 1f));
            btnFocusExtractorText.fontSize = 11;
            btnRecallSurfaceJob = CreatePillButton(equipmentRect, "BtnRecallSurfaceJob", new Vector2(8, -224), new Vector2(262, 28),
                out btnRecallSurfaceJobText, RecallFirstActiveSurfaceCrew, new Color(0.46f, 0.24f, 0.16f, 1f));
            btnRecallSurfaceJobText.fontSize = 11;

            // Right-side Operations Parcel Selection Card (below EquipmentPanel, above BottomCommandDock)
            var parcelRect = CreateSlicedPanel("ParcelSelectionCard", root, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -764), new Vector2(-12, -486), CardBg, panelFrameSprite);
            parcelCardPanel = parcelRect.gameObject;
            parcelTitleText = CreateText("ParcelTitle", parcelRect, new Vector2(10, -6), new Vector2(214, 24),
                15, FontStyle.Bold, TextAnchor.MiddleLeft, AccentCyan);
            CreatePillButton(parcelRect, "BtnCloseParcel", new Vector2(236, -6), new Vector2(32, 24), out var closeParcelTxt, () =>
            {
                if (worldView != null) worldView.ClearParcelSelection();
                else OnWorldParcelSelected(null);
            });
            closeParcelTxt.text = "X";
            parcelDetailsText = CreateText("ParcelDetails", parcelRect, new Vector2(10, -30), new Vector2(258, 114),
                11, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.90f, 0.94f, 0.98f));
            parcelReasonText = CreateText("ParcelReason", parcelRect, new Vector2(10, -146), new Vector2(258, 36),
                12, FontStyle.Bold, TextAnchor.MiddleLeft, AccentAmber);
            btnClaimParcel = CreatePillButton(parcelRect, "BtnClaimParcel", new Vector2(10, -186), new Vector2(124, 34),
                out btnClaimParcelText, () =>
                {
                    if (runner != null && selectedParcel.HasValue)
                        runner.ClaimParcel(selectedParcel.Value);
                }, AccentGreen);
            btnClaimParcelBg = btnClaimParcel.GetComponent<Image>();
            btnSurfaceWalk = CreatePillButton(parcelRect, "BtnSurfaceWalk", new Vector2(142, -186), new Vector2(126, 34),
                out btnSurfaceWalkText, () =>
                {
                    if (runner != null && selectedParcel.HasValue && !string.IsNullOrEmpty(selectedCrewId))
                        runner.AssignSurfaceWork(selectedCrewId, selectedParcel.Value, workRequiredSeconds: 10.0);
                }, AccentBlue);
            btnSurfaceWalkBg = btnSurfaceWalk.GetComponent<Image>();
            btnBuildBlueprint = CreatePillButton(parcelRect, "BtnBuildBlueprint", new Vector2(10, -226), new Vector2(146, 36),
                out btnBuildBlueprintText, () =>
                {
                    if (runner != null && !string.IsNullOrEmpty(selectedCrewId) && !string.IsNullOrEmpty(selectedBlueprintId))
                        runner.AssignBuildBlueprint(selectedCrewId, selectedBlueprintId);
                }, AccentAmber);
            btnBuildBlueprintBg = btnBuildBlueprint.GetComponent<Image>();
            btnBuildBlueprintText.fontSize = 12;
            btnBuildBlueprint.gameObject.SetActive(false);

            btnCancelBlueprint = CreatePillButton(parcelRect, "BtnCancelBlueprint", new Vector2(162, -226), new Vector2(106, 36),
                out btnCancelBlueprintText, () =>
                {
                    if (runner != null && !string.IsNullOrEmpty(selectedBlueprintId))
                        runner.CancelBlueprint(selectedBlueprintId);
                }, new Color(0.55f, 0.20f, 0.18f, 1f));
            btnCancelBlueprintBg = btnCancelBlueprint.GetComponent<Image>();
            btnCancelBlueprintText.fontSize = 11;
            btnCancelBlueprint.gameObject.SetActive(false);

            btnRepairParcel = CreatePillButton(parcelRect, "BtnRepairParcel", new Vector2(10, -226), new Vector2(146, 36),
                out btnRepairParcelText, () =>
                {
                    if (runner != null && !string.IsNullOrEmpty(selectedCrewId) && !string.IsNullOrEmpty(selectedFaultFacilityId))
                        runner.AssignRepairPanel(selectedCrewId, selectedFaultFacilityId);
                }, AccentAmber);
            btnRepairParcelBg = btnRepairParcel.GetComponent<Image>();
            btnRepairParcelText.fontSize = 12;
            btnRepairParcel.gameObject.SetActive(false);

            btnDismantleFacility = CreatePillButton(parcelRect, "BtnDismantleFacility", new Vector2(10, -226), new Vector2(258, 36),
                out btnDismantleFacilityText, () =>
                {
                    if (runner != null && !string.IsNullOrEmpty(selectedDismantleFacilityId))
                        runner.DismantleFacility(selectedDismantleFacilityId);
                }, new Color(0.55f, 0.22f, 0.18f, 1f));
            btnDismantleFacilityBg = btnDismantleFacility.GetComponent<Image>();
            btnDismantleFacilityText.fontSize = 12;
            btnDismantleFacility.gameObject.SetActive(false);

            btnToggleSiteExtractor = CreatePillButton(parcelRect, "BtnToggleSiteExtractor", new Vector2(10, -226), new Vector2(146, 36),
                out btnToggleSiteExtractorText, () =>
                {
                    if (runner != null && !string.IsNullOrEmpty(selectedExtractorFacilityId))
                    {
                        var fac = FindFacilityById(runner.State, selectedExtractorFacilityId);
                        if (fac != null)
                            runner.SetFacilityEnabled(fac.FacilityId, !fac.IsEnabled);
                    }
                }, AccentBlue);
            btnToggleSiteExtractorBg = btnToggleSiteExtractor.GetComponent<Image>();
            btnToggleSiteExtractorText.fontSize = 11;
            btnToggleSiteExtractor.gameObject.SetActive(false);
            parcelCardPanel.SetActive(false);

            // Right-side Operations Build Catalog Panel (below ParcelSelectionCard, above BottomCommandDock)
            var buildRect = CreateSlicedPanel("OperationsBuildPanel", root, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-290, 102), new Vector2(-12, 348), CardBg, panelFrameSprite);
            operationsBuildPanel = buildRect.gameObject;
            var gridRect = btnGridToggle.GetComponent<RectTransform>();
            gridRect.SetParent(buildRect, false);
            gridRect.anchoredPosition = new Vector2(10, -208);
            gridRect.sizeDelta = new Vector2(124, 30);
            buildCatalogTitleText = CreateText("BuildCatalogTitle", buildRect, new Vector2(10, -6), new Vector2(258, 24),
                14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentCyan);
            btnBuildLine = CreatePillButton(buildRect, "BtnBuildLine", new Vector2(10, -34), new Vector2(61, 42),
                out btnBuildLineText, () =>
                {
                    if (worldView != null) worldView.StartBuildMode(FacilityKind.UtilityLine);
                }, ButtonBg);
            btnBuildLineBg = btnBuildLine.GetComponent<Image>();
            btnBuildLineText.fontSize = 10;

            btnBuildSolar = CreatePillButton(buildRect, "BtnBuildSolar", new Vector2(75, -34), new Vector2(61, 42),
                out btnBuildSolarText, () =>
                {
                    if (worldView != null) worldView.StartBuildMode(FacilityKind.SolarPanel);
                }, ButtonBg);
            btnBuildSolarBg = btnBuildSolar.GetComponent<Image>();
            btnBuildSolarText.fontSize = 10;

            btnBuildBattery = CreatePillButton(buildRect, "BtnBuildBattery", new Vector2(140, -34), new Vector2(61, 42),
                out btnBuildBatteryText, () =>
                {
                    if (worldView != null) worldView.StartBuildMode(FacilityKind.Battery);
                }, ButtonBg);
            btnBuildBatteryBg = btnBuildBattery.GetComponent<Image>();
            btnBuildBatteryText.fontSize = 10;

            btnBuildExtractor = CreatePillButton(buildRect, "BtnBuildExtractor", new Vector2(205, -34), new Vector2(61, 42),
                out btnBuildExtractorText, () =>
                {
                    if (worldView != null) worldView.StartBuildMode(FacilityKind.WaterExtractor);
                }, ButtonBg);
            btnBuildExtractorBg = btnBuildExtractor.GetComponent<Image>();
            btnBuildExtractorText.fontSize = 10;

            buildGhostInfoText = CreateText("BuildGhostInfo", buildRect, new Vector2(10, -82), new Vector2(258, 76),
                12, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);

            btnRotateGhost = CreatePillButton(buildRect, "BtnRotateGhost", new Vector2(10, -162), new Vector2(124, 34),
                out btnRotateGhostText, () =>
                {
                    if (worldView != null) worldView.RotateBuildGhost();
                }, ButtonBg);
            btnRotateGhostText.fontSize = 12;

            btnCancelGhost = CreatePillButton(buildRect, "BtnCancelGhost", new Vector2(144, -162), new Vector2(124, 34),
                out btnCancelGhostText, () =>
                {
                    if (worldView != null) worldView.CancelBuildMode();
                }, AccentRed);
            btnCancelGhostText.fontSize = 12;

            buildNoticeText = CreateText("BuildNotice", buildRect, new Vector2(10, -202), new Vector2(258, 38),
                11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.76f, 0.84f, 0.94f));
            operationsBuildPanel.SetActive(false);

            // Earth Orders Modal (Operations mode)
            var ordRect = CreateSlicedPanel("EarthOrdersModal", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -300), new Vector2(420, 300), new Color(0.07f, 0.09f, 0.13f, 0.98f), panelFrameSprite);
            earthOrdersModal = ordRect.gameObject;
            earthOrdersTitle = CreateText("EarthOrdersTitle", ordRect, new Vector2(24, -16), new Vector2(792, 40), 22, FontStyle.Bold, TextAnchor.MiddleCenter, AccentBlue);
            earthOrdersBody = CreateText("EarthOrdersBody", ordRect, new Vector2(24, -64), new Vector2(792, 230), 15, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            earthOrdersHistoryText = CreateText("EarthOrdersHistory", ordRect, new Vector2(24, -304), new Vector2(792, 160), 13, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.92f));
            CreatePillButton(ordRect, "BtnOrderPrev", new Vector2(24, -476), new Vector2(110, 40), out btnOrderPrevText, () =>
            {
                if (earthOrderPageIndex > 0) earthOrderPageIndex--;
                RefreshEarthOrdersUi(runner != null ? runner.State : null);
            });
            CreatePillButton(ordRect, "BtnOrderNext", new Vector2(142, -476), new Vector2(110, 40), out btnOrderNextText, () =>
            {
                earthOrderPageIndex++;
                RefreshEarthOrdersUi(runner != null ? runner.State : null);
            });
            CreatePillButton(ordRect, "BtnOrderAccept", new Vector2(276, -476), new Vector2(160, 40), out btnOrderAcceptText, () => ExecuteEarthOrderAction("accept"), AccentGreen);
            CreatePillButton(ordRect, "BtnOrderAssign", new Vector2(444, -476), new Vector2(190, 40), out btnOrderAssignText, () => ExecuteEarthOrderAction("assign"), AccentBlue);
            CreatePillButton(ordRect, "BtnOrderCancel", new Vector2(642, -476), new Vector2(174, 40), out btnOrderCancelText, () => ExecuteEarthOrderAction("cancel"), AccentRed);
            CreatePillButton(ordRect, "BtnCloseOrders", new Vector2(300, -536), new Vector2(240, 48), out btnOrderCloseText, () => ToggleEarthOrdersModal(false));
            earthOrdersModal.SetActive(false);
        }

        public bool CloseContextDrawer()
        {
            if (earthOrdersModal != null && earthOrdersModal.activeSelf) { ToggleEarthOrdersModal(false); return true; }
            if (!buildDrawerOpen && !systemsDrawerOpen && !toolsOpen) return false;
            buildDrawerOpen = false; systemsDrawerOpen = false; toolsOpen = false;
            if (toolsPanel != null) toolsPanel.SetActive(false);
            RefreshOperationsBuildUi(runner != null ? runner.State : null);
            RefreshEquipmentUi(runner != null ? runner.State : null);
            RefreshParcelSelectionUi(runner != null ? runner.State : null);
            RefreshEarthOrdersUi(runner != null ? runner.State : null);
            return true;
        }

        private void ToggleEarthOrdersModal(bool open)
        {
            if (earthOrdersModal == null) return;
            if (open)
            {
                buildDrawerOpen = false; systemsDrawerOpen = false;
                if (worldView != null) { worldView.CancelBuildMode(); worldView.ClearParcelSelection(); }
                RefreshOperationsBuildUi(runner != null ? runner.State : null);
                RefreshEquipmentUi(runner != null ? runner.State : null);
            }
            earthOrdersModal.SetActive(open);
            MarsOutpostCameraController.IsMenuBlockingCameraInput = open;
            if (open) RefreshEarthOrdersUi(runner != null ? runner.State : null);
            UpdatePauseOverlay(runner != null ? runner.State : null);
        }

        private void ExecuteEarthOrderAction(string action)
        {
            if (runner == null || runner.Simulation == null || runner.State == null) return;
            var orders = runner.State.EarthOrders;
            if (orders == null || orders.Count == 0) return;
            int idx = Mathf.Clamp(earthOrderPageIndex, 0, orders.Count - 1);
            var order = orders[idx];
            if (action == "accept") runner.AcceptEarthOrder(order.OrderId);
            else if (action == "cancel") runner.CancelEarthOrder(order.OrderId);
            else if (action == "assign") runner.AssignEarthOrder(order.OrderId, selectedCrewId);
            RefreshEarthOrdersUi(runner.State);
        }

        private void RefreshEarthOrdersUi(MissionState state)
        {
            bool isOps = state != null && state.Mode == MissionMode.Operations;
            if (btnEarthOrdersObj != null) btnEarthOrdersObj.SetActive(isOps && toolsOpen);
            if (btnSaveOpsObj != null) btnSaveOpsObj.SetActive(isOps && toolsOpen);
            if (btnLoadOpsObj != null) btnLoadOpsObj.SetActive(isOps && toolsOpen);
            if (buildEntry != null) buildEntry.SetActive(isOps);
            if (systemsEntry != null) systemsEntry.SetActive(isOps);
            if (btnSaveOpsText != null) btnSaveOpsText.text = OutpostStrings.TrEn("Kaydet", "Save");
            if (btnLoadOpsText != null) btnLoadOpsText.text = OutpostStrings.TrEn("Yükle", "Load");
            if (!isOps)
            {
                if (earthOrdersModal != null && earthOrdersModal.activeSelf) ToggleEarthOrdersModal(false);
                return;
            }

            int total = state.EarthOrders != null ? state.EarthOrders.Count : 0;
            int pending = 0;
            if (state.EarthOrders != null)
            {
                for (int i = 0; i < state.EarthOrders.Count; i++)
                {
                    var st = state.EarthOrders[i].Status;
                    if (st == EarthOrderStatus.Received || st == EarthOrderStatus.Accepted || st == EarthOrderStatus.InProgress)
                        pending++;
                }
            }

            if (total != lastKnownEarthOrderCount)
            {
                lastKnownEarthOrderCount = total;
            }

            if (btnEarthOrdersText != null)
                btnEarthOrdersText.text = OutpostStrings.TrEn($"Dünya Emirleri ({pending})", $"Earth Orders ({pending})");

            if (earthOrdersModal == null || !earthOrdersModal.activeSelf) return;

            if (earthOrdersTitle != null)
                earthOrdersTitle.text = OutpostStrings.TrEn("DÜNYA OPERASYON EMİRLERİ (Bilim Ödülü: 0)", "EARTH OPERATIONS ORDERS (Science Reward: 0)");
            if (btnOrderPrevText != null) btnOrderPrevText.text = OutpostStrings.TrEn("Önceki", "Prev");
            if (btnOrderNextText != null) btnOrderNextText.text = OutpostStrings.TrEn("Sonraki", "Next");
            if (btnOrderAcceptText != null) btnOrderAcceptText.text = OutpostStrings.TrEn("Kabul Et", "Accept");
            if (btnOrderAssignText != null) btnOrderAssignText.text = OutpostStrings.TrEn($"Ata ({selectedCrewId})", $"Assign ({selectedCrewId})");
            if (btnOrderCancelText != null) btnOrderCancelText.text = OutpostStrings.TrEn("İptal Et", "Cancel");
            if (btnOrderCloseText != null) btnOrderCloseText.text = OutpostStrings.TrEn("Kapat", "Close");

            if (total == 0)
            {
                if (earthOrdersBody != null)
                    earthOrdersBody.text = OutpostStrings.TrEn(
                        "Henüz Dünya emri ulaşmadı. İlk emir Sol 3 başında (ardından her 2 sol) ulaşır.",
                        "No Earth orders arrived yet. First order arrives at Sol 3 start (then every 2 sols).");
                if (earthOrdersHistoryText != null) earthOrdersHistoryText.text = "";
                return;
            }

            earthOrderPageIndex = Mathf.Clamp(earthOrderPageIndex, 0, total - 1);
            var cur = state.EarthOrders[earthOrderPageIndex];
            if (earthOrdersBody != null)
            {
                earthOrdersBody.text = OutpostStrings.TrEn(
                    $"Emir {earthOrderPageIndex + 1}/{total}: {cur.Code} ({cur.OrderId})\n" +
                    $"Başlık: {GetOrderTitle(cur)}\n" +
                    $"Durum: {cur.Status} | Varış: Sol {cur.ArrivalSol} | Önerilen Hedef: Sol {cur.AdvisoryDeadlineSol}\n" +
                    $"Aşama {cur.CurrentStepIndex + 1}/{cur.StepCount} ({OutpostStrings.FormatLocation(cur.CurrentStep.Location)}): {cur.StepProgressSeconds:0.#} / {cur.StepRequiredSeconds:0.#} sn\n" +
                    "Dış aşamadan sonra Sığınağa Dön, ardından sonraki aşamayı ata. Bilim ödülü: 0.",
                    $"Order {earthOrderPageIndex + 1}/{total}: {cur.Code} ({cur.OrderId})\n" +
                    $"Title: {cur.Title}\n" +
                    $"Status: {cur.Status} | Arrival: Sol {cur.ArrivalSol} | Advisory Deadline: Sol {cur.AdvisoryDeadlineSol}\n" +
                    $"Step {cur.CurrentStepIndex + 1}/{cur.StepCount} ({cur.CurrentStep.Location}): {cur.StepProgressSeconds:0.#} / {cur.StepRequiredSeconds:0.#} s\n" +
                    "After exterior work, Return to Shelter, then assign the next stage. Science reward: 0.");
            }

            if (earthOrdersHistoryText != null)
            {
                var lines = new System.Text.StringBuilder();
                lines.AppendLine(OutpostStrings.TrEn("Son Tamamlanan / İptal Edilen Emirler:", "Recent Completed / Cancelled Orders:"));
                int shown = 0;
                for (int i = state.EarthOrders.Count - 1; i >= 0 && shown < 5; i--)
                {
                    var o = state.EarthOrders[i];
                    if (o.Status == EarthOrderStatus.Completed || o.Status == EarthOrderStatus.Cancelled)
                    {
                        lines.AppendLine($"- {o.Code} [{o.Status}]: {o.CompletionSummary}");
                        shown++;
                    }
                }
                if (shown == 0) lines.AppendLine("-");
                earthOrdersHistoryText.text = lines.ToString();
            }
        }

        private static string GetOrderTitle(EarthOrderState order)
        {
            if (!OutpostStrings.IsTurkish) return order.Title;
            switch (order.Kind)
            {
                case EarthOrderKind.SoilSample: return "Toprak örneği topla ve incele";
                case EarthOrderKind.OxygenExperiment: return "Oksijen üretimiyle özel deney yap";
                case EarthOrderKind.RadiationSensorCalibration: return "Dış radyasyon sensörünü kalibre et";
                case EarthOrderKind.ThermalInsulationCheck: return "Habitatın ısı yalıtımını kontrol et";
                case EarthOrderKind.WaterQualityAnalysis: return "Su kalitesi numunesini analiz et";
                case EarthOrderKind.AntennaAlignment: return "Haberleşme antenini hizala";
                default: return "Acil durum stoklarını denetle";
            }
        }

        private void RefreshEquipmentUi(MissionState state)
        {
            if (equipmentPanel == null) return;
            bool isOps = state != null && state.Mode == MissionMode.Operations;
            equipmentPanel.SetActive(isOps && systemsDrawerOpen);
            if (!isOps) return;
            var r = state.Resources;
            int totalPanels = r.Panels != null ? r.Panels.Count : 0;
            int faultedPanels = 0;
            int onlineHealthy = 0;
            if (r.Panels != null)
            {
                for (int i = 0; i < r.Panels.Count; i++)
                {
                    var p = r.Panels[i];
                    if (p.IsFaulted)
                        faultedPanels++;
                    else if (SolarProduction.IsPanelOnline(p, state.Facilities))
                        onlineHealthy++;
                }
            }
            string targetNote = OutpostStrings.FormatActivePanelFaultSummary(state);
            equipmentStatusText.text = OutpostStrings.TrEn(
                $"SİSTEMLER | Panel: {onlineHealthy}/{totalPanels}{targetNote}\nO₂: %{(r.OxygenProducerFaulted ? 50 : 100)} | Su: %{(r.WaterExtractorFaulted ? 50 : 100)}",
                $"SYSTEMS | Panels: {onlineHealthy}/{totalPanels}{targetNote}\nO2: {(r.OxygenProducerFaulted ? 50 : 100)}% | Water: {(r.WaterExtractorFaulted ? 50 : 100)}%");
            if (waterFlowBreakdownText != null)
                waterFlowBreakdownText.text = OutpostStrings.FormatWaterBreakdownSummary(state);
            panelRepairText.text = OutpostStrings.TrEn("Arızalı Paneli Tamir Et · 60sn (Kir Değil)", "Repair Faulted Panel · Outside / 60s");
            oxygenRepairText.text = OutpostStrings.TrEn("O₂ Tamir · 20 Enerji / 40sn", "O2 Repair · 20 Energy / 40s");
            waterRepairText.text = OutpostStrings.TrEn("Su Çıkarıcıyı Düzelt · 10 Bilim", "Repair Water Extractor · 10 Science");
            extractorToggleText.text = r.WaterExtractorEnabled ? OutpostStrings.TrEn("Ana Su: AÇIK", "Master H2O: ON") : OutpostStrings.TrEn("Ana Su: KAPALI", "Master H2O: OFF");
            bool active = state.Outcome == MissionOutcome.InProgress;
            panelRepairButton.interactable = active && faultedPanels > 0;
            oxygenRepairButton.interactable = active && r.OxygenProducerFaulted;
            waterRepairButton.interactable = active && r.WaterExtractorFaulted;
            extractorToggleButton.interactable = active;

            bool hasSiteExtractor = state.Facilities != null && HasAnyWaterExtractorFacility(state);
            if (btnFocusExtractor != null && btnFocusExtractorText != null)
            {
                btnFocusExtractorText.text = hasSiteExtractor
                    ? OutpostStrings.TrEn("Tesise Odak", "Focus Site")
                    : OutpostStrings.TrEn("Buz Su Yok", "No H2O Site");
                btnFocusExtractor.interactable = hasSiteExtractor && state.Mode == MissionMode.Operations;
            }
            if (btnRecallSurfaceJob != null && btnRecallSurfaceJobText != null)
            {
                string busyCrewId = FindActiveSurfaceCrewId(state);
                bool canRecall = active && !string.IsNullOrEmpty(busyCrewId);
                btnRecallSurfaceJob.interactable = canRecall;
                btnRecallSurfaceJobText.text = canRecall
                    ? OutpostStrings.TrEn($"Yüzey Ekibini Geri Çağır ({busyCrewId})", $"Recall Surface Crew ({busyCrewId})")
                    : OutpostStrings.TrEn("Aktif Yüzey İşi Yok", "No Active Surface Job");
            }
        }

        private static bool HasAnyWaterExtractorFacility(MissionState state)
        {
            if (state?.Facilities == null) return false;
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                var f = state.Facilities[i];
                if (f != null && !f.IsProtectedStarter &&
                    FacilityCatalog.TryGetSpec(f.Kind, out var spec) && spec.WaterProductionRate > 0)
                    return true;
            }
            return false;
        }

        private static string FindActiveSurfaceCrewId(MissionState state)
        {
            if (state?.Crew == null || state.SurfaceJobs == null || state.SurfaceJobs.Count == 0)
                return null;
            for (int i = 0; i < state.Crew.Count; i++)
            {
                var c = state.Crew[i];
                if (!string.IsNullOrEmpty(c.ActiveSurfaceJobId))
                    return c.Id;
            }
            return null;
        }

        private void FocusPrimaryExtractorSite()
        {
            var state = runner?.State;
            if (state?.Facilities == null || worldView == null)
                return;

            var extractors = new List<FacilityState>();
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                var f = state.Facilities[i];
                if (f != null && !f.IsProtectedStarter &&
                    FacilityCatalog.TryGetSpec(f.Kind, out var spec) && spec.WaterProductionRate > 0)
                    extractors.Add(f);
            }
            if (extractors.Count == 0)
                return;

            var chosen = extractors[focusExtractorCycleIndex % extractors.Count];
            focusExtractorCycleIndex++;
            if (!worldView.IsGridVisible)
                worldView.SetGridVisible(true);
            worldView.SelectParcel(chosen.Origin);
            if (cameraController == null) cameraController = FindFirstObjectByType<MarsOutpostCameraController>();
            if (cameraController != null && worldView.TryGetParcelWorldCenter(chosen.Origin, out var wp))
                cameraController.FocusWorldPoint(wp, immediate: false);
        }

        private void RecallFirstActiveSurfaceCrew()
        {
            var state = runner?.State;
            string crewId = FindActiveSurfaceCrewId(state);
            if (runner != null && !string.IsNullOrEmpty(crewId))
                runner.ReturnToShelter(crewId);
        }

        private static FacilityState FindFacilityById(MissionState state, string facilityId)
        {
            if (state?.Facilities == null || string.IsNullOrEmpty(facilityId))
                return null;
            for (int i = 0; i < state.Facilities.Count; i++)
                if (state.Facilities[i].FacilityId == facilityId)
                    return state.Facilities[i];
            return null;
        }

        private void RefreshParcelSelectionUi(MissionState state)
        {
            if (parcelCardPanel == null) return;
            ParcelState cell = null;
            bool valid = state != null && state.Mode == MissionMode.Operations && state.Map != null && selectedParcel.HasValue
                && state.Map.TryGetCell(selectedParcel.Value, out cell);
            parcelCardPanel.SetActive(valid && !buildDrawerOpen && !systemsDrawerOpen);
            if (!valid || cell == null)
            {
                selectedBlueprintId = null;
                selectedFaultFacilityId = null;
                if (btnBuildBlueprint != null) btnBuildBlueprint.gameObject.SetActive(false);
                if (btnRepairParcel != null) btnRepairParcel.gameObject.SetActive(false);
                return;
            }

            GridCoord coord = selectedParcel.Value;
            parcelTitleText.text = OutpostStrings.TrEn($"PARSEL ({coord.X}, {coord.Z})", $"PARCEL ({coord.X}, {coord.Z})");

            string terrainStr = OutpostStrings.FormatParcelTerrain(cell.Terrain);
            string walkStr = cell.IsPassable ? OutpostStrings.TrEn("Açık", "Passable") : OutpostStrings.TrEn("Kapalı", "Blocked");
            string exploredStr = cell.IsExplored ? OutpostStrings.TrEn("Keşfedildi", "Explored") : OutpostStrings.TrEn("Keşifsiz", "Unexplored");
            string claimStr = cell.Protection != StarterProtectionKind.None
                ? OutpostStrings.TrEn("Başlangıç Tesisi", "Starter Facility")
                : (cell.IsClaimed ? OutpostStrings.TrEn("Tahsisli", "Claimed") : OutpostStrings.TrEn("Tahsissiz", "Unclaimed"));
            string lineStr = cell.HasUtilityLine
                ? OutpostStrings.TrEn("Var (Kablo)", "Present (Line)")
                : OutpostStrings.TrEn("Yok", "None");

            // Never leak unscanned geology or unexplored solar factor!
            string solarLine = cell.IsExplored
                ? OutpostStrings.TrEn($"Güneş Katsayısı: %{cell.SolarFactor * 100:0}", $"Solar Factor: {cell.SolarFactor * 100:0}%")
                : OutpostStrings.TrEn("Güneş Katsayısı: Bilinmiyor", "Solar Factor: Unknown");
            string resourceLine = OutpostStrings.TrEn("Kaynak: ", "Resource: ") + OutpostStrings.FormatParcelResource(cell);

            FacilityState primaryFac = FindFacilityById(state, cell.FacilityId);
            FacilityState utilBpFac = FindFacilityById(state, cell.UtilityBlueprintId);
            FacilityState bpFac = (utilBpFac != null && utilBpFac.LifecycleStatus == FacilityLifecycleStatus.Blueprint)
                ? utilBpFac
                : (primaryFac != null && primaryFac.LifecycleStatus == FacilityLifecycleStatus.Blueprint ? primaryFac : null);
            FacilityState shownFac = bpFac ?? primaryFac ?? utilBpFac;

            string facLine = "";
            if (shownFac != null)
            {
                string facName = OutpostStrings.FormatFacilityName(shownFac.Kind);
                string statusStr = OutpostStrings.FormatFacilityLifecycle(shownFac.LifecycleStatus);
                if (shownFac.LifecycleStatus == FacilityLifecycleStatus.Blueprint)
                {
                    double pct = shownFac.WorkRequiredSeconds > 0
                        ? Math.Min(100.0, (shownFac.WorkProgressSeconds / shownFac.WorkRequiredSeconds) * 100.0)
                        : 0.0;
                    string cancelNotice = OutpostStrings.FormatCancelBlueprintNotice(shownFac);
                    facLine = OutpostStrings.TrEn(
                        $"\nYapı: {facName} [{statusStr} %{pct:0}]\nİlerleme: {shownFac.WorkProgressSeconds:0.#}/{shownFac.WorkRequiredSeconds:0.#} sn | Enerji: {shownFac.EnergySpentTotal:0.#}/{shownFac.EnergyRequiredTotal:0.#}\n{cancelNotice}",
                        $"\nStruct: {facName} [{statusStr} {pct:0}%]\nWork: {shownFac.WorkProgressSeconds:0.#}/{shownFac.WorkRequiredSeconds:0.#}s | Energy: {shownFac.EnergySpentTotal:0.#}/{shownFac.EnergyRequiredTotal:0.#}\n{cancelNotice}");
                }
                else
                {
                    string connStr = OutpostStrings.FormatFacilityConnectionStatus(shownFac, state.Map, coord);
                    double solarRate = runner?.Simulation?.Config?.Resources?.SolarRate ?? 0.30;
                    double baseBatCap = runner?.Simulation?.Config?.Resources?.BatteryCapacity ?? 100.0;
                    string prodStr = OutpostStrings.FormatSolarFacilityContribution(state, shownFac, solarRate);
                    string capStr = OutpostStrings.FormatBatteryCapacityContribution(state, shownFac, baseBatCap);
                    string extStr = OutpostStrings.FormatExtractorSiteSummary(state, shownFac, cell);
                    string extraStr = !string.IsNullOrEmpty(prodStr) ? prodStr : (!string.IsNullOrEmpty(capStr) ? capStr : extStr);
                    string prodSuffix = string.IsNullOrEmpty(extraStr) ? "" : $"\n{extraStr}";
                    string dismantlePreview = !shownFac.IsProtectedStarter && runner != null
                        ? OutpostStrings.FormatDismantlePreview(state, shownFac, runner.Simulation?.Config?.Resources, runner.CanDismantleFacility(shownFac.FacilityId))
                        : "";
                    string dismantleSuffix = string.IsNullOrEmpty(dismantlePreview) ? "" : $"\n{dismantlePreview}";
                    facLine = OutpostStrings.TrEn(
                        $"\nYapı: {facName} [{statusStr}]\nAğ: {connStr}{prodSuffix}{dismantleSuffix}",
                        $"\nStruct: {facName} [{statusStr}]\nNet: {connStr}{prodSuffix}{dismantleSuffix}");
                }
            }

            parcelDetailsText.text = OutpostStrings.TrEn(
                $"Zemin: {terrainStr} | Geçiş: {walkStr} | {exploredStr}\nTahsis: {claimStr} | Hat: {lineStr}\n{solarLine}\n{resourceLine}{facLine}",
                $"Terrain: {terrainStr} | Walk: {walkStr} | {exploredStr}\nClaim: {claimStr} | Line: {lineStr}\n{solarLine}\n{resourceLine}{facLine}");

            PanelUnitState faultedPanelOnFac = null;
            if (bpFac == null && shownFac != null && shownFac.LifecycleStatus == FacilityLifecycleStatus.Commissioned && state.Resources?.Panels != null)
            {
                for (int i = 0; i < state.Resources.Panels.Count; i++)
                {
                    var p = state.Resources.Panels[i];
                    if (!p.IsFaulted) continue;
                    bool owns = p.FacilityId == shownFac.FacilityId ||
                        (shownFac.LinkedPanelIds != null && shownFac.LinkedPanelIds.Contains(p.Id));
                    if (owns)
                    {
                        faultedPanelOnFac = p;
                        break;
                    }
                }
            }

            if (cell.IsClaimed || cell.Protection != StarterProtectionKind.None)
            {
                if (bpFac != null)
                {
                    string assignedWorker = null;
                    SurfaceJobPhase assignedPhase = SurfaceJobPhase.Travel;
                    if (state.SurfaceJobs != null)
                    {
                        for (int j = 0; j < state.SurfaceJobs.Count; j++)
                        {
                            var sj = state.SurfaceJobs[j];
                            if (sj.Kind == SurfaceJobKind.Construct && sj.TargetId == bpFac.FacilityId &&
                                sj.Phase != SurfaceJobPhase.Completed && !string.IsNullOrEmpty(sj.AssignedCrewId))
                            {
                                assignedWorker = sj.AssignedCrewId;
                                assignedPhase = sj.Phase;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(assignedWorker))
                    {
                        string phaseTr = assignedPhase == SurfaceJobPhase.Travel ? "Yolda" : "İnşa Ediyor";
                        string phaseEn = assignedPhase == SurfaceJobPhase.Travel ? "Traveling" : "Building";
                        parcelReasonText.text = OutpostStrings.TrEn($"Durum: {assignedWorker} ({phaseTr})", $"Status: {assignedWorker} ({phaseEn})");
                        parcelReasonText.color = AccentBlue;
                    }
                    else
                    {
                        parcelReasonText.text = OutpostStrings.TrEn("Taslak inşaata hazır (Ekip ata).", "Blueprint ready for crew construction.");
                        parcelReasonText.color = AccentAmber;
                    }
                }
                else if (faultedPanelOnFac != null)
                {
                    parcelReasonText.text = OutpostStrings.TrEn(
                        $"Arızalı Panel ({faultedPanelOnFac.Id}): {faultedPanelOnFac.RepairProgressSeconds:0.#}/{PanelUnitState.DefaultRepairSeconds:0}sn tamir gerekli.",
                        $"Faulted Panel ({faultedPanelOnFac.Id}): {faultedPanelOnFac.RepairProgressSeconds:0.#}/{PanelUnitState.DefaultRepairSeconds:0}s repair needed.");
                    parcelReasonText.color = AccentAmber;
                }
                else if (shownFac != null && shownFac.LifecycleStatus == FacilityLifecycleStatus.Commissioned && !shownFac.IsConnectedToCore)
                {
                    parcelReasonText.text = OutpostStrings.TrEn(
                        "Bağlantı Yok: Merkeze 4-yönlü hat gerekli (Üretim: 0).",
                        "Not Connected: Needs 4-way line to core (Output: 0).");
                    parcelReasonText.color = AccentAmber;
                }
                else
                {
                    parcelReasonText.text = OutpostStrings.TrEn("Tahsis tamamlandı (Malzeme stok: " + state.Materials + ").", "Claimed (Materials: " + state.Materials + ").");
                    parcelReasonText.color = AccentGreen;
                }
                btnClaimParcelText.text = OutpostStrings.TrEn("Tahsis Edildi", "Already Claimed");
                btnClaimParcel.interactable = false;
                if (btnClaimParcelBg != null) btnClaimParcelBg.color = ButtonBg;
            }
            else
            {
                string failReason = OutpostStrings.FormatClaimFailureReason(state.Map, state, coord);
                btnClaimParcelText.text = OutpostStrings.TrEn(
                    $"Tahsis Et ({MissionSimulation.ClaimParcelCost} Malzeme)",
                    $"Claim Parcel ({MissionSimulation.ClaimParcelCost} Mat)");
                btnClaimParcel.interactable = state.Outcome == MissionOutcome.InProgress;
                if (string.IsNullOrEmpty(failReason))
                {
                    parcelReasonText.text = OutpostStrings.TrEn(
                        $"Hazır ({MissionSimulation.ClaimParcelCost} malzeme, Stok: {state.Materials}).",
                        $"Ready ({MissionSimulation.ClaimParcelCost} mat, Stock: {state.Materials}).");
                    parcelReasonText.color = AccentGreen;
                    if (btnClaimParcelBg != null) btnClaimParcelBg.color = AccentGreen;
                }
                else
                {
                    parcelReasonText.text = OutpostStrings.TrEn($"Gerekçe: {failReason}", $"Reason: {failReason}");
                    parcelReasonText.color = AccentAmber;
                    if (btnClaimParcelBg != null) btnClaimParcelBg.color = new Color(0.42f, 0.24f, 0.14f, 1f);
                }
            }

            if (btnSurfaceWalk != null && btnSurfaceWalkText != null)
            {
                bool canWalk = state.Outcome == MissionOutcome.InProgress && !string.IsNullOrEmpty(selectedCrewId) &&
                    GridNav.TryFindBestWorkCell(state.Map, ParcelMap.AirlockCorridorCoord, coord, allowTargetCell: cell.IsPassable, out _, out _);
                btnSurfaceWalk.interactable = canWalk;
                btnSurfaceWalkText.text = OutpostStrings.TrEn("Ekibi Gönder", "Send Crew");
                if (btnSurfaceWalkBg != null)
                    btnSurfaceWalkBg.color = canWalk ? AccentBlue : ButtonBg;
            }

            if (btnBuildBlueprint != null && btnBuildBlueprintText != null)
            {
                if (bpFac != null)
                {
                    selectedBlueprintId = bpFac.FacilityId;
                    btnBuildBlueprint.gameObject.SetActive(true);
                    bool canBuild = state.Outcome == MissionOutcome.InProgress && !string.IsNullOrEmpty(selectedCrewId);
                    btnBuildBlueprint.interactable = canBuild;
                    btnBuildBlueprintText.text = bpFac.WorkProgressSeconds > 0
                        ? OutpostStrings.TrEn("İnşayı Sürdür (Ekip)", "Resume Build (Crew)")
                        : OutpostStrings.TrEn("İnşa Et (Ekip)", "Build Blueprint (Crew)");
                    if (btnBuildBlueprintBg != null)
                        btnBuildBlueprintBg.color = canBuild ? AccentAmber : ButtonBg;
                }
                else
                {
                    selectedBlueprintId = null;
                    btnBuildBlueprint.gameObject.SetActive(false);
                }
            }

            if (btnCancelBlueprint != null && btnCancelBlueprintText != null)
            {
                if (bpFac != null && runner != null)
                {
                    btnCancelBlueprint.gameObject.SetActive(true);
                    var canCancel = runner.CanCancelBlueprint(bpFac.FacilityId);
                    btnCancelBlueprint.interactable = state.Outcome == MissionOutcome.InProgress && canCancel.Success;
                    btnCancelBlueprintText.text = OutpostStrings.TrEn("Taslağı İptal\n(0 İade)", "Cancel BP\n(0 Refund)");
                    if (btnCancelBlueprintBg != null)
                        btnCancelBlueprintBg.color = canCancel.Success ? new Color(0.62f, 0.22f, 0.18f, 1f) : ButtonBg;
                }
                else
                {
                    btnCancelBlueprint.gameObject.SetActive(false);
                }
            }

            bool hasFaultRepairRow = bpFac == null && faultedPanelOnFac != null && shownFac != null;
            if (btnRepairParcel != null && btnRepairParcelText != null)
            {
                if (hasFaultRepairRow)
                {
                    selectedFaultFacilityId = shownFac.FacilityId;
                    btnRepairParcel.gameObject.SetActive(true);
                    bool canRepair = state.Outcome == MissionOutcome.InProgress && !string.IsNullOrEmpty(selectedCrewId);
                    btnRepairParcel.interactable = canRepair;
                    btnRepairParcelText.text = faultedPanelOnFac.RepairProgressSeconds > 0
                        ? OutpostStrings.TrEn("Tamiri Sürdür (Ekip)", "Resume Repair (Crew)")
                        : OutpostStrings.TrEn("Paneli Tamir Et (Ekip)", "Repair Panel (Crew)");
                    if (btnRepairParcelBg != null)
                        btnRepairParcelBg.color = canRepair ? AccentAmber : ButtonBg;
                }
                else
                {
                    selectedFaultFacilityId = null;
                    btnRepairParcel.gameObject.SetActive(false);
                }
            }

            bool hasSiteToggleRow = bpFac == null && !hasFaultRepairRow && shownFac != null &&
                shownFac.LifecycleStatus == FacilityLifecycleStatus.Commissioned && !shownFac.IsProtectedStarter &&
                runner != null && FacilityCatalog.TryGetSpec(shownFac.Kind, out var siteToggleSpec) &&
                siteToggleSpec.WaterProductionRate > 0;
            if (btnToggleSiteExtractor != null && btnToggleSiteExtractorText != null)
            {
                if (hasSiteToggleRow)
                {
                    selectedExtractorFacilityId = shownFac.FacilityId;
                    btnToggleSiteExtractor.gameObject.SetActive(true);
                    btnToggleSiteExtractor.interactable = state.Outcome == MissionOutcome.InProgress;
                    btnToggleSiteExtractorText.text = shownFac.IsEnabled
                        ? OutpostStrings.TrEn("Saha: AÇIK (Kapat)", "Site: ON (Pause)")
                        : OutpostStrings.TrEn("Saha: KAPALI (Aç)", "Site: OFF (Resume)");
                    if (btnToggleSiteExtractorBg != null)
                        btnToggleSiteExtractorBg.color = shownFac.IsEnabled ? AccentGreen : ButtonBg;
                }
                else
                {
                    selectedExtractorFacilityId = null;
                    btnToggleSiteExtractor.gameObject.SetActive(false);
                }
            }

            bool splitBottomRow = hasFaultRepairRow || hasSiteToggleRow;
            if (btnDismantleFacility != null && btnDismantleFacilityText != null)
            {
                if (bpFac == null && shownFac != null && shownFac.LifecycleStatus == FacilityLifecycleStatus.Commissioned &&
                    !shownFac.IsProtectedStarter && runner != null)
                {
                    selectedDismantleFacilityId = shownFac.FacilityId;
                    btnDismantleFacility.gameObject.SetActive(true);
                    var dismantleRt = btnDismantleFacility.GetComponent<RectTransform>();
                    if (dismantleRt != null)
                    {
                        dismantleRt.anchoredPosition = new Vector2(splitBottomRow ? 162f : 10f, -226f);
                        dismantleRt.sizeDelta = new Vector2(splitBottomRow ? 106f : 258f, 36f);
                    }
                    var canDismantle = runner.CanDismantleFacility(shownFac.FacilityId);
                    FacilityPlacementValidator.PreviewDismantleRefund(
                        shownFac, state.Materials, state.MaterialCapacity, out int appliedRef, out _, out _);
                    btnDismantleFacility.interactable = state.Outcome == MissionOutcome.InProgress && canDismantle.Success;
                    btnDismantleFacilityText.text = splitBottomRow
                        ? OutpostStrings.TrEn($"Sök (+{appliedRef})", $"Dismantle (+{appliedRef})")
                        : OutpostStrings.TrEn($"Tesisi Sök (+{appliedRef} Malz.)", $"Dismantle Facility (+{appliedRef} Mat)");
                    if (btnDismantleFacilityBg != null)
                        btnDismantleFacilityBg.color = canDismantle.Success ? new Color(0.62f, 0.24f, 0.18f, 1f) : ButtonBg;
                }
                else
                {
                    selectedDismantleFacilityId = null;
                    btnDismantleFacility.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshOperationsBuildUi(MissionState state)
        {
            if (operationsBuildPanel == null) return;
            bool isOps = state != null && state.Mode == MissionMode.Operations && state.Map != null;
            operationsBuildPanel.SetActive(isOps && buildDrawerOpen);
            if (!isOps || !buildDrawerOpen) return;

            int bpCount = 0;
            if (state.Facilities != null)
            {
                for (int i = 0; i < state.Facilities.Count; i++)
                {
                    if (state.Facilities[i].LifecycleStatus == FacilityLifecycleStatus.Blueprint)
                        bpCount++;
                }
            }

            buildCatalogTitleText.text = OutpostStrings.TrEn(
                $"İNŞA KATALOĞU | Taslak: {bpCount}",
                $"BUILD CATALOG | Bluepr.: {bpCount}");

            FacilitySpec lineSpec = FacilityCatalog.GetSpec(FacilityKind.UtilityLine);
            FacilitySpec solarSpec = FacilityCatalog.GetSpec(FacilityKind.SolarPanel);
            FacilitySpec batterySpec = FacilityCatalog.GetSpec(FacilityKind.Battery);
            FacilitySpec extractorSpec = FacilityCatalog.GetSpec(FacilityKind.WaterExtractor);
            bool unlockedLine = lineSpec.IsPlayerBuildable && lineSpec.UnlockedInPlayerCatalog;
            bool unlockedSolar = solarSpec.IsPlayerBuildable && solarSpec.UnlockedInPlayerCatalog;
            bool unlockedBattery = batterySpec.IsPlayerBuildable && batterySpec.UnlockedInPlayerCatalog;
            bool unlockedExtractor = extractorSpec.IsPlayerBuildable && extractorSpec.UnlockedInPlayerCatalog;
            btnBuildLine.gameObject.SetActive(unlockedLine);
            btnBuildSolar.gameObject.SetActive(unlockedSolar);
            if (btnBuildBattery != null) btnBuildBattery.gameObject.SetActive(unlockedBattery);
            if (btnBuildExtractor != null) btnBuildExtractor.gameObject.SetActive(unlockedExtractor);

            btnBuildLineText.text = OutpostStrings.TrEn(
                $"Hat\n({lineSpec.MaterialCost} M)",
                $"Line\n({lineSpec.MaterialCost} M)");
            btnBuildSolarText.text = OutpostStrings.TrEn(
                $"Solar\n({solarSpec.MaterialCost} M)",
                $"Solar\n({solarSpec.MaterialCost} M)");
            if (btnBuildBatteryText != null)
            {
                btnBuildBatteryText.text = OutpostStrings.TrEn(
                    $"Bat.\n({batterySpec.MaterialCost} M)",
                    $"Batt\n({batterySpec.MaterialCost} M)");
            }
            if (btnBuildExtractorText != null)
            {
                btnBuildExtractorText.text = OutpostStrings.TrEn(
                    $"Buz Su\n({extractorSpec.MaterialCost} M)",
                    $"Ice H2O\n({extractorSpec.MaterialCost} M)");
            }

            FacilityKind? activeBuild = worldView != null ? worldView.ActiveBuildKind : null;
            bool inProgress = state.Outcome == MissionOutcome.InProgress;
            btnBuildLine.interactable = inProgress;
            btnBuildSolar.interactable = inProgress;
            if (btnBuildBattery != null) btnBuildBattery.interactable = inProgress;
            if (btnBuildExtractor != null) btnBuildExtractor.interactable = inProgress;

            if (btnBuildLineBg != null)
                btnBuildLineBg.color = activeBuild == FacilityKind.UtilityLine ? AccentCyan : (state.Materials >= lineSpec.MaterialCost ? ButtonBg : new Color(0.28f, 0.16f, 0.16f, 0.95f));
            if (btnBuildSolarBg != null)
                btnBuildSolarBg.color = activeBuild == FacilityKind.SolarPanel ? AccentCyan : (state.Materials >= solarSpec.MaterialCost ? ButtonBg : new Color(0.28f, 0.16f, 0.16f, 0.95f));
            if (btnBuildBatteryBg != null)
                btnBuildBatteryBg.color = activeBuild == FacilityKind.Battery ? AccentCyan : (state.Materials >= batterySpec.MaterialCost ? ButtonBg : new Color(0.28f, 0.16f, 0.16f, 0.95f));
            if (btnBuildExtractorBg != null)
                btnBuildExtractorBg.color = activeBuild == FacilityKind.WaterExtractor ? AccentCyan : (state.Materials >= extractorSpec.MaterialCost ? ButtonBg : new Color(0.28f, 0.16f, 0.16f, 0.95f));

            btnRotateGhostText.text = OutpostStrings.TrEn("Döndür (R)", "Rotate (R)");
            btnCancelGhostText.text = OutpostStrings.TrEn("İptal (Esc)", "Cancel (Esc)");
            btnRotateGhost.interactable = activeBuild.HasValue;
            btnCancelGhost.interactable = activeBuild.HasValue;

            if (activeBuild.HasValue && worldView != null)
            {
                string kindName = OutpostStrings.FormatFacilityName(activeBuild.Value);
                string coordStr = worldView.HoveredBuildCell.HasValue ? worldView.HoveredBuildCell.Value.ToString() : "--";
                int rotDeg = (int)worldView.BuildRotation * 90;
                buildGhostInfoText.text = OutpostStrings.TrEn(
                    $"{kindName} | Parsel: {coordStr} | Açı: {rotDeg}°\nMaliyet: {worldView.LastGhostCost} Malzeme | {worldView.LastGhostReason}",
                    $"{kindName} | Cell: {coordStr} | Rot: {rotDeg}°\nCost: {worldView.LastGhostCost} Mat | {worldView.LastGhostReason}");
                buildGhostInfoText.color = worldView.LastGhostValid ? AccentGreen : AccentAmber;
            }
            else
            {
                buildGhostInfoText.text = OutpostStrings.TrEn(
                    $"Bir taslak seç ve haritada tahsisli parsele tıkla.\nMalzeme Stoku: {state.Materials}/{state.MaterialCapacity}",
                    $"Select a blueprint and click a claimed parcel.\nMaterials: {state.Materials}/{state.MaterialCapacity}");
                buildGhostInfoText.color = new Color(0.86f, 0.92f, 0.98f);
            }

            buildNoticeText.text = OutpostStrings.TrEn(
                "Not: Yerleşim sonrası iptal iadesizdir. R: Döndür | Sağ tık/Esc: İptal",
                "Note: Canceling placed blueprints is non-refundable. R: Rotate | Esc: Cancel");
        }

        private void ToggleSourcesModal(bool open)
        {
            if (sourcesModal == null) return;
            sourcesModal.SetActive(open);
            MarsOutpostCameraController.IsMenuBlockingCameraInput = open;
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
            bool showOrders = earthOrdersModal != null && earthOrdersModal.activeSelf;

            bool showSettings = settingsMenu != null && settingsMenu.IsOpen;
            bool shouldShow = isPaused && !showOutcome && !showAlert && !showReport && !showSources && !showOrders && !showSettings && !isScreenshotMode;
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
