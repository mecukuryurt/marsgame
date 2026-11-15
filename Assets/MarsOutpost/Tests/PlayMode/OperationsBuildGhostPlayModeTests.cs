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
    public class OperationsBuildGhostPlayModeTests
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

        [UnityTest]
        public IEnumerator BuildGhost_PreviewColorRotationCancel_And_NoMaterialSpentOnCancel()
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

            int startMaterials = runner.State.Materials;
            int startFacilities = runner.State.Facilities.Count;

            var btnBuildSolarGo = GameObject.Find("BtnBuildSolar");
            Assert.That(btnBuildSolarGo, Is.Not.Null);
            btnBuildSolarGo.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.SolarPanel));
            var ghostRoot = GameObject.Find("BuildGhostRoot");
            Assert.That(ghostRoot, Is.Not.Null);
            Assert.That(ghostRoot.activeInHierarchy, Is.True);

            // Hover valid claimed pad (9,5) -> Green valid preview
            world.SetHoveredBuildCell(ParcelMap.SolarWorkCoord);
            yield return null;
            Assert.That(world.LastGhostValid, Is.True);
            var padMr = ghostRoot.transform.Find("BuildGhostPivot/GhostFootprintPad").GetComponent<Renderer>();
            Assert.That(padMr.sharedMaterial.color.g, Is.GreaterThan(padMr.sharedMaterial.color.r));

            // Rotate 90 -> 180 -> 270 -> 0 via public/UI button
            var btnRotateGo = GameObject.Find("BtnRotateGhost");
            Assert.That(btnRotateGo, Is.Not.Null);
            btnRotateGo.GetComponent<Button>().onClick.Invoke();
            Assert.That(world.BuildRotation, Is.EqualTo(FacilityRotation.Deg90));
            btnRotateGo.GetComponent<Button>().onClick.Invoke();
            Assert.That(world.BuildRotation, Is.EqualTo(FacilityRotation.Deg180));

            // Hover invalid protected cell (8,5) -> Red invalid preview
            world.SetHoveredBuildCell(ParcelMap.AirlockCorridorCoord);
            yield return null;
            Assert.That(world.LastGhostValid, Is.False);
            Assert.That(padMr.sharedMaterial.color.r, Is.GreaterThan(padMr.sharedMaterial.color.g));

            // Escape cancels build mode without spending materials
            Assert.That(world.HandleEscapeCancel(), Is.True);
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.Null);
            Assert.That(ghostRoot.activeInHierarchy, Is.False);
            Assert.That(runner.State.Materials, Is.EqualTo(startMaterials));
            Assert.That(runner.State.Facilities.Count, Is.EqualTo(startFacilities));
        }

        [UnityTest]
        public IEnumerator DoubleClickSameFrame_UiClick_InvalidCell_And_BlueprintScaffoldSpawn()
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

            GridCoord validCell = FindClaimedBuildableCell(runner.State);
            world.StartBuildMode(FacilityKind.SolarPanel);
            world.SetHoveredBuildCell(validCell);
            yield return null;

            // Project cell center to screen space to test screen click + same-frame double click
            runner.State.Map.Config.CellToWorldCenter(validCell, out double wx, out double wz);
            Vector3 worldCenter = new Vector3((float)wx, 0f, (float)wz);
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldCenter);

            // Simulate two ProcessScreenClick calls within the exact same frame
            bool firstClick = world.ProcessScreenClick(screenPos, isRightClick: false);
            bool secondSameFrameClick = world.ProcessScreenClick(screenPos, isRightClick: false);
            Assert.That(firstClick, Is.True);
            Assert.That(secondSameFrameClick, Is.True);
            // Wait 1 frame and simulate consecutive double-click on already-occupied cell
            yield return null;
            world.ProcessScreenClick(screenPos, isRightClick: false);
            yield return null;

            // Only 1 SolarPanel blueprint must be created (-12 materials = 28)
            Assert.That(runner.State.Materials, Is.EqualTo(28));
            int solarBpCount = 0;
            FacilityState placedSolar = null;
            for (int i = 0; i < runner.State.Facilities.Count; i++)
            {
                if (runner.State.Facilities[i].Kind == FacilityKind.SolarPanel)
                {
                    solarBpCount++;
                    placedSolar = runner.State.Facilities[i];
                }
            }
            Assert.That(solarBpCount, Is.EqualTo(1));
            Assert.That(placedSolar, Is.Not.Null);
            Assert.That(placedSolar.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(runner.State.Resources.Panels.Count, Is.EqualTo(3));

            // 3D scaffold visual exists in scene
            var scaffoldGo = GameObject.Find($"FacilityVisual_SolarPanel_{placedSolar.FacilityId}");
            Assert.That(scaffoldGo, Is.Not.Null);
            Assert.That(scaffoldGo.activeInHierarchy, Is.True);

            // Clicking HUD button must not place another blueprint
            var btnGridGo = GameObject.Find("BtnGridToggle");
            Assert.That(btnGridGo, Is.Not.Null);
            btnGridGo.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(runner.State.Materials, Is.EqualTo(28));

            // Invalid protected cell placement attempt rejects atomically
            var invalidRes = world.TryPlaceActiveBlueprintAt(ParcelMap.AirlockCorridorCoord);
            Assert.That(invalidRes.Success, Is.False);
            Assert.That(runner.State.Materials, Is.EqualTo(28));

            // Restart clears scaffold and restores materials
            runner.RestartMission();
            yield return null;
            Assert.That(runner.State.Materials, Is.EqualTo(40));
            Assert.That(runner.State.Facilities.Count, Is.EqualTo(3));
            Assert.That(GameObject.Find($"FacilityVisual_SolarPanel_{placedSolar.FacilityId}"), Is.Null);
        }
    }
}
