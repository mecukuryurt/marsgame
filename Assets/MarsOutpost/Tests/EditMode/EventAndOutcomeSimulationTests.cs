using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class EventAndOutcomeSimulationTests
    {
        private static void Close(double actual, double expected) =>
            Assert.That(actual, Is.EqualTo(expected).Within(1e-7));

        private static CrewState GetCrew(MissionState state, string id)
        {
            foreach (var c in state.Crew)
                if (c.Id == id) return c;
            throw new AssertionException($"Missing crew '{id}'.");
        }

        private static EventState GetEvent(MissionState state, string id)
        {
            foreach (var ev in state.Events)
                if (ev.Id == id) return ev;
            throw new AssertionException($"Missing event '{id}'.");
        }

        [Test]
        public void FiveMinuteWarningAndSingleStartEndTransitionsWorkWithoutSkipping()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(GetEvent(sim.State, "rad-1").Phase, Is.EqualTo(EventPhase.Upcoming));

            sim.Tick(60);
            var rad1 = GetEvent(sim.State, "rad-1");
            Assert.That(rad1.Phase, Is.EqualTo(EventPhase.Warning));
            Assert.That(rad1.WarningTriggered, Is.True);
            Assert.That(rad1.StartTriggered, Is.False);
            Close(rad1.SecondsUntilStart(sim.State.ElapsedSeconds), 300);

            // A single large tick across [60, 420] must not skip the radiation storm.
            sim.Tick(360);
            rad1 = GetEvent(sim.State, "rad-1");
            Assert.That(rad1.Phase, Is.EqualTo(EventPhase.Completed));
            Assert.That(rad1.StartTriggered, Is.True);
            Assert.That(rad1.EndTriggered, Is.True);
            // Crew inside received 60s * 0.15/s = 9.0 risk score.
            Close(GetCrew(sim.State, "crew-1").RiskScore, 9.0);
        }

        [Test]
        public void LargeAndSmallTicksProduceSameEventRiskAndDustEffects()
        {
            var resCfg = new ResourceConfig(initialBattery: 10, batteryCapacity: 100);
            var large = new MissionSimulation(new MissionConfig(resources: resCfg), new MissionScenario());
            var small = new MissionSimulation(new MissionConfig(resources: resCfg), new MissionScenario());

            large.Tick(1000);
            for (int i = 0; i < 4000; i++)
                small.Tick(0.25);

            Close(large.State.Resources.Battery, small.State.Resources.Battery);
            Close(large.State.Resources.AtmosphericDustLoss, small.State.Resources.AtmosphericDustLoss);
            Close(GetCrew(large.State, "crew-1").RiskScore, GetCrew(small.State, "crew-1").RiskScore);
            Assert.That(GetEvent(large.State, "dust-1").Phase, Is.EqualTo(GetEvent(small.State, "dust-1").Phase));
        }

        [Test]
        public void TenSecondReturnContinuesOutsideRadiationExposureUntilArrival()
        {
            var crew = new[] { new CrewState("crew-1", "Ada", initialStamina: 100, initialLocation: CrewLocation.Outside) };
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            sim.Tick(360);
            Assert.That(GetEvent(sim.State, "rad-1").Phase, Is.EqualTo(EventPhase.Active));
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);

            // 10s returning outside (10 * 1.2 = 12.0), then 50s inside (50 * 0.15 = 7.5).
            sim.Tick(60);
            var c = GetCrew(sim.State, "crew-1");
            Assert.That(c.Location, Is.EqualTo(CrewLocation.Inside));
            Close(c.RiskScore, 19.5);
        }

        [Test]
        public void DustReducesSolarWithoutBeingClearedByPanelMaintenance()
        {
            var resCfg = new ResourceConfig(
                batteryCapacity: 100,
                initialBattery: 0,
                solarRate: 0.20,
                oxygenProductionRate: 0,
                recyclingRate: 0,
                waterUsePerCrew: 0,
                oxygenUsePerCrew: 0,
                initialPanelFouling: 0.25,
                panelMaintenanceReduction: 0.25);
            var events = new[] { new EventState("dust-test", "Dust", EventKind.Dust, 0, 100, 200, 0.50) };
            var sim = new MissionSimulation(new MissionConfig(720, 480, resCfg), new MissionScenario(events: events));

            // Complete panel maintenance before dust storm: PanelFouling drops 0.25 -> 0.
            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            sim.Tick(60);
            Close(sim.State.Resources.PanelFouling, 0);

            // Advance to dust start (t=100) and then 50s inside dust storm (t=150).
            sim.Tick(40);
            double batteryBeforeDust = sim.State.Resources.Battery;
            sim.Tick(50);

            Close(sim.State.Resources.AtmosphericDustLoss, 0.50);
            // Solar is 0.20 * (1 - 0) * (1 - 0.50) = 0.10/s -> +5.0 over 50s.
            Close(sim.State.Resources.Battery - batteryBeforeDust, 5.0);
        }

        [Test]
        public void OxygenOutageCounterResetsOnRecoveryAndFailsAtLimit()
        {
            // Oxygen starts at 0; first 15s has 0 solar/O2, then day starts at t=15 with enough O2 production.
            var resCfg = new ResourceConfig(
                batteryCapacity: 0,
                initialBattery: 0,
                initialWater: 50,
                initialOxygen: 0,
                solarRate: 0,
                oxygenProductionRate: 0.05,
                oxygenEnergyPerUnit: 0,
                oxygenWaterPerUnit: 0,
                recyclingRate: 0,
                waterUsePerCrew: 0,
                oxygenUsePerCrew: 0.01);
            // Verify outage counter fails at 20s and freezes.
            var simFail = new MissionSimulation(
                new MissionConfig(720, 480, resCfg, oxygenOutageFailureSeconds: 20),
                new MissionScenario(events: Array.Empty<EventState>()));
            // Make O2 production 0 so it stays depleted.
            var zeroProdCfg = new ResourceConfig(
                batteryCapacity: 0, initialBattery: 0, initialWater: 50, initialOxygen: 0,
                solarRate: 0, oxygenProductionRate: 0, recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0.01);
            var sim = new MissionSimulation(
                new MissionConfig(720, 480, zeroProdCfg, oxygenOutageFailureSeconds: 20),
                new MissionScenario(events: Array.Empty<EventState>()));

            sim.Tick(15);
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Close(sim.State.Resources.ContinuousOxygenOutageSeconds, 15);

            sim.Tick(20);
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.Failed));
            Assert.That(sim.State.FailureReason, Is.EqualTo(MissionFailureReason.OxygenDepleted));
            Close(sim.State.ElapsedSeconds, 20);
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.False);

            // Recovery test: O2 depleted for 15s (battery = 0 at night, day starts at phase 15..100).
            // Config: Sol=100, Day=85? No, Day=0..50 is day, 50..100 is night. Start at t=0 night if day=0?
            // Use custom event or battery=0 at day=0?Wait: let's start with initialOxygen=0.15 -> lasts 5s,
            // depleted for 15s, then wait: simplest recovery check is initialOxygen=0 with solar off for 15s?
            // Let's use day=100, battery=0, solar=0.05, initialOxygen=0, O2Prod=0.05 -> O2 demand 0.03 met immediately at t=0.
            // Or start with battery=0, initialOxygen=0.30 (10s O2) and zero solar? No, we need outage THEN recovery!
            // If we put a dust storm or just use battery=0? At t=0 it's day! If we set daySeconds=15? No, Day is [0, DaySeconds) and Night is [DaySeconds, SolSeconds).
            // Actually if we use Sol=100, Day=85 -> wait: Day is first!
            // With Day=50, Sol=100, initialBattery=0, solar=0.10, O2Prod=0.05, O2Energy=1, O2Demand=0.03:
            // Wait: during day (0..50) O2 accumulates! At night (50..100) O2 drains! Then at t=100 (Sol 2 day!) O2 production resumes!
            var recoveryCfg = new ResourceConfig(
                batteryCapacity: 0, initialBattery: 0, initialWater: 80, initialOxygen: 1.05,
                solarRate: 0.10, oxygenProductionRate: 0.05, oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: 0,
                recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0.01);
            // Day 0..50: O2 starts 1.05, net +0.02/s -> at t=50 O2 = 2.05. Wait: if initialO2=0, Day=10, Sol=30:
            // Day 0..10: +0.20 O2. Night 10..30 (20s): 0.20/0.03 = 6.6667s O2, then 13.3333s outage!
            // At t=30 (Sol 2 Day): solar restarts, O2 prod 0.05 > 0.03 -> outage counter MUST reset to 0!
            var cycleCfg = new ResourceConfig(
                batteryCapacity: 0, initialBattery: 0, initialWater: 80, initialOxygen: 0,
                solarRate: 0.10, oxygenProductionRate: 0.05, oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: 0,
                recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0.01);
            var simRecover = new MissionSimulation(
                new MissionConfig(solSeconds: 30, daySeconds: 10, resources: cycleCfg, targetSols: 3, oxygenOutageFailureSeconds: 20),
                new MissionScenario(events: Array.Empty<EventState>()));

            simRecover.Tick(29);
            Assert.That(simRecover.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(simRecover.State.Resources.ContinuousOxygenOutageSeconds, Is.GreaterThan(10));

            simRecover.Tick(2); // crosses t=30 into Sol 2 Day
            Assert.That(simRecover.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Close(simRecover.State.Resources.ContinuousOxygenOutageSeconds, 0);
        }

        [Test]
        public void FailurePrecedesSuccessAndGoalOutcomeWithRestartWorks()
        {
            // High radiation event guarantees failure even if research goal was already met.
            var events = new[] { new EventState("lethal-rad", "Extreme Solar Storm", EventKind.Radiation, 0, 100, 200, 1.0) };
            var resCfg = new ResourceConfig(solarRate: 0.20, researchEnergyRate: 0.01, researchProgressRate: 1.0);
            var simFailInside = new MissionSimulation(
                new MissionConfig(solSeconds: 100, daySeconds: 80, resources: resCfg, researchGoal: 50, targetSols: 2,
                    insideRadiationRiskRate: 1.0, maxCrewRiskScore: 50),
                new MissionScenario(events: events));
            simFailInside.AssignTask("crew-1", "research");
            simFailInside.Tick(200);
            Assert.That(simFailInside.State.Outcome, Is.EqualTo(MissionOutcome.Failed));
            Assert.That(simFailInside.State.FailureReason, Is.EqualTo(MissionFailureReason.CrewRiskExceeded));
            Close(simFailInside.State.ElapsedSeconds, 150);

            // Safe run hitting research goal succeeds at t=200 and freezes.
            var simWin = new MissionSimulation(
                new MissionConfig(solSeconds: 100, daySeconds: 80, resources: resCfg, researchGoal: 50, targetSols: 2,
                    insideRadiationRiskRate: 0.1, maxCrewRiskScore: 50),
                new MissionScenario(events: events));
            simWin.AssignTask("crew-1", "research");
            simWin.Tick(250);
            Assert.That(simWin.State.Outcome, Is.EqualTo(MissionOutcome.Succeeded));
            Assert.That(simWin.State.FailureReason, Is.EqualTo(MissionFailureReason.None));
            Close(simWin.State.ElapsedSeconds, 200);
            Assert.That(simWin.AssignTask("crew-2", "system-maintenance").Success, Is.False);

            // Restart restores events, risk, and outcome.
            simWin.Restart();
            Assert.That(simWin.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(simWin.State.ElapsedSeconds, Is.EqualTo(0));
            Close(GetCrew(simWin.State, "crew-1").RiskScore, 0);
            Assert.That(GetEvent(simWin.State, "lethal-rad").StartTriggered, Is.False);
            Assert.That(GetEvent(simWin.State, "lethal-rad").EndTriggered, Is.False);
        }

        [Test]
        public void DefaultScenarioEventsTraceToVerifiedNasaScienceEntries()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var rad1 = GetEvent(sim.State, "rad-1");
            var rad2 = GetEvent(sim.State, "rad-2");
            var dust1 = GetEvent(sim.State, "dust-1");

            Assert.That(rad1.SourceRecordId, Is.EqualTo("2024-05-09T13:59:00-SEP-001"));
            Assert.That(rad2.SourceRecordId, Is.EqualTo("2024-05-10T13:35:00-SEP-001"));
            Assert.That(dust1.SourceRecordId, Is.Not.Empty);
            Assert.That(NasaScienceData.Entries.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(NasaScienceData.EducationalDisclaimer, Does.Contain("NOT measured Mars surface"));
        }
    }
}
