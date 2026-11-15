using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsConstructionTests
    {
        private static MissionSimulation CreateCleanOpsSim(
            double initialBattery = 50.0,
            double solarRate = 0.72,
            double maxStamina = 100.0)
        {
            var resCfg = new ResourceConfig(
                initialBattery: initialBattery,
                solarRate: solarRate,
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
                maxStamina: maxStamina,
                researchGoal: 0,
                targetSols: 0,
                maxReserveMeals: 2,
                dailyReserveMealSupply: 1,
                enableOperationsRandomEvents: false,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations);

            return new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
        }

        private static GridCoord ClaimFirstAdjacentBuildable(MissionSimulation sim)
        {
            foreach (var cell in sim.State.Map.Cells)
            {
                if (!cell.IsClaimed && cell.IsExplored && cell.IsBuildable &&
                    cell.Protection == StarterProtectionKind.None &&
                    sim.State.Map.HasClaimedNeighbor(cell.Coord))
                {
                    var res = sim.ClaimParcel(cell.Coord);
                    Assert.That(res.Success, Is.True, res.Reason);
                    return cell.Coord;
                }
            }
            Assert.Fail("No adjacent claimable buildable cell found.");
            return default;
        }

        private static string PlaceAndGetId(MissionSimulation sim, FacilityKind kind, GridCoord coord)
        {
            var res = sim.PlaceBlueprint(kind, coord);
            Assert.That(res.Success, Is.True, res.Reason);
            return sim.State.Facilities[sim.State.Facilities.Count - 1].FacilityId;
        }

        private static FacilityState FindFacility(MissionState state, string id)
        {
            foreach (var f in state.Facilities)
                if (f.FacilityId == id)
                    return f;
            return null;
        }

        private static SurfaceJobState FindJobByTarget(MissionState state, string targetId)
        {
            if (state.SurfaceJobs == null) return null;
            SurfaceJobState found = null;
            foreach (var j in state.SurfaceJobs)
                if (j.TargetId == targetId)
                    found = j;
            return found;
        }

        [Test]
        public void UtilityLineConstructionCompletesLifecycleAndActivatesUtilityLayer()
        {
            var sim = CreateCleanOpsSim();
            var target = ClaimFirstAdjacentBuildable(sim);
            string facId = PlaceAndGetId(sim, FacilityKind.UtilityLine, target);

            var fac = FindFacility(sim.State, facId);
            Assert.That(fac, Is.Not.Null);
            Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(sim.State.Map.GetCell(target).HasUtilityLine, Is.False);
            Assert.That(sim.State.Map.GetCell(target).UtilityBlueprintId, Is.EqualTo(facId));

            var assign = sim.AssignBuildBlueprint("crew-1", facId);
            Assert.That(assign.Success, Is.True, assign.Reason);

            // Step 0.5s: crew must still be in Travel and blueprint must have 0 work progress!
            sim.Tick(0.5);
            fac = FindFacility(sim.State, facId);
            var job = FindJobByTarget(sim.State, facId);
            Assert.That(job.Phase, Is.EqualTo(SurfaceJobPhase.Travel));
            Assert.That(fac.WorkProgressSeconds, Is.EqualTo(0.0).Within(1e-9), "Travel time must not count as construction work.");
            Assert.That(fac.EnergySpentTotal, Is.EqualTo(0.0).Within(1e-9));

            // Advance enough for travel + 5s work
            sim.Tick(25.0);
            fac = FindFacility(sim.State, facId);
            Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fac.IsProductionApplied, Is.True);
            Assert.That(fac.WorkProgressSeconds, Is.EqualTo(5.0).Within(1e-6));
            Assert.That(sim.State.Map.GetCell(target).HasUtilityLine, Is.True);
            Assert.That(sim.State.Map.GetCell(target).UtilityBlueprintId, Is.Null);
            Assert.That(sim.State.Crew[0].ActiveSurfaceJobId, Is.Null);
            Assert.That(sim.State.Crew[0].Status, Is.EqualTo(CrewStatus.Idle));
        }

        [Test]
        public void ConstructionRequiresEnergyAndRespectsLifeSupportPriorityAtZeroBattery()
        {
            // With Battery == 0 and solarRate == 0, construction energy cannot be drawn!
            var sim = CreateCleanOpsSim(initialBattery: 0.0, solarRate: 0.0);
            var target = ClaimFirstAdjacentBuildable(sim);
            string facId = PlaceAndGetId(sim, FacilityKind.SolarPanel, target);

            var assign = sim.AssignBuildBlueprint("crew-1", facId);
            Assert.That(assign.Success, Is.True, assign.Reason);

            // Travel completes, then 25s pass with 0 solar & 0 battery -> work must stall at 0
            sim.Tick(30.0);
            var fac = FindFacility(sim.State, facId);
            var job = FindJobByTarget(sim.State, facId);
            Assert.That(job.Phase, Is.EqualTo(SurfaceJobPhase.Work));
            Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(fac.WorkProgressSeconds, Is.EqualTo(0.0).Within(1e-9), "SolarPanel construction must not advance with 0 energy.");
            Assert.That(fac.EnergySpentTotal, Is.EqualTo(0.0).Within(1e-9));

            // With normal bright morning sunlight, life-support stays priority #1 and surplus commissions SolarPanel cleanly.
            var sunnySim = CreateCleanOpsSim();
            sunnySim.Tick(100.0);
            var target2 = ClaimFirstAdjacentBuildable(sunnySim);
            string facId2 = PlaceAndGetId(sunnySim, FacilityKind.SolarPanel, target2);
            Assert.That(sunnySim.AssignBuildBlueprint("crew-1", facId2).Success, Is.True);
            sunnySim.Tick(60.0);

            var fac2 = FindFacility(sunnySim.State, facId2);
            Assert.That(fac2.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fac2.IsProductionApplied, Is.True);
            Assert.That(fac2.WorkProgressSeconds, Is.EqualTo(45.0).Within(1e-6));
            Assert.That(fac2.EnergySpentTotal, Is.EqualTo(2.0).Within(1e-6));
            Assert.That(sunnySim.State.Resources.UnmetOxygen, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void SingleCrewLockAndPartialProgressPreservedAcrossRecallReplaceAndResume()
        {
            var sim = CreateCleanOpsSim();
            var target = ClaimFirstAdjacentBuildable(sim);
            string facId = PlaceAndGetId(sim, FacilityKind.SolarPanel, target);

            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            // Second crew rejected while crew-1 is assigned (traveling)
            var secondReject = sim.AssignBuildBlueprint("crew-2", facId);
            Assert.That(secondReject.Success, Is.False);

            // Advance into partial Work
            sim.Tick(15.0);
            var job1 = FindJobByTarget(sim.State, facId);
            Assert.That(job1.Phase, Is.EqualTo(SurfaceJobPhase.Work));
            var facAfter1 = FindFacility(sim.State, facId);
            double savedWork1 = facAfter1.WorkProgressSeconds;
            double savedEnergy1 = facAfter1.EnergySpentTotal;
            Assert.That(savedWork1, Is.GreaterThan(1.0).And.LessThan(40.0));
            Assert.That(savedEnergy1, Is.GreaterThan(0.01).And.LessThan(2.0));

            // Recall crew-1 to shelter: partial progress & energy MUST remain
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            facAfter1 = FindFacility(sim.State, facId);
            Assert.That(facAfter1.WorkProgressSeconds, Is.EqualTo(savedWork1).Within(1e-9));
            Assert.That(facAfter1.EnergySpentTotal, Is.EqualTo(savedEnergy1).Within(1e-9));
            Assert.That(facAfter1.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));

            // Now crew-2 CAN take over and only pays remaining work & energy!
            Assert.That(sim.AssignBuildBlueprint("crew-2", facId).Success, Is.True);
            var job2 = FindJobByTarget(sim.State, facId);
            Assert.That(job2.WorkRequiredSeconds, Is.EqualTo(45.0 - savedWork1).Within(1e-6));
            Assert.That(job2.WorkEnergyTotal, Is.EqualTo(2.0 - savedEnergy1).Within(1e-6));

            sim.Tick(10.0);
            double savedWork2 = FindFacility(sim.State, facId).WorkProgressSeconds;
            double savedEnergy2 = FindFacility(sim.State, facId).EnergySpentTotal;
            Assert.That(savedWork2, Is.GreaterThan(savedWork1));

            // Replace crew-2 with an exterior task ("panel-maintenance"): partial progress still preserved
            var repRes = sim.ReplaceTask("crew-2", "panel-maintenance");
            Assert.That(repRes.Success, Is.True, repRes.Reason);
            Assert.That(FindFacility(sim.State, facId).WorkProgressSeconds, Is.EqualTo(savedWork2).Within(1e-9));
            Assert.That(FindFacility(sim.State, facId).EnergySpentTotal, Is.EqualTo(savedEnergy2).Within(1e-9));

            // Complete using crew-3
            Assert.That(sim.AssignBuildBlueprint("crew-3", facId).Success, Is.True);
            sim.Tick(60.0);
            var finalFac = FindFacility(sim.State, facId);
            Assert.That(finalFac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(finalFac.IsProductionApplied, Is.True);
            Assert.That(finalFac.WorkProgressSeconds, Is.EqualTo(45.0).Within(1e-6));
            Assert.That(finalFac.EnergySpentTotal, Is.EqualTo(2.0).Within(1e-6));

            // Cannot re-assign after commissioned
            Assert.That(sim.AssignBuildBlueprint("crew-3", facId).Success, Is.False);
        }

        [Test]
        public void ExhaustionDuringConstructionPreservesProgressAndReleasesFacilityLock()
        {
            // MaxStamina 2.0: enough for short travel (-0.10/s) + few seconds of work (-0.20/s), then exhausts
            var sim = CreateCleanOpsSim(maxStamina: 2.0);
            var target = ClaimFirstAdjacentBuildable(sim);
            string facId = PlaceAndGetId(sim, FacilityKind.SolarPanel, target);

            Assert.That(sim.AssignBuildBlueprint("crew-1", facId).Success, Is.True);
            sim.Tick(25.0);

            Assert.That(sim.State.Crew[0].Status, Is.EqualTo(CrewStatus.Exhausted));
            Assert.That(sim.State.Crew[0].ActiveSurfaceJobId, Is.Null);
            var fac = FindFacility(sim.State, facId);
            Assert.That(fac.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(fac.WorkProgressSeconds, Is.GreaterThan(0.0).And.LessThan(45.0));

            // Fresh crew-2 is allowed to resume
            Assert.That(sim.AssignBuildBlueprint("crew-2", facId).Success, Is.True);
        }

        [Test]
        public void BigAndSmallTickParityAndIdempotentCommissioningAndSaveLoadRoundtrip()
        {
            var bigSim = CreateCleanOpsSim();
            var smallSim = CreateCleanOpsSim();

            var targetA = ClaimFirstAdjacentBuildable(bigSim);
            var targetB = ClaimFirstAdjacentBuildable(smallSim);
            Assert.That(targetA, Is.EqualTo(targetB));

            string idA = PlaceAndGetId(bigSim, FacilityKind.SolarPanel, targetA);
            string idB = PlaceAndGetId(smallSim, FacilityKind.SolarPanel, targetB);

            Assert.That(bigSim.AssignBuildBlueprint("crew-1", idA).Success, Is.True);
            Assert.That(smallSim.AssignBuildBlueprint("crew-1", idB).Success, Is.True);

            // Step mid-Travel -> save/load parity check
            bigSim.Tick(1.0);
            for (int i = 0; i < 4; i++) smallSim.Tick(0.25);

            string travelJson = bigSim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(travelJson, out var loadedTravel, out var err1), Is.True, err1);
            Assert.That(FindJobByTarget(loadedTravel.State, idA).Phase, Is.EqualTo(SurfaceJobPhase.Travel));

            // Step mid-Work
            bigSim.Tick(19.0);
            for (int i = 0; i < 76; i++) smallSim.Tick(0.25);
            loadedTravel.Tick(19.0);

            string workJson = bigSim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(workJson, out var loadedWork, out var err2), Is.True, err2);
            var fWork = FindFacility(loadedWork.State, idA);
            Assert.That(fWork.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Blueprint));
            Assert.That(fWork.WorkProgressSeconds, Is.EqualTo(FindFacility(smallSim.State, idB).WorkProgressSeconds).Within(1e-5));

            // Step past completion + extra idle time (verifies idempotency)
            bigSim.Tick(100.0);
            for (int i = 0; i < 400; i++) smallSim.Tick(0.25);
            loadedTravel.Tick(100.0);
            loadedWork.Tick(100.0);

            var fBig = FindFacility(bigSim.State, idA);
            var fSmall = FindFacility(smallSim.State, idB);
            var fLT = FindFacility(loadedTravel.State, idA);
            var fLW = FindFacility(loadedWork.State, idA);

            Assert.That(fBig.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fSmall.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fLT.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fLW.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));

            Assert.That(fBig.IsProductionApplied, Is.True);
            Assert.That(fSmall.IsProductionApplied, Is.True);
            Assert.That(bigSim.State.Resources.Battery, Is.EqualTo(smallSim.State.Resources.Battery).Within(1e-4));
            Assert.That(bigSim.State.Crew[0].Stamina, Is.EqualTo(smallSim.State.Crew[0].Stamina).Within(1e-5));

            // Save post-Commissioned & reload
            string doneJson = bigSim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(doneJson, out var loadedDone, out var err3), Is.True, err3);
            var fDone = FindFacility(loadedDone.State, idA);
            Assert.That(fDone.LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Commissioned));
            Assert.That(fDone.IsProductionApplied, Is.True);

            // Restart resets cleanly
            loadedDone.Restart();
            Assert.That(loadedDone.State.Facilities.Count, Is.EqualTo(3));
        }
    }
}
