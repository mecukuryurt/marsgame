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
    public class OperationsExtractorTelemetryPlayModeTests
    {
        private static void ConfigureControlledOpsRunner(SimulationRunner runner, double initialWater = 40, double initialBattery = 85)
        {
            var resCfg = new ResourceConfig(
                solarRate: 0.35,
                batteryCapacity: 100,
                waterCapacity: 100,
                initialBattery: initialBattery,
                initialWater: initialWater,
                oxygenProductionRate: 0.05,
                waterUsePerCrew: 0.004,
                waterExtractionRate: 0.008,
                waterExtractorEnergyRate: 0.015);
            var cfg = new MissionConfig(
                solSeconds: 720,
                daySeconds: 480,
                resources: resCfg,
                researchGoal: 0,
                targetSols: 0,
                sleepTimeScale: 8.0,
                maxReserveMeals: 2,
                dailyReserveMealSupply: 1,
                operationsSeed: 424242,
                enableOperationsRandomEvents: false,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: 110,
                materialCapacity: 140);
            runner.InitializeMission(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            runner.SetResearchEnabled(false);
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

        private static void EnsureClaimAndUtilityBridge(SimulationRunner runner, GridCoord target)
        {
            var map = runner.State.Map;
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
            var claimPath = new List<GridCoord>();
            var step = target;
            while (!parent[step].Equals(step))
            {
                claimPath.Add(step);
                step = parent[step];
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
                var connected = UtilityNetwork.ComputeConnectedLineCells(map);
                q.Clear();
                parent.Clear();
                foreach (var lineCoord in connected)
                {
                    parent[lineCoord] = lineCoord;
                    q.Enqueue(lineCoord);
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
                        if (!map.TryGetCell(n, out var nCell) || parent.ContainsKey(n) || !nCell.IsClaimed || !nCell.IsBuildable || nCell.HasUtilityLine || !string.IsNullOrEmpty(nCell.UtilityBlueprintId))
                            continue;
                        parent[n] = cur;
                        q.Enqueue(n);
                    }
                    if (bridgeEnd.HasValue) break;
                }
                Assert.That(bridgeEnd.HasValue, Is.True);
                var linePath = new List<GridCoord>();
                var lStep = bridgeEnd.Value;
                while (!parent[lStep].Equals(lStep))
                {
                    linePath.Add(lStep);
                    lStep = parent[lStep];
                }
                linePath.Reverse();
                for (int i = 0; i < linePath.Count; i++)
                {
                    var lBp = runner.PlaceBlueprint(FacilityKind.UtilityLine, linePath[i]);
                    Assert.That(lBp.Success, Is.True, lBp.Reason);
                    Assert.That(runner.AssignBuildBlueprint("crew-1", lBp.Reason).Success, Is.True);
                    AdvanceRunnerSim(runner, 55.0);
                    runner.ReturnToShelter("crew-1");
                    AdvanceRunnerSim(runner, 25.0);
                }
            }
        }

        [UnityTest]
        public IEnumerator ExtractorVisualReflectsBlueprintPause8xStopReasonsAndTelemetry()
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

            ConfigureControlledOpsRunner(runner, initialWater: 40, initialBattery: 85);
            yield return null;

            var ice = FindScannedIceCell(runner.State.Map);
            Assert.That(ice, Is.Not.Null);
            EnsureClaimAndUtilityBridge(runner, ice.Coord);

            var placeRes = runner.PlaceBlueprint(FacilityKind.WaterExtractor, ice.Coord);
            Assert.That(placeRes.Success, Is.True, placeRes.Reason);
            string facId = placeRes.Reason;
            yield return null;

            var bpGo = GameObject.Find($"FacilityVisual_WaterExtractor_{facId}");
            Assert.That(bpGo, Is.Not.Null);
            Assert.That(bpGo.transform.Find("BlueprintBuildBar"), Is.Not.Null);
            var bpLabel = bpGo.transform.Find("ExtractorBlueprintLabel")?.GetComponent<TextMesh>();
            Assert.That(bpLabel, Is.Not.Null);
            Assert.That(bpLabel.text, Does.Match("KURULUM|BUILD"));

            // Start construction -> verify active recall button can recall crew if clicked on a separate check, then complete build.
            Assert.That(runner.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            AdvanceRunnerSim(runner, 110.0);
            runner.ReturnToShelter("crew-1");
            AdvanceRunnerSim(runner, 25.0);
            yield return null;

            var activeGo = GameObject.Find($"FacilityVisual_WaterExtractor_{facId}");
            Assert.That(activeGo, Is.Not.Null);
            var spindle = activeGo.transform.Find("CommissionedDrillSpindle");
            var label = activeGo.transform.Find("ExtractorStatusLabel")?.GetComponent<TextMesh>();
            Assert.That(spindle, Is.Not.Null);
            Assert.That(label, Is.Not.Null);

            // Running at 1x -> spindle rotates!
            runner.SetPaused(false);
            float yBeforePlay = spindle.localEulerAngles.y;
            for (int f = 0; f < 5; f++) yield return null;
            float yAfterPlay = spindle.localEulerAngles.y;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yBeforePlay, yAfterPlay)), Is.GreaterThan(0.1f));

            // Paused -> spindle stops completely!
            runner.SetPaused(true);
            yield return null;
            float yPausedStart = spindle.localEulerAngles.y;
            for (int f = 0; f < 5; f++) yield return null;
            float yPausedEnd = spindle.localEulerAngles.y;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yPausedStart, yPausedEnd)), Is.LessThan(0.01f));

            // Advance to nighttime so BeginSleep() (8x) is legal -> spindle advances cleanly at 8x!
            for (int k = 0; k < 25 && !runner.State.IsNight; k++)
                AdvanceRunnerSim(runner, 20.0);
            Assert.That(runner.State.IsNight, Is.True);
            runner.SetPaused(false);
            var sleepRes = runner.BeginSleep();
            Assert.That(sleepRes.Success, Is.True, sleepRes.Reason);
            float ySleepStart = spindle.localEulerAngles.y;
            for (int f = 0; f < 4; f++) yield return null;
            float ySleepEnd = spindle.localEulerAngles.y;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(ySleepStart, ySleepEnd)), Is.GreaterThan(0.5f));
            runner.CancelSleep();
            runner.SetPaused(true);

            // Site OFF -> world label shows stop reason & HUD telemetry shows finite/infinite ETA text cleanly.
            Assert.That(runner.SetFacilityEnabled(facId, false).Success, Is.True);
            yield return null;
            Assert.That(label.text, Does.Match("SAHA KAPALI|SITE OFF"));

            world.SelectParcel(ice.Coord);
            yield return null;
            var parcelDetails = typeof(MarsOutpostHud).GetField("parcelDetailsText", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hud) as Text;
            Assert.That(parcelDetails, Is.Not.Null);
            Assert.That(parcelDetails.text, Does.Contain($"({ice.Coord.X},{ice.Coord.Z})"));
            Assert.That(parcelDetails.text, Does.Match("∞ \\(Durdu\\)|∞ \\(Paused\\)"));
            Assert.That(parcelDetails.text, Does.Not.Contain("Infinity"));
            Assert.That(parcelDetails.text, Does.Not.Contain("NaN"));

            // Re-enable site, check water breakdown card + one-click focus + save/load round-trip.
            Assert.That(runner.SetFacilityEnabled(facId, true).Success, Is.True);
            AdvanceRunnerSim(runner, 10.0);
            yield return null;

            var waterBreakdown = typeof(MarsOutpostHud).GetField("waterFlowBreakdownText", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hud) as Text;
            Assert.That(waterBreakdown, Is.Not.Null);
            Assert.That(waterBreakdown.text, Does.Match("Çıkarma|Extract"));
            Assert.That(waterBreakdown.text, Does.Match("Geri Kaz|Recycle"));

            var btnFocus = typeof(MarsOutpostHud).GetField("btnFocusExtractor", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hud) as Button;
            Assert.That(btnFocus, Is.Not.Null);
            Assert.That(btnFocus.interactable, Is.True);
            world.ClearParcelSelection();
            yield return null;
            btnFocus.onClick.Invoke();
            yield return null;
            Assert.That(world.SelectedParcel, Is.EqualTo(ice.Coord));

            string savePath = Path.Combine(Application.temporaryCachePath, "op14_telemetry_playmode_test.json");
            try
            {
                double expectedRecycled = runner.State.Resources.LastRecycledWaterRate;
                Assert.That(runner.SaveOperationsToFile(savePath).Success, Is.True);
                Assert.That(runner.LoadOperationsFromFile(savePath).Success, Is.True);
                yield return null;
                Assert.That(runner.State.Resources.LastRecycledWaterRate, Is.EqualTo(expectedRecycled).Within(1e-9));
                Assert.That(GameObject.Find($"FacilityVisual_WaterExtractor_{facId}"), Is.Not.Null);
            }
            finally
            {
                if (File.Exists(savePath)) File.Delete(savePath);
            }
        }
    }
}
