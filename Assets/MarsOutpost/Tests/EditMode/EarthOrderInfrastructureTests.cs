using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public sealed class EarthOrderInfrastructureTests
    {
        [Test]
        public void EarthOrdersAndResearchReserveTheSameLabInBothDirections()
        {
            var sim = new MissionSimulation(MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            sim.Tick(14 * 720); sim.AcknowledgeSolReport();
            EarthOrderState order = null;
            foreach (var candidate in sim.State.EarthOrders)
                if (candidate.Kind == EarthOrderKind.OxygenExperiment) order = candidate;
            Assert.That(order, Is.Not.Null);
            sim.AcceptEarthOrder(order.OrderId);
            sim.AssignTask("crew-2", "research");
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.False);
            sim.CancelTask("crew-2");
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.AssignTask("crew-2", "research").Success, Is.False);
            Assert.That(sim.State.Crew[0].ActiveTaskId, Is.EqualTo(order.OrderId));
            sim.CancelEarthOrder(order.OrderId);
            Assert.That(sim.AssignTask("crew-2", "research").Success, Is.True);
        }

        [Test]
        public void OxygenExperimentAtFullTankRunsToCompletionWithoutCapacityChatter()
        {
            var cfg = MissionConfig.CreateOperationsDefault(
                new ResourceConfig(initialOxygen: 100, solarRate: 1, waterExtractionRate: .1),
                enableOperationsRandomEvents: false);
            var sim = new MissionSimulation(cfg, new MissionScenario(events: System.Array.Empty<EventState>()));
            sim.Tick(14 * 720);
            sim.AcknowledgeSolReport();
            EarthOrderState order = null;
            foreach (var candidate in sim.State.EarthOrders)
                if (candidate.Kind == EarthOrderKind.OxygenExperiment) order = candidate;
            Assert.That(order, Is.Not.Null);
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            sim.Tick(60);
            foreach (var candidate in sim.State.EarthOrders)
                if (candidate.OrderId == order.OrderId)
                    Assert.That(candidate.Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.Oxygen, Is.EqualTo(100).Within(1e-8));
        }

        [Test]
        public void EarthOrdersArriveAtSolThreeFiveSevenAndLargeTickDoesNotSkip()
        {
            var smallSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 2026, enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            var largeSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 2026, enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            // Sol 1 and Sol 2: no Earth orders
            for (int t = 0; t < 1439; t++)
            {
                smallSim.Tick(1);
                if (smallSim.State.SolReportPauseRequested) smallSim.AcknowledgeSolReport();
            }
            Assert.That(smallSim.State.EarthOrders.Count, Is.EqualTo(0));

            // Advance into Sol 3 (1440s) -> 1st order arrives
            smallSim.Tick(2);
            if (smallSim.State.SolReportPauseRequested) smallSim.AcknowledgeSolReport();
            Assert.That(smallSim.State.SolNumber, Is.EqualTo(3));
            Assert.That(smallSim.State.EarthOrders.Count, Is.EqualTo(1));
            Assert.That(smallSim.State.EarthOrders[0].ArrivalSol, Is.EqualTo(3));
            Assert.That(smallSim.State.EarthOrders[0].AdvisoryDeadlineSol, Is.EqualTo(5));
            Assert.That(smallSim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Received));

            // Advance through Sol 5 (2880s) and Sol 7 (4320s)
            while (smallSim.State.SolNumber < 7)
            {
                smallSim.Tick(60);
                if (smallSim.State.SolReportPauseRequested) smallSim.AcknowledgeSolReport();
            }
            Assert.That(smallSim.State.SolNumber, Is.EqualTo(7));
            Assert.That(smallSim.State.EarthOrders.Count, Is.EqualTo(3));
            Assert.That(smallSim.State.EarthOrders[1].ArrivalSol, Is.EqualTo(5));
            Assert.That(smallSim.State.EarthOrders[2].ArrivalSol, Is.EqualTo(7));

            // Large Tick(3000) does not skip Sol 3 or Sol 5
            largeSim.Tick(3000);
            Assert.That(largeSim.State.EarthOrders.Count, Is.EqualTo(2));
            Assert.That(largeSim.State.EarthOrders[0].Kind, Is.EqualTo(smallSim.State.EarthOrders[0].Kind));
            Assert.That(largeSim.State.EarthOrders[1].Kind, Is.EqualTo(smallSim.State.EarthOrders[1].Kind));
        }

        [Test]
        public void SeededBagDrawsAllSevenKindsWithoutRepeatAndAcceptAssignCancelWorkWithZeroScience()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 9090, enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));

            // Advance to Sol 15 (7 arrivals: Sol 3, 5, 7, 9, 11, 13, 15)
            while (sim.State.SolNumber < 15)
            {
                sim.Tick(120);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            }

            Assert.That(sim.State.EarthOrders.Count, Is.EqualTo(7));
            var seen = new HashSet<EarthOrderKind>();
            for (int i = 0; i < 7; i++)
                seen.Add(sim.State.EarthOrders[i].Kind);
            Assert.That(seen.Count, Is.EqualTo(7));

            // Accept & Assign first order -> InProgress; second order cannot be InProgress simultaneously
            var first = sim.State.EarthOrders[0];
            var second = sim.State.EarthOrders[1];
            Assert.That(sim.AcceptEarthOrder(first.OrderId).Success, Is.True);
            Assert.That(sim.AcceptEarthOrder(second.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(first.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.AssignEarthOrder(second.OrderId, "crew-2").Success, Is.False);

            // Cancel second order -> Cancelled (not Completed)
            Assert.That(sim.CancelEarthOrder(second.OrderId).Success, Is.True);
            Assert.That(sim.State.EarthOrders[1].Status, Is.EqualTo(EarthOrderStatus.Cancelled));

            // Complete first order -> Completed, grants 0 science
            while (sim.State.EarthOrders[0].Status != EarthOrderStatus.Completed)
            {
                if (sim.State.EarthOrders[0].AssignedCrewId == null)
                {
                    if (sim.State.Crew[0].Location == CrewLocation.Outside && sim.State.Crew[0].Status == CrewStatus.Idle)
                    {
                        sim.ReturnToShelter("crew-1");
                        sim.Tick(15);
                    }
                    else
                    {
                        sim.AssignEarthOrder(first.OrderId, "crew-1");
                    }
                }
                sim.Tick(60);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            }
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Earth01SoilSampleRequiresExteriorReturnThenInteriorAnalysisAndZeroScience()
        {
            // Find a seed whose first order at Sol 3 is SoilSample (EARTH-01)
            MissionSimulation sim = null;
            for (int seed = 1; seed < 50; seed++)
            {
                var candidate = new MissionSimulation(
                    MissionConfig.CreateOperationsDefault(operationsSeed: seed, enableOperationsRandomEvents: false),
                    new MissionScenario(events: System.Array.Empty<EventState>()));
                while (candidate.State.SolNumber < 3)
                {
                    candidate.Tick(120);
                    if (candidate.State.SolReportPauseRequested) candidate.AcknowledgeSolReport();
                }
                if (candidate.State.EarthOrders[0].Kind == EarthOrderKind.SoilSample)
                {
                    sim = candidate;
                    break;
                }
            }
            Assert.That(sim, Is.Not.Null);

            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-01"));
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));

            // Work 60s outside -> advances to Step 1 (Inside), crew-1 remains Outside
            double staminaBefore = sim.State.Crew[0].Stamina;
            sim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.Crew[0].Stamina, Is.EqualTo(staminaBefore - 15.0).Within(1e-6));
            Assert.That(sim.State.EarthOrders[0].CurrentStepIndex, Is.EqualTo(1));
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.InProgress));
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));

            // Cannot assign outside crew directly to indoor Step 1
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.False);

            // Return to shelter first, then complete 60s indoor lab analysis
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(12);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);

            double staminaInsideBefore = sim.State.Crew[0].Stamina;
            sim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.Crew[0].Stamina, Is.EqualTo(staminaInsideBefore - 9.0).Within(1e-6));
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // Cannot re-accept or cancel a completed order (no duplicate reports)
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.False);
            Assert.That(sim.CancelEarthOrder(order.OrderId).Success, Is.False);
        }

        private static MissionSimulation CreateSimWithFirstOrder(EarthOrderKind kind, out MissionConfig config, out int matchedSeed, ResourceConfig resources = null)
        {
            for (int seed = 1; seed < 100; seed++)
            {
                var cfg = MissionConfig.CreateOperationsDefault(resources: resources, operationsSeed: seed, enableOperationsRandomEvents: false);
                var candidate = new MissionSimulation(
                    cfg,
                    new MissionScenario(events: System.Array.Empty<EventState>()));
                while (candidate.State.SolNumber < 3)
                {
                    candidate.Tick(120);
                    if (candidate.State.SolReportPauseRequested) candidate.AcknowledgeSolReport();
                }
                if (candidate.State.EarthOrders[0].Kind == kind)
                {
                    config = cfg;
                    matchedSeed = seed;
                    return candidate;
                }
            }
            Assert.Fail($"Could not find seed producing {kind} on Sol 3.");
            config = null;
            matchedSeed = -1;
            return null;
        }

        [Test]
        public void Earth02OxygenExperimentDivertsTenPercentOfActualProductionAndHaltsAtZeroProduction()
        {
            var sim = CreateSimWithFirstOrder(EarthOrderKind.OxygenExperiment, out var config, out int seed);
            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-02"));

            // Baseline 10s at Sol 3 start -> O2 generator produces (at least crew demand if full, or more if below cap)
            double o2BeforeBase = sim.State.Resources.Oxygen;
            sim.Tick(10);
            double baselineNetO2 = sim.State.Resources.Oxygen - o2BeforeBase;
            double crewConsumption = config.Resources.OxygenUsePerCrew * sim.State.Crew.Count * 10.0;
            double expectedProduction = baselineNetO2 + crewConsumption;
            Assert.That(expectedProduction, Is.GreaterThan(0.05));

            // Accept & assign EARTH-02 -> 10% of production diverted, 90% to tank, 10s progress
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            double o2BeforeExp = sim.State.Resources.Oxygen;
            sim.Tick(10);
            double expNetO2 = sim.State.Resources.Oxygen - o2BeforeExp;
            double expectedNetWithSplit = expectedProduction * 0.90 - crewConsumption;
            Assert.That(expNetO2, Is.EqualTo(expectedNetWithSplit).Within(1e-6));
            Assert.That(sim.State.EarthOrders[0].StepProgressSeconds, Is.EqualTo(10.0).Within(1e-6));

            // Finish remaining 50s -> completes with 0 science
            sim.Tick(50);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // Zero O2 production (oxygenProductionRate = 0) -> experiment does not advance
            var zeroCfg = MissionConfig.CreateOperationsDefault(
                resources: new ResourceConfig(oxygenProductionRate: 0),
                operationsSeed: seed,
                enableOperationsRandomEvents: false);
            var zeroSim = new MissionSimulation(zeroCfg, new MissionScenario(events: System.Array.Empty<EventState>()));
            while (zeroSim.State.SolNumber < 3)
            {
                zeroSim.Tick(120);
                if (zeroSim.State.SolReportPauseRequested) zeroSim.AcknowledgeSolReport();
            }
            var zeroOrder = zeroSim.State.EarthOrders[0];
            Assert.That(zeroOrder.Kind, Is.EqualTo(EarthOrderKind.OxygenExperiment));
            Assert.That(zeroSim.AcceptEarthOrder(zeroOrder.OrderId).Success, Is.True);
            Assert.That(zeroSim.AssignEarthOrder(zeroOrder.OrderId, "crew-1").Success, Is.True);
            zeroSim.Tick(10);
            Assert.That(zeroSim.State.EarthOrders[0].StepProgressSeconds, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void Earth03RadiationSensorCalibrationRunsOutsideThenInsideReportAndAppliesRadiationRisk()
        {
            var sim = CreateSimWithFirstOrder(EarthOrderKind.RadiationSensorCalibration, out _, out _);
            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-03"));

            // Accepting does NOT force crew outside
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));

            // Assign Step 0 -> moves crew outside, 60s calibration
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));
            sim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.EarthOrders[0].CurrentStepIndex, Is.EqualTo(1));
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.InProgress));

            // Must return inside before Step 1 (20s terminal report)
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.False);
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(12);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);

            sim.Tick(20);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Earth04ThermalInsulationCheckUsesThreeEnergyUnitsAndDoesNotRepairSystemWear()
        {
            // Compare battery draw during active EARTH-04 against an idle baseline at night (no solar)
            var sim = CreateSimWithFirstOrder(EarthOrderKind.ThermalInsulationCheck, out var config, out int seed);
            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-04"));

            // Advance into night so solar == 0 and battery change reflects exact load
            sim.Tick(config.SolSeconds * 0.85);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();

            // Create identical idle baseline at same timestamp
            var idleSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: seed, enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            while (idleSim.State.SolNumber < 3)
            {
                idleSim.Tick(120);
                if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();
            }
            idleSim.Tick(config.SolSeconds * 0.85);
            if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();

            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);

            double batSimBefore = sim.State.Resources.Battery;
            double batIdleBefore = idleSim.State.Resources.Battery;
            sim.Tick(60);
            idleSim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();

            double step0ExtraEnergy = (batIdleBefore - idleSim.State.Resources.Battery) - (batSimBefore - sim.State.Resources.Battery);
            Assert.That(step0ExtraEnergy, Is.EqualTo(-2.0).Within(1e-5)); // sim drew 2.0 more energy than idle
            Assert.That(sim.State.EarthOrders[0].CurrentStepIndex, Is.EqualTo(1));

            // Return inside and finish 30s report -> draws remaining 1.0 energy (3.0 total)
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(12);
            idleSim.Tick(12);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);

            double batSimBefore2 = sim.State.Resources.Battery;
            double batIdleBefore2 = idleSim.State.Resources.Battery;
            double wearBefore = sim.State.Resources.SystemWear;
            sim.Tick(30);
            idleSim.Tick(30);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();

            double step1ExtraEnergy = (batIdleBefore2 - idleSim.State.Resources.Battery) - (batSimBefore2 - sim.State.Resources.Battery);
            Assert.That(step1ExtraEnergy, Is.EqualTo(-1.0).Within(1e-5));
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.SystemWear, Is.GreaterThanOrEqualTo(wearBefore - 1e-9));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Earth05WaterQualityAnalysisRequiresTwoWaterAndTwoEnergyAndDoesNotHealWaterFault()
        {
            // 1. Low water (< 2.0) -> AssignEarthOrder rejects with clear reason
            var lowWaterRes = new ResourceConfig(initialWater: 0.5, initialWastewater: 0, waterExtractionRate: 0);
            var lowSim = CreateSimWithFirstOrder(EarthOrderKind.WaterQualityAnalysis, out _, out int seed, resources: lowWaterRes);
            var lowOrder = lowSim.State.EarthOrders[0];
            Assert.That(lowOrder.Code, Is.EqualTo("EARTH-05"));
            Assert.That(lowSim.AcceptEarthOrder(lowOrder.OrderId).Success, Is.True);
            var rejectLow = lowSim.AssignEarthOrder(lowOrder.OrderId, "crew-1");
            Assert.That(rejectLow.Success, Is.False);
            Assert.That(rejectLow.Reason, Does.Contain("water").IgnoreCase);

            // 2. Sufficient water -> deducts 2.0 water upfront once, consumes 2.0 energy over 60s, 0 progress at 0 energy
            var sim = CreateSimWithFirstOrder(EarthOrderKind.WaterQualityAnalysis, out var config, out _);
            var order = sim.State.EarthOrders[0];
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);

            double waterBefore = sim.State.Resources.Water;
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.State.Resources.Water, Is.EqualTo(waterBefore - 2.0).Within(1e-6));
            Assert.That(sim.State.EarthOrders[0].UpfrontWaterPaid, Is.True);

            sim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // 3. Zero energy at night -> 0 progress
            var zeroEnergyCfg = MissionConfig.CreateOperationsDefault(
                resources: new ResourceConfig(initialBattery: 0, solarRate: 0),
                operationsSeed: seed,
                enableOperationsRandomEvents: false);
            var zeroSim = new MissionSimulation(zeroEnergyCfg, new MissionScenario(events: System.Array.Empty<EventState>()));
            while (zeroSim.State.SolNumber < 3)
            {
                zeroSim.Tick(120);
                if (zeroSim.State.SolReportPauseRequested) zeroSim.AcknowledgeSolReport();
            }
            var zeroOrder = zeroSim.State.EarthOrders[0];
            Assert.That(zeroSim.AcceptEarthOrder(zeroOrder.OrderId).Success, Is.True);
            Assert.That(zeroSim.AssignEarthOrder(zeroOrder.OrderId, "crew-1").Success, Is.True);
            zeroSim.Tick(10);
            Assert.That(zeroSim.State.EarthOrders[0].StepProgressSeconds, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void Earth06AntennaAlignmentDrawsFiveEnergyOutsideAndRunsLinkTestInsideWithoutChangingCommsOrSchedule()
        {
            var sim = CreateSimWithFirstOrder(EarthOrderKind.AntennaAlignment, out var config, out int seed);
            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-06"));

            // Advance into night so solar == 0 and battery change reflects exact load
            sim.Tick(config.SolSeconds * 0.85);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();

            var idleSim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: seed, enableOperationsRandomEvents: false),
                new MissionScenario(events: System.Array.Empty<EventState>()));
            while (idleSim.State.SolNumber < 3)
            {
                idleSim.Tick(120);
                if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();
            }
            idleSim.Tick(config.SolSeconds * 0.85);
            if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();

            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Outside));

            double batSimBefore = sim.State.Resources.Battery;
            double batIdleBefore = idleSim.State.Resources.Battery;
            sim.Tick(60);
            idleSim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            if (idleSim.State.SolReportPauseRequested) idleSim.AcknowledgeSolReport();

            double step0ExtraEnergy = (batIdleBefore - idleSim.State.Resources.Battery) - (batSimBefore - sim.State.Resources.Battery);
            Assert.That(step0ExtraEnergy, Is.EqualTo(-5.0).Within(1e-5));
            Assert.That(sim.State.EarthOrders[0].CurrentStepIndex, Is.EqualTo(1));

            // Return inside and finish 30s link test
            Assert.That(sim.ReturnToShelter("crew-1").Success, Is.True);
            sim.Tick(12);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            sim.Tick(30);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();

            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // Advance to Sol 5 -> second Earth order still arrives on schedule (Sol 5)
            while (sim.State.SolNumber < 5)
            {
                sim.Tick(120);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            }
            Assert.That(sim.State.EarthOrders.Count, Is.EqualTo(2));
            Assert.That(sim.State.EarthOrders[1].ArrivalSol, Is.EqualTo(5));
        }

        [Test]
        public void Earth07EmergencyStockAuditRunsTwoIndoorStepsWithoutChangingRationsOrScienceAndCompletesBag()
        {
            var sim = CreateSimWithFirstOrder(EarthOrderKind.EmergencyStockAudit, out _, out _);
            var order = sim.State.EarthOrders[0];
            Assert.That(order.Code, Is.EqualTo("EARTH-07"));

            int rationsBefore = sim.State.FoodRationsRemaining;
            Assert.That(sim.AcceptEarthOrder(order.OrderId).Success, Is.True);
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            Assert.That(sim.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));

            // Step 0: 60s stock inventory count
            sim.Tick(60);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            Assert.That(sim.State.EarthOrders[0].CurrentStepIndex, Is.EqualTo(1));
            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.InProgress));

            // Step 1: 30s emergency procedure drill & report
            Assert.That(sim.AssignEarthOrder(order.OrderId, "crew-1").Success, Is.True);
            sim.Tick(30);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();

            Assert.That(sim.State.EarthOrders[0].Status, Is.EqualTo(EarthOrderStatus.Completed));
            Assert.That(sim.State.FoodRationsRemaining, Is.EqualTo(rationsBefore));
            Assert.That(sim.State.Resources.ScienceBalance, Is.EqualTo(0).Within(1e-9));
            Assert.That(sim.State.Resources.ResearchProgress, Is.EqualTo(0).Within(1e-9));

            // Advance until 7 orders have arrived (Sol 15) and verify all 7 distinct kinds appeared
            while (sim.State.EarthOrders.Count < 7)
            {
                sim.Tick(120);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            }
            var kinds = new System.Collections.Generic.HashSet<EarthOrderKind>();
            for (int i = 0; i < 7; i++)
                kinds.Add(sim.State.EarthOrders[i].Kind);
            Assert.That(kinds.Count, Is.EqualTo(7));
        }
    }
}
