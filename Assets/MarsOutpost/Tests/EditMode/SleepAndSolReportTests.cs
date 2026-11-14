using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class SleepAndSolReportTests
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
        public void SleepRejectedDuringDayAndWhenAnyCrewOutsideOrReturning()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());

            // Rejected during day.
            var dayResult = sim.BeginSleep();
            Assert.That(dayResult.Success, Is.False);
            Assert.That(dayResult.Reason, Is.Not.Empty);

            // Send crew-1 outside, advance to night (t=480).
            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            sim.Tick(480);
            Assert.That(sim.State.IsNight, Is.True);

            // Start another outside task so crew-1 stays outside? Wait: panel-maintenance completed at t=60,
            // leaving crew-1 outside! Let's verify crew-1 is outside and BeginSleep is rejected.
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Outside));
            Assert.That(sim.BeginSleep().Success, Is.False);

            // While returning, BeginSleep is still rejected until arrival.
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(5);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Returning));
            Assert.That(sim.BeginSleep().Success, Is.False);

            sim.Tick(5);
            // Also start research with crew-2; BeginSleep stops indoor work cleanly.
            Assert.That(sim.AssignTask("crew-2", "research").Success, Is.True);
            Assert.That(sim.BeginSleep().Success, Is.True);
            Assert.That(sim.State.IsSleeping, Is.True);
            Close(sim.State.RecommendedTimeScale, 8.0);
            Assert.That(GetCrew(sim.State, "crew-2").Status, Is.EqualTo(CrewStatus.Sleeping));
            Assert.That(GetCrew(sim.State, "crew-2").ActiveTaskId, Is.Null);
        }

        [Test]
        public void NightRadiationProcessedDuringSleepAndSolBoundaryProducesSingleReport()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            sim.Tick(480);
            double riskBeforeNight = GetCrew(sim.State, "crew-1").RiskScore;

            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(240); // t=480 -> 720 (Sol 1 end)

            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.IsSleeping, Is.False);
            Close(sim.State.RecommendedTimeScale, 1.0);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Idle));

            // rad-2 [660, 720) must have executed during sleep.
            Assert.That(GetEvent(sim.State, "rad-2").Phase, Is.EqualTo(EventPhase.Completed));
            Close(GetCrew(sim.State, "crew-1").RiskScore - riskBeforeNight, 60 * 0.15);

            // Exactly one Sol 1 report generated.
            Assert.That(sim.State.SolReports.Count, Is.EqualTo(1));
            Assert.That(sim.State.SolReportPauseRequested, Is.True);
            Assert.That(sim.State.PendingSolReport, Is.Not.Null);
            Assert.That(sim.State.PendingSolReport.SolNumber, Is.EqualTo(1));
            Assert.That(sim.State.PendingSolReport.EventsExperienced, Does.Contain("Solar Particle Event #2"));
        }

        [Test]
        public void CancelSleepPreservesPartialRecoveryAndNoFreeStaminaAtSolChange()
        {
            var crew = new[] { new CrewState("crew-1", "Ada", initialStamina: 20) };
            var simSleep = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));
            var simAwake = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            simSleep.Tick(480);
            simAwake.Tick(480);

            // Partial sleep for 50s -> 0.75x unprepped recovery (+15 stamina -> 35).
            Assert.That(simSleep.BeginSleep().Success, Is.True);
            simSleep.Tick(50);
            Close(GetCrew(simSleep.State, "crew-1").Stamina, 35);
            Assert.That(simSleep.CancelSleep().Success, Is.True);
            Assert.That(simSleep.State.IsSleeping, Is.False);
            Assert.That(GetCrew(simSleep.State, "crew-1").Status, Is.EqualTo(CrewStatus.Idle));

            // Finish night without sleeping -> no further stamina gain.
            simSleep.Tick(190);
            Close(GetCrew(simSleep.State, "crew-1").Stamina, 35);

            // Awake simulation crosses Sol 1 -> 2 without sleeping: stamina stays 20 (no free refill!).
            simAwake.Tick(240);
            Assert.That(simAwake.State.SolNumber, Is.EqualTo(2));
            Close(GetCrew(simAwake.State, "crew-1").Stamina, 20);
        }

        [Test]
        public void CriticalOxygenWakesCrewOnceAndReportAcknowledgePreservesAlertPause()
        {
            // Oxygen starts at 37.4 -> after 480s day at 0.03/s, Oxygen is 23.0 at night start;
            // crosses 20.0 exactly 100s into sleep (t=580).
            var resCfg = new ResourceConfig(initialBattery: 0, initialWater: 80, initialOxygen: 37.4,
                solarRate: 0, oxygenProductionRate: 0, recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0.01);
            var crew = new[]
            {
                new CrewState("crew-1", "Ada", initialStamina: 10),
                new CrewState("crew-2", "Bob", initialStamina: 10),
                new CrewState("crew-3", "Cal", initialStamina: 10)
            };
            var sim = new MissionSimulation(
                new MissionConfig(720, 480, resCfg, oxygenOutageFailureSeconds: 200),
                new MissionScenario(crew, events: Array.Empty<EventState>()));

            sim.Tick(480);
            Assert.That(sim.BeginSleep().Success, Is.True);

            // Tick past threshold (crosses 20 at t=580 -> 100s of sleep, then 140s awake to t=720).
            sim.Tick(240);

            Assert.That(sim.State.IsSleeping, Is.False);
            Assert.That(sim.State.LowOxygenAlertCount, Is.EqualTo(1));
            Assert.That(sim.State.CriticalAlertPauseRequested, Is.True);
            Assert.That(sim.State.SolReportPauseRequested, Is.True);
            // Crew slept only 100s at 0.75x (+30 stamina from 10 -> 40), not full 240s!
            Close(GetCrew(sim.State, "crew-1").Stamina, 40);

            // Dismissing the Sol report must NOT clear the critical oxygen alert pause.
            Assert.That(sim.AcknowledgeSolReport().Success, Is.True);
            Assert.That(sim.State.SolReportPauseRequested, Is.False);
            Assert.That(sim.State.CriticalAlertPauseRequested, Is.True);
            Assert.That(sim.State.PauseRequested, Is.True);

            // Dismissing the critical alert clears the remaining pause request.
            Assert.That(sim.AcknowledgeCriticalAlert().Success, Is.True);
            Assert.That(sim.State.PauseRequested, Is.False);
        }

        [Test]
        public void NormalSpeedStepsAndEightXSleepStepsProduceIdenticalResult()
        {
            var crew = new[]
            {
                new CrewState("crew-1", "Ada", initialStamina: 20),
                new CrewState("crew-2", "Bob", initialStamina: 30),
                new CrewState("crew-3", "Cal", initialStamina: 40)
            };
            var normal = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));
            var fast = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            normal.Tick(480);
            fast.Tick(480);

            Assert.That(normal.BeginSleep().Success, Is.True);
            Assert.That(fast.BeginSleep().Success, Is.True);

            // 240 sim seconds: 960 * 0.25s (1x) vs 120 * 2.0s (8x runner step).
            for (int i = 0; i < 960; i++) normal.Tick(0.25);
            for (int i = 0; i < 120; i++) fast.Tick(0.25 * 8.0);

            Close(normal.State.ElapsedSeconds, fast.State.ElapsedSeconds);
            Close(normal.State.Resources.Battery, fast.State.Resources.Battery);
            Close(normal.State.Resources.Water, fast.State.Resources.Water);
            Close(normal.State.Resources.Oxygen, fast.State.Resources.Oxygen);
            Close(GetCrew(normal.State, "crew-1").Stamina, GetCrew(fast.State, "crew-1").Stamina);
            Close(GetCrew(normal.State, "crew-1").RiskScore, GetCrew(fast.State, "crew-1").RiskScore);
            Assert.That(normal.State.IsSleeping, Is.False);
            Assert.That(fast.State.IsSleeping, Is.False);
            Assert.That(normal.State.SolReports.Count, Is.EqualTo(1));
            Assert.That(fast.State.SolReports.Count, Is.EqualTo(1));
        }

        [Test]
        public void SolTwoEndProducesReportAndOutcomeAndRestartClearsSleepAndReports()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
            sim.Tick(200); // > 120 research progress
            Assert.That(sim.Rest("crew-1").Success, Is.True);

            sim.Tick(280); // t=480
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(240); // t=720 (Sol 1)
            Assert.That(sim.AcknowledgeSolReport().Success, Is.True);

            sim.Tick(720); // t=1440 (Sol 2)
            Assert.That(sim.State.SolReports.Count, Is.EqualTo(2));
            Assert.That(sim.State.PendingSolReport.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.Succeeded));

            sim.Restart();
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(sim.State.IsSleeping, Is.False);
            Assert.That(sim.State.SolReports.Count, Is.EqualTo(0));
            Assert.That(sim.State.PendingSolReport, Is.Null);
            Assert.That(sim.State.PauseRequested, Is.False);
            Assert.That(sim.State.LowOxygenAlertCount, Is.EqualTo(0));
        }
    }
}
