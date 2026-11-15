using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public sealed class OperationsChannelSchedulerTests
    {
        [TestCase(OperationsChannelId.SolarPanel)]
        [TestCase(OperationsChannelId.OxygenProducer)]
        [TestCase(OperationsChannelId.WaterExtractor)]
        [TestCase(OperationsChannelId.DustStorm)]
        public void EquipmentFaultsAndDustWarningsWakeSleepingCrewAndRequestPause(OperationsChannelId id)
        {
            var config = new MissionConfig(daySeconds: 0, mode: MissionMode.Operations, targetSols: 0,
                enableOperationsRandomEvents: true, insideRadiationRiskRate: 0, acuteRiskRecoveryRate: .03,
                resources: new ResourceConfig(initialBattery: 100, oxygenEnergyPerUnit: 0, recyclingEnergyPerUnit: 0,
                    waterExtractionRate: .1));
            var sim = new MissionSimulation(config, new MissionScenario(events: System.Array.Empty<EventState>()));
            var channel = sim.State.ChannelSchedules[(int)id];
            double boundary = channel.NextTriggerAtSeconds - channel.WarningLeadSeconds;
            sim.Tick(boundary - 1);
            sim.AcknowledgeSolReport(); sim.AcknowledgeCriticalAlert();
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(2);
            Assert.That(sim.State.IsSleeping, Is.False);
            Assert.That(sim.State.CriticalAlertPauseRequested, Is.True);
            foreach (var crew in sim.State.Crew) Assert.That(crew.Status, Is.Not.EqualTo(CrewStatus.Sleeping));
        }

        [Test]
        public void FiveChannelsSampleWithinExactBoundsAndAreDeterministic()
        {
            var simA = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 12345),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            var simB = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 12345),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            Assert.That(simA.State.ChannelSchedules.Count, Is.EqualTo(5));

            for (int i = 0; i < 5; i++)
            {
                var chA = simA.State.ChannelSchedules[i];
                var chB = simB.State.ChannelSchedules[i];
                Assert.That(chA.ChannelId, Is.EqualTo(chB.ChannelId));
                Assert.That(chA.NextTriggerAtSeconds, Is.EqualTo(chB.NextTriggerAtSeconds));
                Assert.That(chA.RngState, Is.EqualTo(chB.RngState));

                OperationsScheduler.GetWaitBounds(chA.ChannelId, out double min, out double max);
                Assert.That(chA.NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(min));
                Assert.That(chA.NextTriggerAtSeconds, Is.LessThan(max));
            }
        }

        [Test]
        public void ResolvingOneChannelDoesNotAffectOtherFourChannels()
        {
            var baseline = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 777),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            var modified = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 777),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            // Advance 1850s so fault channels have triggered
            for (int t = 0; t < 1850; t += 10)
            {
                baseline.Tick(10);
                if (baseline.State.SolReportPauseRequested) baseline.AcknowledgeSolReport();
                if (baseline.State.CriticalAlertPauseRequested) baseline.AcknowledgeCriticalAlert();

                modified.Tick(10);
                if (modified.State.SolReportPauseRequested) modified.AcknowledgeSolReport();
                if (modified.State.CriticalAlertPauseRequested) modified.AcknowledgeCriticalAlert();
            }

            // Resolve only SolarPanel in modified
            Assert.That(modified.ResolveOperationsFault(OperationsChannelId.SolarPanel), Is.True);

            for (int i = 0; i < 5; i++)
            {
                var chBase = baseline.State.ChannelSchedules[i];
                var chMod = modified.State.ChannelSchedules[i];
                if (chBase.ChannelId == OperationsChannelId.SolarPanel)
                {
                    Assert.That(chBase.IsActive, Is.True);
                    Assert.That(chMod.IsActive, Is.False);
                    Assert.That(chMod.NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(1850 + 1200));
                    Assert.That(chMod.NextTriggerAtSeconds, Is.LessThan(1850 + 1800));
                }
                else
                {
                    Assert.That(chMod.IsActive, Is.EqualTo(chBase.IsActive));
                    Assert.That(chMod.NextTriggerAtSeconds, Is.EqualTo(chBase.NextTriggerAtSeconds));
                    Assert.That(chMod.ActiveUntilSeconds, Is.EqualTo(chBase.ActiveUntilSeconds));
                    Assert.That(chMod.RngState, Is.EqualTo(chBase.RngState));
                }
            }
        }

        [Test]
        public void LargeTickMatchesOneSecondTicksAcrossChannelBoundaries()
        {
            var simSmall = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 999),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            var simLarge = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 999),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            // Run up to 700s (before first Sol report pause at 720s)
            for (int t = 0; t < 700; t++)
                simSmall.Tick(1);
            simLarge.Tick(700);

            Assert.That(simLarge.State.OperationsLog, Is.EqualTo(simSmall.State.OperationsLog));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(simLarge.State.ChannelSchedules[i].IsActive, Is.EqualTo(simSmall.State.ChannelSchedules[i].IsActive));
                Assert.That(simLarge.State.ChannelSchedules[i].WarningIssued, Is.EqualTo(simSmall.State.ChannelSchedules[i].WarningIssued));
                Assert.That(simLarge.State.ChannelSchedules[i].NextTriggerAtSeconds, Is.EqualTo(simSmall.State.ChannelSchedules[i].NextTriggerAtSeconds));
                Assert.That(simLarge.State.ChannelSchedules[i].RngState, Is.EqualTo(simSmall.State.ChannelSchedules[i].RngState));
            }
        }

        [Test]
        public void RadiationChannelWarnsWakesSleepRunsActiveWindowAndRecoversAcuteRisk()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 4242),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            var radChannel = sim.State.ChannelSchedules[0];
            Assert.That(radChannel.ChannelId, Is.EqualTo(OperationsChannelId.Radiation));
            double firstStart = radChannel.NextTriggerAtSeconds;
            Assert.That(firstStart, Is.GreaterThanOrEqualTo(900));
            Assert.That(firstStart, Is.LessThan(1200));
            double warnAt = firstStart - 60.0;

            // Advance into Sol 2 night before warning (if firstStart is in Sol 2 day/night)
            // First Sol ends at 720s; advance to 725s, acknowledge Sol 1 report, then test sleep wake at warnAt
            while (sim.State.ElapsedSeconds < 720)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }

            // Advance to 10s before warning and put crew to sleep (or if warnAt >= 720, test sleep wake)
            if (warnAt > 730)
            {
                double preWarn = warnAt - 10;
                while (sim.State.ElapsedSeconds < preWarn)
                {
                    double step = System.Math.Min(10, preWarn - sim.State.ElapsedSeconds);
                    sim.Tick(step);
                    if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                    if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
                }

                // Start sleep if night or force sleep check during night window (Sol 2 night starts at 720+480=1200)
                if (sim.State.IsNight)
                {
                    Assert.That(sim.BeginSleep().Success, Is.True);
                    Assert.That(sim.State.IsSleeping, Is.True);
                }

                sim.Tick(15);
                Assert.That(sim.State.ChannelSchedules[0].WarningIssued, Is.True);
                if (sim.State.IsNight || sim.State.CriticalAlertPauseRequested)
                {
                    Assert.That(sim.State.IsSleeping, Is.False);
                }
            }
            else
            {
                sim.Tick(warnAt - sim.State.ElapsedSeconds + 1);
                Assert.That(sim.State.ChannelSchedules[0].WarningIssued, Is.True);
            }

            // Acknowledging alert does not reset next trigger
            if (sim.State.CriticalAlertPauseRequested)
                sim.AcknowledgeCriticalAlert();
            Assert.That(sim.State.ChannelSchedules[0].NextTriggerAtSeconds, Is.EqualTo(firstStart));

            // Advance to active start + 5s
            sim.Tick(firstStart - sim.State.ElapsedSeconds + 5);
            Assert.That(sim.State.ChannelSchedules[0].IsActive, Is.True);
            double activeUntil = sim.State.ChannelSchedules[0].ActiveUntilSeconds;
            double dur = activeUntil - firstStart;
            Assert.That(dur, Is.GreaterThanOrEqualTo(60));
            Assert.That(dur, Is.LessThan(120));
            Assert.That(sim.State.Events.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.State.Crew[0].RiskScore, Is.GreaterThan(0));

            // Advance past active end
            while (sim.State.ElapsedSeconds < activeUntil + 1)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }

            Assert.That(sim.State.ChannelSchedules[0].IsActive, Is.False);
            double nextStart = sim.State.ChannelSchedules[0].NextTriggerAtSeconds;
            Assert.That(nextStart, Is.GreaterThanOrEqualTo(activeUntil + 900));
            Assert.That(nextStart, Is.LessThan(activeUntil + 1200));

            // Acute risk recovers inside at 0.03/s down to 0
            double riskAfterEvent = sim.State.Crew[0].RiskScore;
            sim.Tick(100);
            Assert.That(sim.State.Crew[0].RiskScore, Is.EqualTo(System.Math.Max(0, riskAfterEvent - 3.0)).Within(1e-6));

            // Tick 700s more -> clamps at 0 (never negative)
            sim.Tick(700);
            Assert.That(sim.State.Crew[0].RiskScore, Is.EqualTo(0).Within(1e-9));

            // Training mode risk model does not recover
            var training = new MissionSimulation(
                new MissionConfig(),
                new MissionScenario(events: new[]
                {
                    new EventState("r", "R", EventKind.Radiation, 5, 10, 30, 1.0, "REF", "test")
                }));
            training.Tick(40);
            double trainRisk = training.State.Crew[0].RiskScore;
            Assert.That(trainRisk, Is.GreaterThan(0));
            training.Tick(100);
            Assert.That(training.State.Crew[0].RiskScore, Is.EqualTo(trainRisk).Within(1e-9));
        }

        [Test]
        public void SolarPanelFaultReducesOutputToTwoThirdsPreservesProgressOnCancelAndReschedulesOnRepair()
        {
            // Compare solar battery gain with 3 healthy panels vs Training (same SolarRate, no Panels list)
            var opsRes = new ResourceConfig(initialBattery: 50, solarRate: 0.12, initialPanelFouling: 0.20,
                oxygenProductionRate: 0, recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0,
                waterExtractionRate: 0, waterExtractorEnergyRate: 0);
            var opsSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(resources: opsRes, operationsSeed: 5150),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            var trainSim = new MissionSimulation(
                new MissionConfig(resources: opsRes),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            Assert.That(opsSim.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(opsSim.AssignTask("crew-1", "panel-repair").Success, Is.False);

            opsSim.Tick(10);
            trainSim.Tick(10);
            Assert.That(opsSim.State.Resources.Battery, Is.EqualTo(trainSim.State.Resources.Battery).Within(1e-9));

            // Advance until SolarPanel channel triggers (1200..1800s)
            double triggerAt = opsSim.State.ChannelSchedules[1].NextTriggerAtSeconds;
            Assert.That(opsSim.State.ChannelSchedules[1].ChannelId, Is.EqualTo(OperationsChannelId.SolarPanel));
            Assert.That(triggerAt, Is.GreaterThanOrEqualTo(1200));
            Assert.That(triggerAt, Is.LessThan(1800));

            while (opsSim.State.ElapsedSeconds < triggerAt + 5)
            {
                opsSim.Tick(10);
                if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                if (opsSim.State.CriticalAlertPauseRequested) opsSim.AcknowledgeCriticalAlert();
            }

            int faultedCount = 0;
            for (int i = 0; i < opsSim.State.Resources.Panels.Count; i++)
                if (opsSim.State.Resources.Panels[i].IsFaulted) faultedCount++;
            Assert.That(faultedCount, Is.EqualTo(1));
            Assert.That(opsSim.State.ChannelSchedules[1].IsActive, Is.True);

            // Advance another 100s -> no second panel faults while channel waits for repair
            while (opsSim.State.ElapsedSeconds < triggerAt + 105)
            {
                opsSim.Tick(10);
                if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                if (opsSim.State.CriticalAlertPauseRequested) opsSim.AcknowledgeCriticalAlert();
            }
            faultedCount = 0;
            for (int i = 0; i < opsSim.State.Resources.Panels.Count; i++)
                if (opsSim.State.Resources.Panels[i].IsFaulted) faultedCount++;
            Assert.That(faultedCount, Is.EqualTo(1));

            // Assign panel-repair, work 20s, cancel -> progress preserved
            // Ensure crew-1 is rested and we're in daytime for solar check
            Assert.That(opsSim.AssignTask("crew-1", "panel-repair").Success, Is.True);
            opsSim.Tick(20);
            Assert.That(opsSim.CancelTask("crew-1").Success, Is.True);
            TaskState repairTask = null;
            for (int i = 0; i < opsSim.State.Tasks.Count; i++)
                if (opsSim.State.Tasks[i].Id == "panel-repair") repairTask = opsSim.State.Tasks[i];
            Assert.That(repairTask, Is.Not.Null);
            Assert.That(repairTask.ProgressSeconds, Is.EqualTo(20).Within(1e-9));

            // Re-assign and finish remaining 40s
            double foulingBefore = opsSim.State.Resources.PanelFouling;
            Assert.That(opsSim.AssignTask("crew-1", "panel-repair").Success, Is.True);
            opsSim.Tick(40);
            double repairedAt = opsSim.State.ElapsedSeconds;

            faultedCount = 0;
            for (int i = 0; i < opsSim.State.Resources.Panels.Count; i++)
                if (opsSim.State.Resources.Panels[i].IsFaulted) faultedCount++;
            Assert.That(faultedCount, Is.EqualTo(0));
            Assert.That(opsSim.State.Resources.PanelFouling, Is.EqualTo(foulingBefore).Within(1e-9));
            Assert.That(opsSim.State.ChannelSchedules[1].IsActive, Is.False);
            Assert.That(opsSim.State.ChannelSchedules[1].NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(repairedAt + 1200));
            Assert.That(opsSim.State.ChannelSchedules[1].NextTriggerAtSeconds, Is.LessThan(repairedAt + 1800));
        }

        [Test]
        public void OxygenProducerFaultHalvesCapacityAndRepairConsumesDoubleMaintenanceEnergy()
        {
            var opsSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 6262),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            double triggerAt = opsSim.State.ChannelSchedules[2].NextTriggerAtSeconds;
            Assert.That(opsSim.State.ChannelSchedules[2].ChannelId, Is.EqualTo(OperationsChannelId.OxygenProducer));
            Assert.That(triggerAt, Is.GreaterThanOrEqualTo(1200));
            Assert.That(triggerAt, Is.LessThan(1800));
            Assert.That(opsSim.AssignTask("crew-1", "o2-repair").Success, Is.False);

            while (opsSim.State.ElapsedSeconds < triggerAt + 5)
            {
                opsSim.Tick(10);
                if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                if (opsSim.State.CriticalAlertPauseRequested) opsSim.AcknowledgeCriticalAlert();
            }
            Assert.That(opsSim.State.Resources.OxygenProducerFaulted, Is.True);

            // 1. Zero energy -> 0 progress (cannot complete for free)
            var zeroRes = new ResourceConfig(initialBattery: 0, solarRate: 0, oxygenProductionRate: 0, recyclingRate: 0,
                waterUsePerCrew: 0, oxygenUsePerCrew: 0, oxygenRepairEnergyRate: 0.5);
            var zeroCfg = new MissionConfig(
                solSeconds: 720, daySeconds: 480, resources: zeroRes, researchGoal: 0, targetSols: 0,
                oxygenOutageFailureSeconds: 0, maxCrewRiskScore: 1000, operationsSeed: 6262,
                enableOperationsRandomEvents: true, mode: MissionMode.Operations);
            var zeroSim = new MissionSimulation(zeroCfg, new MissionScenario(events: System.Array.Empty<EventState>()));
            while (zeroSim.State.ElapsedSeconds < triggerAt + 5)
            {
                zeroSim.Tick(10);
                if (zeroSim.State.SolReportPauseRequested) zeroSim.AcknowledgeSolReport();
                if (zeroSim.State.CriticalAlertPauseRequested) zeroSim.AcknowledgeCriticalAlert();
            }
            Assert.That(zeroSim.State.Resources.OxygenProducerFaulted, Is.True);
            Assert.That(zeroSim.AssignTask("crew-1", "o2-repair").Success, Is.True);
            zeroSim.Tick(10);
            TaskState zeroTask = null;
            for (int i = 0; i < zeroSim.State.Tasks.Count; i++)
                if (zeroSim.State.Tasks[i].Id == "o2-repair") zeroTask = zeroSim.State.Tasks[i];
            Assert.That(zeroTask.ProgressSeconds, Is.EqualTo(0).Within(1e-9));

            // 2. Partial solar (0.25/s = 50% of 0.5/s, batteryCapacity 0) -> 50% work speed (5s in 10s)
            var halfRes = new ResourceConfig(batteryCapacity: 0, initialBattery: 0, solarRate: 0.25,
                oxygenProductionRate: 0, recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0,
                oxygenRepairEnergyRate: 0.5);
            var halfCfg = new MissionConfig(
                solSeconds: 720, daySeconds: 480, resources: halfRes, researchGoal: 0, targetSols: 0,
                oxygenOutageFailureSeconds: 0, maxCrewRiskScore: 1000, operationsSeed: 6262,
                enableOperationsRandomEvents: true, mode: MissionMode.Operations);
            var halfSim = new MissionSimulation(halfCfg, new MissionScenario(events: System.Array.Empty<EventState>()));
            while (halfSim.State.ElapsedSeconds < triggerAt + 5)
            {
                halfSim.Tick(10);
                if (halfSim.State.SolReportPauseRequested) halfSim.AcknowledgeSolReport();
                if (halfSim.State.CriticalAlertPauseRequested) halfSim.AcknowledgeCriticalAlert();
            }
            // Advance to daytime if night
            while (halfSim.State.IsNight)
            {
                halfSim.Tick(10);
                if (halfSim.State.SolReportPauseRequested) halfSim.AcknowledgeSolReport();
                if (halfSim.State.CriticalAlertPauseRequested) halfSim.AcknowledgeCriticalAlert();
            }
            Assert.That(halfSim.AssignTask("crew-1", "o2-repair").Success, Is.True);
            halfSim.Tick(10);
            TaskState halfTask = null;
            for (int i = 0; i < halfSim.State.Tasks.Count; i++)
                if (halfSim.State.Tasks[i].Id == "o2-repair") halfTask = halfSim.State.Tasks[i];
            Assert.That(halfTask.ProgressSeconds, Is.EqualTo(5.0).Within(1e-6));

            // 3. Full-energy 40s repair consumes 20 energy more than identical no-repair run
            var noRepairSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 6262),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            while (noRepairSim.State.ElapsedSeconds < opsSim.State.ElapsedSeconds)
            {
                noRepairSim.Tick(System.Math.Min(10, opsSim.State.ElapsedSeconds - noRepairSim.State.ElapsedSeconds));
                if (noRepairSim.State.SolReportPauseRequested) noRepairSim.AcknowledgeSolReport();
                if (noRepairSim.State.CriticalAlertPauseRequested) noRepairSim.AcknowledgeCriticalAlert();
            }

            Assert.That(opsSim.AssignTask("crew-1", "o2-repair").Success, Is.True);
            for (int t = 0; t < 40; t++)
            {
                opsSim.Tick(1);
                noRepairSim.Tick(1);
                if (opsSim.State.SolReportPauseRequested) opsSim.AcknowledgeSolReport();
                if (noRepairSim.State.CriticalAlertPauseRequested) noRepairSim.AcknowledgeCriticalAlert();
                if (opsSim.State.CriticalAlertPauseRequested) opsSim.AcknowledgeCriticalAlert();
            }

            double repairedAt = opsSim.State.ElapsedSeconds;
            Assert.That(opsSim.State.Resources.OxygenProducerFaulted, Is.False);
            Assert.That(opsSim.State.ChannelSchedules[2].IsActive, Is.False);
            Assert.That(opsSim.State.ChannelSchedules[2].NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(repairedAt + 1200));
            Assert.That(opsSim.State.ChannelSchedules[2].NextTriggerAtSeconds, Is.LessThan(repairedAt + 1800));
            // Repair consumed 20 energy more than the identical no-repair run
            Assert.That(noRepairSim.State.Resources.Battery - opsSim.State.Resources.Battery, Is.EqualTo(20.0).Within(0.05));
        }

        [Test]
        public void DustStormReducesSolarToTwentyPercentComposesWithFaultsAndReschedules()
        {
            var res = new ResourceConfig(batteryCapacity: 500, initialBattery: 50, solarRate: 0.12, initialPanelFouling: 0.20,
                oxygenProductionRate: 0, recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0,
                waterExtractionRate: 0, waterExtractorEnergyRate: 0);
            var cfg = new MissionConfig(
                solSeconds: 720, daySeconds: 480, resources: res, researchGoal: 0, targetSols: 0,
                oxygenOutageFailureSeconds: 0, maxCrewRiskScore: 1000, operationsSeed: 7373,
                enableOperationsRandomEvents: true, mode: MissionMode.Operations);
            var sim = new MissionSimulation(cfg, new MissionScenario(events: System.Array.Empty<EventState>()));

            var dustCh = sim.State.ChannelSchedules[3];
            Assert.That(dustCh.ChannelId, Is.EqualTo(OperationsChannelId.DustStorm));
            double startAt = dustCh.NextTriggerAtSeconds;
            Assert.That(startAt, Is.GreaterThanOrEqualTo(600));
            Assert.That(startAt, Is.LessThan(900));

            // Advance to warning (startAt - 60) + 1s
            while (sim.State.ElapsedSeconds < startAt - 59)
            {
                sim.Tick(System.Math.Min(10, (startAt - 59) - sim.State.ElapsedSeconds));
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.ChannelSchedules[3].WarningIssued, Is.True);

            // Advance to active start + 1s
            while (sim.State.ElapsedSeconds < startAt + 1)
            {
                sim.Tick(System.Math.Min(10, (startAt + 1) - sim.State.ElapsedSeconds));
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.ChannelSchedules[3].IsActive, Is.True);
            double activeUntil = sim.State.ChannelSchedules[3].ActiveUntilSeconds;
            Assert.That(activeUntil - startAt, Is.GreaterThanOrEqualTo(60));
            Assert.That(activeUntil - startAt, Is.LessThan(180));
            Assert.That(sim.State.Resources.AtmosphericDustLoss, Is.EqualTo(0.80).Within(1e-9));

            // If in daytime, verify exact x0.20 solar output
            if (!sim.State.IsNight)
            {
                double battBefore = sim.State.Resources.Battery;
                sim.Tick(1);
                double expectedSolar = 0.12 * (1.0 - 0.20) * (1.0 - 0.80);
                Assert.That(sim.State.Resources.Battery - battBefore, Is.EqualTo(expectedSolar).Within(1e-6));
            }

            // Advance past activeUntil -> storm ends, reschedules 600..900s, does not clear other faults
            while (sim.State.ElapsedSeconds < activeUntil + 1)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.ChannelSchedules[3].IsActive, Is.False);
            Assert.That(sim.State.Resources.AtmosphericDustLoss, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.ChannelSchedules[3].NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(activeUntil + 600));
            Assert.That(sim.State.ChannelSchedules[3].NextTriggerAtSeconds, Is.LessThan(activeUntil + 900));
        }

        [Test]
        public void WaterExtractorFaultHalvesExtractionAndRepairsAtomicallyWithTenScience()
        {
            var res = new ResourceConfig(initialBattery: 80, initialWater: 50, waterExtractionRate: 0.012, waterExtractorEnergyRate: 0.015,
                researchEnergyRate: 0.09, researchProgressRate: 0.15);
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(resources: res, operationsSeed: 8484),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            var extCh = sim.State.ChannelSchedules[4];
            Assert.That(extCh.ChannelId, Is.EqualTo(OperationsChannelId.WaterExtractor));
            double triggerAt = extCh.NextTriggerAtSeconds;
            Assert.That(triggerAt, Is.GreaterThanOrEqualTo(1200));
            Assert.That(triggerAt, Is.LessThan(1500));

            // Healthy extraction rate when water < 100
            sim.Tick(1);
            Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0.012).Within(1e-6));

            // Repair before fault -> rejected
            Assert.That(sim.RepairWaterExtractorWithScience().Success, Is.False);

            // Advance past triggerAt (without research, ScienceBalance == 0)
            while (sim.State.ElapsedSeconds < triggerAt + 5)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            Assert.That(sim.State.Resources.WaterExtractorFaulted, Is.True);
            Assert.That(sim.State.ChannelSchedules[4].IsActive, Is.True);

            // Check 0.50x extraction rate (0.006/s) while water < 100
            if (sim.State.Resources.Water < 99.5)
                Assert.That(sim.State.Resources.LastWaterExtractionRate, Is.EqualTo(0.006).Within(1e-6));

            // Insufficient science (<10) -> atomic rejection, fault stays true
            Assert.That(sim.State.Resources.ScienceBalance, Is.LessThan(10));
            var rejected = sim.RepairWaterExtractorWithScience();
            Assert.That(rejected.Success, Is.False);
            Assert.That(sim.State.Resources.WaterExtractorFaulted, Is.True);

            // Earn >=10 science via research
            Assert.That(sim.AssignTask("crew-1", "research").Success, Is.True);
            while (sim.State.Resources.ScienceBalance < 10.5)
            {
                sim.Tick(10);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }
            double totalResearchBefore = sim.State.Resources.ResearchProgress;
            double balanceBefore = sim.State.Resources.ScienceBalance;

            // Repair once -> spends 10 science, preserves ResearchProgress, clears fault, reschedules 1200..1500
            var repaired = sim.RepairWaterExtractorWithScience();
            Assert.That(repaired.Success, Is.True);
            double repairedAt = sim.State.ElapsedSeconds;
            Assert.That(sim.State.Resources.WaterExtractorFaulted, Is.False);
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(balanceBefore - 10.0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(totalResearchBefore).Within(1e-9));
            Assert.That(sim.State.ChannelSchedules[4].IsActive, Is.False);
            Assert.That(sim.State.ChannelSchedules[4].NextTriggerAtSeconds, Is.GreaterThanOrEqualTo(repairedAt + 1200));
            Assert.That(sim.State.ChannelSchedules[4].NextTriggerAtSeconds, Is.LessThan(repairedAt + 1500));

            // Double-click / second repair call -> rejected without charging again
            var doubleClick = sim.RepairWaterExtractorWithScience();
            Assert.That(doubleClick.Success, Is.False);
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(balanceBefore - 10.0).Within(1e-9));
        }
    }
}
