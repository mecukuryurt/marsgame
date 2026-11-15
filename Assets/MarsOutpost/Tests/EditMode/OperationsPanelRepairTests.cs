using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsPanelRepairTests
    {
        private static MissionSimulation CreateOpsSim(bool enableEvents = true, int seed = 424242, int initialMaterials = 60)
        {
            var resCfg = new ResourceConfig(
                solarRate: 0.30,
                batteryCapacity: 200,
                initialBattery: 120,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
                initialPanelFouling: 0.25,
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
                operationsSeed: seed,
                enableOperationsRandomEvents: enableEvents,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations,
                initialMaterials: initialMaterials,
                materialCapacity: Math.Max(80, initialMaterials));
            return new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
        }

        private static void EnsureClaimed(MissionSimulation sim, GridCoord coord)
        {
            if (!sim.State.Map.GetCell(coord).IsClaimed)
                Assert.That(sim.ClaimParcel(coord).Success, Is.True);
        }

        private static GridCoord FindAdjacentConnectedBuildable(ParcelMap map)
        {
            foreach (var c in map.Cells)
            {
                if (c.IsExplored && c.IsBuildable && c.Protection == StarterProtectionKind.None &&
                    UtilityNetwork.IsCoordConnectedPreview(map, c.Coord))
                    return c.Coord;
            }
            Assert.Fail("Expected adjacent core-connected buildable tile.");
            return default;
        }

        private static GridCoord FindDisconnectedBuildable(ParcelMap map)
        {
            foreach (var c in map.Cells)
            {
                if (!c.IsExplored || !c.IsBuildable || c.Protection != StarterProtectionKind.None)
                    continue;
                if (UtilityNetwork.IsCoordConnectedPreview(map, c.Coord))
                    continue;
                if (c.IsClaimed)
                    return c.Coord;
                foreach (var n in map.Config.GetCardinalNeighbors(c.Coord))
                {
                    if (map.TryGetCell(n, out var nc) && nc.IsClaimed)
                        return c.Coord;
                }
            }
            Assert.Fail("Expected claimable unconnected buildable tile.");
            return default;
        }

        private static void AdvanceClearingPauses(MissionSimulation sim, double seconds, double step = 10.0)
        {
            double remaining = seconds;
            while (remaining > 1e-6)
            {
                double chunk = Math.Min(step, remaining);
                sim.Tick(chunk);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
                remaining -= chunk;
            }
        }

        [Test]
        public void BuildingPanelsNeverAltersOtherFourChannelSchedulesAndTargetsOnlyOnlinePanels()
        {
            var baseline = CreateOpsSim(enableEvents: true, seed: 777111);
            var expanded = CreateOpsSim(enableEvents: true, seed: 777111, initialMaterials: 60);

            var connCoord = FindAdjacentConnectedBuildable(expanded.State.Map);
            var unconnCoord = FindDisconnectedBuildable(expanded.State.Map);
            EnsureClaimed(expanded, connCoord);
            EnsureClaimed(expanded, unconnCoord);

            Assert.That(expanded.PlaceBlueprint(FacilityKind.SolarPanel, connCoord).Success, Is.True);
            string connFacId = expanded.State.Facilities[expanded.State.Facilities.Count - 1].FacilityId;
            Assert.That(expanded.AssignBuildBlueprint("crew-1", connFacId).Success, Is.True);

            Assert.That(expanded.PlaceBlueprint(FacilityKind.SolarPanel, unconnCoord).Success, Is.True);
            string unconnFacId = expanded.State.Facilities[expanded.State.Facilities.Count - 1].FacilityId;
            Assert.That(expanded.AssignBuildBlueprint("crew-2", unconnFacId).Success, Is.True);

            baseline.Tick(80.0);
            expanded.Tick(80.0);
            expanded.ReturnToShelter("crew-1");
            expanded.ReturnToShelter("crew-2");
            baseline.Tick(30.0);
            expanded.Tick(30.0);

            // Non-solar channels retain identical RNG state & next trigger time despite panel construction.
            for (int i = 0; i < baseline.State.ChannelSchedules.Count; i++)
            {
                var bCh = baseline.State.ChannelSchedules[i];
                var eCh = expanded.State.ChannelSchedules[i];
                if (bCh.ChannelId != OperationsChannelId.SolarPanel)
                {
                    Assert.That(eCh.RngState, Is.EqualTo(bCh.RngState));
                    Assert.That(eCh.NextTriggerAtSeconds, Is.EqualTo(bCh.NextTriggerAtSeconds).Within(1e-9));
                }
            }

            // Look up disconnected panel ID via unconnFac (build completion order depends on walk distance).
            FacilityState unconnFac = null;
            FacilityState connFac = null;
            foreach (var f in expanded.State.Facilities)
            {
                if (f.FacilityId == unconnFacId) unconnFac = f;
                else if (f.FacilityId == connFacId) connFac = f;
            }
            Assert.That(unconnFac, Is.Not.Null);
            Assert.That(connFac, Is.Not.Null);
            Assert.That(unconnFac.IsConnectedToCore, Is.False);
            Assert.That(connFac.IsConnectedToCore, Is.True);
            string disconnectedPanelId = unconnFac.LinkedPanelIds[0];

            var solarCh = expanded.State.ChannelSchedules[1];
            double toTrigger = solarCh.NextTriggerAtSeconds - expanded.State.ElapsedSeconds + 5.0;
            AdvanceClearingPauses(expanded, toTrigger);

            var activeSolar = expanded.State.ChannelSchedules[1];
            Assert.That(activeSolar.IsActive, Is.True);
            Assert.That(activeSolar.ActiveTargetId, Is.Not.Null.And.Not.Empty);
            Assert.That(activeSolar.ActiveTargetId, Is.Not.EqualTo(disconnectedPanelId));

            // Subsequent ticks do not overwrite ActiveTargetId.
            string firstTarget = activeSolar.ActiveTargetId;
            AdvanceClearingPauses(expanded, 20.0);
            Assert.That(expanded.State.ChannelSchedules[1].ActiveTargetId, Is.EqualTo(firstTarget));
        }

        [Test]
        public void TargetedRepairWalksOverlandFixesSinglePanelPreservesProgressAndSeparatesDust()
        {
            // Verify across deterministic seed 1003 where SolarPanel hits connected 4th panel OR test both panel-by-panel repair directly.
            var sim = CreateOpsSim(enableEvents: true, seed: 424242, initialMaterials: 60);
            var connCoord = FindAdjacentConnectedBuildable(sim.State.Map);
            EnsureClaimed(sim, connCoord);
            Assert.That(sim.PlaceBlueprint(FacilityKind.SolarPanel, connCoord).Success, Is.True);
            string facId = sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            sim.Tick(80.0);
            sim.ReturnToShelter("crew-1");
            sim.Tick(30.0);

            string fourthPanelId = sim.State.Resources.Panels[3].Id;

            // Advance until SolarPanel fault triggers.
            double toSolar = sim.State.ChannelSchedules[1].NextTriggerAtSeconds - sim.State.ElapsedSeconds + 2.0;
            AdvanceClearingPauses(sim, toSolar);
            string activeTarget = sim.State.ChannelSchedules[1].ActiveTargetId;
            Assert.That(sim.State.ChannelSchedules[1].IsActive, Is.True);

            // Inject a second independent faulted panel into save data (simulating dual fault: starter + 4th) so we verify single-target repair!
            string rawJson = sim.SaveToJson();
            string secondId = activeTarget == fourthPanelId ? "panel-1" : fourthPanelId;
            string dualFaultJson = System.Text.RegularExpressions.Regex.Replace(
                rawJson,
                $@"(""id""\s*:\s*""{System.Text.RegularExpressions.Regex.Escape(secondId)}""[^}}]*?""faulted""\s*:\s*)false",
                "${1}true");
            Assert.That(dualFaultJson, Is.Not.EqualTo(rawJson), "Failed to inject second fault in test save payload.");
            Assert.That(MissionSimulation.TryLoadFromJson(dualFaultJson, out var dualSim, out string err), Is.True, err);

            int faultedBefore = 0;
            foreach (var p in dualSim.State.Resources.Panels)
                if (p.IsFaulted) faultedBefore++;
            Assert.That(faultedBefore, Is.EqualTo(2));

            // If activeTarget belongs to built fac, CanDismantleFacility must reject!
            if (activeTarget == fourthPanelId)
            {
                var dismantleBlocked = dualSim.CanDismantleFacility(facId);
                Assert.That(dismantleBlocked.Success, Is.False);
            }

            // Test separation: running PanelMaintenance (fouling clean) must NOT clear any fault.
            double foulingBefore = dualSim.State.Resources.PanelFouling;
            Assert.That(dualSim.AssignTask("crew-2", "panel-maintenance").Success, Is.True);
            AdvanceClearingPauses(dualSim, 60.0);
            Assert.That(dualSim.State.Resources.PanelFouling, Is.LessThan(foulingBefore));
            int stillFaulted = 0;
            foreach (var p in dualSim.State.Resources.Panels)
                if (p.IsFaulted) stillFaulted++;
            Assert.That(stillFaulted, Is.EqualTo(2));

            // Now dispatch targeted overland repair to activeTarget -> verify Travel phase, cancel mid-work preserves progress.
            Assert.That(dualSim.AssignRepairPanel("crew-1", activeTarget).Success, Is.True);
            bool sawTravel = false;
            for (int t = 0; t < 20; t++)
            {
                dualSim.Tick(1.0);
                var job = dualSim.State.SurfaceJobs[dualSim.State.SurfaceJobs.Count - 1];
                if (job.Phase == SurfaceJobPhase.Travel) sawTravel = true;
                if (job.Phase == SurfaceJobPhase.Work) break;
            }
            Assert.That(sawTravel, Is.True, "AssignRepairPanel must traverse surface route to target panel.");

            dualSim.Tick(20.0);
            Assert.That(dualSim.CancelTask("crew-1").Success, Is.True);

            PanelUnitState partialPanel = null;
            foreach (var p in dualSim.State.Resources.Panels)
                if (p.Id == activeTarget) partialPanel = p;
            Assert.That(partialPanel.IsFaulted, Is.True);
            Assert.That(partialPanel.RepairProgressSeconds, Is.InRange(19.0, 21.0));

            // Save & Load mid-repair must preserve ActiveTargetId & RepairProgressSeconds.
            string midJson = dualSim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(midJson, out var reloaded, out string err2), Is.True, err2);
            Assert.That(reloaded.State.ChannelSchedules[1].ActiveTargetId, Is.EqualTo(activeTarget));
            PanelUnitState reloadedTarget = null;
            foreach (var p in reloaded.State.Resources.Panels)
                if (p.Id == activeTarget) reloadedTarget = p;
            Assert.That(reloadedTarget.RepairProgressSeconds, Is.InRange(19.0, 21.0));

            // Resume repair and finish -> ONLY activeTarget becomes healthy, secondId stays faulted, PanelFouling untouched!
            double foulingAtRepairStart = reloaded.State.Resources.PanelFouling;
            Assert.That(reloaded.AssignRepairPanel("crew-1", activeTarget).Success, Is.True);
            AdvanceClearingPauses(reloaded, 45.0);

            PanelUnitState fixedTarget = null;
            PanelUnitState stillBrokenOther = null;
            foreach (var p in reloaded.State.Resources.Panels)
            {
                if (p.Id == activeTarget) fixedTarget = p;
                if (p.Id == secondId) stillBrokenOther = p;
            }
            Assert.That(fixedTarget.IsFaulted, Is.False);
            Assert.That(fixedTarget.RepairProgressSeconds, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(stillBrokenOther.IsFaulted, Is.True, "Repairing one panel must NOT repair another faulted panel.");
            Assert.That(reloaded.State.Resources.PanelFouling, Is.LessThanOrEqualTo(foulingAtRepairStart + 0.02),
                "Repairing fault does not clean panel fouling.");

            // Double repair on already-healthy panel must be rejected.
            Assert.That(reloaded.AssignRepairPanel("crew-1", activeTarget).Success, Is.False);

            // Dismantle guard allows dismantle once active target is repaired.
            Assert.That(reloaded.CanDismantleFacility(facId).Success, Is.True);

            // Solar channel rescheduled after activeTarget repair!
            Assert.That(reloaded.State.ChannelSchedules[1].IsActive, Is.False);
            Assert.That(reloaded.State.ChannelSchedules[1].ActiveTargetId, Is.Null);
            Assert.That(reloaded.State.ChannelSchedules[1].NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(reloaded.State.ElapsedSeconds));
        }
    }
}
