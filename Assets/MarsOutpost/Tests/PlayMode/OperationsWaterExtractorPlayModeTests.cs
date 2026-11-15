using System.Collections;
using System.Collections.Generic;
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
    public class OperationsWaterExtractorPlayModeTests
    {
        private static void AdvanceRunnerSim(SimulationRunner runner, double seconds)
        {
            runner.Simulation.Tick(seconds);
            runner.SetPaused(runner.Stepper.UserPaused);
        }

        private static ParcelState FindScannedIceCell(ParcelMap map)
        {
            foreach (var cell in map.Cells)
            {
                if (cell.IsExplored && cell.IsScanned && cell.IsBuildable &&
                    cell.Protection == StarterProtectionKind.None &&
                    cell.ResourceKind == ParcelResourceKind.Ice &&
                    cell.RemainingWater > 0)
                {
                    return cell;
                }
            }
            Assert.Fail("Expected scanned Ice cell.");
            return null;
        }

        private static void EnsureClaimAndUtilityBridge(SimulationRunner runner, GridCoord target)
        {
            var map = runner.State.Map;
            var parent = new Dictionary<GridCoord, GridCoord>();
            var q = new Queue<GridCoord>();
            foreach (var cell in map.Cells)
            {
                if (cell.IsClaimed || cell.Protection != StarterProtectionKind.None)
                {
                    parent[cell.Coord] = cell.Coord;
                    q.Enqueue(cell.Coord);
                }
            }
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == target) break;
                foreach (var n in map.Config.GetCardinalNeighbors(cur))
                {
                    if (!map.TryGetCell(n, out var nCell) || !nCell.IsExplored || parent.ContainsKey(n)) continue;
                    parent[n] = cur;
                    q.Enqueue(n);
                }
            }
            var claimPath = new List<GridCoord>();
            var trace = target;
            while (parent.TryGetValue(trace, out var prev) && prev != trace)
            {
                claimPath.Add(trace);
                trace = prev;
            }
            claimPath.Reverse();
            for (int i = 0; i < claimPath.Count; i++)
            {
                if (!runner.State.Map.GetCell(claimPath[i]).IsClaimed)
                    Assert.That(runner.ClaimParcel(claimPath[i]).Success, Is.True);
            }

            if (!UtilityNetwork.IsCoordConnectedPreview(runner.State.Map, target))
            {
                map = runner.State.Map;
                var connectedLines = UtilityNetwork.ComputeConnectedLineCells(map);
                q.Clear();
                parent.Clear();
                foreach (var coordLine in connectedLines)
                {
                    parent[coordLine] = coordLine;
                    q.Enqueue(coordLine);
                }
                GridCoord? bridgeEnd = null;
                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    foreach (var n in map.Config.GetCardinalNeighbors(cur))
                    {
                        if (n == target && cur != target)
                        {
                            bridgeEnd = cur;
                            break;
                        }
                        if (!map.TryGetCell(n, out var nCell) || !nCell.IsExplored || !nCell.IsBuildable ||
                            nCell.Protection != StarterProtectionKind.None || parent.ContainsKey(n))
                            continue;
                        parent[n] = cur;
                        q.Enqueue(n);
                    }
                    if (bridgeEnd.HasValue) break;
                }
                Assert.That(bridgeEnd.HasValue, Is.True);
                var bridgeSteps = new List<GridCoord>();
                trace = bridgeEnd.Value;
                while (parent.TryGetValue(trace, out var prev) && prev != trace)
                {
                    bridgeSteps.Add(trace);
                    trace = prev;
                }
                bridgeSteps.Reverse();
                for (int i = 0; i < bridgeSteps.Count; i++)
                {
                    var step = bridgeSteps[i];
                    if (!runner.State.Map.GetCell(step).HasUtilityLine)
                    {
                        if (!runner.State.Map.GetCell(step).IsClaimed)
                            Assert.That(runner.ClaimParcel(step).Success, Is.True);
                        string lId = runner.PlaceBlueprint(FacilityKind.UtilityLine, step).Reason;
                        Assert.That(runner.AssignBuildBlueprint("crew-1", lId).Success, Is.True);
                        AdvanceRunnerSim(runner, 60.0);
                        runner.ReturnToShelter("crew-1");
                        AdvanceRunnerSim(runner, 25.0);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator ExtractorCatalogPlacesBuildsDepletesFiniteIceTogglesPerSiteAndSurvivesSaveLoad()
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

            GameObject.Find("BtnBuildMode").GetComponent<Button>().onClick.Invoke();
            yield return null;

            var btnBuildExField = typeof(MarsOutpostHud).GetField("btnBuildExtractor", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnToggleExField = typeof(MarsOutpostHud).GetField("btnToggleSiteExtractor", BindingFlags.Instance | BindingFlags.NonPublic);
            var parcelDetailsField = typeof(MarsOutpostHud).GetField("parcelDetailsText", BindingFlags.Instance | BindingFlags.NonPublic);
            var btnBuildEx = btnBuildExField?.GetValue(hud) as Button;
            var btnToggleEx = btnToggleExField?.GetValue(hud) as Button;
            var parcelDetails = parcelDetailsField?.GetValue(hud) as Text;
            Assert.That(btnBuildEx, Is.Not.Null);
            Assert.That(btnToggleEx, Is.Not.Null);
            Assert.That(parcelDetails, Is.Not.Null);
            Assert.That(btnBuildEx.gameObject.activeInHierarchy, Is.True);

            btnBuildEx.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.WaterExtractor));

            var ice = FindScannedIceCell(runner.State.Map);
            EnsureClaimAndUtilityBridge(runner, ice.Coord);
            var placeRes = runner.PlaceBlueprint(FacilityKind.WaterExtractor, ice.Coord);
            Assert.That(placeRes.Success, Is.True, placeRes.Reason);
            string facId = placeRes.Reason;

            Assert.That(runner.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            AdvanceRunnerSim(runner, 110.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            yield return null;

            var visualGo = GameObject.Find($"FacilityVisual_WaterExtractor_{facId}");
            Assert.That(visualGo, Is.Not.Null);
            Assert.That(visualGo.transform.Find("CommissionedDrillRig"), Is.Not.Null);

            double remBefore = runner.State.Map.GetCell(ice.Coord).RemainingWater;
            AdvanceRunnerSim(runner, 40.0);
            yield return null;
            double remAfter = runner.State.Map.GetCell(ice.Coord).RemainingWater;
            Assert.That(remAfter, Is.LessThan(remBefore));

            GameObject.Find("BtnBuildMode").GetComponent<Button>().onClick.Invoke();
            world.SelectParcel(ice.Coord);
            yield return null;
            Assert.That(btnToggleEx.gameObject.activeInHierarchy, Is.True);
            Assert.That(parcelDetails.text, Does.Match("Çıkarıcı|Extractor"));

            // Per-site toggle OFF pauses extraction -> RemainingWater stops dropping!
            btnToggleEx.onClick.Invoke();
            yield return null;
            double remPaused = runner.State.Map.GetCell(ice.Coord).RemainingWater;
            AdvanceRunnerSim(runner, 25.0);
            yield return null;
            Assert.That(runner.State.Map.GetCell(ice.Coord).RemainingWater, Is.EqualTo(remPaused).Within(1e-9));

            string savePath = Path.Combine(Application.temporaryCachePath, "op13_extractor_playmode_test.json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                yield return null;

                Assert.That(runner.State.Map.GetCell(ice.Coord).RemainingWater, Is.EqualTo(remPaused).Within(1e-9));
                FacilityState loadedFac = null;
                for (int i = 0; i < runner.State.Facilities.Count; i++)
                {
                    if (runner.State.Facilities[i].FacilityId == facId)
                    {
                        loadedFac = runner.State.Facilities[i];
                        break;
                    }
                }
                Assert.That(loadedFac, Is.Not.Null);
                Assert.That(loadedFac.IsEnabled, Is.False);
            }
            finally
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
        }
    }
}
