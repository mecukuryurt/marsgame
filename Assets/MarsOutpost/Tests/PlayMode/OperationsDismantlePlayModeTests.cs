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
    public class OperationsDismantlePlayModeTests
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
        public IEnumerator GhostCancelBlueprintCancelAndDismantleFlowUpdateHudWorldAndSurviveSaveLoad()
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

            var btnCancelGhostField = typeof(MarsOutpostHud).GetField("btnCancelGhost", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnCancelBpField = typeof(MarsOutpostHud).GetField("btnCancelBlueprint", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnDismantleField = typeof(MarsOutpostHud).GetField("btnDismantleFacility", BindingFlags.Instance | BindingFlags.NonPublic);
            var parcelDetailsField = typeof(MarsOutpostHud).GetField("parcelDetailsText", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnCancelGhost = btnCancelGhostField?.GetValue(hud) as Button;
            var btnCancelBp = btnCancelBpField?.GetValue(hud) as Button;
            var btnDismantle = btnDismantleField?.GetValue(hud) as Button;
            var parcelDetails = parcelDetailsField?.GetValue(hud) as Text;
            Assert.That(btnCancelGhost, Is.Not.Null);
            Assert.That(btnCancelBp, Is.Not.Null);
            Assert.That(btnDismantle, Is.Not.Null);
            Assert.That(parcelDetails, Is.Not.Null);

            int startMaterials = runner.State.Materials;

            // 1. Ghost cancel spends 0 materials.
            world.StartBuildMode(FacilityKind.Battery);
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.Battery));
            btnCancelGhost.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.Null);
            Assert.That(runner.State.Materials, Is.EqualTo(startMaterials));

            // 2. Place a Battery blueprint -> select parcel -> cancel blueprint -> 0 refund & visual destroyed.
            var target = FindAdjacentConnectedBuildable(runner.State.Map);
            if (!runner.State.Map.GetCell(target).IsClaimed)
                Assert.That(runner.ClaimParcel(target).Success, Is.True);
            int matAfterClaim = runner.State.Materials;
            int batCost = FacilityCatalog.GetSpec(FacilityKind.Battery).MaterialCost;

            Assert.That(runner.PlaceBlueprint(FacilityKind.Battery, target).Success, Is.True);
            string bpId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            yield return null;
            Assert.That(GameObject.Find($"FacilityVisual_Battery_{bpId}"), Is.Not.Null);

            world.SelectParcel(target);
            yield return null;
            Assert.That(btnCancelBp.gameObject.activeInHierarchy, Is.True);
            Assert.That(parcelDetails.text, Does.Match("0 malz|0 mat"));

            btnCancelBp.onClick.Invoke();
            yield return null;
            Assert.That(runner.State.Materials, Is.EqualTo(matAfterClaim - batCost));
            Assert.That(GameObject.Find($"FacilityVisual_Battery_{bpId}"), Is.Null);
            Assert.That(btnCancelBp.gameObject.activeInHierarchy, Is.False);

            // 3. Place & commission a Battery -> dismantle -> +50% refund & visual destroyed & save/load keeps cleared.
            Assert.That(runner.PlaceBlueprint(FacilityKind.Battery, target).Success, Is.True);
            string builtId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            Assert.That(runner.AssignBuildBlueprint("crew-1", builtId).Success, Is.True);
            AdvanceRunnerSim(runner, 95.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            yield return null;

            Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
            Assert.That(GameObject.Find($"FacilityVisual_Battery_{builtId}"), Is.Not.Null);

            world.SelectParcel(target);
            yield return null;
            Assert.That(btnDismantle.gameObject.activeInHierarchy, Is.True);
            Assert.That(parcelDetails.text, Does.Contain($"+{batCost / 2}"));

            int matBeforeDismantle = runner.State.Materials;
            btnDismantle.onClick.Invoke();
            yield return null;

            Assert.That(runner.State.Materials, Is.EqualTo(matBeforeDismantle + batCost / 2));
            Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));
            Assert.That(GameObject.Find($"FacilityVisual_Battery_{builtId}"), Is.Null);
            Assert.That(btnDismantle.gameObject.activeInHierarchy, Is.False);

            string savePath = Path.Combine(Application.temporaryCachePath, "op12_dismantle_playmode_test.json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                yield return null;

                Assert.That(runner.DismantleFacility(builtId).Success, Is.False);
                Assert.That(runner.State.Materials, Is.EqualTo(matBeforeDismantle + batCost / 2));
                Assert.That(GameObject.Find($"FacilityVisual_Battery_{builtId}"), Is.Null);
            }
            finally
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
        }
    }
}
