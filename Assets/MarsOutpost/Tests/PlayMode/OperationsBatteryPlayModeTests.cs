using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsBatteryPlayModeTests
    {
        private static void AdvanceRunnerSim(SimulationRunner runner, double seconds)
        {
            runner.Simulation.Tick(seconds);
            runner.SetPaused(runner.Stepper.UserPaused);
        }

        private static GridCoord FindAdjacentConnectedBuildable(ParcelMap map)
        {
            foreach (var cell in map.Cells)
            {
                if (cell.IsExplored && cell.IsBuildable && cell.Protection == StarterProtectionKind.None &&
                    UtilityNetwork.IsCoordConnectedPreview(map, cell.Coord))
                {
                    return cell.Coord;
                }
            }
            Assert.Fail("Expected adjacent core-connected buildable tile.");
            return default;
        }

        [UnityTest]
        public IEnumerator BatteryButtonBuildsVisualizesExtendsHudCapTo125AndSurvivesPauseAndSaveLoad()
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

            var btnBuildBatteryField = typeof(MarsOutpostHud).GetField("btnBuildBattery", BindingFlags.Instance | BindingFlags.NonPublic);
            var batteryTextField = typeof(MarsOutpostHud).GetField("batteryText", BindingFlags.Instance | BindingFlags.NonPublic);
            var parcelDetailsField = typeof(MarsOutpostHud).GetField("parcelDetailsText", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnBuildBattery = btnBuildBatteryField?.GetValue(hud) as Button;
            var batteryText = batteryTextField?.GetValue(hud) as Text;
            var parcelDetails = parcelDetailsField?.GetValue(hud) as Text;
            Assert.That(btnBuildBattery, Is.Not.Null);
            Assert.That(btnBuildBattery.gameObject.activeInHierarchy, Is.True);
            Assert.That(batteryText, Is.Not.Null);
            Assert.That(parcelDetails, Is.Not.Null);

            // Switch to deterministic high-solar Operations config so daytime reliably exceeds 105 without random storms.
            var opsConfig = MissionConfig.CreateOperationsDefault(
                resources: new ResourceConfig(
                    solarRate: 0.35,
                    batteryCapacity: 100,
                    initialBattery: 88,
                    initialPanelFouling: 0.0),
                enableOperationsRandomEvents: false);
            runner.InitializeMission(opsConfig, new MissionScenario());
            yield return null;

            btnBuildBattery.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.Battery));

            var target = FindAdjacentConnectedBuildable(runner.State.Map);
            if (!runner.State.Map.GetCell(target).IsClaimed)
                Assert.That(runner.ClaimParcel(target).Success, Is.True);
            Assert.That(runner.PlaceBlueprint(FacilityKind.Battery, target).Success, Is.True);
            string facId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            Assert.That(runner.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            AdvanceRunnerSim(runner, 95.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            yield return null;

            Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));

            var batteryVisualGo = GameObject.Find($"FacilityVisual_Battery_{facId}");
            Assert.That(batteryVisualGo, Is.Not.Null);
            Assert.That(batteryVisualGo.transform.Find("CommissionedBatteryBank"), Is.Not.Null);

            // Disable research/water so remaining Sol 1 daytime surplus pushes battery > 105.
            runner.SetResearchEnabled(false);
            runner.SetWaterExtractorEnabled(false);
            for (int step = 0; step < 30 && runner.State.Resources.Battery <= 105.0; step++)
                AdvanceRunnerSim(runner, 10.0);
            Assert.That(runner.State.Resources.Battery, Is.GreaterThan(105.0));
            yield return null;

            Assert.That(batteryText.text, Does.Contain("/125"));

            world.SelectParcel(target);
            yield return null;
            Assert.That(parcelDetails.text, Does.Contain("+25"));
            Assert.That(parcelDetails.text, Does.Contain("125"));

            string savePath = Path.Combine(Application.temporaryCachePath, "op11_battery_playmode_test.json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                runner.SetPaused(true);
                yield return null;
                runner.SetPaused(false);
                yield return null;
                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                yield return null;

                Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
                Assert.That(runner.State.Resources.Battery, Is.GreaterThan(105.0));
                Assert.That(batteryText.text, Does.Contain("/125"));
            }
            finally
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
        }
    }
}
