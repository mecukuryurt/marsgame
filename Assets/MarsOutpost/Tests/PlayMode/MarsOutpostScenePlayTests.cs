using System.Collections;
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
            Assert.That(audio.IsMuted, Is.False);

            // Audio controls
            var btnMute = GameObject.Find("BtnMuteAudio")?.GetComponent<Button>();
            var btnVolDown = GameObject.Find("BtnVolDown")?.GetComponent<Button>();
            Assert.That(btnMute, Is.Not.Null);
            Assert.That(btnVolDown, Is.Not.Null);
            btnMute.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.True);
            btnVolDown.onClick.Invoke();
            Assert.That(audio.Volume, Is.LessThan(0.55f));
            btnMute.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.False);

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
    }
}
