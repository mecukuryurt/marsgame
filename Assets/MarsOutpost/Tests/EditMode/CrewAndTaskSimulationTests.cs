using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class CrewAndTaskSimulationTests
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
        public void CannotAssignTwoTasksToSameCrewMember()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);

            var second = sim.AssignTask("crew-1", "system-maintenance");
            Assert.That(second.Success, Is.False);
            Assert.That(second.Reason, Is.Not.Empty);
            Assert.That(GetCrew(sim.State, "crew-1").ActiveTaskId, Is.EqualTo("research"));
            Assert.That(GetTask(sim.State, "system-maintenance").AssignedCrewId, Is.Null);
        }

        [Test]
        public void CannotShareSingleTaskStationBetweenTwoCrewMembers()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);

            var second = sim.AssignTask("crew-2", "research");
            Assert.That(second.Success, Is.False);
            Assert.That(second.Reason, Is.Not.Empty);
            Assert.That(GetTask(sim.State, "research").AssignedCrewId, Is.EqualTo("crew-1"));
            Assert.That(GetCrew(sim.State, "crew-2").Status, Is.EqualTo(CrewStatus.Idle));
        }

        [Test]
        public void TaskStopsAtZeroStaminaAndExhaustedCrewMustRest()
        {
            var crew = new[] { new CrewState("crew-1", "Ada", initialStamina: 7.0) };
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            // -0.35 stamina/s -> 7.0 stamina lasts exactly 20s of a 60s task.
            sim.Tick(40);

            var c = GetCrew(sim.State, "crew-1");
            var t = GetTask(sim.State, "panel-maintenance");
            Close(c.Stamina, 0);
            Assert.That(c.Status, Is.EqualTo(CrewStatus.Exhausted));
            Assert.That(c.ActiveTaskId, Is.Null);
            Assert.That(t.AssignedCrewId, Is.Null);
            Assert.That(t.IsCompleted, Is.False);
            Close(t.ProgressSeconds, 20);

            // Cannot reassign work while exhausted.
            var blocked = sim.AssignTask("crew-1", "panel-maintenance");
            Assert.That(blocked.Success, Is.False);
            Assert.That(blocked.Reason, Is.Not.Empty);
        }

        [Test]
        public void ExhaustedCrewOutsideCanReturnToShelterEvenAtZeroStamina()
        {
            var crew = new[] { new CrewState("crew-1", "Ada", initialStamina: 1.0, initialLocation: CrewLocation.Outside) };
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario(crew));

            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            // Exhausted at ~2.857s.
            sim.Tick(5);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Exhausted));
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Outside));

            // Resting outside must be rejected.
            Assert.That(sim.Rest("crew-1").Success, Is.False);

            // Return to shelter works at 0 stamina and takes 10 seconds.
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(5);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Returning));
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Outside));

            sim.Tick(5);
            var c = GetCrew(sim.State, "crew-1");
            Assert.That(c.Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(c.Status, Is.EqualTo(CrewStatus.Exhausted));
            Close(c.Stamina, 0);

            // Now inside, crew can rest and recover.
            Assert.That(sim.Rest("crew-1").Success, Is.True);
            sim.Tick(50);
            Close(GetCrew(sim.State, "crew-1").Stamina, 10);
            Assert.That(sim.AssignTask("crew-1", "system-maintenance").Success, Is.True);
        }

        [Test]
        public void OneTimeMaintenanceRewardAppliedOnceAcrossCancelAndReassign()
        {
            var resCfg = new ResourceConfig(initialPanelFouling: 0.40, panelMaintenanceReduction: 0.25,
                initialSystemWear: 0.30, systemMaintenanceReduction: 0.20);
            var sim = new MissionSimulation(new MissionConfig(resources: resCfg), new MissionScenario());

            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            sim.Tick(30);
            Close(GetTask(sim.State, "panel-maintenance").ProgressSeconds, 30);
            Close(sim.State.Resources.PanelFouling, 0.40);

            // Cancel via ReturnToShelter preserves progress and does not grant reward early.
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(10);
            Close(GetTask(sim.State, "panel-maintenance").ProgressSeconds, 30);
            Close(sim.State.Resources.PanelFouling, 0.40);

            // Reassign another crew member for the remaining 30s.
            Assert.That(sim.AssignTask("crew-2", "panel-maintenance").Success, Is.True);
            sim.Tick(30);
            var task = GetTask(sim.State, "panel-maintenance");
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.RewardApplied, Is.True);
            Close(sim.State.Resources.PanelFouling, 0.15);

            // Reassigning a completed one-time task is rejected.
            Assert.That(sim.AssignTask("crew-2", "panel-maintenance").Success, Is.False);
            Close(sim.State.Resources.PanelFouling, 0.15);

            // System maintenance also applies once.
            Assert.That(sim.AssignTask("crew-3", "system-maintenance").Success, Is.True);
            sim.Tick(45);
            Assert.That(GetTask(sim.State, "system-maintenance").IsCompleted, Is.True);
            Close(sim.State.Resources.SystemWear, 0.10);
            Assert.That(sim.AssignTask("crew-3", "system-maintenance").Success, Is.False);
        }

        [Test]
        public void ResearchRequiresCrewAndEnergyAndScalesWithRemainingSolar()
        {
            // Solar = 0.06, O2 uses 0.05/s -> net 0.01/s surplus solar (half of 0.02 researchEnergyRate).
            // With initialBattery = 0.1 and batteryCapacity = 0.1 (already full after step 1 & 2):
            // 0.1 battery drains at -0.01/s over 10s? Wait: let's set initialBattery = 0, capacity = 0 for pure solar test,
            // then test mid-tick battery depletion in a second simulation.
            var solarOnlyCfg = new ResourceConfig(batteryCapacity: 0, initialBattery: 0, initialWater: 50, initialOxygen: 50,
                solarRate: 0.06, oxygenProductionRate: 0.05, oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: 0,
                recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0,
                researchEnergyRate: 0.02, researchProgressRate: 1.0);
            var sim = new MissionSimulation(new MissionConfig(720, 480, solarOnlyCfg), new MissionScenario());

            // 1. No crew assigned -> 0 progress even though ResearchEnabled is true.
            sim.Tick(10);
            Close(sim.State.Resources.ResearchProgress, 0);

            // 2. Crew assigned, ResearchEnabled = false -> 0 progress, 0 stamina loss.
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
            Assert.That(sim.SetResearchEnabled(false).Success, Is.True);
            sim.Tick(10);
            Close(sim.State.Resources.ResearchProgress, 0);
            Close(GetCrew(sim.State, "crew-1").Stamina, 100);

            // 3. ResearchEnabled = true -> runs at 0.5x proportional to remaining 0.01 solar.
            Assert.That(sim.SetResearchEnabled(true).Success, Is.True);
            sim.Tick(10);
            Close(sim.State.Resources.ResearchProgress, 5.0);
            Close(sim.State.Resources.LastResearchRateFactor, 0.5);
            Close(GetCrew(sim.State, "crew-1").Stamina, 100 - 0.15 * 0.5 * 10);
            Close(sim.State.Resources.Battery, 0);

            // 4. Mid-tick battery depletion transitions cleanly from 1.0x to 0.5x.
            var transitionCfg = new ResourceConfig(batteryCapacity: 100, initialBattery: 0.05, initialWater: 50, initialOxygen: 50,
                solarRate: 0.06, oxygenProductionRate: 0.05, oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: 0,
                recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0,
                researchEnergyRate: 0.02, researchProgressRate: 1.0);
            var sim2 = new MissionSimulation(new MissionConfig(720, 480, transitionCfg), new MissionScenario());
            Assert.That(sim2.AssignTask("crew-1", "research").Success, Is.True);
            sim2.Tick(10);
            // First 5s: battery drains 0.05 at -0.01/s, research runs 1.0x (= 5.0).
            // Next 5s: battery = 0, research runs 0.5x (= 2.5). Total = 7.5.
            Close(sim2.State.Resources.Battery, 0);
            Close(sim2.State.Resources.ResearchProgress, 7.5);
            Close(sim2.State.Resources.LastResearchRateFactor, 0.5);
        }

        [Test]
        public void IndoorTasksRequireInsideLocationAndRestartResetsAll()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            Assert.That(sim.AssignTask("crew-1", "panel-maintenance").Success, Is.True);
            sim.Tick(60);

            // Finished outside task leaves crew outside; indoor task must be rejected until returned.
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Outside));
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.False);

            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(10);
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);

            sim.Tick(20);
            Assert.That(sim.State.Resources.ResearchProgress, Is.GreaterThan(0));

            sim.Restart();
            Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(0));
            Close(sim.State.Resources.ResearchProgress, 0);
            Close(GetCrew(sim.State, "crew-1").Stamina, 100);
            Assert.That(GetCrew(sim.State, "crew-1").Status, Is.EqualTo(CrewStatus.Idle));
            Assert.That(GetCrew(sim.State, "crew-1").Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(GetTask(sim.State, "panel-maintenance").IsCompleted, Is.False);
            Assert.That(GetTask(sim.State, "panel-maintenance").ProgressSeconds, Is.EqualTo(0));
        }

        [Test]
        public void LargeAndSmallTicksProduceSameCrewStaminaAndTaskProgress()
        {
            var resCfg = new ResourceConfig(initialBattery: 2, initialWater: 20, initialOxygen: 20,
                solarRate: 0.08, oxygenProductionRate: 0.05, oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: 0.2,
                recyclingRate: 0.02, recyclingEnergyPerUnit: 0.5, waterUsePerCrew: 0.004, oxygenUsePerCrew: 0.01,
                researchEnergyRate: 0.03, researchProgressRate: 1.0, initialPanelFouling: 0.25);

            var large = new MissionSimulation(new MissionConfig(720, 480, resCfg), new MissionScenario());
            var small = new MissionSimulation(new MissionConfig(720, 480, resCfg), new MissionScenario());

            large.AssignTask("crew-1", "panel-maintenance");
            large.AssignTask("crew-2", "research");
            small.AssignTask("crew-1", "panel-maintenance");
            small.AssignTask("crew-2", "research");

            large.Tick(120);
            for (int i = 0; i < 480; i++)
                small.Tick(0.25);

            Close(large.State.Resources.Battery, small.State.Resources.Battery);
            Close(large.State.Resources.ResearchProgress, small.State.Resources.ResearchProgress);
            Close(large.State.Resources.PanelFouling, small.State.Resources.PanelFouling);
            Close(GetCrew(large.State, "crew-1").Stamina, GetCrew(small.State, "crew-1").Stamina);
            Close(GetCrew(large.State, "crew-2").Stamina, GetCrew(small.State, "crew-2").Stamina);
            Assert.That(GetTask(large.State, "panel-maintenance").IsCompleted, Is.True);
            Assert.That(GetTask(small.State, "panel-maintenance").IsCompleted, Is.True);
        }
    }
}
