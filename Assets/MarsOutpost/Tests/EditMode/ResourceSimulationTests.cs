using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class ResourceSimulationTests
    {
        private static MissionSimulation Create(ResourceConfig resources, double day = 0) =>
            new MissionSimulation(new MissionConfig(100, day, resources), new MissionScenario());
        private static void Close(double actual, double expected) => Assert.That(actual, Is.EqualTo(expected).Within(1e-7));

        [Test]
        public void SolarOnlyWorksDuringDayAndStopsAtCapacity()
        {
            var sim = Create(new ResourceConfig(initialBattery: 0, solarRate: 1, oxygenProductionRate: 0,
                recyclingRate: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0), 50);
            sim.Tick(75);
            Close(sim.State.Resources.Battery, 50);
            sim.Tick(25);
            Close(sim.State.Resources.Battery, 50);
        }

        [Test]
        public void NoInputsMeansNoProductionAndConsumptionIsReported()
        {
            var sim = Create(new ResourceConfig(initialBattery: 0, initialWater: 0, initialOxygen: 0,
                initialWastewater: 0, solarRate: 0, waterUsePerCrew: 1, oxygenUsePerCrew: 1));
            sim.Tick(10);
            Close(sim.State.Resources.Oxygen, 0);
            Close(sim.State.Resources.Water, 0);
            Close(sim.State.Resources.UnmetWater, 30);
            Close(sim.State.Resources.UnmetOxygen, 30);
        }

        [Test]
        public void RecyclingUsesOnlyWastewaterAndCanSupplyEmptyWater()
        {
            var sim = Create(new ResourceConfig(initialWater: 0, initialWastewater: 5, solarRate: 0,
                oxygenProductionRate: 0, recyclingRate: 2, recyclingEnergyPerUnit: 0, waterUsePerCrew: 0, oxygenUsePerCrew: 0));
            sim.Tick(10);
            Close(sim.State.Resources.Water, 5);
            Close(sim.State.Resources.Wastewater, 0);
        }

        [Test]
        public void EmptyWaterCanFeedCrewAndLifeSupportFromWastewater()
        {
            var sim = Create(new ResourceConfig(initialWater: 0, initialWastewater: 10, initialOxygen: 0,
                oxygenProductionRate: 1, oxygenWaterPerUnit: 1, oxygenEnergyPerUnit: 0,
                recyclingRate: 2, recyclingEnergyPerUnit: 0, waterUsePerCrew: 0.5, oxygenUsePerCrew: 0));
            sim.Tick(5);
            Close(sim.State.Resources.Water, 0);
            Close(sim.State.Resources.Oxygen, 2.5);
            Close(sim.State.Resources.Wastewater, 7.5);
            Close(sim.State.Resources.UnmetWater, 0);
        }

        [Test]
        public void SolarAtEmptyBatteryGoesToLifeSupportBeforeRecycling()
        {
            var sim = Create(new ResourceConfig(initialBattery: 0, initialWater: 50, initialOxygen: 0,
                initialWastewater: 50, solarRate: 1, oxygenProductionRate: 1, oxygenEnergyPerUnit: 1,
                oxygenWaterPerUnit: 0, recyclingRate: 1, recyclingEnergyPerUnit: 1,
                waterUsePerCrew: 0, oxygenUsePerCrew: 0), 100);
            sim.Tick(10);
            Close(sim.State.Resources.Oxygen, 10);
            Close(sim.State.Resources.Wastewater, 50);
            Close(sim.State.Resources.Battery, 0);
        }

        [Test]
        public void ConsumptionRunsOnTickAndRestartRestoresInitialValues()
        {
            var sim = Create(new ResourceConfig(solarRate: 0, oxygenProductionRate: 0, recyclingRate: 0,
                waterUsePerCrew: 1, oxygenUsePerCrew: 2));
            var original = sim.State.Resources;
            sim.SetResearchEnabled(false);
            sim.Tick(5);
            Close(sim.State.Resources.Water, 65);
            Close(sim.State.Resources.Oxygen, 50);
            Close(sim.State.Resources.Wastewater, 15);
            Assert.That(original.ResearchEnabled, Is.True);
            sim.Restart();
            Close(sim.State.Resources.Water, 80);
            Close(sim.State.Resources.UnmetWater, 0);
            Assert.That(sim.State.Resources.ResearchEnabled, Is.True);
        }

        [Test]
        public void DepletionAndDayBoundariesArePartitionIndependent()
        {
            var cfg = new ResourceConfig(initialBattery: 2, initialWater: 3, initialOxygen: 4, initialWastewater: 2,
                solarRate: 0.4, oxygenProductionRate: 0.3, oxygenEnergyPerUnit: 2, oxygenWaterPerUnit: 0.5,
                recyclingRate: 0.2, recyclingEnergyPerUnit: 1, waterUsePerCrew: 0.05, oxygenUsePerCrew: 0.1);
            var large = Create(cfg, 50);
            var small = Create(cfg, 50);
            large.Tick(200);
            for (int i = 0; i < 2000; i++) small.Tick(0.1);
            Close(large.State.Resources.Battery, small.State.Resources.Battery);
            Close(large.State.Resources.Water, small.State.Resources.Water);
            Close(large.State.Resources.Oxygen, small.State.Resources.Oxygen);
            Close(large.State.Resources.Wastewater, small.State.Resources.Wastewater);
            Close(large.State.Resources.UnmetWater, small.State.Resources.UnmetWater);
            Close(large.State.Resources.UnmetOxygen, small.State.Resources.UnmetOxygen);
        }

        [Test]
        public void OverflowAndWorkLimitFailAtomically()
        {
            var sim = new MissionSimulation(
                new MissionConfig(100, 0, new ResourceConfig(), targetSols: 0, oxygenOutageFailureSeconds: 0),
                new MissionScenario());
            var before = sim.State;
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.Tick(double.PositiveInfinity));
            Assert.That(sim.State, Is.SameAs(before));
            Assert.Throws<InvalidOperationException>(() => sim.Tick(20000000));
            Assert.That(sim.State, Is.SameAs(before));
        }

        [Test]
        public void FreeOxygenProductionWorksWithoutWaterAndStopsAtCapacity()
        {
            var sim = Create(new ResourceConfig(initialWater: 0, initialWastewater: 0, initialOxygen: 99,
                oxygenProductionRate: 1, oxygenWaterPerUnit: 0, oxygenEnergyPerUnit: 0,
                waterUsePerCrew: 0, oxygenUsePerCrew: 0));
            sim.Tick(10);
            Close(sim.State.Resources.Oxygen, 100);
            Close(sim.State.Resources.Water, 0);
        }

        [Test]
        public void DefaultsSurviveTwoSolsAndResearchToggleDoesNotCreateUnstaffedLoad()
        {
            var a = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var b = new MissionSimulation(new MissionConfig(), new MissionScenario());
            b.SetResearchEnabled(false);
            a.Tick(1440); b.Tick(1440);
            Assert.That(a.State.Resources.Oxygen, Is.GreaterThan(0));
            Close(a.State.Resources.UnmetOxygen, 0);
            Close(a.State.Resources.UnmetWater, 0);
            Close(a.State.Resources.Battery, b.State.Resources.Battery);
        }

        [Test]
        public void FractionalResourceAndCalendarBoundariesMakeProgress()
        {
            var random = new Random(43);
            for (int i = 0; i < 50; i++)
            {
                var cfg = new ResourceConfig(initialBattery: random.NextDouble() * 5, initialWater: random.NextDouble() * 5,
                    initialOxygen: random.NextDouble() * 5, initialWastewater: random.NextDouble() * 5,
                    solarRate: random.NextDouble() * .5, oxygenProductionRate: random.NextDouble() * .5,
                    oxygenEnergyPerUnit: 1, oxygenWaterPerUnit: random.NextDouble(), recyclingRate: random.NextDouble() * .5,
                    recyclingEnergyPerUnit: .5, waterUsePerCrew: random.NextDouble() * .05, oxygenUsePerCrew: random.NextDouble() * .1);
                var large = Create(cfg, 50);
                var small = Create(cfg, 50);
                large.Tick(300);
                for (int step = 0; step < 1000; step++) small.Tick(.3);
                var a = large.State.Resources; var b = small.State.Resources;
                Close(a.Battery, b.Battery); Close(a.Water, b.Water); Close(a.Oxygen, b.Oxygen);
                Close(a.Wastewater, b.Wastewater); Close(a.UnmetWater, b.UnmetWater); Close(a.UnmetOxygen, b.UnmetOxygen);
            }
        }
    }
}
