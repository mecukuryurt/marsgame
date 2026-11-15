using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class OperationsLongRunSoakTests
    {
        [TestCase(101)]
        [TestCase(202)]
        [TestCase(303)]
        public void OperationsRuns100SolsWithRandomChannelsEarthOrdersAndMidRunSaveLoadWithoutNaNOrBoundsViolation(int seed)
        {
            var cfg = MissionConfig.CreateOperationsDefault(operationsSeed: seed, enableOperationsRandomEvents: true);
            var sim = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            MissionSimulation loadedAtSol50 = null;

            double step = 10.0;
            double targetElapsed = 100.0 * cfg.SolSeconds;

            while (sim.State.ElapsedSeconds < targetElapsed && sim.State.Outcome == MissionOutcome.InProgress)
            {
                var st = sim.State;
                if (st.SolReportPauseRequested) sim.AcknowledgeSolReport();
                if (st.CriticalAlertPauseRequested) sim.AcknowledgeCriticalAlert();

                if (st.Resources.WaterExtractorFaulted && st.Resources.ScienceBalance >= 10.0)
                    sim.RepairWaterExtractorWithScience();

                sim.SetResearchEnabled(st.Resources.Battery > 60.0 && !st.Resources.OxygenProducerFaulted && st.Resources.Oxygen > 60.0);

                bool radiationActive = false;
                for (int ch = 0; ch < st.ChannelSchedules.Count; ch++)
                {
                    if (st.ChannelSchedules[ch].ChannelId == OperationsChannelId.Radiation &&
                        (st.ChannelSchedules[ch].IsActive || st.ChannelSchedules[ch].WarningIssued))
                    {
                        radiationActive = true;
                        break;
                    }
                }

                if (radiationActive && st.Crew[0].Location == CrewLocation.Outside && st.Crew[0].Status != CrewStatus.Returning)
                    sim.ReturnToShelter("crew-1");

                if (st.Crew[0].Status == CrewStatus.Idle || st.Crew[0].Status == CrewStatus.Sheltered || st.Crew[0].Status == CrewStatus.Exhausted)
                {
                    if (st.Crew[0].Stamina < 25 || st.Crew[0].Status == CrewStatus.Exhausted)
                    {
                        if (st.Crew[0].Location == CrewLocation.Outside) sim.ReturnToShelter("crew-1");
                        else sim.Rest("crew-1");
                    }
                    else if (st.Resources.OxygenProducerFaulted)
                    {
                        if (st.Crew[0].Location == CrewLocation.Outside) sim.ReturnToShelter("crew-1");
                        else sim.AssignTask("crew-1", "o2-repair");
                    }
                    else
                    {
                        bool panelFaulted = false;
                        for (int p = 0; p < st.Resources.Panels.Count; p++)
                            if (st.Resources.Panels[p].IsFaulted) { panelFaulted = true; break; }
                        if (panelFaulted && !radiationActive)
                            sim.AssignTask("crew-1", "panel-repair");
                        else if (st.Resources.PanelFouling > 0.20 && !radiationActive)
                            sim.AssignTask("crew-1", "panel-maintenance");
                        else if (st.Resources.SystemWear > 0.20)
                        {
                            if (st.Crew[0].Location == CrewLocation.Outside) sim.ReturnToShelter("crew-1");
                            else sim.AssignTask("crew-1", "system-maintenance");
                        }
                        else if (st.Crew[0].Location == CrewLocation.Outside)
                            sim.ReturnToShelter("crew-1");
                    }
                }
                if (st.Crew.Count > 1 && (st.Crew[1].Status == CrewStatus.Idle || st.Crew[1].Status == CrewStatus.Sheltered || st.Crew[1].Status == CrewStatus.Exhausted) && st.Crew[1].Location == CrewLocation.Inside)
                {
                    if (st.Crew[1].Stamina < 25 || st.Crew[1].Status == CrewStatus.Exhausted) sim.Rest("crew-2");
                    else sim.AssignTask("crew-2", "research");
                }

                sim.Tick(step);
                if (loadedAtSol50 == null && sim.State.SolNumber >= 50)
                {
                    string json = sim.SaveToJson();
                    Assert.That(MissionSimulation.TryLoadFromJson(json, out loadedAtSol50, out string err), Is.True, err);
                    Assert.That(loadedAtSol50.State.ElapsedSeconds, Is.EqualTo(sim.State.ElapsedSeconds).Within(1e-9));
                    // Actually run the remaining 50 sols from the restored state.
                    sim = loadedAtSol50;
                }

                // Invariants every step
                var r = sim.State.Resources;
                Assert.That(MissionConfig.IsFinite(r.Battery) && r.Battery >= 0 && r.Battery <= cfg.Resources.BatteryCapacity + 1e-9, Is.True);
                Assert.That(MissionConfig.IsFinite(r.Water) && r.Water >= 0 && r.Water <= cfg.Resources.WaterCapacity + 1e-9, Is.True);
                Assert.That(MissionConfig.IsFinite(r.Oxygen) && r.Oxygen >= 0 && r.Oxygen <= cfg.Resources.OxygenCapacity + 1e-9, Is.True);
                Assert.That(MissionConfig.IsFinite(r.Wastewater) && r.Wastewater >= 0 && r.Wastewater <= cfg.Resources.WastewaterCapacity + 1e-9, Is.True);
                Assert.That(MissionConfig.IsFinite(r.ScienceBalance) && r.ScienceBalance >= 0, Is.True);
                Assert.That(MissionConfig.IsFinite(r.ResearchProgress) && r.ResearchProgress >= 0, Is.True);
            }

            Assert.That(sim.State.Outcome, Is.EqualTo(MissionOutcome.InProgress),
                $"FailureReason={sim.State.FailureReason}, Sol={sim.State.SolNumber}, Bat={sim.State.Resources.Battery:F2}, O2={sim.State.Resources.Oxygen:F2}, Wear={sim.State.Resources.SystemWear:F2}, Fouling={sim.State.Resources.PanelFouling:F2}, O2Fault={sim.State.Resources.OxygenProducerFaulted}, Crew0={sim.State.Crew[0].Status}");
            Assert.That(sim.State.SolNumber, Is.GreaterThanOrEqualTo(100));
            Assert.That(sim.State.EarthOrders.Count, Is.EqualTo(50)); // Sol 3, 5, ..., 101

            var seenKinds = new HashSet<EarthOrderKind>();
            for (int i = 0; i < sim.State.EarthOrders.Count; i++)
                seenKinds.Add(sim.State.EarthOrders[i].Kind);
            Assert.That(seenKinds.Count, Is.EqualTo(7));
        }
    }
}
