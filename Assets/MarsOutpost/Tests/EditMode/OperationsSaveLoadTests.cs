using System;
using System.IO;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsSaveLoadTests
    {
        [Test]
        public void CustomOperationsClockAndResourceRatesSurviveLoad()
        {
            var cfg = new MissionConfig(solSeconds: 100, daySeconds: 70, targetSols: 0,
                mode: MissionMode.Operations, earthOrderArrivalIntervalSols: 2,
                resources: new ResourceConfig(solarRate: .7, initialBattery: 40, initialOxygen: 90, waterExtractionRate: .03));
            var sim = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            sim.Tick(210); sim.AcknowledgeSolReport();
            Assert.That(MissionSimulation.TryLoadFromJson(sim.SaveToJson(), out var loaded, out var error), Is.True, error);
            Assert.That(loaded.State.SolNumber, Is.EqualTo(3));
            sim.Tick(35); loaded.Tick(35);
            Assert.That(loaded.State.Resources.Battery, Is.EqualTo(sim.State.Resources.Battery).Within(1e-9));
            Assert.That(loaded.State.Resources.Oxygen, Is.EqualTo(sim.State.Resources.Oxygen).Within(1e-9));
        }

        [TestCase(OperationsChannelId.Radiation, false)]
        [TestCase(OperationsChannelId.Radiation, true)]
        [TestCase(OperationsChannelId.DustStorm, false)]
        [TestCase(OperationsChannelId.DustStorm, true)]
        public void SaveDuringHazardPreservesWarningActiveEffectsAndFutureTrajectory(OperationsChannelId id, bool active)
        {
            var sim = new MissionSimulation(MissionConfig.CreateOperationsDefault(),
                new MissionScenario(events: Array.Empty<EventState>()));
            var channel = sim.State.ChannelSchedules[(int)id];
            double target = channel.NextTriggerAtSeconds - (active ? -1 : 30);
            sim.Tick(target);
            Assert.That(sim.State.Events.Count, Is.GreaterThan(0));
            Assert.That(MissionSimulation.TryLoadFromJson(sim.SaveToJson(), out var loaded, out var error), Is.True, error);
            Assert.That(loaded.State.Events.Count, Is.EqualTo(sim.State.Events.Count));
            for (int i = 0; i < sim.State.Events.Count; i++)
            {
                Assert.That(loaded.State.Events[i].Phase, Is.EqualTo(sim.State.Events[i].Phase));
                Assert.That(loaded.State.Events[i].EndSeconds, Is.EqualTo(sim.State.Events[i].EndSeconds));
            }
            sim.AcknowledgeSolReport(); loaded.AcknowledgeSolReport();
            sim.AcknowledgeCriticalAlert(); loaded.AcknowledgeCriticalAlert();
            sim.Tick(20); loaded.Tick(20);
            Assert.That(loaded.State.Crew[0].RiskScore, Is.EqualTo(sim.State.Crew[0].RiskScore).Within(1e-9));
            Assert.That(loaded.State.Resources.Battery, Is.EqualTo(sim.State.Resources.Battery).Within(1e-9));
        }

        [Test]
        public void SaveAtSolReportRestoresReportAndCanResumeAfterAcknowledgement()
        {
            var sim = new MissionSimulation(MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: Array.Empty<EventState>()));
            sim.Tick(720);
            Assert.That(sim.State.PendingSolReport, Is.Not.Null);
            Assert.That(MissionSimulation.TryLoadFromJson(sim.SaveToJson(), out var loaded, out var error), Is.True, error);
            Assert.That(loaded.State.PendingSolReport, Is.Not.Null);
            Assert.That(loaded.State.SolReports.Count, Is.EqualTo(sim.State.SolReports.Count));
            Assert.That(loaded.State.PendingSolReport.EndBattery, Is.EqualTo(sim.State.PendingSolReport.EndBattery));
            Assert.That(loaded.AcknowledgeSolReport().Success, Is.True);
            loaded.Tick(1);
            Assert.That(loaded.State.ElapsedSeconds, Is.EqualTo(721));
        }

        [Test]
        public void NonFiniteOrOutOfRangeResourceSnapshotIsRejectedBeforeReplacingSimulation()
        {
            var sim = new MissionSimulation(MissionConfig.CreateOperationsDefault(), new MissionScenario(events: Array.Empty<EventState>()));
            string json = sim.SaveToJson();
            foreach (string bad in new[] { "-1", "101", "\"NaN\"", "\"Infinity\"" })
            {
                string corrupt = json.Replace("\"battery\":50", "\"battery\":" + bad);
                Assert.That(corrupt, Is.Not.EqualTo(json));
                Assert.That(MissionSimulation.TryLoadFromJson(corrupt, out var loaded, out var error), Is.False, bad);
                Assert.That(loaded, Is.Null);
                Assert.That(error, Is.Not.Empty);
            }
        }

        [Test]
        public void OperationsSaveLoadPreservesExactStatePrngAndDeterminismWithZeroOfflineJump()
        {
            var cfg = MissionConfig.CreateOperationsDefault(operationsSeed: 4242, enableOperationsRandomEvents: true);
            var sim = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));

            // Advance into Sol 3 with active tasks, orders, research/science, and channel states
            sim.AssignTask("crew-2", "research");
            for (int i = 0; i < 30; i++)
            {
                sim.Tick(120);
                if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
            }

            Assert.That(sim.State.SolNumber, Is.GreaterThanOrEqualTo(3));
            Assert.That(sim.State.Resources.ScienceBalance, Is.GreaterThan(0));
            Assert.That(sim.State.EarthOrders.Count, Is.GreaterThanOrEqualTo(1));

            var order = sim.State.EarthOrders[0];
            sim.AcceptEarthOrder(order.OrderId);
            sim.AssignEarthOrder(order.OrderId, "crew-1");
            sim.Tick(15);
            if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
            if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();

            string tmpDir = Path.Combine(Path.GetTempPath(), "marsgame-saveload-tests");
            Directory.CreateDirectory(tmpDir);
            string savePath = Path.Combine(tmpDir, "ops_save.json");
            if (File.Exists(savePath)) File.Delete(savePath);

            try
            {
                // Atomic save to file
                Assert.That(sim.SaveToFile(savePath).Success, Is.True);
                Assert.That(File.Exists(savePath), Is.True);
                Assert.That(File.Exists(savePath + ".tmp"), Is.False);

                // Load back -> exact elapsed time (zero offline jump) and identical future trajectory
                Assert.That(MissionSimulation.TryLoadFromFile(savePath, out var loaded, out string err), Is.True, err);
                Assert.That(loaded.State.ElapsedSeconds, Is.EqualTo(sim.State.ElapsedSeconds).Within(1e-9));
                Assert.That(loaded.State.Resources.ScienceBalance, Is.EqualTo(sim.State.Resources.ScienceBalance).Within(1e-9));
                Assert.That(loaded.State.Resources.Battery, Is.EqualTo(sim.State.Resources.Battery).Within(1e-9));
                Assert.That(loaded.State.EarthOrders.Count, Is.EqualTo(sim.State.EarthOrders.Count));
                Assert.That(loaded.State.EarthOrders[0].StepProgressSeconds, Is.EqualTo(sim.State.EarthOrders[0].StepProgressSeconds).Within(1e-9));

                // Tick both for 600s -> deterministic match across PRNG channels, orders, resources
                for (int i = 0; i < 10; i++)
                {
                    sim.Tick(60);
                    loaded.Tick(60);
                    if (sim.State.SolReportPauseRequested) sim.AcknowledgeSolReport();
                    if (loaded.State.SolReportPauseRequested) loaded.AcknowledgeSolReport();
                    if (sim.State.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();
                    if (loaded.State.CriticalAlertPauseRequested) loaded.AcknowledgeCriticalAlert();
                }

                Assert.That(loaded.State.Resources.Battery, Is.EqualTo(sim.State.Resources.Battery).Within(1e-9));
                Assert.That(loaded.State.Resources.Water, Is.EqualTo(sim.State.Resources.Water).Within(1e-9));
                Assert.That(loaded.State.Resources.Oxygen, Is.EqualTo(sim.State.Resources.Oxygen).Within(1e-9));
                Assert.That(loaded.State.Resources.ScienceBalance, Is.EqualTo(sim.State.Resources.ScienceBalance).Within(1e-9));
                Assert.That(loaded.State.EarthOrders.Count, Is.EqualTo(sim.State.EarthOrders.Count));
                for (int i = 0; i < sim.State.ChannelSchedules.Count; i++)
                {
                    Assert.That(loaded.State.ChannelSchedules[i].RngState, Is.EqualTo(sim.State.ChannelSchedules[i].RngState));
                    Assert.That(loaded.State.ChannelSchedules[i].NextTriggerAtSeconds, Is.EqualTo(sim.State.ChannelSchedules[i].NextTriggerAtSeconds));
                }
            }
            finally
            {
                if (File.Exists(savePath)) File.Delete(savePath);
            }
        }

        [Test]
        public void CorruptOrUnsupportedSaveJsonIsRejectedCleanly()
        {
            Assert.That(MissionSimulation.TryLoadFromJson("{bad json", out var sim1, out string err1), Is.False);
            Assert.That(sim1, Is.Null);
            Assert.That(err1, Is.Not.Empty);

            Assert.That(MissionSimulation.TryLoadFromJson("{\"schemaVersion\":999}", out var sim2, out string err2), Is.False);
            Assert.That(sim2, Is.Null);
            Assert.That(err2, Does.Contain("999"));
        }
    }
}
