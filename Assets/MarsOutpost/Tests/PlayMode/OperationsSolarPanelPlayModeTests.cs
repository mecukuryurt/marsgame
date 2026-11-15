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
    public class OperationsSolarPanelPlayModeTests
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
        public IEnumerator FourSolarPanelsRenderDistinctlyAndHudReflectsLiveCountAndOutput()
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

            var equipTextField = typeof(MarsOutpostHud).GetField("equipmentStatusText", BindingFlags.Instance | BindingFlags.NonPublic);
            var parcelDetailsField = typeof(MarsOutpostHud).GetField("parcelDetailsText", BindingFlags.Instance | BindingFlags.NonPublic);
            var equipText = equipTextField?.GetValue(hud) as Text;
            var parcelDetails = parcelDetailsField?.GetValue(hud) as Text;
            Assert.That(equipText, Is.Not.Null);
            Assert.That(parcelDetails, Is.Not.Null);

            yield return null;
            Assert.That(equipText.text, Does.Contain("3/3"));

            var target = FindAdjacentConnectedBuildable(runner.State.Map);
            if (!runner.State.Map.GetCell(target).IsClaimed)
                Assert.That(runner.ClaimParcel(target).Success, Is.True);
            Assert.That(runner.PlaceBlueprint(FacilityKind.SolarPanel, target).Success, Is.True);
            string facId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            Assert.That(runner.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            AdvanceRunnerSim(runner, 80.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            yield return null;

            Assert.That(runner.State.Resources.Panels.Count, Is.EqualTo(4));
            Assert.That(equipText.text, Does.Contain("4/4"));

            var starterArrayGo = GameObject.Find("SolarArrayStation");
            var fourthPanelGo = GameObject.Find($"FacilityVisual_SolarPanel_{facId}");
            Assert.That(starterArrayGo, Is.Not.Null);
            Assert.That(fourthPanelGo, Is.Not.Null);
            Assert.That(Vector3.Distance(starterArrayGo.transform.position, fourthPanelGo.transform.position), Is.GreaterThan(0.5f));
            Assert.That(fourthPanelGo.transform.Find("CommissionedSolarDeck"), Is.Not.Null);

            world.SelectParcel(target);
            yield return null;
            Assert.That(parcelDetails.text, Does.Match("Üretim:|Output:"));
        }
    }
}
