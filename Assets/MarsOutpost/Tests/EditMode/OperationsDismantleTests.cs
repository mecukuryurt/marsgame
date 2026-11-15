using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsDismantleTests
    {
        private static MissionSimulation CreateControlledOpsSim(
            double initialBattery = 80,
            int initialMaterials = 60,
            int materialCapacity = 80,
            EventState[] events = null)
        {
            var resCfg = new ResourceConfig(
                solarRate: 0.35,
                batteryCapacity: 100,
                initialBattery: initialBattery,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: 0.0,
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
                materialCapacity: materialCapacity);
            var sim = new MissionSimulation(cfg, new MissionScenario(events: events ?? Array.Empty<EventState>()));
            sim.SetResearchEnabled(false);
            sim.SetWaterExtractorEnabled(false);
            return sim;
        }

        private static void EnsureClaimed(MissionSimulation sim, GridCoord coord)
        {
            if (!sim.State.Map.GetCell(coord).IsClaimed)
            {
                var res = sim.ClaimParcel(coord);
                Assert.That(res.Success, Is.True, $"Claim {coord} failed: {res.Reason}");
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

        private static FacilityState FindFacility(MissionState state, string facId)
        {
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                if (state.Facilities[i].FacilityId == facId)
                    return state.Facilities[i];
            }
            return null;
        }

        [Test]
        public void CancelBlueprintRefundsZeroMaterialsForfeitsSpentWorkEnergyAndRejectsRepeatAndLoadReplay()
        {
            var sim = CreateControlledOpsSim(initialBattery: 80, initialMaterials: 60, materialCapacity: 80);
            GridCoord target = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, target);
            int matAfterClaim = sim.State.Materials; // 54

            int solarCost = FacilityCatalog.GetSpec(FacilityKind.SolarPanel).MaterialCost; // 16
            var place = sim.PlaceBlueprint(FacilityKind.SolarPanel, target);
            Assert.That(place.Success, Is.True, place.Reason);
            string facId = place.Reason;
            Assert.That(sim.State.Materials, Is.EqualTo(matAfterClaim - solarCost));
            Assert.That(sim.State.Map.GetCell(target).IsPassable, Is.False);

            // Dismantle cannot be called on a blueprint.
            Assert.That(sim.DismantleFacility(facId).Success, Is.False);

            // Start construction partially, spend work energy, then recall crew cleanly.
            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            sim.Tick(20.0);
            double spentEnergy = FindFacility(sim.State, facId).EnergySpentTotal;
            Assert.That(spentEnergy, Is.GreaterThan(0.0));
            // Cannot cancel while crew is actively assigned to work on it.
            Assert.That(sim.CancelBlueprint(facId).Success, Is.False);

            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(25.0);

            int matBeforeCancel = sim.State.Materials;
            double batBeforeCancel = sim.State.Resources.Battery;
            var cancelRes = sim.CancelBlueprint(facId);
            Assert.That(cancelRes.Success, Is.True, cancelRes.Reason);
            Assert.That(sim.State.Materials, Is.EqualTo(matBeforeCancel));
            Assert.That(sim.State.MaterialsRefundedTotal, Is.EqualTo(0));
            Assert.That(sim.State.MaterialsForfeitedTotal, Is.EqualTo(solarCost));
            Assert.That(sim.State.BuildEnergyLostTotal, Is.EqualTo(spentEnergy).Within(1e-6));
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(batBeforeCancel).Within(1e-6));
            Assert.That(FindFacility(sim.State, facId), Is.Null);
            Assert.That(sim.State.Map.GetCell(target).FacilityId, Is.Null);
            Assert.That(sim.State.Map.GetCell(target).IsPassable, Is.True);

            // Second click & Save/Load -> second cancel must reject cleanly.
            Assert.That(sim.CancelBlueprint(facId).Success, Is.False);
            string json = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(json, out var loaded, out string err), Is.True, err);
            Assert.That(loaded.State.Materials, Is.EqualTo(matBeforeCancel));
            Assert.That(loaded.State.MaterialsForfeitedTotal, Is.EqualTo(solarCost));
            Assert.That(loaded.State.BuildEnergyLostTotal, Is.EqualTo(spentEnergy).Within(1e-6));
            Assert.That(loaded.CancelBlueprint(facId).Success, Is.False);
            Assert.That(loaded.State.Materials, Is.EqualTo(matBeforeCancel));
        }

        [Test]
        public void DismantleFacilityGrantsOneTime50PercentCappedRefundCleansPanelsAndRejectsReplay()
        {
            var sim = CreateControlledOpsSim(initialBattery: 80, initialMaterials: 76, materialCapacity: 80);
            GridCoord target = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, target); // 76 - 6 = 70
            int solarCost = FacilityCatalog.GetSpec(FacilityKind.SolarPanel).MaterialCost; // 16

            string facId = sim.PlaceBlueprint(FacilityKind.SolarPanel, target).Reason; // 70 - 16 = 54
            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            sim.Tick(80.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(25.0);

            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(4));
            Assert.That(sim.CancelBlueprint(facId).Success, Is.False); // Commissioned cannot be cancelled via CancelBlueprint.

            // Advance Sol 1 -> 3 to trigger daily supplies (+8/sol) so materials approach cap (78 / 80) -> tests capacity clamping!
            sim.Tick(1500.0);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            int matBeforeDismantle = sim.State.Materials; // <= 80

            // Wait: force exact near-cap scenario cleanly via a dedicated cap sim check too, and check preview matches applied!
            FacilityState fac = FindFacility(sim.State, facId);
            FacilityPlacementValidator.PreviewDismantleRefund(
                fac, sim.State.Materials, sim.Config.MaterialCapacity, out int expectedApplied, out int rawRefund, out _);
            Assert.That(rawRefund, Is.EqualTo(solarCost / 2));

            var disRes = sim.DismantleFacility(facId);
            Assert.That(disRes.Success, Is.True, disRes.Reason);
            Assert.That(sim.State.Materials, Is.EqualTo(Math.Min(sim.Config.MaterialCapacity, matBeforeDismantle + rawRefund)));
            Assert.That(sim.State.MaterialsRefundedTotal, Is.EqualTo(expectedApplied));
            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(FindFacility(sim.State, facId), Is.Null);
            Assert.That(sim.State.Map.GetCell(target).FacilityId, Is.Null);
            Assert.That(sim.State.Map.GetCell(target).IsPassable, Is.True);

            // Second click & Save/Load -> DismantleFacility must fail with 0 duplicate refund!
            int matAfterDismantle = sim.State.Materials;
            Assert.That(sim.DismantleFacility(facId).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(matAfterDismantle));

            string json = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(json, out var loaded, out string err), Is.True, err);
            Assert.That(loaded.State.Materials, Is.EqualTo(matAfterDismantle));
            Assert.That(loaded.State.MaterialsRefundedTotal, Is.EqualTo(expectedApplied));
            Assert.That(loaded.DismantleFacility(facId).Success, Is.False);
            Assert.That(loaded.State.Materials, Is.EqualTo(matAfterDismantle));
        }

        [Test]
        public void DismantleCapsAtMaterialCapacityAndBlocksStarterFaultTargetAndCrewOccupant()
        {
            // Capacity cap test: initialMaterials 94, cap 96 -> place Battery (cost 16 => 78) + claim 6 (=72), wait?
            // Simpler: initialMaterials 74, cap 78 -> claim 6 (68) -> place Battery 16 (52) -> won't exceed cap unless resupplied.
            // Use initialMaterials 80, cap 80 -> Claim (-6 = 74) -> Battery (-16 = 58) -> Sol 2 (+8 = 66) -> Sol 3 (+8 = 74) -> raw refund 8 -> applied 6!
            var sim = CreateControlledOpsSim(initialBattery: 85, initialMaterials: 80, materialCapacity: 80);
            GridCoord adj = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, adj);
            string batId = sim.PlaceBlueprint(FacilityKind.Battery, adj).Reason;
            Assert.That(sim.AssignBuildBlueprint("crew-1", batId).Success, Is.True);
            sim.Tick(95.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(25.0);

            // Advance 2 Sols so daily supply brings materials within <8 of MaterialCapacity (80).
            sim.Tick(720.0);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            sim.Tick(720.0);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            int expectedSpace = sim.Config.MaterialCapacity - sim.State.Materials;
            Assert.That(expectedSpace, Is.GreaterThan(0).And.LessThan(8));

            var batFac = FindFacility(sim.State, batId);
            FacilityPlacementValidator.PreviewDismantleRefund(batFac, sim.State.Materials, sim.Config.MaterialCapacity, out int applied, out int raw, out int capped);
            Assert.That(raw, Is.EqualTo(8));
            Assert.That(applied, Is.EqualTo(expectedSpace));
            Assert.That(capped, Is.EqualTo(raw - expectedSpace));

            // Starter dismantle must always reject.
            Assert.That(sim.DismantleFacility("starter-solar").Success, Is.False);
            Assert.That(sim.DismantleFacility("core-habitat").Success, Is.False);

            // Occupant / surface walk guard on a UtilityLine parcel:
            // Find another adjacent buildable cell, build UtilityLine, stand crew-1 on it -> dismantling line rejects while crew stands there!
            GridCoord lineCoord = default;
            foreach (var c in sim.State.Map.Cells)
            {
                if (c.Coord != adj && c.IsExplored && c.IsBuildable && c.Protection == StarterProtectionKind.None &&
                    UtilityNetwork.IsCoordConnectedPreview(sim.State.Map, c.Coord))
                {
                    lineCoord = c.Coord;
                    break;
                }
            }
            EnsureClaimed(sim, lineCoord);
            string lineId = sim.PlaceBlueprint(FacilityKind.UtilityLine, lineCoord).Reason;
            Assert.That(sim.AssignBuildBlueprint("crew-2", lineId).Success, Is.True);
            sim.Tick(60.0);
            Assert.That(sim.ReturnToShelter("crew-2").Success, Is.True);
            sim.Tick(25.0);

            // Move crew-1 to stand on lineCoord (`allowTargetCell: true`).
            Assert.That(sim.AssignSurfaceWork("crew-1", lineCoord, workRequiredSeconds: 15.0, allowTargetCell: true).Success, Is.True);
            sim.Tick(30.0); // walks onto lineCoord and completes surface work (standing there Idle Outside)
            Assert.That(sim.DismantleFacility(lineId).Success, Is.False);

            // Return crew-1 home -> dismantling line succeeds cleanly.
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(30.0);
            Assert.That(sim.DismantleFacility(lineId).Success, Is.True);
        }

        [Test]
        public void DismantlingOverchargedBatteryPreviewsExactEnergySpillAndLogsOverflow()
        {
            var sim = CreateControlledOpsSim(initialBattery: 92, initialMaterials: 80, materialCapacity: 100);
            GridCoord adj = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, adj);
            string batId = sim.PlaceBlueprint(FacilityKind.Battery, adj).Reason;
            Assert.That(sim.AssignBuildBlueprint("crew-1", batId).Success, Is.True);
            sim.Tick(95.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(25.0);

            // Charge > 110 during daytime.
            double toLateDay = 450.0 - sim.State.ElapsedSeconds;
            if (toLateDay > 0) sim.Tick(toLateDay);
            Assert.That(sim.State.Resources.Battery, Is.GreaterThan(110.0));

            var batFac = FindFacility(sim.State, batId);
            FacilityPlacementValidator.PreviewStorageSpillOnDismantle(sim.Config.Resources, sim.State, batFac, out double eSpill, out double wSpill);
            Assert.That(eSpill, Is.EqualTo(sim.State.Resources.Battery - 100.0).Within(1e-6));
            Assert.That(wSpill, Is.EqualTo(0.0));

            Assert.That(sim.DismantleFacility(batId).Success, Is.True);
            Assert.That(sim.State.EffectiveBatteryCapacity, Is.EqualTo(100.0).Within(1e-9));
            Assert.That(sim.State.Resources.Battery, Is.EqualTo(100.0).Within(1e-6));
            Assert.That(sim.State.Resources.BatteryOverflowLostTotal, Is.EqualTo(eSpill).Within(1e-6));
        }
    }
}
