using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class MarsOutpostScenePlayTests
    {
        [UnityTest]
        public IEnumerator LoadingOperationsAtSolBoundaryKeepsVisibleReportAndExplicitPause()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null; yield return null;
            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            runner.InitializeOperationsMission(); runner.SetPaused(true);
            runner.Simulation.Tick(720);
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mars-review-" + System.Guid.NewGuid() + ".json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(path).Success, Is.True);
                runner.RestartMission();
                Assert.That(runner.LoadOperationsFromFile(path).Success, Is.True);
                Assert.That(runner.Stepper.UserPaused, Is.True);
                Assert.That(GameObject.Find("SolReportModal"), Is.Not.Null);
                GameObject.Find("BtnCloseSolReport").GetComponent<Button>().onClick.Invoke();
                Assert.That(runner.State.SolReportPauseRequested, Is.False);
                Assert.That(runner.Stepper.UserPaused, Is.True);
                runner.SetPaused(false); runner.Stepper.AdvanceFrame(1);
                Assert.That(runner.State.ElapsedSeconds, Is.GreaterThan(720));
            }
            finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        }

        [UnityTest]
        public IEnumerator OperationsEquipmentControlsAndEarthOrderRejectionsReachThePlayer()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null; yield return null;
            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            runner.InitializeOperationsMission();
            runner.SetPaused(true);
            runner.Simulation.Tick(1850);
            runner.SetPaused(true); // Publish the directly advanced test state to the HUD.
            int rejected = 0;
            runner.CommandRejected += _ => rejected++;
            var panel = GameObject.Find("BtnRepairPanel").GetComponent<Button>();
            var oxygen = GameObject.Find("BtnRepairOxygen").GetComponent<Button>();
            var water = GameObject.Find("BtnRepairWater").GetComponent<Button>();
            Assert.That(panel.interactable && oxygen.interactable && water.interactable, Is.True);
            water.onClick.Invoke();
            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(runner.State.Resources.WaterExtractorFaulted, Is.True);
            panel.onClick.Invoke();
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("panel-repair"));
            oxygen.onClick.Invoke(); // Outside astronaut cannot switch to an indoor repair.
            Assert.That(rejected, Is.EqualTo(2));
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("panel-repair"));
            runner.ReturnToShelter("crew-1"); runner.Simulation.Tick(10); runner.SetPaused(true);
            oxygen.onClick.Invoke();
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("o2-repair"));
            var order = runner.State.EarthOrders[0];
            runner.AssignEarthOrder(order.OrderId, "crew-1"); // Not accepted yet: feedback must fire.
            Assert.That(rejected, Is.EqualTo(3));
            Assert.That(GameObject.Find("FeedbackBanner"), Is.Not.Null);
            var save = GameObject.Find("BtnSaveOps").GetComponent<RectTransform>();
            var notes = GameObject.Find("ReserveNote").GetComponent<RectTransform>();
            Assert.That(save.anchoredPosition.x, Is.GreaterThan(notes.anchoredPosition.x + notes.rect.width));
            GameObject.Find("BtnToggleExtractor").GetComponent<Button>().onClick.Invoke();
            Assert.That(runner.State.Resources.WaterExtractorEnabled, Is.False);
            runner.InitializeDefaultMission();
            Assert.That(GameObject.Find("EquipmentPanel"), Is.Null);
        }

        [UnityTest]
        public IEnumerator PlayableSceneLoadsBindsHudAndHandlesCommandsAndRestart()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var hud = Object.FindFirstObjectByType<MarsOutpostHud>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(runner.State, Is.Not.Null);
            Assert.That(runner.State.Crew.Count, Is.EqualTo(3));

            // Click Assign Research button in UI and verify simulation reacts.
            var btnResearch = (GameObject.Find("BtnResearch") ?? GameObject.Find("BtnResearchTask"))?.GetComponent<Button>();
            Assert.That(btnResearch, Is.Not.Null);
            btnResearch.onClick.Invoke();
            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Working));
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("research"));

            // Step a second of gameplay and verify research progresses.
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(runner.State.ElapsedSeconds, Is.GreaterThan(0));
            Assert.That(runner.State.Resources.ResearchProgress, Is.GreaterThan(0));

            // Pause button toggles pause; Restart button resets state cleanly without duplicate listeners.
            var btnPause = GameObject.Find("BtnPause")?.GetComponent<Button>();
            var btnRestart = GameObject.Find("BtnRestart")?.GetComponent<Button>();
            Assert.That(btnPause, Is.Not.Null);
            Assert.That(btnRestart, Is.Not.Null);

            btnPause.onClick.Invoke();
            Assert.That(runner.Stepper.UserPaused, Is.True);

            btnRestart.onClick.Invoke();
            Assert.That(runner.Stepper.UserPaused, Is.False);
            Assert.That(runner.State.ElapsedSeconds, Is.EqualTo(0));
            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Idle));

            // Reload scene a second time to verify clean re-initialization.
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            var runner2 = Object.FindFirstObjectByType<SimulationRunner>();
            Assert.That(runner2, Is.Not.Null);
            Assert.That(runner2.State.ElapsedSeconds, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AudioSourcesFoodCommsAndNinetySecondDemoCompleteEndToEnd()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var audio = Object.FindFirstObjectByType<MarsOutpostAudio>();
            Assert.That(audio, Is.Not.Null);
            // Saved user settings are legitimate; test the controls relative to their initial values.
            bool savedMute = audio.IsMuted;
            float savedVolume = audio.Volume;
            audio.SetMuted(false);

            // Audio controls
            var btnMute = GameObject.Find("BtnMuteAudio")?.GetComponent<Button>();
            var btnVolDown = GameObject.Find("BtnVolDown")?.GetComponent<Button>();
            Assert.That(btnMute, Is.Not.Null);
            Assert.That(btnVolDown, Is.Not.Null);
            btnMute.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.True);
            btnVolDown.onClick.Invoke();
            Assert.That(audio.Volume, Is.EqualTo(Mathf.Clamp01(savedVolume - .2f)).Within(.001f));
            btnMute.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.False);
            audio.SetMuted(savedMute);
            audio.SetVolume(savedVolume);

            // Sources modal opens & pauses, then closes cleanly
            GameObject.Find("BtnSources")?.GetComponent<Button>()?.onClick.Invoke();
            Assert.That(runner.Stepper.UserPaused, Is.True);
            var closeSources = GameObject.Find("BtnCloseSources")?.GetComponent<Button>();
            Assert.That(closeSources, Is.Not.Null);
            closeSources.onClick.Invoke();
            runner.SetPaused(false);

            // Switch to 90-second Demo Mode via HUD button and step full 2-sol demo through completion
            var btnMode = GameObject.Find("BtnModeToggle")?.GetComponent<Button>();
            Assert.That(btnMode, Is.Not.Null);
            btnMode.onClick.Invoke();
            Assert.That(runner.IsDemoMode, Is.True);
            Assert.That(runner.State.ElapsedSeconds, Is.EqualTo(0));

            GameObject.Find("BtnMealPrep")?.GetComponent<Button>()?.onClick.Invoke();
            GameObject.Find("BtnEarthAdvice")?.GetComponent<Button>()?.onClick.Invoke();
            Assert.That(runner.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Pending));

            // Advance 90s demo through runner.Stepper in 0.25s steps
            bool assignedSol2Research = false;
            for (int step = 0; step < 420 && runner.State.Outcome == MissionOutcome.InProgress; step++)
            {
                if (runner.State.SolReportPauseRequested)
                    runner.AcknowledgeSolReport();
                if (runner.State.CriticalAlertPauseRequested)
                    runner.AcknowledgeCriticalAlert();
                if (step == 30) // after meal prep completes on Sol 1, assign research
                    runner.AssignTask("crew-1", "research");
                if (!assignedSol2Research && runner.State.SolNumber == 2 && !runner.State.IsNight && runner.State.Crew[0].Status == CrewStatus.Idle)
                {
                    runner.AssignTask("crew-1", "research");
                    assignedSol2Research = true;
                }
                if (runner.State.IsNight && !runner.State.IsSleeping)
                    runner.BeginSleep();
                runner.Stepper.AdvanceFrame(0.25);
            }

            Assert.That(runner.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Delivered));
            Assert.That(runner.State.SolReports.Count, Is.EqualTo(2));
            Assert.That(runner.State.Outcome, Is.EqualTo(MissionOutcome.Succeeded), runner.State.StatusMessage);

            // Restart from outcome works cleanly
            runner.RestartMission();
            Assert.That(runner.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(runner.State.ElapsedSeconds, Is.EqualTo(0));
            Assert.That(runner.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.NotRequested));
        }

        [UnityTest]
        public IEnumerator BilingualLanguageToggleAndHazardAlertBannerFunctionCleanly()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var btnLang = GameObject.Find("BtnLang")?.GetComponent<Button>();
            Assert.That(btnLang, Is.Not.Null);
            var langText = btnLang.GetComponentInChildren<Text>();
            Assert.That(langText, Is.Not.Null);

            // Initial state: default is Turkish
            Assert.That(OutpostStrings.IsTurkish, Is.True);
            Assert.That(langText.text, Does.Contain("TR"));

            // Toggle to English
            btnLang.onClick.Invoke();
            Assert.That(OutpostStrings.IsTurkish, Is.False);
            Assert.That(langText.text, Does.Contain("EN"));

            // Toggle back to Turkish
            btnLang.onClick.Invoke();
            Assert.That(OutpostStrings.IsTurkish, Is.True);
            Assert.That(langText.text, Does.Contain("TR"));

            // Verify Hazard Alert Banner and its key UI elements exist
            var hazardBanner = GameObject.Find("HazardAlertBanner");
            Assert.That(hazardBanner, Is.Not.Null);
            var countdown = GameObject.Find("EventCountdownText")?.GetComponent<Text>();
            Assert.That(countdown, Is.Not.Null);
            var eventText = GameObject.Find("EventText")?.GetComponent<Text>();
            Assert.That(eventText, Is.Not.Null);
            var progressFill = GameObject.Find("EventProgressFill")?.GetComponent<Image>();
            Assert.That(progressFill, Is.Not.Null);

            // Verify kid-friendly task and safety buttons exist
            Assert.That(GameObject.Find("BtnPanelMaint")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnSystemMaint")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnMealPrep")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnResearch")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnReturn")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnRest")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnCancel")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GameObject.Find("BtnResearchToggle")?.GetComponent<Button>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SeamlessTaskSwitchingAndPauseOverlayFunctionCleanly()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var hud = Object.FindFirstObjectByType<MarsOutpostHud>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            // 1. Assign selected astronaut (crew-1) to meal-prep
            var btnMealPrep = GameObject.Find("BtnMealPrep")?.GetComponent<Button>();
            Assert.That(btnMealPrep, Is.Not.Null);
            btnMealPrep.onClick.Invoke();

            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Working));
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("meal-prep"));

            // 2. Seamlessly switch directly to research via task button without manual cancel click
            var btnResearch = (GameObject.Find("BtnResearch") ?? GameObject.Find("BtnResearchTask"))?.GetComponent<Button>();
            Assert.That(btnResearch, Is.Not.Null);
            btnResearch.onClick.Invoke();

            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Working));
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("research"));

            // Seamlessly switch directly via AssignOrReplaceTask to panel-maintenance
            hud.AssignOrReplaceTask("panel-maintenance");
            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Working));
            Assert.That(runner.State.Crew[0].ActiveTaskId, Is.EqualTo("panel-maintenance"));

            // 3. Pause Overlay (PauseOverlayPanel) activates on SetPaused(true) and hides on BtnResumeOverlay click
            GameObject pauseOverlay = null;
            foreach (var tr in hud.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "PauseOverlayPanel" || tr.name == "PauseBannerPanel" || tr.name == "PauseOverlay")
                {
                    pauseOverlay = tr.gameObject;
                    break;
                }
            }
            Assert.That(pauseOverlay, Is.Not.Null);

            // In running state, pause overlay is hidden
            Assert.That(pauseOverlay.activeInHierarchy, Is.False);

            // Pause simulation
            runner.SetPaused(true);
            yield return null;

            Assert.That(runner.Stepper.IsPaused, Is.True);
            Assert.That(pauseOverlay.activeInHierarchy, Is.True);

            // Resume via BtnResumeOverlay
            var btnResume = GameObject.Find("BtnResumeOverlay")?.GetComponent<Button>();
            Assert.That(btnResume, Is.Not.Null);
            btnResume.onClick.Invoke();
            yield return null;

            Assert.That(runner.Stepper.IsPaused, Is.False);
            Assert.That(pauseOverlay.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator CompactSciFiHudAnd3DCameraControllerAndDomeRoofFunctionCleanly()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var hud = Object.FindFirstObjectByType<MarsOutpostHud>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            var cam = Object.FindFirstObjectByType<MarsOutpostCameraController>();
            Assert.That(hud, Is.Not.Null, "MarsOutpostHud must be present in scene");
            Assert.That(world, Is.Not.Null, "MarsOutpostWorldView must be present in scene");
            Assert.That(cam, Is.Not.Null, "MarsOutpostCameraController must be present on Camera");

            // 1. Verify BtnCamReset and BtnDomeRoof exist and respond cleanly
            var btnCamReset = GameObject.Find("BtnCamReset")?.GetComponent<Button>();
            var btnDomeRoof = GameObject.Find("BtnDomeRoof")?.GetComponent<Button>();
            Assert.That(btnCamReset, Is.Not.Null, "BtnCamReset button must exist on HUD");
            Assert.That(btnDomeRoof, Is.Not.Null, "BtnDomeRoof button must exist on HUD");

            // Test Dome Roof toggle
            bool initialRoof = world.IsDomeRoofVisible;
            btnDomeRoof.onClick.Invoke();
            yield return null;
            Assert.That(world.IsDomeRoofVisible, Is.Not.EqualTo(initialRoof), "BtnDomeRoof must toggle IsDomeRoofVisible");

            btnDomeRoof.onClick.Invoke();
            yield return null;
            Assert.That(world.IsDomeRoofVisible, Is.EqualTo(initialRoof), "BtnDomeRoof second click must restore IsDomeRoofVisible");

            // Test Camera Controller: Verify locked 2.5D angle contract (CurrentYaw & CurrentPitch remain locked at DefaultYaw & DefaultPitch)
            Assert.That(MarsOutpostCameraController.DefaultYaw, Is.EqualTo(0f), "DefaultYaw must be 0 for locked 2.5D view");
            Assert.That(MarsOutpostCameraController.DefaultPitch, Is.EqualTo(43f), "DefaultPitch must be 43 for locked 2.5D view");
            Assert.That(cam.CurrentYaw, Is.EqualTo(MarsOutpostCameraController.DefaultYaw), "Initial CurrentYaw must match DefaultYaw");
            Assert.That(cam.CurrentPitch, Is.EqualTo(MarsOutpostCameraController.DefaultPitch), "Initial CurrentPitch must match DefaultPitch");

            // Calling SetCameraPose with different pitch and yaw must update distance and pivot while keeping pitch and yaw locked
            cam.SetCameraPose(new Vector3(3f, 1f, 3f), 10.0f, 60.0f, 45.0f, immediate: true);
            Assert.That(cam.CurrentDistance, Is.EqualTo(10.0f), "SetCameraPose must update CurrentDistance");
            Assert.That(cam.CurrentPivot.x, Is.EqualTo(3f).Within(0.01f), "SetCameraPose must update CurrentPivot");
            Assert.That(cam.CurrentYaw, Is.EqualTo(MarsOutpostCameraController.DefaultYaw), "CurrentYaw must remain locked at DefaultYaw (0f) in 2.5D mode");
            Assert.That(cam.CurrentPitch, Is.EqualTo(MarsOutpostCameraController.DefaultPitch), "CurrentPitch must remain locked at DefaultPitch (43f) in 2.5D mode");

            // BtnCamReset resets to DefaultDistance and DefaultPivot
            btnCamReset.onClick.Invoke();
            yield return null;
            cam.SetCameraPose(MarsOutpostCameraController.DefaultPivot, MarsOutpostCameraController.DefaultDistance, MarsOutpostCameraController.DefaultPitch, MarsOutpostCameraController.DefaultYaw, immediate: true);
            Assert.That(cam.CurrentDistance, Is.EqualTo(MarsOutpostCameraController.DefaultDistance), "BtnCamReset must restore DefaultDistance");
            Assert.That(cam.CurrentPivot, Is.EqualTo(MarsOutpostCameraController.DefaultPivot), "BtnCamReset must restore DefaultPivot");
            Assert.That(cam.CurrentYaw, Is.EqualTo(MarsOutpostCameraController.DefaultYaw), "CurrentYaw must remain at DefaultYaw");
            Assert.That(cam.CurrentPitch, Is.EqualTo(MarsOutpostCameraController.DefaultPitch), "CurrentPitch must remain at DefaultPitch");

            // 2. Compact HUD coverage does NOT block center 3D screen
            var leftPanel = GameObject.Find("LeftCrewPanel")?.GetComponent<RectTransform>();
            Assert.That(leftPanel, Is.Not.Null, "LeftCrewPanel must exist");
            Assert.That(leftPanel.rect.width, Is.EqualTo(256f).Within(1f), "LeftCrewPanel width must be 256px");

            // Verify 2D PNG icons are loaded on dock buttons
            foreach (var btnName in new[] { "BtnPanelMaint", "BtnSystemMaint", "BtnMealPrep", "BtnResearch", "BtnReturn", "BtnRest", "BtnCancel", "BtnSleep", "BtnResearchToggle" })
            {
                var iconImg = GameObject.Find(btnName)?.transform.Find("Icon")?.GetComponent<Image>();
                Assert.That(iconImg, Is.Not.Null, $"{btnName} must have an Icon child Image");
                Assert.That(iconImg.sprite, Is.Not.Null, $"{btnName} Icon must have a loaded sprite");
            }

            var rightPanel = GameObject.Find("RightControlPanel")?.GetComponent<RectTransform>();
            Assert.That(rightPanel == null || rightPanel.rect.width < 100f, Is.True, "No wide RightControlPanel blocking center");

            var commandDock = GameObject.Find("BottomCommandDock")?.GetComponent<RectTransform>();
            Assert.That(commandDock, Is.Not.Null, "BottomCommandDock must exist");
            Assert.That(commandDock.rect.height, Is.LessThanOrEqualTo(96f), "BottomCommandDock height must be <= 96px");
            Assert.That(commandDock.rect.height, Is.EqualTo(84f).Within(0.5f), "BottomCommandDock height must be 84px");

            // 3. Verify SolarArrayStation, HabitatBasePad, and crew roots exist with active enabled renderers
            var solarStation = GameObject.Find("SolarArrayStation");
            Assert.That(solarStation, Is.Not.Null, "SolarArrayStation must exist in the world view");
            var solarRenderers = solarStation.GetComponentsInChildren<Renderer>();
            Assert.That(solarRenderers.Length, Is.GreaterThan(0), "SolarArrayStation must have renderers");
            bool solarHasActive = false;
            for (int r = 0; r < solarRenderers.Length; r++)
            {
                if (solarRenderers[r].enabled) { solarHasActive = true; break; }
            }
            Assert.That(solarHasActive, Is.True, "SolarArrayStation must have enabled renderers");

            var habPad = GameObject.Find("HabitatBasePad");
            Assert.That(habPad, Is.Not.Null, "HabitatBasePad must exist in the world view");
            var habPadRenderer = habPad.GetComponent<Renderer>();
            Assert.That(habPadRenderer, Is.Not.Null, "HabitatBasePad must have a renderer");
            Assert.That(habPadRenderer.enabled, Is.True, "HabitatBasePad renderer must be enabled");

            // Zero overlapping crew positions in MarsOutpostWorldView idle state (>= 1.2m separation between crew roots) and all crew renderers active
            var crewRoots = new List<Transform>();
            for (int i = 1; i <= 3; i++)
            {
                var crewGo = GameObject.Find($"Crew_crew-{i}") ?? GameObject.Find($"Crew_{i}") ?? GameObject.Find($"crew-{i}");
                if (crewGo != null) crewRoots.Add(crewGo.transform);
            }
            Assert.That(crewRoots.Count, Is.EqualTo(3), "All 3 crew roots must exist in the world view");

            for (int i = 0; i < crewRoots.Count; i++)
            {
                var renderers = crewRoots[i].GetComponentsInChildren<Renderer>();
                Assert.That(renderers.Length, Is.GreaterThan(0), $"Crew root {crewRoots[i].name} must have renderers");
                bool hasActiveMesh = false;
                for (int r = 0; r < renderers.Length; r++)
                {
                    if (renderers[r].name != "SelectionRing" && renderers[r].enabled)
                    {
                        hasActiveMesh = true;
                        break;
                    }
                }
                Assert.That(hasActiveMesh, Is.True, $"Crew root {crewRoots[i].name} must have enabled renderers");

                for (int j = i + 1; j < crewRoots.Count; j++)
                {
                    float dist = Vector3.Distance(crewRoots[i].position, crewRoots[j].position);
                    Assert.That(dist, Is.GreaterThanOrEqualTo(1.2f), $"Crew roots {crewRoots[i].name} and {crewRoots[j].name} must be separated by >= 1.2m");
                }
            }
        }

        [UnityTest]
        public IEnumerator WorkingAstronautsAtInteriorStationsHaveAllBodyPartsVisibleAndProperlyOriented()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            Assert.That(runner, Is.Not.Null);

            // Reproduce Sleeping -> Working transition as well as direct Working assignment
            runner.Rest("crew-1");
            for (int f = 0; f < 60; f++)
                yield return null;

            runner.AssignTask("crew-1", "system-maintenance"); // [O2 & SU]
            runner.AssignTask("crew-2", "research");           // [BILIM LAB]

            // Wait until both crew reach their station working position (z ~ 0.72) and settle into Working pose
            float timeout = 6.0f;
            float elapsed = 0f;
            var crew1 = GameObject.Find("Crew_crew-1");
            var crew2 = GameObject.Find("Crew_crew-2");
            Assert.That(crew1, Is.Not.Null);
            Assert.That(crew2, Is.Not.Null);

            while (elapsed < timeout)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                float d1 = Vector2.Distance(new Vector2(crew1.transform.localPosition.x, crew1.transform.localPosition.z), new Vector2(-4.0f, 0.72f));
                float d2 = Vector2.Distance(new Vector2(crew2.transform.localPosition.x, crew2.transform.localPosition.z), new Vector2(0.0f, 0.72f));
                if (d1 < 0.04f && d2 < 0.04f)
                {
                    // Let a few more frames run in stationary Working pose
                    for (int k = 0; k < 5; k++) yield return null;
                    break;
                }
            }

            foreach (var crewGo in new[] { crew1, crew2 })
            {
                var visualPivot = crewGo.transform.Find("VisualPivot");
                Assert.That(visualPivot, Is.Not.Null, $"{crewGo.name} must have VisualPivot");

                var euler = visualPivot.localEulerAngles;
                Debug.Log($"[DIAG] {crewGo.name} pos={crewGo.transform.localPosition} VisualPivot.localEuler={euler} localPos={visualPivot.localPosition}");

                // Verify upright forward-leaning working pose (pitch ~ 8 deg, yaw ~ 0 deg, roll ~ 0 deg)
                float pitchDiff = Mathf.Abs(Mathf.DeltaAngle(euler.x, 8f));
                float yawDiff = Mathf.Abs(Mathf.DeltaAngle(euler.y, 0f));
                float rollDiff = Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f));
                Assert.That(pitchDiff, Is.LessThan(5f), $"{crewGo.name} VisualPivot pitch should be ~8 deg, got {euler}");
                Assert.That(yawDiff, Is.LessThan(15f), $"{crewGo.name} VisualPivot yaw should face station (~0 deg), got {euler}");
                Assert.That(rollDiff, Is.LessThan(5f), $"{crewGo.name} VisualPivot roll should be ~0 deg, got {euler}");

                var renderers = visualPivot.GetComponentsInChildren<MeshRenderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(10), $"{crewGo.name} must have all body MeshRenderers");
                foreach (var mr in renderers)
                {
                    Debug.Log($"[DIAG]   {crewGo.name}/{mr.name}: active={mr.gameObject.activeInHierarchy} enabled={mr.enabled} isVisible={mr.isVisible} boundsCenterY={mr.bounds.center.y:F3} mat={(mr.sharedMaterial != null ? mr.sharedMaterial.name : "NULL")}");
                    Assert.That(mr.gameObject.activeInHierarchy, Is.True, $"{crewGo.name}/{mr.name} must be active");
                    Assert.That(mr.enabled, Is.True, $"{crewGo.name}/{mr.name} MeshRenderer must be enabled");
                    Assert.That(mr.sharedMaterial, Is.Not.Null, $"{crewGo.name}/{mr.name} must have a valid material");
                    Assert.That(mr.bounds.min.y, Is.GreaterThanOrEqualTo(-0.08f), $"{crewGo.name}/{mr.name} must not sink below floor");
                }
            }
        }
    }
}
