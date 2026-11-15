using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsNetworkTests
    {
        private static MissionSimulation CreateCleanOpsSim(int initialMaterials = 40, int operationsSeed = 424242)
        {
            var resCfg = new ResourceConfig(
                initialBattery: 50,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: 0.20,
                panelMaintenanceReduction: 0.20,
                initialSystemWear: 0.15,
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
                operationsSeed: operationsSeed,
                enableOperationsRandomEvents: false,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: initialMaterials,
                materialCapacity: Math.Max(80, initialMaterials));
            return new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
        }

        private static string PlaceAndGetId(MissionSimulation sim, FacilityKind kind, GridCoord coord)
        {
            var res = sim.PlaceBlueprint(kind, coord);
            Assert.That(res.Success, Is.True, $"Place {kind} at {coord} failed: {res.Reason}");
            return sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
        }

        private static string BuildAndCommission(MissionSimulation sim, FacilityKind kind, GridCoord coord, string crewId = "crew-1")
        {
            string facId = PlaceAndGetId(sim, kind, coord);
            var assign = sim.AssignBuildBlueprint(crewId, facId);
            Assert.That(assign.Success, Is.True, $"Assign {crewId} to {facId} at {coord} failed: {assign.Reason}");
            sim.Tick(80.0);
            var fac = FindFacility(sim.State, facId);
            Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            foreach (var c in sim.State.Crew)
            {
                if (c.Id == crewId && c.Location == CrewLocation.Outside)
                {
                    sim.ReturnToShelter(crewId);
                    sim.Tick(25.0);
                    break;
                }
            }
            return facId;
        }

        private static FacilityState FindFacility(MissionState state, string id)
        {
            foreach (var f in state.Facilities)
                if (f.FacilityId == id)
                    return f;
            return null;
        }

        private static List<GridCoord> FindBuildableChainFromCore(MissionSimulation sim, int length)
        {
            // Find cardinal chain [c0, c1, ..., c_{length-1}] of buildable claimable cells where c0 borders Airlock/Core or SolarWork
            var map = sim.State.Map;
            var seedCoords = new List<GridCoord>();
            foreach (var c in map.Cells)
            {
                if (c.Protection != StarterProtectionKind.None && c.HasUtilityLine)
                    seedCoords.Add(c.Coord);
            }

            foreach (var seed in seedCoords)
            {
                foreach (var n in map.Config.GetCardinalNeighbors(seed))
                {
                    if (!map.TryGetCell(n, out var nCell) || !nCell.IsExplored || !nCell.IsBuildable || nCell.Protection != StarterProtectionKind.None)
                        continue;

                    var chain = new List<GridCoord> { n };
                    var used = new HashSet<GridCoord>(seedCoords) { n };
                    if (ExtendChain(map, chain, used, length))
                        return chain;
                }
            }

            Assert.Fail($"Could not find buildable chain of length {length} from core.");
            return null;
        }

        private static bool ExtendChain(ParcelMap map, List<GridCoord> chain, HashSet<GridCoord> used, int targetLen)
        {
            if (chain.Count >= targetLen) return true;
            var cur = chain[chain.Count - 1];
            foreach (var n in map.Config.GetCardinalNeighbors(cur))
            {
                if (used.Contains(n)) continue;
                if (!map.TryGetCell(n, out var cell) || !cell.IsExplored || !cell.IsBuildable || cell.Protection != StarterProtectionKind.None)
                    continue;
                // Avoid accidental shortcut back to earlier core cells
                bool touchesStarter = false;
                foreach (var nn in map.Config.GetCardinalNeighbors(n))
                {
                    if (map.TryGetCell(nn, out var nnCell) && nnCell.HasUtilityLine && nnCell.Protection != StarterProtectionKind.None)
                    {
                        touchesStarter = true;
                        break;
                    }
                }
                if (touchesStarter) continue;

                used.Add(n);
                chain.Add(n);
                if (ExtendChain(map, chain, used, targetLen))
                    return true;
                chain.RemoveAt(chain.Count - 1);
                used.Remove(n);
            }
            return false;
        }

        [Test]
        public void StarterSolarWorkApronConnectsNewLineAndAdjacentPanel()
        {
            var sim = CreateCleanOpsSim();
            var apron = ParcelMap.SolarWorkCoord;
            var panelCoord = new GridCoord(10, 5);
            var map = sim.State.Map;
            Assert.That(map.GetCell(apron).IsClaimed, Is.True);
            Assert.That(map.GetCell(apron).IsPassable, Is.True);
            Assert.That(map.GetCell(ParcelMap.StarterSolarCoord).HasUtilityLine, Is.True);
            Assert.That(sim.ClaimParcel(panelCoord).Success, Is.True);

            var lineId = BuildAndCommission(sim, FacilityKind.UtilityLine, apron);
            Assert.That(FindFacility(sim.State, lineId).IsConnectedToCore, Is.True);
            var panelId = BuildAndCommission(sim, FacilityKind.SolarPanel, panelCoord);
            Assert.That(FindFacility(sim.State, panelId).IsConnectedToCore, Is.True);
            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(4));
            Assert.That(SolarProduction.GetPanelHealthFactor(sim.State.Resources, sim.State.Facilities), Is.GreaterThan(1));
            Assert.That(MissionSimulation.TryLoadFromJson(sim.SaveToJson(), out var loaded, out var error), Is.True, error);
            Assert.That(FindFacility(loaded.State, panelId).IsConnectedToCore, Is.True);
        }

        [Test]
        public void StarterFacilitiesStayConnectedToCoreAndPreserveBaselineResources()
        {
            var sim = CreateCleanOpsSim();
            foreach (var fac in sim.State.Facilities)
            {
                Assert.That(fac.IsProtectedStarter, Is.True);
                Assert.That(fac.IsConnectedToCore, Is.True);
            }

            // Starter airlock return corridor remains passable
            Assert.That(sim.State.Map.GetCell(ParcelMap.AirlockCorridorCoord).IsPassable, Is.True);

            // Baseline ticks produce normal O2/Battery without errors
            double bat0 = sim.State.Resources.Battery;
            sim.Tick(60.0);
            Assert.That(sim.State.Resources.UnmetOxygen, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(sim.State.Resources.Battery, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(sim.State.Resources.Battery, Is.Not.NaN);
        }

        [Test]
        public void UnfinishedDiagonalAndIsolatedLoopLinesDoNotConnectUntilBridgeCompletedAndDisconnectOnRemoval()
        {
            var sim = CreateCleanOpsSim();
            // Build a chain of 4 cells: bridge(c0) -> c1 -> c2 -> c3 (c1,c2,c3 isolated until bridge c0 is built!)
            var chain = FindBuildableChainFromCore(sim, 4);
            for (int i = 0; i < chain.Count; i++)
            {
                if (!sim.State.Map.GetCell(chain[i]).IsClaimed)
                    Assert.That(sim.ClaimParcel(chain[i]).Success, Is.True);
            }

            // Place & commission a SolarPanel at chain[2] BEFORE building bridge c0 or line c1
            string farPanelId = BuildAndCommission(sim, FacilityKind.SolarPanel, chain[2], "crew-1");
            Assert.That(FindFacility(sim.State, farPanelId).IsConnectedToCore, Is.False,
                "Disconnected SolarPanel must have IsConnectedToCore == false.");

            // Place unfinished BLUEPRINT line at bridge chain[0] and chain[1] -> MUST NOT conduct!
            string bpBridge = PlaceAndGetId(sim, FacilityKind.UtilityLine, chain[0]);
            Assert.That(FindFacility(sim.State, bpBridge).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, farPanelId).IsConnectedToCore, Is.False);

            // Commission chain[1] AND chain[3] while bridge chain[0] is still Blueprint -> isolated utility island stays FALSE
            string line1 = BuildAndCommission(sim, FacilityKind.UtilityLine, chain[1], "crew-2");
            string line3 = BuildAndCommission(sim, FacilityKind.UtilityLine, chain[3], "crew-2");
            Assert.That(FindFacility(sim.State, line1).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, line3).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, farPanelId).IsConnectedToCore, Is.False);

            // Diagonal check: place a utility line at any cell that ONLY diagonally touches core (and doesn't touch chain)
            foreach (var cell in sim.State.Map.Cells)
            {
                if (cell.IsClaimed && cell.IsBuildable && cell.Protection == StarterProtectionKind.None &&
                    cell.FacilityId == null && cell.UtilityBlueprintId == null && !cell.HasUtilityLine)
                {
                    if (!UtilityNetwork.IsCoordConnectedPreview(sim.State.Map, cell.Coord))
                    {
                        string diagLine = BuildAndCommission(sim, FacilityKind.UtilityLine, cell.Coord, "crew-3");
                        Assert.That(FindFacility(sim.State, diagLine).IsConnectedToCore, Is.False,
                            "Diagonal or isolated cell must not connect without cardinal utility line.");
                        break;
                    }
                }
            }

            // Now complete the bridge line at chain[0] -> bpBridge, line1, and adjacent farPanel flip to CONNECTED!
            // Note: line3 at chain[3] is separated by chain[2] (SolarPanel without UtilityLine yet), so line3 stays FALSE until chain[2] gets a UtilityLine.
            var assignBridge = sim.AssignBuildBlueprint("crew-1", bpBridge);
            Assert.That(assignBridge.Success, Is.True, assignBridge.Reason);
            sim.Tick(60.0);
            if (sim.State.Crew[0].Location == CrewLocation.Outside)
            {
                sim.ReturnToShelter("crew-1");
                sim.Tick(25.0);
            }

            Assert.That(FindFacility(sim.State, bpBridge).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(sim.State, line1).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(sim.State, farPanelId).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(sim.State, line3).IsConnectedToCore, Is.False,
                "Non-utility SolarPanel on chain[2] must not pass cable through without a UtilityLine.");

            // Now add UtilityLine on chain[2] sharing the cell with farPanel -> line3 connects too!
            string line2 = BuildAndCommission(sim, FacilityKind.UtilityLine, chain[2], "crew-3");
            Assert.That(FindFacility(sim.State, line2).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(sim.State, line3).IsConnectedToCore, Is.True);
            Assert.That(UtilityNetwork.GetCardinalLineMask(sim.State.Map, chain[1]), Is.GreaterThan(0));

            // Save & Load preserves exact topology
            string connectedJson = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(connectedJson, out var loadedConnected, out string err1), Is.True, err1);
            Assert.That(FindFacility(loadedConnected.State, farPanelId).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(loadedConnected.State, line2).IsConnectedToCore, Is.True);
            Assert.That(FindFacility(loadedConnected.State, line3).IsConnectedToCore, Is.True);

            // Remove bridge on loaded sim -> isolated segment flips back to FALSE deterministically!
            Assert.That(loadedConnected.RemoveUtilityLine(bpBridge).Success, Is.True);
            Assert.That(FindFacility(loadedConnected.State, line1).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(loadedConnected.State, line2).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(loadedConnected.State, line3).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(loadedConnected.State, farPanelId).IsConnectedToCore, Is.False);
            // Protected starter remains TRUE
            Assert.That(FindFacility(loadedConnected.State, ParcelMap.StarterSolarSpec.FacilityId).IsConnectedToCore, Is.True);

            // Save after cut & reload
            string cutJson = loadedConnected.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(cutJson, out var loadedCut, out string err2), Is.True, err2);
            Assert.That(FindFacility(loadedCut.State, farPanelId).IsConnectedToCore, Is.False);
        }

        [Test]
        public void IsolatedClosedUtilityLoopRemainsDisconnectedUntilBridged()
        {
            MissionSimulation sim = null;
            GridCoord q00 = default;
            bool foundQuad = false;

            for (int seed = 100; seed < 300 && !foundQuad; seed++)
            {
                var candidate = CreateCleanOpsSim(initialMaterials: 140, operationsSeed: seed);
                for (int pass = 0; pass < 6; pass++)
                {
                    var curMap = candidate.State.Map;
                    var toClaim = new List<GridCoord>();
                    foreach (var c in curMap.Cells)
                    {
                        if (!c.IsClaimed && c.IsExplored && c.IsBuildable && c.Protection == StarterProtectionKind.None && curMap.HasClaimedNeighbor(c.Coord))
                            toClaim.Add(c.Coord);
                    }
                    foreach (var coord in toClaim)
                        candidate.ClaimParcel(coord);
                }

                var cmap = candidate.State.Map;
                for (int z = 0; z < cmap.Config.Height - 1 && !foundQuad; z++)
                {
                    for (int x = 0; x < cmap.Config.Width - 1 && !foundQuad; x++)
                    {
                        var a = new GridCoord(x, z);
                        var b = new GridCoord(x + 1, z);
                        var c = new GridCoord(x + 1, z + 1);
                        var d = new GridCoord(x, z + 1);
                        if (cmap.TryGetCell(a, out var ca) && cmap.TryGetCell(b, out var cb) &&
                            cmap.TryGetCell(c, out var cc) && cmap.TryGetCell(d, out var cd) &&
                            ca.IsClaimed && ca.IsBuildable && ca.Protection == StarterProtectionKind.None &&
                            cb.IsClaimed && cb.IsBuildable && cb.Protection == StarterProtectionKind.None &&
                            cc.IsClaimed && cc.IsBuildable && cc.Protection == StarterProtectionKind.None &&
                            cd.IsClaimed && cd.IsBuildable && cd.Protection == StarterProtectionKind.None &&
                            !UtilityNetwork.IsCoordConnectedPreview(cmap, a) &&
                            !UtilityNetwork.IsCoordConnectedPreview(cmap, b) &&
                            !UtilityNetwork.IsCoordConnectedPreview(cmap, c) &&
                            !UtilityNetwork.IsCoordConnectedPreview(cmap, d))
                        {
                            sim = candidate;
                            q00 = a;
                            foundQuad = true;
                        }
                    }
                }
            }

            Assert.That(foundQuad, Is.True, "Expected an isolated 2x2 buildable quad in explored claimed zone.");

            // Commission 4 UtilityLine facilities forming a closed loop
            string l1 = BuildAndCommission(sim, FacilityKind.UtilityLine, q00, "crew-1");
            string l2 = BuildAndCommission(sim, FacilityKind.UtilityLine, new GridCoord(q00.X + 1, q00.Z), "crew-2");
            string l3 = BuildAndCommission(sim, FacilityKind.UtilityLine, new GridCoord(q00.X + 1, q00.Z + 1), "crew-3");
            string l4 = BuildAndCommission(sim, FacilityKind.UtilityLine, new GridCoord(q00.X, q00.Z + 1), "crew-1");

            Assert.That(FindFacility(sim.State, l1).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, l2).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, l3).IsConnectedToCore, Is.False);
            Assert.That(FindFacility(sim.State, l4).IsConnectedToCore, Is.False);
        }
    }
}
