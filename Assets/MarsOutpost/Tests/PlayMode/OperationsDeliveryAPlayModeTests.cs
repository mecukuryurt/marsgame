using System;
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

namespace MarsOutpost.Tests
{
    public class OperationsDeliveryAPlayModeTests
    {
        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            return typeof(MarsOutpostHud).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as T;
        }

        private static void AdvanceRunnerSim(SimulationRunner runner, double seconds)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(seconds / 2.0));
            double stepDt = seconds / steps;
            for (int i = 0; i < steps; i++)
            {
                runner.Stepper.Simulation.Tick(stepDt);
                if (runner.State.SolReportPauseRequested) runner.AcknowledgeSolReport();
                if (runner.State.CriticalAlertPauseRequested) runner.AcknowledgeCriticalAlert();
            }
            typeof(SimulationRunner).GetMethod("PublishState", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(runner, null);
        }

        private static FacilityState FindFacility(MissionState state, string facId)
        {
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                if (state.Facilities[i].FacilityId == facId)
                    return state.Facilities[i];
            }
            return null;
        }

        private static ParcelState FindScannedIceCell(ParcelMap map)
        {
            foreach (var c in map.Cells)
            {
                if (c.IsExplored && c.IsScanned && c.IsBuildable &&
                    c.Protection == StarterProtectionKind.None &&
                    c.ResourceKind == ParcelResourceKind.Ice &&
                    c.RemainingWater > 0)
                    return c;
            }
            return null;
        }

        private static List<GridCoord> FindClaimPathTo(ParcelMap map, GridCoord target)
        {
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
            while (q.Count > 0 && !parent.ContainsKey(target))
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
            var path = new List<GridCoord>();
            if (!parent.ContainsKey(target)) return path;
            var step = target;
            while (!parent[step].Equals(step))
            {
                path.Add(step);
                step = parent[step];
            }
            path.Reverse();
            return path;
        }

        private static List<GridCoord> FindUtilityBridgePath(ParcelMap map, GridCoord target)
        {
            var connected = UtilityNetwork.ComputeConnectedLineCells(map);
            var q = new Queue<GridCoord>();
            var parent = new Dictionary<GridCoord, GridCoord>();
            foreach (var lc in connected)
            {
                parent[lc] = lc;
                q.Enqueue(lc);
            }
            GridCoord? end = null;
            while (q.Count > 0 && !end.HasValue)
            {
                var cur = q.Dequeue();
                foreach (var n in map.Config.GetCardinalNeighbors(cur))
                {
                    if (n == target && cur != target) { end = cur; break; }
                    if (!map.TryGetCell(n, out var nCell) || parent.ContainsKey(n) || !nCell.IsClaimed || !nCell.IsBuildable || nCell.HasUtilityLine || !string.IsNullOrEmpty(nCell.UtilityBlueprintId))
                        continue;
                    parent[n] = cur;
                    q.Enqueue(n);
                }
            }
            var path = new List<GridCoord>();
            if (!end.HasValue) return path;
            var step = end.Value;
            while (!parent[step].Equals(step))
            {
                path.Add(step);
                step = parent[step];
            }
            path.Reverse();
            return path;
        }

        private static void EnsureMaterialsFor(SimulationRunner runner, int neededMaterials)
        {
            // Advance Sols cleanly (with crew inside shelter resting/researching) if daily supply is needed.
            for (int guard = 0; guard < 8 && runner.State.Materials < neededMaterials; guard++)
                AdvanceRunnerSim(runner, 720.0);
            Assert.That(runner.State.Materials, Is.GreaterThanOrEqualTo(neededMaterials), "Insufficient materials after Sol supply.");
        }

        [UnityTest]
        public IEnumerator DeliveryA_EndToEndHudFlow_ClaimLineSolarBatteryExtractor_SaveReloadRepairDismantle()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = UnityEngine.Object.FindFirstObjectByType<SimulationRunner>();
            var world = UnityEngine.Object.FindFirstObjectByType<MarsOutpostWorldView>();
            var hud = UnityEngine.Object.FindFirstObjectByType<MarsOutpostHud>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            runner.InitializeOperationsMission();
            runner.SetPaused(true);
            yield return null;

            var btnClaim = GetPrivateField<Button>(hud, "btnClaimParcel");
            var btnBuildLine = GetPrivateField<Button>(hud, "btnBuildLine");
            var btnBuildSolar = GetPrivateField<Button>(hud, "btnBuildSolar");
            var btnBuildBattery = GetPrivateField<Button>(hud, "btnBuildBattery");
            var btnBuildExtractor = GetPrivateField<Button>(hud, "btnBuildExtractor");
            var btnBuildBlueprint = GetPrivateField<Button>(hud, "btnBuildBlueprint");
            var btnDismantle = GetPrivateField<Button>(hud, "btnDismantleFacility");
            Assert.That(btnClaim, Is.Not.Null);
            Assert.That(btnBuildLine, Is.Not.Null);
            Assert.That(btnBuildSolar, Is.Not.Null);
            Assert.That(btnBuildBattery, Is.Not.Null);
            Assert.That(btnBuildExtractor, Is.Not.Null);
            Assert.That(btnBuildBlueprint, Is.Not.Null);
            Assert.That(btnDismantle, Is.Not.Null);

            var ice = FindScannedIceCell(runner.State.Map);
            Assert.That(ice, Is.Not.Null);

            // 1. Claim path to Ice cell via HUD btnClaimParcel + verify exact cost deduction.
            var claimPath = FindClaimPathTo(runner.State.Map, ice.Coord);
            Assert.That(claimPath.Count, Is.GreaterThanOrEqualTo(1));
            foreach (var coord in claimPath)
            {
                EnsureMaterialsFor(runner, MissionSimulation.ClaimParcelCost);
                int matBefore = runner.State.Materials;
                world.SelectParcel(coord);
                yield return null;
                Assert.That(btnClaim.interactable, Is.True);
                btnClaim.onClick.Invoke();
                yield return null;
                Assert.That(runner.State.Map.GetCell(coord).IsClaimed, Is.True);
                Assert.That(runner.State.Materials, Is.EqualTo(matBefore - MissionSimulation.ClaimParcelCost));
            }

            // Also claim two buildable non-ice cells that are directly adjacent to the core utility network (for 4th SolarPanel and Battery).
            var extraCells = new List<GridCoord>();
            foreach (var c in runner.State.Map.Cells)
            {
                if (!c.IsClaimed && c.IsExplored && c.IsBuildable && c.Protection == StarterProtectionKind.None &&
                    c.Coord != ice.Coord && UtilityNetwork.IsCoordConnectedPreview(runner.State.Map, c.Coord))
                {
                    if (runner.ClaimParcel(c.Coord).Success)
                    {
                        extraCells.Add(c.Coord);
                        if (extraCells.Count >= 2) break;
                    }
                }
            }
            Assert.That(extraCells.Count, Is.GreaterThanOrEqualTo(2));

            // 2. Build UtilityLine bridge to ice cell using HUD btnBuildLine + btnBuildBlueprint.
            var bridge = FindUtilityBridgePath(runner.State.Map, ice.Coord);
            Assert.That(bridge.Count, Is.GreaterThanOrEqualTo(1));
            foreach (var lineCoord in bridge)
            {
                EnsureMaterialsFor(runner, FacilityCatalog.GetSpec(FacilityKind.UtilityLine).MaterialCost);
                btnBuildLine.onClick.Invoke();
                yield return null;
                Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.UtilityLine));

                int matBeforeLine = runner.State.Materials;
                var lineRes = runner.PlaceBlueprint(FacilityKind.UtilityLine, lineCoord);
                Assert.That(lineRes.Success, Is.True, lineRes.Reason);
                Assert.That(runner.State.Materials, Is.EqualTo(matBeforeLine - FacilityCatalog.GetSpec(FacilityKind.UtilityLine).MaterialCost));

                world.SelectParcel(lineCoord);
                yield return null;
                btnBuildBlueprint.onClick.Invoke();
                AdvanceRunnerSim(runner, 60.0);
                runner.ReturnToShelter("crew-1");
                AdvanceRunnerSim(runner, 25.0);
                Assert.That(FindFacility(runner.State, lineRes.Reason).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            }

            // 3. Build 4th SolarPanel via HUD + mid-construction Save/Load persistence!
            GridCoord solarCoord = extraCells[0];
            EnsureMaterialsFor(runner, FacilityCatalog.GetSpec(FacilityKind.SolarPanel).MaterialCost);
            btnBuildSolar.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.SolarPanel));

            int matBeforeSolar = runner.State.Materials;
            var solarRes = runner.PlaceBlueprint(FacilityKind.SolarPanel, solarCoord);
            Assert.That(solarRes.Success, Is.True, solarRes.Reason);
            string solarFacId = solarRes.Reason;
            Assert.That(runner.State.Materials, Is.EqualTo(matBeforeSolar - FacilityCatalog.GetSpec(FacilityKind.SolarPanel).MaterialCost));

            world.SelectParcel(solarCoord);
            yield return null;
            btnBuildBlueprint.onClick.Invoke();
            AdvanceRunnerSim(runner, 35.0); // mid-construction!
            var midSolar = FindFacility(runner.State, solarFacId);
            Assert.That(midSolar.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            double midWork = midSolar.WorkProgressSeconds;
            Assert.That(midWork, Is.GreaterThan(0.0));

            string savePath = Path.Combine(Application.temporaryCachePath, "op15_delivery_a_midbuild.json");
            try
            {
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                var reloadedMidSolar = FindFacility(runner.State, solarFacId);
                Assert.That(reloadedMidSolar.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
                Assert.That(reloadedMidSolar.WorkProgressSeconds, Is.EqualTo(midWork).Within(1e-6));
            }
            finally
            {
                if (File.Exists(savePath)) File.Delete(savePath);
            }

            // Finish building 4th SolarPanel -> verify 4 panels exist!
            AdvanceRunnerSim(runner, 85.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            Assert.That(FindFacility(runner.State, solarFacId).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(runner.State.Resources.Panels.Count, Is.EqualTo(4));

            // 4. Build Battery via HUD -> verify EffectiveBatteryCapacity == 150!
            GridCoord batCoord = extraCells[1];
            EnsureMaterialsFor(runner, FacilityCatalog.GetSpec(FacilityKind.Battery).MaterialCost);
            btnBuildBattery.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.Battery));

            var batRes = runner.PlaceBlueprint(FacilityKind.Battery, batCoord);
            Assert.That(batRes.Success, Is.True, batRes.Reason);
            string batFacId = batRes.Reason;
            world.SelectParcel(batCoord);
            yield return null;
            btnBuildBlueprint.onClick.Invoke();
            AdvanceRunnerSim(runner, 110.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            Assert.That(FindFacility(runner.State, batFacId).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            double expectedCap = 100.0 + FacilityCatalog.GetSpec(FacilityKind.Battery).BatteryCapacityDelta;
            Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(expectedCap).Within(1e-6));

            // 5. Build WaterExtractor on Ice cell via HUD -> verify extraction reduces RemainingWater!
            EnsureMaterialsFor(runner, FacilityCatalog.GetSpec(FacilityKind.WaterExtractor).MaterialCost);
            btnBuildExtractor.onClick.Invoke();
            yield return null;
            Assert.That(world.ActiveBuildKind, Is.EqualTo(FacilityKind.WaterExtractor));

            var exRes = runner.PlaceBlueprint(FacilityKind.WaterExtractor, ice.Coord);
            Assert.That(exRes.Success, Is.True, exRes.Reason);
            string exFacId = exRes.Reason;
            world.SelectParcel(ice.Coord);
            yield return null;
            btnBuildBlueprint.onClick.Invoke();
            AdvanceRunnerSim(runner, 120.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            Assert.That(FindFacility(runner.State, exFacId).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));

            double remBefore = runner.State.Map.GetCell(ice.Coord).RemainingWater;
            AdvanceRunnerSim(runner, 30.0);
            Assert.That(runner.State.Map.GetCell(ice.Coord).RemainingWater, Is.LessThan(remBefore));
            Assert.That(runner.State.Resources.TotalSiteExtractedWater, Is.GreaterThan(0.0));

            // 6. Dismantle Battery via HUD btnDismantleFacility -> verify 50% material refund & capacity returns to 100!
            int matBeforeDismantle = runner.State.Materials;
            world.SelectParcel(batCoord);
            yield return null;
            Assert.That(btnDismantle.gameObject.activeInHierarchy, Is.True);
            Assert.That(btnDismantle.interactable, Is.True);
            btnDismantle.onClick.Invoke();
            yield return null;
            Assert.That(FindFacility(runner.State, batFacId), Is.Null);
            Assert.That(runner.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-6));
            Assert.That(runner.State.Materials, Is.EqualTo(matBeforeDismantle + FacilityCatalog.GetSpec(FacilityKind.Battery).MaterialCost / 2));
            Assert.That(runner.State.MaterialsRefundedTotal, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DeliveryA_TrainingAndDemoModeRegression_RemainIntact()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = UnityEngine.Object.FindFirstObjectByType<SimulationRunner>();
            var hud = UnityEngine.Object.FindFirstObjectByType<MarsOutpostHud>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            var btnGridToggleObj = GetPrivateField<GameObject>(hud, "btnGridToggleObj");
            var operationsBuildPanel = GetPrivateField<GameObject>(hud, "operationsBuildPanel");

            // Demo Mode check
            runner.InitializeDemoMission();
            yield return null;
            Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Demo));
            if (btnGridToggleObj != null) Assert.That(btnGridToggleObj.activeSelf, Is.False);
            if (operationsBuildPanel != null) Assert.That(operationsBuildPanel.activeSelf, Is.False);
            AdvanceRunnerSim(runner, 20.0);
            Assert.That(runner.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));

            // Training Mode check
            runner.InitializeDefaultMission();
            yield return null;
            Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Training));
            if (btnGridToggleObj != null) Assert.That(btnGridToggleObj.activeSelf, Is.False);
            if (operationsBuildPanel != null) Assert.That(operationsBuildPanel.activeSelf, Is.False);
            Assert.That(runner.AssignTask("crew-1", "research").Success, Is.True);
            double sciBefore = runner.State.Resources.ResearchProgress;
            AdvanceRunnerSim(runner, 30.0);
            Assert.That(runner.State.Resources.ResearchProgress, Is.GreaterThan(sciBefore));
        }
    }
}
