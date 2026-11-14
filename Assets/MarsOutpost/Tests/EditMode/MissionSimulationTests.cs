using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class MissionSimulationTests
    {
        private static MissionSimulation Create(MissionConfig config = null, MissionScenario scenario = null) =>
            new MissionSimulation(config ?? new MissionConfig(), scenario ?? new MissionScenario());

        [Test]
        public void CalendarChangesAtConfiguredDayAndSolBoundaries()
        {
            var sim = Create(new MissionConfig(10, 6));
            Assert.That(sim.State.SolNumber, Is.EqualTo(1));
            Assert.That(sim.State.IsNight, Is.False);
            sim.Tick(6);
            Assert.That(sim.State.IsNight, Is.True);
            sim.Tick(4);
            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.SecondsIntoSol, Is.EqualTo(0));
            Assert.That(sim.State.IsNight, Is.False);
        }

        [Test]
        public void ZeroAndInvalidDeltasDoNotChangeTime()
        {
            var sim = Create();
            sim.Tick(10);
            sim.Tick(0);
            foreach (var value in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue })
            {
                var before = sim.State;
                Assert.Throws<ArgumentOutOfRangeException>(() => sim.Tick(value));
                Assert.That(sim.State, Is.SameAs(before));
                Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(10));
            }
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            foreach (var value in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => new MissionConfig(value, 0));
            foreach (var value in new[] { -1d, 11d, double.NaN, double.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => new MissionConfig(10, value));
        }

        [Test]
        public void RestartRestoresInitialCalendarAndCopiedScenario()
        {
            var crew = new List<CrewState> { new CrewState("a", "Ada") };
            var scenario = new MissionScenario(crew);
            var sim = Create(scenario: scenario);
            crew.Clear();
            Assert.Throws<NotSupportedException>(() => ((IList<CrewState>)scenario.Crew).Clear());
            sim.Tick(800);
            sim.Restart();
            Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(0));
            Assert.That(sim.State.SolNumber, Is.EqualTo(1));
            Assert.That(sim.State.Crew.Count, Is.EqualTo(1));
            Assert.That(sim.State.Crew[0].Name, Is.EqualTo("Ada"));
            Assert.Throws<NotSupportedException>(() => ((IList<CrewState>)sim.State.Crew).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<TaskState>)sim.State.Tasks).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<EventState>)sim.State.Events).Clear());
        }

        [Test]
        public void LargeAndSmallStepsProduceSameCalendar()
        {
            var large = Create();
            var small = Create();
            large.Tick(1500);
            for (int i = 0; i < 6000; i++) small.Tick(0.25);
            Assert.That(small.State.ElapsedSeconds, Is.EqualTo(large.State.ElapsedSeconds));
            Assert.That(small.State.SolNumber, Is.EqualTo(large.State.SolNumber));
            Assert.That(small.State.SecondsIntoSol, Is.EqualTo(large.State.SecondsIntoSol));
            Assert.That(small.State.IsNight, Is.EqualTo(large.State.IsNight));
        }

        [Test]
        public void FractionalStepsReachSolBoundary()
        {
            var sim = Create();
            for (int i = 0; i < 7200; i++) sim.Tick(0.1);
            Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(720));
            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.SecondsIntoSol, Is.EqualTo(0));
            Assert.That(sim.State.IsNight, Is.False);
        }

        [Test]
        public void DefaultCrewAndUnsupportedCommandsAreExplicit()
        {
            var sim = Create();
            Assert.That(sim.State.Crew.Count, Is.EqualTo(3));
            var result = sim.AssignTask("crew-1", "task");
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reason, Is.Not.Empty);
            Assert.That(sim.BeginSleep().Success, Is.False);
            Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(0));
        }
    }
}
