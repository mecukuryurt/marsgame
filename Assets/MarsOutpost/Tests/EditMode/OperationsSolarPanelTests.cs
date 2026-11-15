using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsSolarPanelTests
    {
        private static MissionSimulation CreateControlledOpsSim(
            double initialBattery = 20,
            double initialPanelFouling = 0.20,
            EventState[] events = null,
            int initialMaterials = 60)
        {
            var resCfg = new ResourceConfig(
                solarRate: 0.30,
                batteryCapacity: 200,
                initialBattery: initialBattery,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: initialPanelFouling,
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
                operationsSeed: 424242,
                enableOperationsRandomEvents: false,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: initialMaterials,
                materialCapacity: Math.Max(80, initialMaterials));
            var sim = new MissionSimulation(cfg, new MissionScenario(events: events ?? Array.Empty<EventState>()));
            sim.SetResearchEnabled(false);
            sim.SetWaterExtractorEnabled(false);
            return sim;
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
            Assert.Fail("Expected adjacent core-connected buildable tile on Operations map.");
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

        private static void EnsureClaimed(MissionSimulation sim, GridCoord coord)
        {
            if (!sim.State.Map.GetCell(coord).IsClaimed)
            {
                var claim = sim.ClaimParcel(coord);
                Assert.That(claim.Success, Is.True, $"Claim {coord} failed: {claim.Reason}");
            }
        }

        private static FacilityState FindFacility(MissionState state, string facId)
        {
            for (int i = 0; i < state.Facilities.Count; i++)
                if (state.Facilities[i].FacilityId == facId)
                    return state.Facilities[i];
            return null;
        }

        [Test]
        public void CommissioningNewConnectedSolarPanelAddsExactThirdWeightAndBoostsDayCharge()
        {
            var baselineSim = CreateControlledOpsSim(initialBattery: 20);
            double baseFactor = SolarProduction.GetPanelHealthFactor(baselineSim.State.Resources, baselineSim.State.Facilities);
            Assert.That(baseFactor, Is.EqualTo(1.0).Within(1e-9));

            var bpSim = CreateControlledOpsSim(initialBattery: 20);
            var target = FindAdjacentConnectedBuildable(bpSim.State.Map);
            EnsureClaimed(bpSim, target);
            double tileSolarFactor = bpSim.State.Map.GetCell(target).SolarFactor;

            var placeRes = bpSim.PlaceBlueprint(FacilityKind.SolarPanel, target);
            Assert.That(placeRes.Success, Is.True, placeRes.Reason);
            string facId = bpSim.State.Facilities[bpSim.State.Facilities.Count - 1].FacilityId;

            // Unfinished blueprint adds 0 panels and 0 production.
            Assert.That(bpSim.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(SolarProduction.GetPanelHealthFactor(bpSim.State.Resources, bpSim.State.Facilities), Is.EqualTo(1.0).Within(1e-9));

            baselineSim.Tick(10.0);
            bpSim.Tick(10.0);
            Assert.That(bpSim.State.Resources.Battery, Is.EqualTo(baselineSim.State.Resources.Battery).Within(1e-6));

            // Commission the blueprint and align battery before measuring exact delta.
            Assert.That(bpSim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            bpSim.Tick(80.0);
            Assert.That(bpSim.ReturnToShelter("crew-1").Success, Is.True);
            bpSim.Tick(30.0);

            var builtFac = FindFacility(bpSim.State, facId);
            Assert.That(builtFac, Is.Not.Null);
            Assert.That(builtFac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(builtFac.IsConnectedToCore, Is.True);
            Assert.That(builtFac.LinkedPanelIds.Count, Is.EqualTo(1));
            Assert.That(bpSim.State.Resources.Panels.Count, Is.EqualTo(4));

            var newPanel = bpSim.State.Resources.Panels[3];
            Assert.That(newPanel.Weight, Is.EqualTo(1.0 / 3.0).Within(1e-9));
            Assert.That(newPanel.SolarFactor, Is.EqualTo(tileSolarFactor).Within(1e-9));
            Assert.That(newPanel.FacilityId, Is.EqualTo(facId));

            // Total health factor is 1.0 + (1/3)*tileSolarFactor (NOT normalized 1/4).
            double expectedTotalFactor = 1.0 + (1.0 / 3.0) * tileSolarFactor;
            Assert.That(SolarProduction.GetPanelHealthFactor(bpSim.State.Resources, bpSim.State.Facilities),
                Is.EqualTo(expectedTotalFactor).Within(1e-9));

            // Compare 10s battery gain after identical baseline elapsed alignment (both at 120s).
            baselineSim.Tick(110.0);
            Assert.That(bpSim.State.ElapsedSeconds, Is.EqualTo(baselineSim.State.ElapsedSeconds).Within(1e-9));
            double baseB0 = baselineSim.State.Resources.Battery;
            baselineSim.Tick(10.0);
            double baseGain10 = baselineSim.State.Resources.Battery - baseB0;

            double bpB0 = bpSim.State.Resources.Battery;
            bpSim.Tick(10.0);
            double bpGain10 = bpSim.State.Resources.Battery - bpB0;

            double expectedExtraPerSec = bpSim.Config.Resources.SolarRate * (1.0 / 3.0) * tileSolarFactor * (1.0 - bpSim.State.Resources.PanelFouling);
            Assert.That(bpGain10 - baseGain10, Is.EqualTo(expectedExtraPerSec * 10.0).Within(0.01));
        }

        [Test]
        public void DisconnectedNightDustAndFaultedSolarPanelsFollowContract()
        {
            var events = new[]
            {
                new EventState("dust-test", "Test Dust", EventKind.Dust, 180, 200, 300, 0.50, "TEST", "Half solar dust storm")
            };
            var sim = CreateControlledOpsSim(initialBattery: 40, initialPanelFouling: 0.0, events: events);
            var chain = FindTwoStepChainFromCore(sim.State.Map);
            var lineCoord = chain[0];
            var panelCoord = chain[1];
            EnsureClaimed(sim, lineCoord);
            EnsureClaimed(sim, panelCoord);
            double tileFactor = sim.State.Map.GetCell(panelCoord).SolarFactor;

            // Place & build disconnected panel first -> 4 panels exist, but disconnected one produces 0.
            var placePanel = sim.PlaceBlueprint(FacilityKind.SolarPanel, panelCoord);
            Assert.That(placePanel.Success, Is.True, placePanel.Reason);
            string panelFacId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            Assert.That(sim.AssignBuildBlueprint("crew-1", panelFacId).Success, Is.True);
            sim.Tick(80.0);

            var panelFac = FindFacility(sim.State, panelFacId);
            Assert.That(panelFac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(panelFac.IsConnectedToCore, Is.False);
            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(4));
            Assert.That(SolarProduction.GetPanelHealthFactor(sim.State.Resources, sim.State.Facilities), Is.EqualTo(1.0).Within(1e-9));
            double currentDisconnected = SolarProduction.ComputeFacilitySolarOutput(sim.Config.Resources.SolarRate, sim.State, panelFac, out _);
            Assert.That(currentDisconnected, Is.EqualTo(0.0).Within(1e-9));

            // Bridge utility line -> becomes connected and immediately contributes.
            var placeLine = sim.PlaceBlueprint(FacilityKind.UtilityLine, lineCoord);
            Assert.That(placeLine.Success, Is.True, placeLine.Reason);
            string lineFacId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            Assert.That(sim.AssignBuildBlueprint("crew-1", lineFacId, replace: true).Success, Is.True);
            sim.Tick(50.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(30.0);

            panelFac = FindFacility(sim.State, panelFacId);
            Assert.That(panelFac.IsConnectedToCore, Is.True);
            double expectedConnected = 1.0 + (1.0 / 3.0) * tileFactor;
            Assert.That(SolarProduction.GetPanelHealthFactor(sim.State.Resources, sim.State.Facilities),
                Is.EqualTo(expectedConnected).Within(1e-9));

            // Advance into active dust storm window [200s..300s] -> 50% dust multiplier.
            double toDust = 220.0 - sim.State.ElapsedSeconds;
            if (toDust > 0)
                sim.Tick(toDust);
            Assert.That(sim.State.Resources.AtmosphericDustLoss, Is.EqualTo(0.50).Within(1e-9));
            double dustOut = SolarProduction.ComputeFacilitySolarOutput(sim.Config.Resources.SolarRate, sim.State, panelFac, out double baseDayOut);
            Assert.That(baseDayOut, Is.EqualTo(sim.Config.Resources.SolarRate * (1.0 / 3.0) * tileFactor).Within(1e-9));
            Assert.That(dustOut, Is.EqualTo(baseDayOut * (1.0 - sim.State.Resources.PanelFouling) * 0.50).Within(1e-9));

            // Fault 4th panel -> its factor drops to 0 cleanly; remaining 3 starter panels stay at 1.0.
            var testRes = sim.State.Resources.Copy();
            testRes.Panels[3].IsFaulted = true;
            Assert.That(SolarProduction.GetPanelHealthFactor(testRes, sim.State.Facilities), Is.EqualTo(1.0).Within(1e-9));
            testRes.Panels[0].IsFaulted = true;
            Assert.That(SolarProduction.GetPanelHealthFactor(testRes, sim.State.Facilities), Is.EqualTo(2.0 / 3.0).Within(1e-9));

            // Advance to Night (>= 480s) -> solar output must be 0.
            sim.Tick(500.0 - sim.State.ElapsedSeconds);
            Assert.That(sim.State.IsNight, Is.True);
            Assert.That(SolarProduction.ComputeFacilitySolarOutput(sim.Config.Resources.SolarRate, sim.State, panelFac, out _),
                Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void CompleteBlueprintIdempotenceAndSaveLoadPreserveFourPanels()
        {
            var sim = CreateControlledOpsSim(initialBattery: 30);
            var target = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, target);
            var placeRes = sim.PlaceBlueprint(FacilityKind.SolarPanel, target);
            Assert.That(placeRes.Success, Is.True, placeRes.Reason);
            string facId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;

            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            sim.Tick(80.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(25.0);

            Assert.That(sim.State.Resources.Panels.Count, Is.EqualTo(4));
            double factorBefore = SolarProduction.GetPanelHealthFactor(sim.State.Resources, sim.State.Facilities);

            string json = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(json, out var loaded, out string loadErr), Is.True, loadErr);
            Assert.That(loaded.State.Resources.Panels.Count, Is.EqualTo(4));
            Assert.That(loaded.State.Resources.Panels[3].FacilityId, Is.EqualTo(facId));
            Assert.That(SolarProduction.GetPanelHealthFactor(loaded.State.Resources, loaded.State.Facilities),
                Is.EqualTo(factorBefore).Within(1e-9));

            var builtFac = FindFacility(loaded.State, facId);
            Assert.That(builtFac.LinkedPanelIds.Count, Is.EqualTo(1));
            Assert.That(builtFac.LinkedPanelIds[0], Is.EqualTo(loaded.State.Resources.Panels[3].Id));
        }
    }
}
