using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsSurfaceNavTests
    {
        private static MissionSimulation CreateOpsSim(double maxStamina = 100)
        {
            var cfg = new MissionConfig(
                solSeconds: 720,
                daySeconds: 480,
                resources: new ResourceConfig(initialBattery: 50, waterExtractionRate: 0.012, waterExtractorEnergyRate: 0.015),
                returnToShelterSeconds: 10,
                maxStamina: maxStamina,
                researchGoal: 0,
                targetSols: 0,
                operationsSeed: 424242,
                enableOperationsRandomEvents: false,
                mode: MissionMode.Operations);
            return new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
        }

        [Test]
        public void DeterministicFourNeighborPathAndNoPathRejectionAreAtomic()
        {
            var sim = CreateOpsSim();
            var map = sim.State.Map;

            // Adjacent tie-break check for a non-passable cell or standard cell with allowTargetCell=false:
            // Pick (8, 3) when starting at (8, 5): North of (8,3) is (8,4) directly along path -> length 2.
            Assert.That(GridNav.TryFindBestWorkCell(map, ParcelMap.AirlockCorridorCoord, new GridCoord(8, 3), false, out var workCell, out var path), Is.True);
            Assert.That(workCell, Is.EqualTo(new GridCoord(8, 4)));
            Assert.That(path.Count, Is.EqualTo(2));
            for (int i = 1; i < path.Count; i++)
                Assert.That(path[i - 1].ManhattanDistance(path[i]), Is.EqualTo(1));

            // Make (8, 3) and all 4 cardinal neighbors around a test pocket unpassable on a fresh copy via command test:
            // First assign valid work to crew-1, then verify rejected assign on unreachable cell preserves crew-1's job.
            Assert.That(sim.AssignSurfaceWork("crew-1", new GridCoord(8, 3), workRequiredSeconds: 12).Success, Is.True);
            string prevJobId = sim.State.Crew[0].ActiveSurfaceJobId;

            // Block target (12, 12) and all its 4 neighbors directly via internal test helper? Better: block all 4 neighbors of Airlock (8,5) on crew-2?
            // Instead test GridNav directly on a blocked pocket + rejected AssignSurfaceWork out of bounds/busy.
            var testMap = ParcelMap.Generate(424242);
            var blockedTarget = new GridCoord(2, 2);
            foreach (var n in testMap.Config.GetCardinalNeighbors(blockedTarget))
                testMap.GetCell(n).IsPassable = false;
            testMap.GetCell(blockedTarget).IsPassable = false;

            Assert.That(GridNav.TryFindBestWorkCell(testMap, ParcelMap.AirlockCorridorCoord, blockedTarget, false, out _, out _), Is.False);
            Assert.That(sim.AssignSurfaceWork("crew-1", new GridCoord(99, 99), 10, replace: true).Success, Is.False);
            Assert.That(sim.State.Crew[0].ActiveSurfaceJobId, Is.EqualTo(prevJobId));
        }

        [Test]
        public void LargeVsSmallTickEquivalenceTravelHasZeroWorkProgressAndSingleReturnOwner()
        {
            var simBig = CreateOpsSim();
            var simSmall = CreateOpsSim();

            // Target (12, 5): work cell (11, 5). AirlockOuter = (2.45, -0.35), path centers: (9,5)->(6,-2), (10,5)->(10,-2), (11,5)->(14,-2).
            var target = new GridCoord(12, 5);
            Assert.That(simBig.AssignSurfaceWork("crew-1", target, workRequiredSeconds: 10.0).Success, Is.True);
            Assert.That(simSmall.AssignSurfaceWork("crew-1", target, workRequiredSeconds: 10.0).Success, Is.True);

            var initialJob = simBig.State.SurfaceJobs[0];
            double travelDuration = initialJob.LegTotalDistance / initialJob.TravelSpeed;
            Assert.That(travelDuration, Is.GreaterThan(4.0));

            // Halfway through Travel: work progress MUST still be 0
            double midTravel = travelDuration * 0.5;
            simBig.Tick(midTravel);
            for (int i = 0; i < 50; i++)
                simSmall.Tick(midTravel / 50.0);

            Assert.That(simBig.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Travel));
            Assert.That(simBig.State.SurfaceJobs[0].WorkProgressSeconds, Is.EqualTo(0));
            Assert.That(simBig.State.Crew[0].SurfacePosX, Is.EqualTo(simSmall.State.Crew[0].SurfacePosX).Within(1e-6));
            Assert.That(simBig.State.Crew[0].SurfacePosZ, Is.EqualTo(simSmall.State.Crew[0].SurfacePosZ).Within(1e-6));
            Assert.That(simBig.State.Crew[0].Stamina, Is.EqualTo(simSmall.State.Crew[0].Stamina).Within(1e-6));

            // Advance just past arrival + halfway through work (5s of 10s work)
            double remTravelPlus5 = (travelDuration - midTravel) + 5.0;
            simBig.Tick(remTravelPlus5);
            for (int i = 0; i < 100; i++)
                simSmall.Tick(remTravelPlus5 / 100.0);

            Assert.That(simBig.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Work));
            Assert.That(simBig.State.SurfaceJobs[0].WorkProgressSeconds, Is.EqualTo(5.0).Within(1e-6));
            Assert.That(simSmall.State.SurfaceJobs[0].WorkProgressSeconds, Is.EqualTo(5.0).Within(1e-6));
            Assert.That(simBig.State.Crew[0].Stamina, Is.EqualTo(simSmall.State.Crew[0].Stamina).Within(1e-6));

            // Finish work -> Idle outside at work location
            simBig.Tick(5.0);
            Assert.That(simBig.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Completed));
            Assert.That(simBig.State.Crew[0].Status, Is.EqualTo(CrewStatus.Idle));
            Assert.That(simBig.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));
            Assert.That(simBig.State.Crew[0].HasSurfacePosition, Is.True);

            // Recall via ReturnToShelter -> uses route + airlock pass time, NOT double-counted legacy 10s
            Assert.That(simBig.ReturnToShelter("crew-1").Success, Is.True);
            double expectedReturnSec = simBig.State.Crew[0].ReturnRemainingSeconds;
            Assert.That(expectedReturnSec, Is.EqualTo(travelDuration + GridNav.AirlockPassSeconds).Within(0.05));

            simBig.Tick(2.0);
            Assert.That(simBig.State.Crew[0].Status, Is.EqualTo(CrewStatus.Returning));
            Assert.That(simBig.State.Crew[0].ReturnRemainingSeconds, Is.EqualTo(expectedReturnSec - 2.0).Within(1e-6));

            simBig.Tick(expectedReturnSec - 2.0 + 0.01);
            Assert.That(simBig.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(simBig.State.Crew[0].Status, Is.EqualTo(CrewStatus.Sheltered));
            Assert.That(simBig.State.Crew[0].HasSurfacePosition, Is.False);
        }

        [Test]
        public void V2LegacyReturnToShelterPreservesTenSecondRegression()
        {
            var sim = CreateOpsSim();
            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            sim.Tick(2.0);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            Assert.That(sim.State.Crew[0].HasSurfacePosition, Is.False);
            Assert.That(sim.State.Crew[0].ReturnRemainingSeconds, Is.EqualTo(10.0).Within(1e-9));
            sim.Tick(9.5);
            Assert.That(sim.State.Crew[0].Status, Is.EqualTo(CrewStatus.Returning));
            sim.Tick(0.6);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
        }

        [Test]
        public void RejectionsPreserveSurfaceJobAndExhaustionReleasesJobKeepingProgressOutside()
        {
            var sim = CreateOpsSim(maxStamina: 2.5);
            Assert.That(sim.AssignSurfaceWork("crew-1", new GridCoord(8, 3), workRequiredSeconds: 20.0, workStaminaRate: -0.5).Success, Is.True);
            string jobId = sim.State.Crew[0].ActiveSurfaceJobId;

            // Advance into Work phase cleanly (~2.2s travel to (8,4))
            sim.Tick(3.0);
            Assert.That(sim.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Work));
            double progressBeforeReject = sim.State.SurfaceJobs[0].WorkProgressSeconds;

            // Rejected commands must NOT disturb current SurfaceJob:
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.False);
            Assert.That(sim.ReplaceTask("crew-1", "research").Success, Is.False);
            Assert.That(sim.Rest("crew-1").Success, Is.False);
            Assert.That(sim.AssignSurfaceWork("crew-1", new GridCoord(8, 2), 10, replace: false).Success, Is.False);
            Assert.That(sim.State.Crew[0].ActiveSurfaceJobId, Is.EqualTo(jobId));
            Assert.That(sim.State.SurfaceJobs[0].AssignedCrewId, Is.EqualTo("crew-1"));
            Assert.That(sim.State.SurfaceJobs[0].WorkProgressSeconds, Is.EqualTo(progressBeforeReject));

            // Advance to nighttime -> BeginSleep rejects while crew-1 is outside (no teleport!)
            var simNight = CreateOpsSim();
            Assert.That(simNight.AssignSurfaceWork("crew-1", new GridCoord(8, 3), workRequiredSeconds: 600.0, workStaminaRate: -0.05).Success, Is.True);
            simNight.Tick(490);
            Assert.That(simNight.State.IsNight, Is.True);
            Assert.That(simNight.BeginSleep().Success, Is.False);
            Assert.That(simNight.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));

            // Exhaustion releases job, preserves progress, and leaves astronaut outside
            sim.Tick(10.0);
            Assert.That(sim.State.Crew[0].Status, Is.EqualTo(CrewStatus.Exhausted));
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));
            Assert.That(sim.State.Crew[0].ActiveSurfaceJobId, Is.Null);
            Assert.That(sim.State.SurfaceJobs[0].AssignedCrewId, Is.Null);
            Assert.That(sim.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Work));
            Assert.That(sim.State.SurfaceJobs[0].WorkProgressSeconds, Is.GreaterThan(progressBeforeReject));

            // CancelTask cleanly releases another crew member's SurfaceJob
            Assert.That(simNight.CancelTask("crew-1").Success, Is.True);
            Assert.That(simNight.State.Crew[0].ActiveSurfaceJobId, Is.Null);
            Assert.That(simNight.State.SurfaceJobs[0].AssignedCrewId, Is.Null);
        }

        [Test]
        public void MidTravelAndMidReturnSaveLoadCloneRestartPreserveExactRouteAndProgress()
        {
            var sim = CreateOpsSim();
            Assert.That(sim.AssignSurfaceWork("crew-1", new GridCoord(12, 5), workRequiredSeconds: 12.0).Success, Is.True);
            var snapBefore = sim.State;
            sim.Tick(3.5); // Mid-travel

            // Previous immutable snapshot must stay at t=0
            Assert.That(snapBefore.SurfaceJobs[0].LegDistanceTraveled, Is.EqualTo(0));

            string saveTravelJson = sim.SaveToJson();
            Assert.That(saveTravelJson, Does.Contain($"\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision}"));
            Assert.That(MissionSimulation.TryLoadFromJson(saveTravelJson, out var loadedTravel, out var err1), Is.True, err1);

            Assert.That(loadedTravel.State.Crew[0].SurfacePosX, Is.EqualTo(sim.State.Crew[0].SurfacePosX).Within(1e-12));
            Assert.That(loadedTravel.State.Crew[0].SurfacePosZ, Is.EqualTo(sim.State.Crew[0].SurfacePosZ).Within(1e-12));
            Assert.That(loadedTravel.State.SurfaceJobs[0].LegDistanceTraveled, Is.EqualTo(sim.State.SurfaceJobs[0].LegDistanceTraveled).Within(1e-12));

            // Advance both 12s -> both reach Work phase identically
            sim.Tick(12.0);
            loadedTravel.Tick(12.0);
            Assert.That(loadedTravel.State.SurfaceJobs[0].Phase, Is.EqualTo(sim.State.SurfaceJobs[0].Phase));
            Assert.That(loadedTravel.State.SurfaceJobs[0].WorkProgressSeconds, Is.EqualTo(sim.State.SurfaceJobs[0].WorkProgressSeconds).Within(1e-9));

            // Mid-return save/load
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(2.5);
            string saveReturnJson = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(saveReturnJson, out var loadedReturn, out var err2), Is.True, err2);
            Assert.That(loadedReturn.State.Crew[0].Status, Is.EqualTo(CrewStatus.Returning));
            Assert.That(loadedReturn.State.Crew[0].ReturnRemainingSeconds, Is.EqualTo(sim.State.Crew[0].ReturnRemainingSeconds).Within(1e-12));
            Assert.That(loadedReturn.State.Crew[0].SurfacePosX, Is.EqualTo(sim.State.Crew[0].SurfacePosX).Within(1e-12));

            // Revision 1 -> 2 migration produces empty surfaceJobs cleanly
            var freshSim = CreateOpsSim();
            string freshRev1 = freshSim.SaveToJson().Replace($"\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision}", "\"expansionRevision\":1");
            Assert.That(MissionSimulation.TryLoadFromJson(freshRev1, out var migratedRev1, out var err3), Is.True, err3);
            Assert.That(migratedRev1.State.SurfaceJobs.Count, Is.EqualTo(0));

            // Restart resets SurfaceJobs and crew surface position
            sim.Restart();
            Assert.That(sim.State.SurfaceJobs.Count, Is.EqualTo(0));
            Assert.That(sim.State.Crew[0].HasSurfacePosition, Is.False);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
        }
    }
}
