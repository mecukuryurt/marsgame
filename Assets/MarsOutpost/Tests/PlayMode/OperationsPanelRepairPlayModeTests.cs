using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsPanelRepairPlayModeTests
    {
        private static void AdvanceRunnerClearingPauses(SimulationRunner runner, double totalSeconds)
        {
            double rem = totalSeconds;
            while (rem > 1e-6)
            {
                double step = rem > 10.0 ? 10.0 : rem;
                runner.Simulation.Tick(step);
                if (runner.State.SolReportPauseRequested) runner.AcknowledgeSolReport();
                if (runner.State.CriticalAlertPauseRequested) runner.AcknowledgeCriticalAlert();
                rem -= step;
            }
            runner.SetPaused(runner.Stepper.UserPaused);
        }

        [UnityTest]
        public IEnumerator HudDisplaysFaultTargetAndDispatchesSurfaceRepairFromParcelCard()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            var hud = Object.FindFirstObjectByType<MarsOutpostHud>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            runner.InitializeOperationsMission();
            yield return null;

            var solarCh = runner.State.ChannelSchedules[1];
            double toTrigger = solarCh.NextTriggerAtSeconds - runner.State.ElapsedSeconds + 5.0;
            AdvanceRunnerClearingPauses(runner, toTrigger);
            yield return null;

            Assert.That(runner.State.ChannelSchedules[1].IsActive, Is.True);
            string targetId = runner.State.ChannelSchedules[1].ActiveTargetId;
            Assert.That(targetId, Is.Not.Null.And.Not.Empty);

            var equipTextField = typeof(MarsOutpostHud).GetField("equipmentStatusText", BindingFlags.Instance | BindingFlags.NonPublic);
            var equipText = equipTextField?.GetValue(hud) as Text;
            Assert.That(equipText, Is.Not.Null);
            Assert.That(equipText.text, Does.Contain(targetId));

            // Select starter solar parcel and repair via parcel card BtnRepairParcel
            world.SelectParcel(ParcelMap.StarterSolarSpec.Footprint[0]);
            yield return null;

            var btnRepairParcelGo = GameObject.Find("BtnRepairParcel");
            Assert.That(btnRepairParcelGo, Is.Not.Null);
            var btnRepairParcel = btnRepairParcelGo.GetComponent<Button>();
            Assert.That(btnRepairParcel.interactable, Is.True);

            btnRepairParcel.onClick.Invoke();
            yield return null;

            Assert.That(runner.State.Crew[0].ActiveSurfaceJobId, Is.Not.Null.And.Not.Empty);

            // Advance until repair finishes -> 3/3 online and BtnRepairParcel hides
            AdvanceRunnerClearingPauses(runner, 85.0);
            yield return null;

            Assert.That(equipText.text, Does.Contain("3/3"));
            Assert.That(GameObject.Find("BtnRepairParcel"), Is.Null);
        }
    }
}
