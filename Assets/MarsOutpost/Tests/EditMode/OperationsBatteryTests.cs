using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsBatteryTests
    {
        private static MissionSimulation CreateControlledOpsSim(
            double initialBattery = 60,
            double initialPanelFouling = 0.0,
            int initialMaterials = 90)
        {
            var resCfg = new ResourceConfig(
                solarRate: 0.35,
                batteryCapacity: 100,
                initialBattery: initialBattery,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: initialPanelFouling,
                panelMaintenanceReduction: 0.20,
                initialSystemWear: 0.0,
                systemMaintenanceReduction: 0.15,
                waterExtractionRate: 0.012,
                waterExtractorEnergyRate: 0.015);
            var cfg = new MissionConfig(
                solSeconds: 720,
                daySeconds: 480,
                resources: resCfg,
                researchGoal: 0,
                targetSols: 0,
                maxReserveMeals: 2,
                dailyReserveMealSupply: 1,
                operationsSeed: 424242,
                enableOperationsRandomEvents: false,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: initialMaterials,
                materialCapacity: Math.Max(120, initialMaterials));
            var sim = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            sim.SetResearchEnabled(false);
            sim.SetWaterExtractorEnabled(false);
            return sim;
        }

        private static void EnsureClaimed(MissionSimulation sim, GridCoord coord)
        {
            if (!sim.State.Map.GetCell(coord).IsClaimed)
            {
                var res = sim.ClaimParcel(coord);
                Assert.That(res.Success, Is.True, $"Failed to claim {coord}: {res.Reason}");
            }
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

        private static List<GridCoord> FindTwoStepChainFromCore(ParcelMap map)
        {
            foreach (var c in map.Cells)
            {
                if (c.Protection == StarterProtectionKind.None || !c.HasUtilityLine)
                    continue;
                foreach (var n1 in map.Config.GetCardinalNeighbors(c.Coord))
                {
                    if (!map.TryGetCell(n1, out var cell1) || !cell1.IsExplored || !cell1.IsBuildable || cell1.Protection != StarterProtectionKind.None)
                        continue;
                    foreach (var n2 in map.Config.GetCardinalNeighbors(n1))
                    {
                        if (n2 == c.Coord) continue;
                        if (!map.TryGetCell(n2, out var cell2) || !cell2.IsExplored || !cell2.IsBuildable || cell2.Protection != StarterProtectionKind.None)
                            continue;
                        if (UtilityNetwork.IsCoordConnectedPreview(map, n2))
                            continue;
                        return new List<GridCoord> { n1, n2 };
                    }
                }
            }
            Assert.Fail("Expected 2-step chain from core.");
            return null;
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

        private static string BuildAndCommission(MissionSimulation sim, FacilityKind kind, GridCoord coord, string crewId = "crew-1", double buildSeconds = 90.0)
        {
            EnsureClaimed(sim, coord);
            var place = sim.PlaceBlueprint(kind, coord);
            Assert.That(place.Success, Is.True, $"Place {kind} at {coord} failed: {place.Reason}");
            string facId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            var assign = sim.AssignBuildBlueprint(crewId, facId);
            Assert.That(assign.Success, Is.True, $"Assign build {facId} failed: {assign.Reason}");
            sim.Tick(buildSeconds);
            Assert.That(FindFacility(sim.State, facId).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            sim.ReturnToShelter(crewId);
            sim.Tick(25.0);
            return facId;
        }

        [Test]
        public void BlueprintAndUnconnectedBatteryAddZeroCapacity_CommissionedConnectedBatteryAdds25WithoutFreeEnergy()
        {
            var sim = CreateControlledOpsSim(initialBattery: 60);
            var chain = FindTwoStepChainFromCore(sim.State.Map);
            GridCoord n1 = chain[0];
            GridCoord n2 = chain[1];

            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));

            EnsureClaimed(sim, n1);
            EnsureClaimed(sim, n2);
            Assert.That(sim.PlaceBlueprint(FacilityKind.Battery, n2).Success, Is.True);
            string batId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));

            // Commissioning at n2 while unconnected keeps capacity at 100.
            Assert.That(sim.AssignBuildBlueprint("crew-1", batId).Success, Is.True);
            sim.Tick(95.0);
            var batFac = FindFacility(sim.State, batId);
            Assert.That(batFac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(batFac.IsConnectedToCore, Is.False);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));
            sim.ReturnToShelter("crew-1");
            sim.Tick(25.0);

            // Connect n1 with UtilityLine -> capacity expands to 125 without granting free battery energy.
            Assert.That(sim.PlaceBlueprint(FacilityKind.UtilityLine, n1).Success, Is.True);
            string lineId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            Assert.That(sim.AssignBuildBlueprint("crew-1", lineId).Success, Is.True);

            double batteryPrev = sim.State.Resources.Battery;
            bool connectedObserved = false;
            for (int step = 0; step < 80; step++)
            {
                sim.Tick(1.0);
                double batNow = sim.State.Resources.Battery;
                Assert.That(batNow - batteryPrev, Is.LessThan(2.0), "Connecting a battery must not inject free stored energy.");
                batteryPrev = batNow;
                if (FindFacility(sim.State, batId).IsConnectedToCore)
                {
                    connectedObserved = true;
                    break;
                }
            }

            Assert.That(connectedObserved, Is.True);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
        }

        [Test]
        public void DayChargingFillsAbove100ToExact125AndNightDischargesContinuouslyThrough100()
        {
            var sim = CreateControlledOpsSim(initialBattery: 92, initialPanelFouling: 0.0);
            GridCoord target = FindAdjacentConnectedBuildable(sim.State.Map);
            BuildAndCommission(sim, FacilityKind.Battery, target);

            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));

            // Daytime solar surplus should fill battery above 100 up to exact 125 clamp.
            double toLateDay = 460.0 - sim.State.ElapsedSeconds;
            Assert.That(toLateDay, Is.GreaterThan(0.0));
            sim.Tick(toLateDay);
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(125.0).Within(1e-6));

            // Stamina and risk limits remain at 100.
            for (int i = 0; i < sim.State.Crew.Count; i++)
            {
                Assert.That(sim.State.Crew[i].Stamina, Is.LessThanOrEqualTo(100.0 + 1e-9));
                Assert.That(sim.State.Crew[i].RiskScore, Is.LessThanOrEqualTo(100.0 + 1e-9));
            }

            // Advance into night and enable research so night consumption cleanly crosses 100 within Sol 1 night.
            sim.Tick(481.0 - sim.State.ElapsedSeconds);
            Assert.That(sim.State.IsNight, Is.True);
            sim.SetResearchEnabled(true);
            Assert.That(sim.AssignTask("crew-2", "research").Success, Is.True);

            double prev = sim.State.Resources.Battery;
            bool crossed100Smoothly = false;
            for (int s = 0; s < 220; s++)
            {
                sim.Tick(1.0);
                double cur = sim.State.Resources.Battery;
                double drop = prev - cur;
                Assert.That(drop, Is.GreaterThan(0.0).And.LessThan(1.0),
                    $"Unexpected discontinuity discharging across 100: {prev} -> {cur}");
                if (prev > 100.0 && cur <= 100.0)
                    crossed100Smoothly = true;
                prev = cur;
                if (crossed100Smoothly)
                    break;
            }
            Assert.That(crossed100Smoothly, Is.True, "Expected smooth night discharge across 100 boundary.");
        }

        [Test]
        public void DisconnectingBatteryWithHighChargeClampsTo100AndLogsNonRetroactiveOverflow()
        {
            var sim = CreateControlledOpsSim(initialBattery: 85, initialPanelFouling: 0.0, initialMaterials: 110);
            var chain = FindTwoStepChainFromCore(sim.State.Map);
            GridCoord n1 = chain[0];
            GridCoord n2 = chain[1];

            string lineId = BuildAndCommission(sim, FacilityKind.UtilityLine, n1);
            string batId = BuildAndCommission(sim, FacilityKind.Battery, n2);
            var batFac = FindFacility(sim.State, batId);
            Assert.That(batFac.IsConnectedToCore, Is.True);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));

            // Charge well past 100 during remaining daytime.
            double toAfternoon = 450.0 - sim.State.ElapsedSeconds;
            if (toAfternoon > 0)
                sim.Tick(toAfternoon);
            Assert.That(sim.State.Resources.Battery, Is.GreaterThan(110.0));

            double previewLoss = ColonyStorage.PreviewBatteryLossIfFacilityDisconnected(sim.Config.Resources, sim.State, batFac);
            Assert.That(previewLoss, Is.EqualTo(sim.State.Resources.Battery - 100.0).Within(1e-6));

            var removeRes = sim.RemoveUtilityLine(lineId);
            Assert.That(removeRes.Success, Is.True, removeRes.Reason);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(100.0).Within(1e-6));
            Assert.That(sim.State.Resources.BatteryOverflowLostTotal, Is.EqualTo(previewLoss).Within(1e-6));

            // Advance into night before reconnecting so no solar generation confounds verifying no free restore.
            sim.Tick(481.0 - sim.State.ElapsedSeconds);
            Assert.That(sim.State.IsNight, Is.True);
            BuildAndCommission(sim, FacilityKind.UtilityLine, n1);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
            Assert.That(sim.State.Resources.Battery, Is.LessThanOrEqualTo(100.0 + 1e-6));
            Assert.That(sim.State.Resources.BatteryOverflowLostTotal, Is.EqualTo(previewLoss).Within(1e-6));
        }

        [Test]
        public void SaveLoadRoundTripPreservesHighChargeEffectiveCapAndOverflowAndRejectsTamperedOverCap()
        {
            var sim = CreateControlledOpsSim(initialBattery: 85, initialPanelFouling: 0.0, initialMaterials: 120);
            var chain = FindTwoStepChainFromCore(sim.State.Map);
            GridCoord n1 = chain[0];
            GridCoord n2 = chain[1];
            GridCoord adj = FindAdjacentConnectedBuildable(sim.State.Map);
            if (adj == n1 || adj == n2)
            {
                // Pick another connected buildable cell for Battery A so both batteries are independent.
                foreach (var cell in sim.State.Map.Cells)
                {
                    if (cell.Coord != n1 && cell.Coord != n2 && cell.IsExplored && cell.IsBuildable &&
                        cell.Protection == StarterProtectionKind.None && UtilityNetwork.IsCoordConnectedPreview(sim.State.Map, cell.Coord))
                    {
                        adj = cell.Coord;
                        break;
                    }
                }
            }

            BuildAndCommission(sim, FacilityKind.Battery, adj);
            string lineId = BuildAndCommission(sim, FacilityKind.UtilityLine, n1);
            BuildAndCommission(sim, FacilityKind.Battery, n2);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(150.0).Within(1e-9));

            // Charge above 125 during daytime.
            double toAfternoon = 460.0 - sim.State.ElapsedSeconds;
            if (toAfternoon > 0)
                sim.Tick(toAfternoon);
            Assert.That(sim.State.Resources.Battery, Is.GreaterThan(130.0));

            // Remove n1 so Battery 2 disconnects: effective cap 150 -> 125, Battery clamped at 125 (>100), overflow logged (>0).
            Assert.That(sim.RemoveUtilityLine(lineId).Success, Is.True);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(125.0).Within(1e-6));
            double expectedOverflow = sim.State.Resources.BatteryOverflowLostTotal;
            Assert.That(expectedOverflow, Is.GreaterThan(5.0));

            string json = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(json, out var loaded, out string loadErr), Is.True, loadErr);
            Assert.That(loaded.State.EffectiveBatteryCapacity, Is.EqualTo(125.0).Within(1e-9));
            Assert.That(loaded.State.Resources.Battery, Is.EqualTo(125.0).Within(1e-6));
            Assert.That(loaded.State.Resources.BatteryOverflowLostTotal, Is.EqualTo(expectedOverflow).Within(1e-6));

            // Tampering battery above effectiveBatteryCapacity must be rejected by ValidateSnapshot.
            string tampered = Regex.Replace(json, "\"battery\":[0-9.]+", "\"battery\":140");
            Assert.That(tampered, Is.Not.EqualTo(json));
            Assert.That(MissionSimulation.TryLoadFromJson(tampered, out _, out string tamperErr), Is.False);
            Assert.That(tamperErr, Does.Contain("battery"));
        }
    }
}
