using MarsOutpost.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MarsOutpost.Tests
{
    public class DefaultMissionBalanceTests
    {
        [Test]
        public void ResearchPowerTradesBatteryForProgress()
        {
            var owner = new GameObject("Power tradeoff test");
            try
            {
                var runner = owner.AddComponent<SimulationRunner>();
                runner.InitializeDefaultMission();
                var active = runner.Simulation;
                Assert.That(active.AssignTask("crew-1", "research").Success, Is.True);
                active.Tick(300);

                runner.InitializeDefaultMission();
                var conserving = runner.Simulation;
                Assert.That(conserving.AssignTask("crew-1", "research").Success, Is.True);
                Assert.That(conserving.SetResearchEnabled(false).Success, Is.True);
                conserving.Tick(300);

                Assert.That(conserving.State.Resources.Battery, Is.GreaterThan(active.State.Resources.Battery));
                Assert.That(conserving.State.Resources.ResearchProgress, Is.LessThan(active.State.Resources.ResearchProgress));
                Assert.That(conserving.State.Crew[0].Stamina, Is.GreaterThan(active.State.Crew[0].Stamina));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(0, MissionOutcome.Failed)]
        [TestCase(1, MissionOutcome.Succeeded)]
        [TestCase(2, MissionOutcome.Succeeded)]
        public void DefaultMissionRewardsActivePlans(int plan, MissionOutcome expected)
        {
            var owner = new GameObject("Balance test runner");
            try
            {
                var runner = owner.AddComponent<SimulationRunner>();
                runner.InitializeDefaultMission();
                var sim = runner.Simulation;
                Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
                if (plan == 1) Assert.That(sim.AssignTask("crew-2", "meal-prep").Success, Is.True);
                if (plan == 2) Assert.That(sim.AssignTask("crew-3", "panel-maintenance").Success, Is.True);

                for (int t = 0; t < 1440 && sim.State.Outcome == MissionOutcome.InProgress; t++)
                {
                    // Prepared sleep restores the first researcher for a second shift.
                    if (plan == 1 && t == 480) Assert.That(sim.BeginSleep().Success, Is.True);
                    if (plan == 1 && t == 720) Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
                    // Alternatively, clean the panel, rotate researchers, and rest between shifts.
                    if (plan == 2 && t == 60) Assert.That(sim.ReturnToShelter("crew-3").Success, Is.True);
                    if (plan == 2 && t == 600)
                    {
                        Assert.That(sim.CancelTask("crew-1").Success, Is.True);
                        Assert.That(sim.Rest("crew-1").Success, Is.True);
                        Assert.That(sim.AssignTask("crew-2", "research").Success, Is.True);
                    }
                    if (plan == 2 && t == 1200)
                    {
                        Assert.That(sim.CancelTask("crew-2").Success, Is.True);
                        Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
                    }
                    sim.Tick(1);
                    if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                    if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
                }
                Assert.That(sim.State.Outcome, Is.EqualTo(expected));
                if (plan == 0) Assert.That(sim.State.FailureReason, Is.EqualTo(MissionFailureReason.ResearchGoalMissed));
                else Assert.That(sim.State.Resources.Oxygen, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void OperationsModeDoesNotEndAtTwoSolsAndIsolatesTrainingAndDemo()
        {
            var owner = new GameObject("Operations mode test");
            try
            {
                var runner = owner.AddComponent<SimulationRunner>();
                runner.InitializeOperationsMission();
                Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Operations));
                Assert.That(runner.State.Mode, Is.EqualTo(MissionMode.Operations));
                Assert.That(runner.State.Events.Count, Is.EqualTo(0));

                var sim = runner.Simulation;
                for (int t = 0; t < 1500; t++)
                {
                    sim.Tick(1);
                    if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                    if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
                }

                // Operations continues past 2 sols (1440s) without ending
                Assert.That(sim.State.ElapsedSeconds, Is.EqualTo(1500));
                Assert.That(sim.State.SolNumber, Is.EqualTo(3));
                Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));

                // CycleMode: Operations -> Demo -> Training -> Operations
                runner.CycleMode();
                Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Demo));
                Assert.That(runner.IsDemoMode, Is.True);

                runner.CycleMode();
                Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Training));
                Assert.That(runner.IsDemoMode, Is.False);
                Assert.That(runner.ActiveResearchGoal, Is.EqualTo(120));

                runner.CycleMode();
                Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Operations));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void ScienceBalanceSpendsAtomicallyAndDailyMealLogisticsRefillsAtSolStart()
        {
            var sim = new MissionSimulation(MissionConfig.CreateOperationsDefault(), new MissionScenario(events: System.Array.Empty<EventState>()));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(2));

            // Insufficient balance is rejected atomically
            var rejected = sim.SpendScience(10, "Extractor repair");
            Assert.That(rejected.Success, Is.False);
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // Assign crew-1 to research to earn >10 science
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
            for (int t = 0; t < 200; t++)
            {
                sim.Tick(1);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }

            double earnedResearch = sim.State.Resources.ResearchProgress;
            Assert.That(earnedResearch, Is.GreaterThanOrEqualTo(10.0));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(earnedResearch).Within(1e-9));

            // Spend 10 science: total ResearchProgress stays intact, ScienceBalance drops by 10
            var spent = sim.SpendScience(10, "Extractor repair");
            Assert.That(spent.Success, Is.True);
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(earnedResearch).Within(1e-9));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(earnedResearch - 10.0).Within(1e-9));

            // Advance to Sol 1 night (480s) -> consumes 1 ration -> 1 remaining
            for (int t = 200; t < 485; t++)
            {
                sim.Tick(1);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(1));

            // Advance to Sol 2 start (720s) -> Operations refills +1 up to cap 2
            for (int t = 485; t < 720; t++)
            {
                sim.Tick(1);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(2));

            // Training mode preserves 2-ration stock without Sol-start refill
            var training = new MissionSimulation(new MissionConfig(), new MissionScenario());
            for (int t = 0; t < 720; t++)
            {
                training.Tick(1);
                if (training.State.SolReportPauseRequested) training.AcknowledgeSolReport();
                if (training.State.CriticalAlertPauseRequested) training.AcknowledgeCriticalAlert();
            }
            Assert.That(training.State.FoodRationsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void WaterExtractorRespectsEnergyPriorityTankCapToggleAndHealthyLongRun()
        {
            // 1. Zero energy -> 0 extraction
            var zeroPowerRes = new ResourceConfig(
                initialBattery: 0,
                solarRate: 0,
                waterExtractionRate: 0.012,
                waterExtractorEnergyRate: 0.015);
            var zeroSim = new MissionSimulation(MissionConfig.CreateOperationsDefault(zeroPowerRes), new MissionScenario(events: System.Array.Empty<EventState>()));
            zeroSim.Tick(1);
            Assert.That(zeroSim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0).Within(1e-9));

            // 2. Insufficient power -> proportional extraction (O2 + rec take priority)
            // With O2 full (100), O2 demand = 0.03 -> o2Energy = 0.03; rec = 0.012 -> recEnergy = 0.006; total life support = 0.036
            // Give solar = 0.036 + 0.0075 (half of 0.015) = 0.0435 with 0 battery
            var lowPowerRes = new ResourceConfig(
                initialBattery: 0,
                initialOxygen: 100,
                solarRate: 0.0435,
                waterExtractionRate: 0.012,
                waterExtractorEnergyRate: 0.015);
            var lowSim = new MissionSimulation(MissionConfig.CreateOperationsDefault(lowPowerRes), new MissionScenario(events: System.Array.Empty<EventState>()));
            lowSim.Tick(1);
            Assert.That(lowSim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0.006).Within(1e-4));

            // 3. Toggle off/on and tank-full throttling (healthy base without random faults)
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            Assert.That(sim.SetWaterExtractorEnabled(false).Success, Is.True);
            sim.Tick(1);
            Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.SetWaterExtractorEnabled(true).Success, Is.True);

            // Advance until water tank reaches 100 (starts at 80, net gain ~0.006/s -> ~3400s = ~5 sols)
            for (int t = 0; t < 4320; t++) // 6 sols
            {
                sim.Tick(1);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(sim.State.Resources.Water, Is.EqualTo(100).Within(1e-6));
            Assert.That(sim.State.Resources.Oxygen, Is.GreaterThan(50));
            Assert.That(sim.State.Resources.Battery, Is.GreaterThan(20));
            // At tank cap, extraction throttles to net water demand (0.006), below max 0.012
            Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.LessThan(0.012 - 1e-4));
            Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.GreaterThan(0.004));

            // 4. Continue to 100 sols healthy automatic operation
            for (int t = 4320; t < 72000; t += 10)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.SolNumber, Is.EqualTo(101));
            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(sim.State.Resources.Water, Is.EqualTo(100).Within(1e-6));
            Assert.That(sim.State.Resources.Oxygen, Is.GreaterThan(50));
            Assert.That(sim.State.Resources.Battery, Is.GreaterThan(20));
        }
    }
}
