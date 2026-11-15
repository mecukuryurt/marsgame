using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsBlueprintTests
    {
        private static MissionSimulation CreateCleanOpsSim()
        {
            var cfg = MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false);
            return new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
        }

        private static List<GridCoord> FindAdjacentClaimableBuildableCells(MissionSimulation sim, int maxCount)
        {
            var claimed = new List<GridCoord>();
            for (int pass = 0; pass < 4 && claimed.Count < maxCount; pass++)
            {
                foreach (var cell in sim.State.Map.Cells)
                {
                    if (claimed.Count >= maxCount) break;
                    if (cell.IsClaimed || !cell.IsExplored || !cell.IsBuildable || cell.Protection != StarterProtectionKind.None)
                        continue;
                    if (!sim.State.Map.HasClaimedNeighbor(cell.Coord))
                        continue;
                    var res = sim.ClaimParcel(cell.Coord);
                    if (res.Success)
                        claimed.Add(cell.Coord);
                }
            }
            return claimed;
        }

        [Test]
        public void CatalogExposesOnlyOp06UnlockedBlueprintsWithExactF02Contracts()
        {
            var unlocked = FacilityCatalog.GetPlayerCatalogSpecs();
            Assert.That(unlocked.Count, Is.EqualTo(4));

            var line = FacilityCatalog.GetSpec(FacilityKind.UtilityLine);
            Assert.That(line.MaterialCost, Is.EqualTo(1));
            Assert.That(line.NetWorkSeconds, Is.EqualTo(5.0).Within(1e-6));
            Assert.That(line.WorkEnergyCost, Is.EqualTo(0.0).Within(1e-6));
            Assert.That(line.UsesUtilityLayerOnly, Is.True);
            Assert.That(line.BlocksPassability, Is.False);
            Assert.That(line.UnlockedInPlayerCatalog, Is.True);

            var solar = FacilityCatalog.GetSpec(FacilityKind.SolarPanel);
            Assert.That(solar.MaterialCost, Is.EqualTo(12));
            Assert.That(solar.NetWorkSeconds, Is.EqualTo(45.0).Within(1e-6));
            Assert.That(solar.WorkEnergyCost, Is.EqualTo(2.0).Within(1e-6));
            Assert.That(solar.UsesUtilityLayerOnly, Is.False);
            Assert.That(solar.BlocksPassability, Is.True);
            Assert.That(solar.UnlockedInPlayerCatalog, Is.True);

            var battery = FacilityCatalog.GetSpec(FacilityKind.Battery);
            Assert.That(battery.MaterialCost, Is.EqualTo(16));
            Assert.That(battery.NetWorkSeconds, Is.EqualTo(45.0).Within(1e-6));
            Assert.That(battery.WorkEnergyCost, Is.EqualTo(2.0).Within(1e-6));
            Assert.That(battery.BatteryCapacityDelta, Is.EqualTo(25.0).Within(1e-6));
            Assert.That(battery.UnlockedInPlayerCatalog, Is.True);

            var extractor = FacilityCatalog.GetSpec(FacilityKind.WaterExtractor);
            Assert.That(extractor.MaterialCost, Is.EqualTo(18));
            Assert.That(extractor.NetWorkSeconds, Is.EqualTo(60.0).Within(1e-6));
            Assert.That(extractor.WorkEnergyCost, Is.EqualTo(3.0).Within(1e-6));
            Assert.That(extractor.RequiredResource, Is.EqualTo(ParcelResourceKind.Ice));
            Assert.That(extractor.UnlockedInPlayerCatalog, Is.True);

            Assert.That(FacilityCatalog.GetSpec(FacilityKind.RegolithFurnace).UnlockedInPlayerCatalog, Is.False);
        }

        [Test]
        public void PlaceBlueprintRejectsInvalidTargetsAtomicallyWithZeroStateDelta()
        {
            var sim = CreateCleanOpsSim();
            var claimed = FindAdjacentClaimableBuildableCells(sim, 3);
            Assert.That(claimed.Count, Is.EqualTo(3), "Expected at least 3 claimable buildable parcels in starter ring.");

            // Claim 3 cells spent 6 materials (40 -> 34)
            Assert.That(sim.State.Materials, Is.EqualTo(34));
            var snapshotBefore = sim.State;
            int facCountBefore = snapshotBefore.Facilities.Count;

            // 1. Unexplored cell (0,0)
            var rUnexplored = sim.PlaceBlueprint(FacilityKind.SolarPanel, new GridCoord(0, 0));
            Assert.That(rUnexplored.Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(34));
            Assert.That(sim.State.Facilities.Count, Is.EqualTo(facCountBefore));

            // 2. Unclaimed explored cell
            GridCoord unclaimedExplored = default;
            bool foundUnclaimed = false;
            foreach (var c in sim.State.Map.Cells)
            {
                if (c.IsExplored && !c.IsClaimed && c.IsBuildable)
                {
                    unclaimedExplored = c.Coord;
                    foundUnclaimed = true;
                    break;
                }
            }
            Assert.That(foundUnclaimed, Is.True);
            var rUnclaimed = sim.PlaceBlueprint(FacilityKind.UtilityLine, unclaimedExplored);
            Assert.That(rUnclaimed.Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(34));

            // 3. Protected starter pad (Airlock corridor & Starter solar)
            Assert.That(sim.PlaceBlueprint(FacilityKind.UtilityLine, ParcelMap.AirlockCorridorCoord).Success, Is.False);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, ParcelMap.StarterSolarCoord).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(34));

            // 4. Out of bounds
            Assert.That(sim.PlaceBlueprint(FacilityKind.UtilityLine, new GridCoord(-1, 5)).Success, Is.False);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, new GridCoord(16, 16)).Success, Is.False);

            // 5. Insufficient materials check (place 2 solar panels: 34 - 24 = 10 < 12)
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, claimed[0]).Success, Is.True);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, claimed[1]).Success, Is.True);
            Assert.That(sim.State.Materials, Is.EqualTo(10));
            var rLow = sim.PlaceBlueprint(FacilityKind.SolarPanel, claimed[2]);
            Assert.That(rLow.Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(10));
            Assert.That(sim.State.Map.GetCell(claimed[2]).FacilityId, Is.Null);

            // 6. Blocking crew return corridor or placing under standing crew
            var simCorridor = CreateCleanOpsSim();
            // Send crew-1 onto SolarWorkCoord (9,5)
            Assert.That(simCorridor.AssignSurfaceWork("crew-1", ParcelMap.SolarWorkCoord, workRequiredSeconds: 120.0, allowTargetCell: true).Success, Is.True);
            simCorridor.Tick(8.0); // crew leaves airlock and walks to/on SolarWorkCoord
            var cellCrew = simCorridor.State.Crew[0];
            Assert.That(cellCrew.HasSurfacePosition, Is.True);
            Assert.That(simCorridor.State.Map.Config.TryWorldToCell(cellCrew.SurfacePosX, cellCrew.SurfacePosZ, out GridCoord standingCoord), Is.True);
            int matsBeforeCrewBlock = simCorridor.State.Materials;
            var rUnderCrew = simCorridor.PlaceBlueprint(FacilityKind.SolarPanel, standingCoord);
            Assert.That(rUnderCrew.Success, Is.False);
            Assert.That(simCorridor.State.Materials, Is.EqualTo(matsBeforeCrewBlock));

            // Snapshot isolation check
            Assert.That(snapshotBefore.Facilities.Count, Is.EqualTo(facCountBefore));
            Assert.That(snapshotBefore.Map.GetCell(claimed[0]).FacilityId, Is.Null);
        }

        [Test]
        public void UtilityLayerAndFacilitySlotCoexistWhileDuplicatesFail_AndUnfinishedBlueprintsProduceNothing()
        {
            var sim = CreateCleanOpsSim();
            var baselineSim = CreateCleanOpsSim();

            GridCoord target = ParcelMap.SolarWorkCoord; // Claimed & buildable OutpostPad
            var rLine = sim.PlaceBlueprint(FacilityKind.UtilityLine, target, FacilityRotation.Deg0);
            Assert.That(rLine.Success, Is.True, rLine.Reason);
            Assert.That(sim.State.Materials, Is.EqualTo(39));

            // Same cell allows one non-utility facility blueprint alongside utility blueprint
            var rSolar = sim.PlaceBlueprint(FacilityKind.SolarPanel, target, FacilityRotation.Deg180);
            Assert.That(rSolar.Success, Is.True, rSolar.Reason);
            Assert.That(sim.State.Materials, Is.EqualTo(27));

            // Duplicates in either layer must be rejected atomically
            Assert.That(sim.PlaceBlueprint(FacilityKind.UtilityLine, target).Success, Is.False);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, target).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(27));

            // Verify blueprint state & zero production/capacity/channel changes after ticking
            sim.Tick(60.0);
            baselineSim.Tick(60.0);

            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(baselineSim.State.Resources.Battery).Within(1e-9));
            Assert.That(sim.State.Resources.Water, Is.EqualTo(baselineSim.State.Resources.Water).Within(1e-9));

            for (int i = 0; i < sim.State.Facilities.Count; i++)
            {
                var fac = sim.State.Facilities[i];
                if (!fac.IsProtectedStarter)
                {
                    Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
                    Assert.That(fac.IsProductionApplied, Is.False);
                    Assert.That(fac.IsConnectedToCore, Is.False);
                }
            }
        }

        [Test]
        public void Revision3SaveLoadRoundTrip_ValidationRejection_And_RestartReset()
        {
            var sim = CreateCleanOpsSim();
            var claimed = FindAdjacentClaimableBuildableCells(sim, 1);
            Assert.That(claimed.Count, Is.EqualTo(1));
            Assert.That(sim.PlaceBlueprint(FacilityKind.UtilityLine, ParcelMap.SolarWorkCoord, FacilityRotation.Deg90).Success, Is.True);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, claimed[0], FacilityRotation.Deg270).Success, Is.True);

            int expectedMaterials = sim.State.Materials;
            int expectedFacilities = sim.State.Facilities.Count;

            string json = sim.SaveToJson();
            Assert.That(json, Does.Contain($"\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision}"));
            Assert.That(MissionSimulation.TryLoadFromJson(json, out var loaded, out string err), Is.True, err);

            Assert.That(loaded.State.Materials, Is.EqualTo(expectedMaterials));
            Assert.That(loaded.State.Facilities.Count, Is.EqualTo(expectedFacilities));
            Assert.That(loaded.State.Map.GetCell(ParcelMap.SolarWorkCoord).UtilityBlueprintId, Is.Not.Null);
            Assert.That(loaded.State.Map.GetCell(claimed[0]).FacilityId, Is.Not.Null);

            // Find loaded solar blueprint and confirm rotation/status/materials preserved
            FacilityState loadedSolar = null;
            for (int i = 0; i < loaded.State.Facilities.Count; i++)
            {
                if (loaded.State.Facilities[i].Kind == FacilityKind.SolarPanel)
                    loadedSolar = loaded.State.Facilities[i];
            }
            Assert.That(loadedSolar, Is.Not.Null);
            Assert.That(loadedSolar.Rotation, Is.EqualTo(FacilityRotation.Deg270));
            Assert.That(loadedSolar.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(loadedSolar.PaidMaterials, Is.EqualTo(12));
            Assert.That(loadedSolar.IsProductionApplied, Is.False);

            // Tampered save claiming blueprint has production applied must fail validation cleanly
            string badJson = json.Replace("\"isProductionApplied\":false", "\"isProductionApplied\":true");
            Assert.That(MissionSimulation.TryLoadFromJson(badJson, out _, out _), Is.False);

            // Restart must reset all player blueprints and restore starter state
            sim.Restart();
            Assert.That(sim.State.Materials, Is.EqualTo(40));
            Assert.That(sim.State.Facilities.Count, Is.EqualTo(3));
            Assert.That(sim.State.Map.GetCell(ParcelMap.SolarWorkCoord).UtilityBlueprintId, Is.Null);
        }
    }
}
