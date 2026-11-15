using System;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsExpansionSaveTests
    {
        [Test]
        public void TrainingAndDemoDoNotLeakExpansionMapOrMaterials()
        {
            var training = new MissionSimulation(new MissionConfig(mode: MissionMode.Training), new MissionScenario());
            Assert.That(training.State.Map, Is.Null);
            Assert.That(training.State.Materials, Is.EqualTo(0));
            Assert.That(training.ClaimParcel(8, 4).Success, Is.False);

            var demo = new MissionSimulation(new MissionConfig(mode: MissionMode.Demo), new MissionScenario());
            Assert.That(demo.State.Map, Is.Null);
            Assert.That(demo.State.Materials, Is.EqualTo(0));
            Assert.That(demo.ClaimParcel(8, 4).Success, Is.False);
        }

        [Test]
        public void ClaimParcelEnforcesAtomicRulesAndPreservesOldSnapshots()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: Array.Empty<EventState>()));

            var initialSnapshot = sim.State;
            Assert.That(initialSnapshot.Materials, Is.EqualTo(40));
            Assert.That(initialSnapshot.MaterialCapacity, Is.EqualTo(80));
            Assert.That(initialSnapshot.LastMaterialSupplySol, Is.EqualTo(1));
            Assert.That(initialSnapshot.Map, Is.Not.Null);

            // Out of bounds
            Assert.That(sim.ClaimParcel(-1, 5).Success, Is.False);
            Assert.That(sim.ClaimParcel(16, 5).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(40));

            // Unexplored
            Assert.That(sim.ClaimParcel(0, 0).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(40));

            // Explored but not adjacent to any claimed cell
            Assert.That(sim.ClaimParcel(11, 8).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(40));

            // Already claimed starter cell (double-click / already owned)
            Assert.That(sim.ClaimParcel(ParcelMap.SolarWorkCoord).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(40));

            // Valid explored cell adjacent to AirlockCorridorCoord (8,5): (8,4)
            var claimOk = sim.ClaimParcel(8, 4);
            Assert.That(claimOk.Success, Is.True, claimOk.Reason);
            Assert.That(sim.State.Materials, Is.EqualTo(38));
            Assert.That(sim.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.True);

            // Previous snapshot must remain untouched
            Assert.That(initialSnapshot.Materials, Is.EqualTo(40));
            Assert.That(initialSnapshot.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.False);

            // Second claim on same cell fails atomically
            Assert.That(sim.ClaimParcel(8, 4).Success, Is.False);
            Assert.That(sim.State.Materials, Is.EqualTo(38));

            // Insufficient materials rejection
            var lowMatCfg = new MissionConfig(mode: MissionMode.Operations, initialMaterials: 1, materialCapacity: 80, dailyMaterialSupply: 8);
            var lowSim = new MissionSimulation(lowMatCfg, new MissionScenario(events: Array.Empty<EventState>()));
            Assert.That(lowSim.ClaimParcel(8, 4).Success, Is.False);
            Assert.That(lowSim.State.Materials, Is.EqualTo(1));
            Assert.That(lowSim.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.False);
        }

        [Test]
        public void DailyMaterialSupplyGrantsOncePerSolAndSurvivesSameSolLoad()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: Array.Empty<EventState>()));

            Assert.That(sim.ClaimParcel(8, 4).Success, Is.True);
            Assert.That(sim.State.Materials, Is.EqualTo(38));

            // Advance to Sol 2 (720s) -> +8 materials => 46, LastMaterialSupplySol == 2
            sim.Tick(720);
            Assert.That(sim.State.SolNumber, Is.EqualTo(2));
            Assert.That(sim.State.Materials, Is.EqualTo(46));
            Assert.That(sim.State.LastMaterialSupplySol, Is.EqualTo(2));

            // Save & load on same Sol (with pending Sol report) -> no duplicate supply
            string saveJson = sim.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(saveJson, out var loaded, out var err), Is.True, err);
            Assert.That(loaded.State.Materials, Is.EqualTo(46));
            Assert.That(loaded.State.LastMaterialSupplySol, Is.EqualTo(2));
            Assert.That(loaded.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.True);

            Assert.That(loaded.AcknowledgeSolReport().Success, Is.True);
            loaded.Tick(10);
            Assert.That(loaded.State.Materials, Is.EqualTo(46));

            // Advance to Sol 3 -> +8 once => 54
            loaded.Tick(710);
            Assert.That(loaded.State.SolNumber, Is.EqualTo(3));
            Assert.That(loaded.State.Materials, Is.EqualTo(54));
            Assert.That(loaded.State.LastMaterialSupplySol, Is.EqualTo(3));

            // Cap at 80
            for (int i = 0; i < 6; i++)
            {
                loaded.AcknowledgeSolReport();
                loaded.Tick(720);
            }
            Assert.That(loaded.State.Materials, Is.EqualTo(80));
        }

        [Test]
        public void SchemaV3ValidationRejectsUnsupportedRevisionAndAllowsFourPanels()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(enableOperationsRandomEvents: false),
                new MissionScenario(events: Array.Empty<EventState>()));
            string v3Json = sim.SaveToJson();
            Assert.That(v3Json, Does.Contain("\"schemaVersion\":3"));
            Assert.That(v3Json, Does.Contain($"\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision}"));

            // Future revision rejected cleanly
            string badRev = v3Json.Replace($"\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision}", "\"expansionRevision\":99");
            Assert.That(MissionSimulation.TryLoadFromJson(badRev, out var badSim, out var badErr), Is.False);
            Assert.That(badSim, Is.Null);
            Assert.That(badErr, Does.Contain("99"));

            // Adding a 4th panel unit in JSON is valid (starter panel-1..3 check, <=12 max)
            string panelMarker = "\"panels\":[";
            int pIdx = v3Json.IndexOf(panelMarker, StringComparison.Ordinal);
            Assert.That(pIdx, Is.GreaterThan(0));
            int pInsert = pIdx + panelMarker.Length;
            string fourPanelsJson = v3Json.Insert(pInsert, "{\"id\":\"panel-4\",\"outputWeight\":0.3333333333333333,\"faulted\":false},");
            Assert.That(fourPanelsJson, Is.Not.EqualTo(v3Json));
            Assert.That(MissionSimulation.TryLoadFromJson(fourPanelsJson, out var fourSim, out var fourErr), Is.True, fourErr);
            Assert.That(fourSim.State.Resources.Panels.Count, Is.EqualTo(4));

            // Missing starter panel-1 is rejected
            string missingStarterPanel = v3Json.Replace("\"id\":\"panel-1\"", "\"id\":\"panel-x\"");
            Assert.That(MissionSimulation.TryLoadFromJson(missingStarterPanel, out _, out _), Is.False);

            // Restart resets materials and map cleanly
            sim.ClaimParcel(8, 4);
            sim.Restart();
            Assert.That(sim.State.Materials, Is.EqualTo(40));
            Assert.That(sim.State.LastMaterialSupplySol, Is.EqualTo(1));
            Assert.That(sim.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.False);
        }

        [Test]
        public void LegacyV1AndV2SavesMigrateIdempotentlyWithoutFreePanelsOrExtractors()
        {
            var sim = new MissionSimulation(
                MissionConfig.CreateOperationsDefault(operationsSeed: 5150, enableOperationsRandomEvents: true),
                new MissionScenario(events: Array.Empty<EventState>()));

            // Advance into a Sol 2 hazard window + Sol report
            var radChannel = sim.State.ChannelSchedules[(int)OperationsChannelId.Radiation];
            sim.Tick(Math.Max(720.0, radChannel.NextTriggerAtSeconds + 5.0));
            double expectedWaterRate = sim.State.Resources.LastWaterExtractionRate;
            long currentSol = sim.State.SolNumber;
            int eventCount = sim.State.Events.Count;

            // Craft a V2 legacy save by stripping V3 fields and setting schemaVersion=2
            string v3Json = sim.SaveToJson();
            int mapIdx = v3Json.IndexOf(",\"materials\":", StringComparison.Ordinal);
            Assert.That(mapIdx, Is.GreaterThan(0));
            string v2Json = v3Json.Substring(0, mapIdx) + "}";
            v2Json = v2Json
                .Replace($"\"schemaVersion\":3,\"expansionRevision\":{OperationsSaveCodec.CurrentExpansionRevision},", "\"schemaVersion\":2,");

            Assert.That(MissionSimulation.TryLoadFromJson(v2Json, out var migratedFromV2, out var errV2), Is.True, errV2);
            Assert.That(migratedFromV2.State.Materials, Is.EqualTo(40));
            Assert.That(migratedFromV2.State.LastMaterialSupplySol, Is.EqualTo(currentSol));
            Assert.That(migratedFromV2.State.Map, Is.Not.Null);
            Assert.That(migratedFromV2.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(migratedFromV2.State.Resources.LastWaterExtractionRate, Is.EqualTo(expectedWaterRate).Within(1e-12));
            Assert.That(migratedFromV2.State.Events.Count, Is.EqualTo(eventCount));
            Assert.That(migratedFromV2.State.PendingSolReport, Is.Not.Null);

            // Craft a V1 legacy save (schemaVersion=1, no config block, no events array -> reconstructed from active channel)
            int cfgStart = v2Json.IndexOf("\"config\":", StringComparison.Ordinal);
            int modeStart = v2Json.IndexOf("},\"mode\":", StringComparison.Ordinal);
            Assert.That(modeStart, Is.GreaterThan(cfgStart));
            string v1NoConfig = v2Json.Remove(cfgStart, (modeStart + 2) - cfgStart);
            int evStart = v1NoConfig.IndexOf(",\"events\":[", StringComparison.Ordinal);
            int solRepStart = v1NoConfig.IndexOf(",\"solReports\":[", StringComparison.Ordinal);
            string v1Json = v1NoConfig.Remove(evStart, solRepStart - evStart)
                .Replace("\"schemaVersion\":2,", "\"schemaVersion\":1,");
            Assert.That(MissionSimulation.TryLoadFromJson(v1Json, out var migratedFromV1, out var errV1), Is.True, errV1);
            Assert.That(migratedFromV1.State.Materials, Is.EqualTo(40));
            Assert.That(migratedFromV1.State.LastMaterialSupplySol, Is.EqualTo(currentSol));
            Assert.That(migratedFromV1.State.Resources.Panels.Count, Is.EqualTo(3));
            Assert.That(migratedFromV1.State.Events.Count, Is.GreaterThanOrEqualTo(1));

            // Idempotence: save migrated V2 as V3, reload, verify no extra material grant or RNG drift
            migratedFromV2.ClaimParcel(8, 4);
            string reSavedV3 = migratedFromV2.SaveToJson();
            Assert.That(MissionSimulation.TryLoadFromJson(reSavedV3, out var reloadedV3, out var errV3), Is.True, errV3);
            Assert.That(reloadedV3.State.Materials, Is.EqualTo(38));
            Assert.That(reloadedV3.State.LastMaterialSupplySol, Is.EqualTo(currentSol));
            Assert.That(reloadedV3.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.True);

            for (int i = 0; i < 5; i++)
            {
                Assert.That(reloadedV3.State.ChannelSchedules[i].RngState, Is.EqualTo(sim.State.ChannelSchedules[i].RngState));
                Assert.That(reloadedV3.State.ChannelSchedules[i].NextTriggerAtSeconds, Is.EqualTo(sim.State.ChannelSchedules[i].NextTriggerAtSeconds));
            }
        }
    }
}
