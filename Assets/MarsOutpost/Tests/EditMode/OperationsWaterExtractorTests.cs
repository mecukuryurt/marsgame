using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsWaterExtractorTests
    {
        private static MissionSimulation CreateControlledOpsSim(
            double initialBattery = 80,
            double initialWater = 50,
            double solarRate = 0.35,
            double waterExtractionRate = 0.012,
            double waterExtractorEnergyRate = 0.015,
            double waterUsePerCrew = 0.004,
            double oxygenProductionRate = 0.05,
            int initialMaterials = 90,
            bool enableRandomEvents = false)
        {
            var resCfg = new ResourceConfig(
                solarRate: solarRate,
                batteryCapacity: 100,
                waterCapacity: 100,
                initialBattery: initialBattery,
                initialWater: initialWater,
                oxygenProductionRate: oxygenProductionRate,
                waterUsePerCrew: waterUsePerCrew,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: 0.0,
                panelMaintenanceReduction: 0.20,
                initialSystemWear: 0.0,
                systemMaintenanceReduction: 0.15,
                waterExtractionRate: waterExtractionRate,
                waterExtractorEnergyRate: waterExtractorEnergyRate);
            var cfg = new MissionConfig(
                solSeconds: 720,
                daySeconds: 480,
                resources: resCfg,
                researchGoal: 0,
                targetSols: 0,
                maxReserveMeals: 2,
                dailyReserveMealSupply: 1,
                operationsSeed: 424242,
                enableOperationsRandomEvents: enableRandomEvents,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: initialMaterials,
                materialCapacity: Math.Max(120, initialMaterials));
            var sim = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            sim.SetResearchEnabled(false);
            return sim;
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
            Assert.Fail("Expected at least one explored scanned Ice parcel on starter Operations map.");
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

        private static void EnsureClaimPathTo(MissionSimulation sim, GridCoord target)
        {
            // Claim step-by-step from already claimed cells towards target via simple BFS.
            var map = sim.State.Map;
            if (map.GetCell(target).IsClaimed)
                return;

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
                if (cur == target)
                    break;
                foreach (var n in map.Config.GetCardinalNeighbors(cur))
                {
                    if (!map.TryGetCell(n, out var nCell) || !nCell.IsExplored || parent.ContainsKey(n))
                        continue;
                    parent[n] = cur;
                    q.Enqueue(n);
                }
            }

            var path = new List<GridCoord>();
            var trace = target;
            while (parent.TryGetValue(trace, out var prev) && prev != trace)
            {
                path.Add(trace);
                trace = prev;
            }
            path.Reverse();
            for (int i = 0; i < path.Count; i++)
            {
                if (!sim.State.Map.GetCell(path[i]).IsClaimed)
                    Assert.That(sim.ClaimParcel(path[i]).Success, Is.True);
            }
        }

        private static string ConnectAndBuildWaterExtractor(MissionSimulation sim, GridCoord iceCoord, string crewId = "crew-1")
        {
            EnsureClaimPathTo(sim, iceCoord);
            // Ensure cardinal UtilityLine reachability from core if iceCoord isn't adjacent to an existing line.
            if (!UtilityNetwork.IsCoordConnectedPreview(sim.State.Map, iceCoord))
            {
                // Find path of claimable buildable cells from any core-connected line cell to a neighbor of iceCoord.
                var map = sim.State.Map;
                var connectedLines = UtilityNetwork.ComputeConnectedLineCells(map);
                var q = new Queue<GridCoord>();
                var parent = new Dictionary<GridCoord, GridCoord>();
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
                        if (n == iceCoord && cur != iceCoord)
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
                Assert.That(bridgeEnd.HasValue, Is.True, "Expected utility bridge route to Ice parcel.");
                var steps = new List<GridCoord>();
                var trace = bridgeEnd.Value;
                while (parent.TryGetValue(trace, out var prev) && prev != trace)
                {
                    steps.Add(trace);
                    trace = prev;
                }
                steps.Reverse();
                for (int i = 0; i < steps.Count; i++)
                {
                    if (!sim.State.Map.GetCell(steps[i]).HasUtilityLine)
                    {
                        EnsureClaimPathTo(sim, steps[i]);
                        string lId = sim.PlaceBlueprint(FacilityKind.UtilityLine, steps[i]).Reason;
                        Assert.That(sim.AssignBuildBlueprint(crewId, lId).Success, Is.True);
                        sim.Tick(60.0);
                        sim.ReturnToShelter(crewId);
                        sim.Tick(25.0);
                    }
                }
            }

            var place = sim.PlaceBlueprint(FacilityKind.WaterExtractor, iceCoord);
            Assert.That(place.Success, Is.True, place.Reason);
            string facId = place.Reason;
            Assert.That(sim.AssignBuildBlueprint(crewId, facId).Success, Is.True);
            sim.Tick(110.0);
            Assert.That(FindFacility(sim.State, facId).LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            sim.ReturnToShelter(crewId);
            sim.Tick(25.0);
            return facId;
        }

        [Test]
        public void StarterMapHasScannedIceAndRejectsWrongResourceOrUnscannedSitesAtomically()
        {
            var sim = CreateControlledOpsSim();
            var ice = FindScannedIceCell(sim.State.Map);
            Assert.That(ice.RemainingWater, Is.GreaterThanOrEqualTo(250.0));

            // Unclaimed ice rejected with zero material loss.
            int startMat = sim.State.Materials;
            if (!ice.IsClaimed)
            {
                Assert.That(sim.PlaceBlueprint(FacilityKind.WaterExtractor, ice.Coord).Success, Is.False);
                Assert.That(sim.State.Materials, Is.EqualTo(startMat));
            }

            // Claim an adjacent non-Ice buildable cell -> placing WaterExtractor must reject cleanly.
            foreach (var cell in sim.State.Map.Cells)
            {
                if (cell.IsExplored && cell.IsBuildable && cell.Protection == StarterProtectionKind.None &&
                    cell.ResourceKind != ParcelResourceKind.Ice &&
                    UtilityNetwork.IsCoordConnectedPreview(sim.State.Map, cell.Coord))
                {
                    EnsureClaimPathTo(sim, cell.Coord);
                    int matBefore = sim.State.Materials;
                    var wrongRes = sim.PlaceBlueprint(FacilityKind.WaterExtractor, cell.Coord);
                    Assert.That(wrongRes.Success, Is.False);
                    Assert.That(sim.State.Materials, Is.EqualTo(matBefore));
                    break;
                }
            }
        }

        [Test]
        public void ConnectedSiteExtractsFiniteWaterAlongsideLegacy_RespectsPerSiteAndMasterTogglesAndZeroConnection()
        {
            var sim = CreateControlledOpsSim(initialBattery: 85, initialWater: 30);
            var ice = FindScannedIceCell(sim.State.Map);
            string facId = ConnectAndBuildWaterExtractor(sim, ice.Coord);
            var built = FindFacility(sim.State, facId);
            Assert.That(built.IsConnectedToCore, Is.True);

            double waterStart = sim.State.Resources.Water;
            double remStart = sim.State.Map.GetCell(ice.Coord).RemainingWater;
            double siteTotalBefore = sim.State.Resources.TotalSiteExtractedWater;
            double legacyTotalBefore = sim.State.Resources.TotalLegacyExtractedWater;

            sim.Tick(60.0);

            var facAfter = FindFacility(sim.State, facId);
            double remAfter = sim.State.Map.GetCell(ice.Coord).RemainingWater;
            double drawnFromSite = remStart - remAfter;
            double nominalSite = FacilityCatalog.GetSpec(FacilityKind.WaterExtractor).WaterProductionRate * sim.State.Map.GetCell(ice.Coord).YieldFactor;

            Assert.That(drawnFromSite, Is.EqualTo(nominalSite * 60.0).Within(1e-6));
            Assert.That(sim.State.Resources.TotalSiteExtractedWater - siteTotalBefore, Is.EqualTo(drawnFromSite).Within(1e-6));
            Assert.That(sim.State.Resources.TotalLegacyExtractedWater - legacyTotalBefore,
                Is.EqualTo(sim.Config.Resources.WaterExtractionRate * 60.0).Within(1e-6));
            Assert.That(facAfter.LastWaterExtractionRate, Is.EqualTo(nominalSite).Within(1e-6));
            Assert.That(sim.State.Resources.LastWaterExtractionRate,
                Is.EqualTo(sim.Config.Resources.WaterExtractionRate + nominalSite).Within(1e-6));
            Assert.That(sim.State.Resources.Water, Is.GreaterThan(waterStart));

            // Per-site toggle OFF pauses ONLY the new site while legacy keeps running.
            Assert.That(sim.SetFacilityEnabled(facId, false).Success, Is.True);
            double remPaused = sim.State.Map.GetCell(ice.Coord).RemainingWater;
            double legacyBeforePause = sim.State.Resources.TotalLegacyExtractedWater;
            sim.Tick(20.0);
            Assert.That(sim.State.Map.GetCell(ice.Coord).RemainingWater, Is.EqualTo(remPaused).Within(1e-9));
            Assert.That(FindFacility(sim.State, facId).LastWaterExtractionRate, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(sim.State.Resources.TotalLegacyExtractedWater, Is.GreaterThan(legacyBeforePause));

            // Re-enable site, disable master switch -> both legacy and site extract 0!
            Assert.That(sim.SetFacilityEnabled(facId, true).Success, Is.True);
            Assert.That(sim.SetWaterExtractorEnabled(false).Success, Is.True);
            double legacyStopped = sim.State.Resources.TotalLegacyExtractedWater;
            double remMasterOff = sim.State.Map.GetCell(ice.Coord).RemainingWater;
            sim.Tick(20.0);
            Assert.That(sim.State.Resources.TotalLegacyExtractedWater, Is.EqualTo(legacyStopped).Within(1e-9));
            Assert.That(sim.State.Map.GetCell(ice.Coord).RemainingWater, Is.EqualTo(remMasterOff).Within(1e-9));
            Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void ZeroPowerFullTankGlobalServiceFaultAndScienceRepairPreserveDepositIntegrity()
        {
            // Test full tank first (zero crew/O2 water consumption, InitialWater == WaterCapacity == 100):
            var fullSim = CreateControlledOpsSim(
                initialBattery: 80,
                initialWater: 100,
                waterExtractionRate: 0,
                waterExtractorEnergyRate: 0,
                waterUsePerCrew: 0,
                oxygenProductionRate: 0);
            var iceFull = FindScannedIceCell(fullSim.State.Map);
            ConnectAndBuildWaterExtractor(fullSim, iceFull.Coord);
            double remFullStart = fullSim.State.Map.GetCell(iceFull.Coord).RemainingWater;
            double batBeforeFullTick = fullSim.State.Resources.Battery;
            fullSim.Tick(60.0);
            Assert.That(fullSim.State.Resources.Water, Is.EqualTo(100.0).Within(1e-6));
            Assert.That(fullSim.State.Map.GetCell(iceFull.Coord).RemainingWater, Is.EqualTo(remFullStart).Within(1e-9));
            Assert.That(fullSim.State.Resources.Battery, Is.EqualTo(batBeforeFullTick).Within(1e-6)); // No energy wasted when tank full!

            // Test zero power (night + 0 battery) + global ×0.50 fault + 10 science repair:
            var powerSim = CreateControlledOpsSim(initialBattery: 0.0, initialWater: 30, enableRandomEvents: true);
            // Force fast-forward to night when solar = 0 and battery = 0? First build during daytime with research to earn > 10 science!
            var opsSim = CreateControlledOpsSim(initialBattery: 85, initialWater: 30, enableRandomEvents: true);
            var iceOps = FindScannedIceCell(opsSim.State.Map);
            string facId = ConnectAndBuildWaterExtractor(opsSim, iceOps.Coord);

            // Earn >= 12 science cleanly while advancing until water extractor fault triggers OR set up science first.
            opsSim.SetResearchEnabled(true);
            Assert.That(opsSim.AssignTask("crew-2", "research").Success, Is.True);
            opsSim.Tick(100.0); // 100 * 0.15 = 15 science!
            Assert.That(opsSim.State.Resources.ScienceBalance, Is.GreaterThanOrEqualTo(10.0));

            // Advance until WaterExtractor channel faults (or max 2000s checking Sol reports).
            bool sawFault = opsSim.State.Resources.WaterExtractorFaulted;
            for (int step = 0; step < 300 && !sawFault; step++)
            {
                opsSim.Tick(20.0);
                if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                if (opsSim.State.CriticalAlertPauseRequested) opsSim.AcknowledgeCriticalAlert();
                if (opsSim.State.Resources.WaterExtractorFaulted)
                {
                    sawFault = true;
                    break;
                }
            }
            if (sawFault)
            {
                // Ensure daytime so solar > 0 and extractor rate clearly shows ×0.50 reduction!
                if (opsSim.State.IsNight)
                {
                    opsSim.Tick(opsSim.Config.SolSeconds - (opsSim.State.ElapsedSeconds % opsSim.Config.SolSeconds) + 10.0);
                    if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                }
                double rateWhileFaulted = FindFacility(opsSim.State, facId).LastWaterExtractionRate;
                double legacyWhileFaulted = opsSim.State.Resources.LastLegacyWaterExtractionRate;
                double remBeforeRepair = opsSim.State.Map.GetCell(iceOps.Coord).RemainingWater;
                double sciBeforeRepair = opsSim.State.Resources.ScienceBalance;

                Assert.That(opsSim.RepairWaterExtractorWithScience().Success, Is.True);
                Assert.That(opsSim.State.Resources.ScienceBalance, Is.EqualTo(sciBeforeRepair - 10.0).Within(1e-6));
                Assert.That(opsSim.State.Map.GetCell(iceOps.Coord).RemainingWater, Is.EqualTo(remBeforeRepair).Within(1e-9),
                    "10 science repair must NEVER add water back into the site reserve.");

                opsSim.Tick(1.0);
                if (rateWhileFaulted > 0)
                    Assert.That(FindFacility(opsSim.State, facId).LastWaterExtractionRate, Is.EqualTo(rateWhileFaulted * 2.0).Within(1e-6));
                if (legacyWhileFaulted > 0)
                    Assert.That(opsSim.State.Resources.LastLegacyWaterExtractionRate, Is.EqualTo(legacyWhileFaulted * 2.0).Within(1e-6));
            }

            // Explicit zero battery + night test:
            var zeroSim = CreateControlledOpsSim(initialBattery: 25.0, initialWater: 30, solarRate: 0.0, waterExtractionRate: 0);
            var iceZero = FindScannedIceCell(zeroSim.State.Map);
            string zFacId = ConnectAndBuildWaterExtractor(zeroSim, iceZero.Coord);
            zeroSim.SetResearchEnabled(true);
            zeroSim.AssignTask("crew-2", "research");
            for (int k = 0; k < 40 && (!zeroSim.State.IsNight || zeroSim.State.Resources.Battery > 0); k++)
            {
                zeroSim.Tick(20.0);
                if (zeroSim.State.CriticalAlertPauseRequested) zeroSim.AcknowledgeCriticalAlert();
                if (zeroSim.State.SolReportPauseRequested) zeroSim.AcknowledgeSolReport();
            }
            Assert.That(zeroSim.State.IsNight, Is.True);
            Assert.That(zeroSim.State.Resources.Battery, Is.EqualTo(0.0).Within(1e-9));

            double remAtZeroPower = zeroSim.State.Map.GetCell(iceZero.Coord).RemainingWater;
            zeroSim.Tick(20.0);
            Assert.That(zeroSim.State.Map.GetCell(iceZero.Coord).RemainingWater, Is.EqualTo(remAtZeroPower).Within(1e-9));
            Assert.That(FindFacility(zeroSim.State, zFacId).LastWaterExtractionRate, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void DepletionBoundaryMatchesBetweenLargeAndSmallTicksAndPersistsInRevision4Save()
        {
            var simA = CreateControlledOpsSim(initialBattery: 85, initialWater: 30, waterExtractionRate: 0);
            var iceA = FindScannedIceCell(simA.State.Map);
            string facId = ConnectAndBuildWaterExtractor(simA, iceA.Coord);

            string baseJson = simA.SaveToJson();
            string cellRwPattern = $"(\"x\":{iceA.Coord.X},\"z\":{iceA.Coord.Z},[^\\}}]*\"rw\":)[0-9.E+-]+";
            string smallRemJson = Regex.Replace(baseJson, cellRwPattern, "${1}3.5", RegexOptions.CultureInvariant);
            Assert.That(smallRemJson, Is.Not.EqualTo(baseJson));

            Assert.That(MissionSimulation.TryLoadFromJson(smallRemJson, out var simLarge, out string err1), Is.True, err1);
            Assert.That(MissionSimulation.TryLoadFromJson(smallRemJson, out var simSmall, out string err2), Is.True, err2);

            // Tick 500s (plenty to exhaust 3.5 water units completely: 0.012/s * ~1.0 yield needs ~291s).
            simLarge.Tick(500.0);
            for (int i = 0; i < 500; i++)
                simSmall.Tick(1.0);

            Assert.That(simLarge.State.Map.GetCell(iceA.Coord).RemainingWater, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(simSmall.State.Map.GetCell(iceA.Coord).RemainingWater, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(simLarge.State.Resources.Water, Is.EqualTo(simSmall.State.Resources.Water).Within(1e-6));
            Assert.That(simLarge.State.Resources.Battery, Is.EqualTo(simSmall.State.Resources.Battery).Within(1e-6));
            Assert.That(simLarge.State.Resources.TotalSiteExtractedWater,
                Is.EqualTo(simSmall.State.Resources.TotalSiteExtractedWater).Within(1e-6));
            Assert.That(FindFacility(simLarge.State, facId).LastWaterExtractionRate, Is.EqualTo(0.0).Within(1e-9));

            // Save/Load round-trip check for Revision 4 + per-site toggle + telemetry + depleted RemainingWater == 0.
            Assert.That(simLarge.SetFacilityEnabled(facId, false).Success, Is.True);
            string postJson = simLarge.SaveToJson();
            Assert.That(postJson, Does.Contain("\"expansionRevision\":4"));
            Assert.That(MissionSimulation.TryLoadFromJson(postJson, out var reloaded, out string err3), Is.True, err3);
            var reFac = FindFacility(reloaded.State, facId);
            Assert.That(reFac.IsEnabled, Is.False);
            Assert.That(reloaded.State.Map.GetCell(iceA.Coord).RemainingWater, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(reloaded.State.Resources.TotalSiteExtractedWater,
                Is.EqualTo(simLarge.State.Resources.TotalSiteExtractedWater).Within(1e-6));
        }
    }
}
