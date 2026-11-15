using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsBuildMotionPlayModeTests
    {
        private static GridCoord FindClaimedBuildableCell(MissionState state)
        {
            foreach (var cell in state.Map.Cells)
            {
                if (cell.IsClaimed && cell.IsBuildable && cell.Protection == StarterProtectionKind.None && string.IsNullOrEmpty(cell.FacilityId))
                    return cell.Coord;
            }
            return ParcelMap.SolarWorkCoord;
        }

        private static void AdvanceRunnerSim(SimulationRunner runner, double seconds)
        {
            runner.Simulation.Tick(seconds);
            runner.SetPaused(runner.Stepper.UserPaused);
        }

        [UnityTest]
        public IEnumerator HudBuildAssignment_PlaysAnimatedWork_PausesCleanly_TransitionsToCommissionedAndSurvivesSaveLoad()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);

            runner.InitializeOperationsMission();
            yield return null;

            // Place SolarPanel blueprint on claimed buildable parcel
            GridCoord targetCoord = FindClaimedBuildableCell(runner.State);
            var place = runner.PlaceBlueprint(FacilityKind.SolarPanel, targetCoord);
            Assert.That(place.Success, Is.True, place.Reason);
            string facId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            yield return null;

            // Verify blueprint scaffold exists initially
            var visualGo = GameObject.Find($"FacilityVisual_SolarPanel_{facId}");
            Assert.That(visualGo, Is.Not.Null);
            Assert.That(visualGo.transform.Find("BlueprintVolume"), Is.Not.Null);
            Assert.That(visualGo.transform.Find("CommissionedSolarDeck"), Is.Null);

            // Select the parcel -> click BtnBuildBlueprint
            world.SelectParcel(targetCoord);
            yield return null;

            var parcelCard = GameObject.Find("ParcelSelectionCard");
            Assert.That(parcelCard, Is.Not.Null);
            Assert.That(parcelCard.activeInHierarchy, Is.True);

            var btnBuildGo = GameObject.Find("BtnBuildBlueprint");
            Assert.That(btnBuildGo, Is.Not.Null);
            Assert.That(btnBuildGo.activeInHierarchy, Is.True);
            btnBuildGo.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(runner.State.Crew[0].ActiveSurfaceJobId, Is.Not.Null);

            // Advance simulation out of Travel into Work
            AdvanceRunnerSim(runner, 10.0);
            yield return null;
            yield return null;

            var detailsText = GameObject.Find("ParcelDetails")?.GetComponent<Text>();
            Assert.That(detailsText, Is.Not.Null);
            Assert.That(detailsText.text, Does.Contain("sn").Or.Contain("s |"));

            // Check construction arm animation plays when unpaused
            runner.SetPaused(false);
            yield return null;

            var crewRoot = GameObject.Find("Crew_crew-1");
            Assert.That(crewRoot, Is.Not.Null);
            Transform rightShoulder = crewRoot.transform.Find("VisualPivot/RightShoulderPivot");
            Assert.That(rightShoulder, Is.Not.Null);

            Quaternion rot1 = rightShoulder.localRotation;
            float elapsed = 0f;
            bool moved = false;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
                if (Quaternion.Angle(rot1, rightShoulder.localRotation) > 0.5f)
                {
                    moved = true;
                    break;
                }
            }
            Assert.That(moved, Is.True, "Crew RightShoulder arm should animate during active construction work.");

            // Pause freezes motion cleanly
            runner.SetPaused(true);
            yield return null;
            Quaternion pausedRot = rightShoulder.localRotation;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(Quaternion.Angle(pausedRot, rightShoulder.localRotation), Is.LessThan(0.05f),
                "Construction arm animation must freeze when simulation is paused.");

            // Unpause and complete construction via sim step
            runner.SetPaused(false);
            AdvanceRunnerSim(runner, 60.0);
            yield return null;
            yield return null;

            // Visual must swap from blueprint scaffold to CommissionedSolarDeck
            visualGo = GameObject.Find($"FacilityVisual_SolarPanel_{facId}");
            Assert.That(visualGo, Is.Not.Null);
            Assert.That(visualGo.transform.Find("BlueprintVolume"), Is.Null);
            Assert.That(visualGo.transform.Find("CommissionedSolarDeck"), Is.Not.Null);
            Assert.That(GameObject.Find("BtnBuildBlueprint"), Is.Null, "BtnBuildBlueprint should hide once commissioned.");

            // Save & Load roundtrip preserves commissioned visual and zero extra blueprints
            string savePath = Path.Combine(Application.temporaryCachePath, "op07_build_motion_save.json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                runner.InitializeOperationsMission();
                yield return null;
                Assert.That(GameObject.Find($"FacilityVisual_SolarPanel_{facId}"), Is.Null);

                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                visualGo = GameObject.Find($"FacilityVisual_SolarPanel_{facId}");
                Assert.That(visualGo, Is.Not.Null);
                Assert.That(visualGo.transform.Find("CommissionedSolarDeck"), Is.Not.Null);
            }
            finally
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
        }
    }
}
