using NUnit.Framework;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests
{
    public class FoodAndCommsSimulationTests
    {
        private static void Close(double actual, double expected) =>
            Assert.That(actual, Is.EqualTo(expected).Within(1e-7));

        private static CrewState GetCrew(MissionState state, string id)
        {
            foreach (var c in state.Crew)
                if (c.Id == id) return c;
            throw new AssertionException($"Missing crew '{id}'.");
        }

        private static TaskState GetTask(MissionState state, string id)
        {
            foreach (var t in state.Tasks)
                if (t.Id == id) return t;
            throw new AssertionException($"Missing task '{id}'.");
        }

        [Test]
        public void MealPreparationCostsStaminaGrantsFullNightRecoveryAndResetsNextSol()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(2));
            Assert.That(sim.State.MealPreparedThisSol, Is.False);
            Close(sim.State.CurrentNightSleepFactor, 0.75);

            // Meal prep: 30s inside at -0.10/s stamina => 97.0 stamina remaining.
            Assert.That(sim.AssignTask("crew-1", "meal-prep").Success, Is.True);
            sim.Tick(30);
            Close(GetCrew(sim.State, "crew-1").Stamina, 97.0);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Idle));
            Assert.That(GetTask(sim.State, "meal-prep").IsCompleted, Is.True);
            Assert.That(sim.State.MealPreparedThisSol, Is.True);

            // Drain crew-2 stamina to 20 by resting? Wait: work crew-2 on panel-maintenance for 60s -> -21 stamina (79).
            Assert.That(sim.AssignTask("crew-2", "panel-maintenance").Success, Is.True);
            sim.Tick(60);
            Close(GetCrew(sim.State, "crew-2").Stamina, 79.0);
            Assert.That(sim.ReturnToShelter("crew-2").Success, Is.True);
            sim.Tick(10); // returning 10s at -0.10/s => 78.0 stamina
            Close(GetCrew(sim.State, "crew-2").Stamina, 78.0);

            // Advance to night (t = 480). Rations drop once (2 -> 1), sleep factor becomes 1.0.
            sim.Tick(380);
            Assert.That(sim.State.IsNight, Is.True);
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));
            Close(sim.State.CurrentNightSleepFactor, 1.0);

            // Sleep 50s at full 1.0x recovery (+0.40/s => +20 => 98.0 stamina).
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(50);
            Close(GetCrew(sim.State, "crew-2").Stamina, 98.0);

            // Finish Sol 1 night -> Sol 2 resets meal prep state and unlocks Sol 2 meal task.
            sim.Tick(190);
            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));
            Assert.That(sim.State.MealPreparedThisSol, Is.False);
            Assert.That(GetTask(sim.State, "meal-prep").IsCompleted, Is.False);
            Close(sim.State.CurrentNightSleepFactor, 0.75);
        }

        [Test]
        public void UnpreppedSleepFactorIsZeroPointSevenFiveAndCannotBeFarmedByReenteringSleep()
        {
            var crew = new[] { new CrewState("crew-1", "Ada", initialStamina: 10) };
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            // Skip meal prep during day -> reach night t = 480.
            sim.Tick(480);
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));
            Assert.That(sim.State.MealPreparedThisSol, Is.False);
            Close(sim.State.CurrentNightSleepFactor, 0.75);

            // Partial sleep 20s: +0.40 * 0.75 * 20 = +6.0 stamina => 16.0.
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(20);
            Close(GetCrew(sim.State, "crew-1").Stamina, 16.0);

            // Cancel sleep and even prepare meal after sleeping has started -> ration must NOT drop again
            // and sleep factor for tonight stays locked at 0.75!
            Assert.That(sim.CancelSleep().Success, Is.True);
            Assert.That(sim.AssignTask("crew-1", "meal-prep").Success, Is.True);
            sim.Tick(30); // costs -3.0 => 13.0 stamina
            Close(GetCrew(sim.State, "crew-1").Stamina, 13.0);
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));
            Close(sim.State.CurrentNightSleepFactor, 0.75);

            // Re-enter sleep for 40s: still 0.75x (+12.0 => 25.0) and ration stays 1!
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(40);
            Close(GetCrew(sim.State, "crew-1").Stamina, 25.0);
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void EarthAdviceArrivesAfter45SimSecondsBlocksSpamAndPausesWhenPaused()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var stepper = new SimulationStepper(sim);

            // Advance to 30s before first event (t = 330; rad-1 starts at t = 360).
            sim.Tick(330);
            Assert.That(sim.State.ShouldSuggestEarthAdvice, Is.True);
            Assert.That(sim.RequestEarthAdvice().Success, Is.True);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Pending));

            // Anti-spam: second request rejected while pending.
            var spam1 = sim.RequestEarthAdvice();
            Assert.That(spam1.Success, Is.False);
            Assert.That(spam1.Reason, Does.Contain("already in transit"));

            // While user paused, real time passes cleanly without advancing the 45s relay.
            stepper.SetUserPaused(true);
            stepper.AdvanceFrame(20.0);
            Close(sim.State.ElapsedSeconds, 330);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Pending));
            Close(sim.State.EarthAdviceRemainingSeconds, 45);

            // Unpause and advance 44s: still in transit.
            stepper.SetUserPaused(false);
            for (int i = 0; i < 440; i++) stepper.AdvanceFrame(0.1);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Pending));
            Assert.That(sim.State.EarthAdviceMessage, Is.Empty);

            // Advance remaining 1s -> exact 45s mark (t = 375, during active rad-1 [360, 420]).
            stepper.AdvanceFrame(1.0);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Delivered));
            Assert.That(sim.State.EarthAdviceMessage, Does.Contain("orbital position"));
            Assert.That(sim.State.EarthAdviceMessage, Does.Contain("45s"));
            Assert.That(sim.State.EarthAdviceMessage, Does.Contain("Solar Particle Event #1"));

            // Anti-spam after delivery: second request rejected.
            var spam2 = sim.RequestEarthAdvice();
            Assert.That(spam2.Success, Is.False);
            Assert.That(spam2.Reason, Does.Contain("once per mission"));
        }

        [Test]
        public void EarthAdviceAdvancesDuringSleepAndTimestampsPastEventsAndResetsOnRestart()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            // Request at t = 400 (right before rad-1 ends at t = 420) -> reply arrives at t = 445.
            sim.Tick(400);
            Assert.That(sim.RequestEarthAdvice().Success, Is.True);
            sim.Tick(45);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Delivered));
            Assert.That(sim.State.EarthAdviceMessage, Does.Contain("POST-EVENT NOTE"));
            Assert.That(sim.State.EarthAdviceMessage, Does.Contain("Sol 1 06:00-Sol 1 07:00"));

            // Restart resets rations, meal prep state, and comms availability.
            sim.Restart();
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(2));
            Assert.That(sim.State.MealPreparedThisSol, Is.False);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.NotRequested));
            Assert.That(sim.State.EarthAdviceMessage, Is.Empty);

            // Verify comms timer also advances properly during 8x sleep.
            sim.Tick(480); // Nightfall
            Assert.That(sim.BeginSleep().Success, Is.True);
            Assert.That(sim.RequestEarthAdvice().Success, Is.True);
            var stepper = new SimulationStepper(sim);
            // 6 real seconds at 8x sleep = 48 sim seconds (> 45s).
            for (int i = 0; i < 60; i++) stepper.AdvanceFrame(0.1);
            Assert.That(sim.State.EarthAdviceStatus, Is.EqualTo(EarthAdviceStatus.Delivered));
        }
    }
}
