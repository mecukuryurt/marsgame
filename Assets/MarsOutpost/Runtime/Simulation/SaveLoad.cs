using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MarsOutpost
{
    internal static class OperationsSaveCodec
    {
        public const int CurrentSchemaVersion = 3;
        public const int CurrentExpansionRevision = 4;

        public static string Serialize(MissionConfig config, double elapsedSeconds, double timeCompensation, SimulationMutableState sim)
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Prop("schemaVersion", CurrentSchemaVersion);
            w.Prop("expansionRevision", CurrentExpansionRevision);
            WriteConfig(w, config);
            w.Prop("mode", (int)config.Mode);
            w.Prop("operationsSeed", config.OperationsSeed);
            w.Prop("enableOperationsRandomEvents", config.EnableOperationsRandomEvents);
            w.Prop("elapsedSeconds", elapsedSeconds);
            w.Prop("timeCompensation", timeCompensation);
            w.Prop("savedAtUtcTicks", DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            w.Prop("outcome", (int)sim.Outcome);
            w.Prop("failureReason", (int)sim.FailureReason);
            w.Prop("statusMessage", sim.StatusMessage ?? "");
            w.Prop("isSleeping", sim.IsSleeping);
            w.Prop("canTriggerLowOxygenAlert", sim.CanTriggerLowOxygenAlert);
            w.Prop("criticalAlertPauseRequested", sim.CriticalAlertPauseRequested);
            w.Prop("activeAlertMessage", sim.ActiveAlertMessage ?? "");
            w.Prop("lowOxygenAlertCount", sim.LowOxygenAlertCount);
            w.Prop("solReportPauseRequested", sim.SolReportPauseRequested);
            w.Prop("foodRationsRemaining", sim.FoodRationsRemaining);
            w.Prop("mealPreparedThisSol", sim.MealPreparedThisSol);
            w.Prop("nightMealSettledSol", sim.NightMealSettledSol);
            w.Prop("nightRationAvailable", sim.NightRationAvailable);
            w.Prop("sleepStartedThisSol", sim.SleepStartedThisSol);
            w.Prop("nightSleepRecoveryFactor", sim.NightSleepRecoveryFactor);
            w.Prop("earthAdviceStatus", (int)sim.EarthAdviceStatus);
            w.Prop("earthAdviceRequestedAtSeconds", sim.EarthAdviceRequestedAtSeconds);
            w.Prop("earthAdviceExpectedAtSeconds", sim.EarthAdviceExpectedAtSeconds);
            w.Prop("earthAdviceDeliveredAtSeconds", sim.EarthAdviceDeliveredAtSeconds);
            w.Prop("earthAdviceMessage", sim.EarthAdviceMessage ?? "");

            // Resources
            w.Key("resources");
            w.BeginObject();
            var r = sim.Resources;
            w.Prop("battery", r.Battery);
            w.Prop("water", r.Water);
            w.Prop("oxygen", r.Oxygen);
            w.Prop("wastewater", r.Wastewater);
            w.Prop("unmetWater", r.UnmetWater);
            w.Prop("unmetOxygen", r.UnmetOxygen);
            w.Prop("researchEnabled", r.ResearchEnabled);
            w.Prop("researchProgress", r.ResearchProgress);
            w.Prop("scienceBalance", r.ScienceBalance);
            w.Prop("panelFouling", r.PanelFouling);
            w.Prop("systemWear", r.SystemWear);
            w.Prop("atmosphericDustLoss", r.AtmosphericDustLoss);
            w.Prop("lastResearchRateFactor", r.LastResearchRateFactor);
            w.Prop("continuousOxygenOutageSeconds", r.ContinuousOxygenOutageSeconds);
            w.Prop("waterExtractorEnabled", r.WaterExtractorEnabled);
            w.Prop("waterExtractorFaulted", r.WaterExtractorFaulted);
            w.Prop("oxygenProducerFaulted", r.OxygenProducerFaulted);
            w.Prop("lastWaterExtractionRate", r.LastWaterExtractionRate);
            w.Prop("lastLegacyWaterExtractionRate", r.LastLegacyWaterExtractionRate);
            w.Prop("lastSiteWaterExtractionRate", r.LastSiteWaterExtractionRate);
            w.Prop("lastRecycledWaterRate", r.LastRecycledWaterRate);
            w.Prop("totalExtractedWater", r.TotalExtractedWater);
            w.Prop("totalLegacyExtractedWater", r.TotalLegacyExtractedWater);
            w.Prop("totalSiteExtractedWater", r.TotalSiteExtractedWater);
            w.Prop("totalRecycledWater", r.TotalRecycledWater);
            w.Prop("batteryOverflowLostTotal", r.BatteryOverflowLostTotal);
            w.Prop("waterOverflowLostTotal", r.WaterOverflowLostTotal);
            w.Key("panels");
            w.BeginArray();
            if (r.Panels != null)
            {
                for (int i = 0; i < r.Panels.Count; i++)
                {
                    var p = r.Panels[i];
                    w.BeginObject();
                    w.Prop("id", p.Id);
                    w.Prop("outputWeight", p.Weight);
                    w.Prop("faulted", p.IsFaulted);
                    w.Prop("facilityId", p.FacilityId);
                    w.Prop("solarFactor", p.SolarFactor);
                    w.Prop("repairProgressSeconds", p.RepairProgressSeconds);
                    w.EndObject();
                }
            }
            w.EndArray();
            w.EndObject();

            // Crew
            w.Key("crew");
            w.BeginArray();
            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var c = sim.Crew[i];
                w.BeginObject();
                w.Prop("id", c.Id);
                w.Prop("name", c.Name);
                w.Prop("location", (int)c.Location);
                w.Prop("status", (int)c.Status);
                w.Prop("stamina", c.Stamina);
                w.Prop("riskScore", c.RiskScore);
                w.Prop("activeTaskId", c.ActiveTaskId);
                w.Prop("returnRemainingSeconds", c.ReturnRemainingSeconds);
                w.Prop("activeSurfaceJobId", c.ActiveSurfaceJobId);
                w.Prop("surfacePosX", c.SurfacePosX);
                w.Prop("surfacePosZ", c.SurfacePosZ);
                w.Prop("hasSurfacePosition", c.HasSurfacePosition);
                w.EndObject();
            }
            w.EndArray();

            // Tasks
            w.Key("tasks");
            w.BeginArray();
            for (int i = 0; i < sim.Tasks.Count; i++)
            {
                var t = sim.Tasks[i];
                w.BeginObject();
                w.Prop("id", t.Id);
                w.Prop("progressSeconds", t.ProgressSeconds);
                w.Prop("isCompleted", t.IsCompleted);
                w.Prop("rewardApplied", t.RewardApplied);
                w.EndObject();
            }
            w.EndArray();

            // Events must survive a load during a warning or active hazard.
            w.Key("events"); w.BeginArray();
            foreach (var ev in sim.Events)
            {
                w.BeginObject();
                w.Prop("id", ev.Id); w.Prop("title", ev.Title); w.Prop("kind", (int)ev.Kind);
                w.Prop("warningSeconds", ev.WarningSeconds); w.Prop("startSeconds", ev.StartSeconds);
                w.Prop("endSeconds", ev.EndSeconds); w.Prop("intensity", ev.Intensity);
                w.Prop("sourceRecordId", ev.SourceRecordId); w.Prop("sourceNote", ev.SourceNote);
                w.Prop("phase", (int)ev.Phase); w.Prop("warningTriggered", ev.WarningTriggered);
                w.Prop("startTriggered", ev.StartTriggered); w.Prop("endTriggered", ev.EndTriggered);
                w.EndObject();
            }
            w.EndArray();
            w.Key("solReports"); w.BeginArray();
            foreach (var report in sim.SolReports) WriteReport(w, report);
            w.EndArray();
            w.Prop("pendingReportSol", sim.PendingSolReport != null ? sim.PendingSolReport.SolNumber : 0L);

            // SolTracker
            w.Key("solTracker");
            w.BeginObject();
            var st = sim.SolTracker;
            w.Prop("solNumber", st.SolNumber);
            w.Prop("startBattery", st.StartBattery);
            w.Prop("startWater", st.StartWater);
            w.Prop("startOxygen", st.StartOxygen);
            w.Prop("startResearch", st.StartResearch);
            w.Key("startStamina");
            w.BeginObject();
            foreach (var kv in st.StartStamina)
                w.Prop(kv.Key, kv.Value);
            w.EndObject();
            w.Key("completedTasks");
            w.BeginArray();
            for (int i = 0; i < st.CompletedTasks.Count; i++) w.Value(st.CompletedTasks[i]);
            w.EndArray();
            w.Key("eventsExperienced");
            w.BeginArray();
            for (int i = 0; i < st.EventsExperienced.Count; i++) w.Value(st.EventsExperienced[i]);
            w.EndArray();
            w.EndObject();

            // ChannelSchedules
            w.Key("channelSchedules");
            w.BeginArray();
            if (sim.ChannelSchedules != null)
            {
                for (int i = 0; i < sim.ChannelSchedules.Count; i++)
                {
                    var ch = sim.ChannelSchedules[i];
                    w.BeginObject();
                    w.Prop("channelId", (int)ch.ChannelId);
                    w.Prop("rngState", ch.RngState.ToString(CultureInfo.InvariantCulture));
                    w.Prop("nextTriggerAtSeconds", ch.NextTriggerAtSeconds);
                    w.Prop("warningLeadSeconds", ch.WarningLeadSeconds);
                    w.Prop("warningIssued", ch.WarningIssued);
                    w.Prop("isActive", ch.IsActive);
                    w.Prop("activeUntilSeconds", ch.ActiveUntilSeconds);
                    w.Prop("activeTargetId", ch.ActiveTargetId);
                    w.EndObject();
                }
            }
            w.EndArray();

            // EarthOrderBag
            w.Key("earthOrderBag");
            if (sim.EarthOrderBag == null)
            {
                w.NullValue();
            }
            else
            {
                w.BeginObject();
                w.Prop("rngState", sim.EarthOrderBag.RngState.ToString(CultureInfo.InvariantCulture));
                w.Prop("issuedCount", sim.EarthOrderBag.IssuedCount);
                w.Key("remainingBag");
                w.BeginArray();
                for (int i = 0; i < sim.EarthOrderBag.RemainingBag.Count; i++)
                    w.Value((int)sim.EarthOrderBag.RemainingBag[i]);
                w.EndArray();
                w.EndObject();
            }

            // EarthOrders
            w.Key("earthOrders");
            w.BeginArray();
            if (sim.EarthOrders != null)
            {
                for (int i = 0; i < sim.EarthOrders.Count; i++)
                {
                    var o = sim.EarthOrders[i];
                    w.BeginObject();
                    w.Prop("orderId", o.OrderId);
                    w.Prop("kind", (int)o.Kind);
                    w.Prop("code", o.Code);
                    w.Prop("title", o.Title);
                    w.Prop("status", (int)o.Status);
                    w.Prop("arrivalSol", o.ArrivalSol);
                    w.Prop("advisoryDeadlineSol", o.AdvisoryDeadlineSol);
                    w.Prop("currentStepIndex", o.CurrentStepIndex);
                    w.Prop("stepProgressSeconds", o.StepProgressSeconds);
                    w.Prop("stepRequiredSeconds", o.StepRequiredSeconds);
                    w.Prop("assignedCrewId", o.AssignedCrewId);
                    w.Prop("completionSummary", o.CompletionSummary ?? "");
                    w.Prop("upfrontWaterPaid", o.UpfrontWaterPaid);
                    w.EndObject();
                }
            }
            w.EndArray();

            // OperationsLog
            w.Key("operationsLog");
            w.BeginArray();
            if (sim.OperationsLog != null)
            {
                for (int i = 0; i < sim.OperationsLog.Count; i++)
                    w.Value(sim.OperationsLog[i]);
            }
            w.EndArray();

            // Expansion V3 (Revision 1+)
            w.Prop("materials", sim.Materials);
            w.Prop("lastMaterialSupplySol", sim.LastMaterialSupplySol);
            w.Prop("nextEntityId", sim.NextEntityId);
            w.Prop("materialsRefundedTotal", sim.MaterialsRefundedTotal);
            w.Prop("materialsForfeitedTotal", sim.MaterialsForfeitedTotal);
            w.Prop("buildEnergyLostTotal", sim.BuildEnergyLostTotal);
            WriteParcelMap(w, sim.Map);
            WriteSurfaceJobs(w, sim.SurfaceJobs);
            WriteFacilities(w, sim.Facilities);

            w.EndObject();
            return w.ToString();
        }

        public static bool TryDeserialize(string json, out MissionSimulation simulation, out string error)
        {
            simulation = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Save data is empty.";
                return false;
            }

            Dictionary<string, object> root;
            try
            {
                var parsed = JsonReader.Parse(json);
                root = parsed as Dictionary<string, object>;
                if (root == null)
                {
                    error = "Save root must be a JSON object.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"Invalid save JSON: {ex.Message}";
                return false;
            }

            try
            {
                int schema = GetInt(root, "schemaVersion", -1);
                if (schema != 1 && schema != 2 && schema != CurrentSchemaVersion)
                {
                    error = $"Unsupported save schema version: {schema}.";
                    return false;
                }
                if (schema >= 3)
                {
                    int rev = GetInt(root, "expansionRevision", -1);
                    if (rev < 1 || rev > CurrentExpansionRevision)
                    {
                        error = $"Unsupported expansion revision: {rev}.";
                        return false;
                    }
                }

                var mode = (MissionMode)GetInt(root, "mode", (int)MissionMode.Operations);
                if (mode != MissionMode.Operations)
                {
                    error = $"Save/load only supports Operations mode (got {mode}).";
                    return false;
                }

                int seed = GetInt(root, "operationsSeed", 1);
                bool randomEvents = GetBool(root, "enableOperationsRandomEvents", true);
                double elapsed = GetDouble(root, "elapsedSeconds", 0);
                double timeComp = GetDouble(root, "timeCompensation", 0);
                if (!MissionConfig.IsFinite(elapsed) || elapsed < 0)
                {
                    error = "Invalid elapsedSeconds in save.";
                    return false;
                }

                if (!MissionConfig.IsFinite(timeComp)) throw new InvalidDataException("Invalid time compensation.");
                var cfgObj = GetObject(root, "config");
                if (schema >= 2 && cfgObj == null) throw new InvalidDataException("Missing config block.");
                var cfg = cfgObj != null ? ReadConfig(cfgObj) : MissionConfig.CreateOperationsDefault(operationsSeed: seed, enableOperationsRandomEvents: randomEvents);
                var loaded = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
                loaded.ApplyLoadedSnapshot(root, elapsed, timeComp);
                simulation = loaded;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to restore save state: {ex.Message}";
                return false;
            }
        }

        internal static void ApplyToState(Dictionary<string, object> root, SimulationMutableState sim, MissionConfig config, double elapsed)
        {
            sim.Outcome = (MissionOutcome)GetInt(root, "outcome", (int)MissionOutcome.InProgress);
            sim.FailureReason = (MissionFailureReason)GetInt(root, "failureReason", (int)MissionFailureReason.None);
            sim.StatusMessage = GetString(root, "statusMessage", "");
            sim.IsSleeping = GetBool(root, "isSleeping", false);
            sim.CanTriggerLowOxygenAlert = GetBool(root, "canTriggerLowOxygenAlert", true);
            sim.CriticalAlertPauseRequested = GetBool(root, "criticalAlertPauseRequested", false);
            sim.ActiveAlertMessage = GetString(root, "activeAlertMessage", "");
            sim.LowOxygenAlertCount = GetInt(root, "lowOxygenAlertCount", 0);
            sim.SolReportPauseRequested = GetBool(root, "solReportPauseRequested", false);
            sim.FoodRationsRemaining = GetInt(root, "foodRationsRemaining", 0);
            sim.MealPreparedThisSol = GetBool(root, "mealPreparedThisSol", false);
            sim.NightMealSettledSol = GetLong(root, "nightMealSettledSol", 0);
            sim.NightRationAvailable = GetBool(root, "nightRationAvailable", false);
            sim.SleepStartedThisSol = GetBool(root, "sleepStartedThisSol", false);
            sim.NightSleepRecoveryFactor = GetDouble(root, "nightSleepRecoveryFactor", 1.0);
            sim.EarthAdviceStatus = (EarthAdviceStatus)GetInt(root, "earthAdviceStatus", 0);
            sim.EarthAdviceRequestedAtSeconds = GetDouble(root, "earthAdviceRequestedAtSeconds", double.NaN);
            sim.EarthAdviceExpectedAtSeconds = GetDouble(root, "earthAdviceExpectedAtSeconds", double.NaN);
            sim.EarthAdviceDeliveredAtSeconds = GetDouble(root, "earthAdviceDeliveredAtSeconds", double.NaN);
            sim.EarthAdviceMessage = GetString(root, "earthAdviceMessage", "");

            var eventArray = GetArray(root, "events");
            sim.Events.Clear();
            if (eventArray != null)
            {
                foreach (Dictionary<string, object> e in eventArray)
                {
                    var ev = new EventState(GetString(e, "id", ""), GetString(e, "title", ""),
                        (EventKind)GetInt(e, "kind", 0), GetDouble(e, "warningSeconds", 0),
                        GetDouble(e, "startSeconds", 0), GetDouble(e, "endSeconds", 0), GetDouble(e, "intensity", 0),
                        GetString(e, "sourceRecordId", ""), GetString(e, "sourceNote", ""));
                    ev.Phase = (EventPhase)GetInt(e, "phase", 0);
                    ev.WarningTriggered = GetBool(e, "warningTriggered", false);
                    ev.StartTriggered = GetBool(e, "startTriggered", false);
                    ev.EndTriggered = GetBool(e, "endTriggered", false);
                    sim.Events.Add(ev);
                }
            }
            sim.SolReports.Clear();
            var reports = GetArray(root, "solReports");
            if (reports != null)
                foreach (Dictionary<string, object> report in reports) sim.SolReports.Add(ReadReport(report));
            long pendingSol = GetLong(root, "pendingReportSol", 0);
            sim.PendingSolReport = sim.SolReports.Find(report => report.SolNumber == pendingSol);
            // Legacy v1 did not save reports; remove its orphaned report pause.
            if (GetInt(root, "schemaVersion", 1) == 1 && sim.PendingSolReport == null)
                sim.SolReportPauseRequested = false;

            // Resources
            var resObj = GetObject(root, "resources");
            if (resObj == null) throw new InvalidDataException("Missing resources block.");
            var r = sim.Resources;
            r.Battery = GetDouble(resObj, "battery", r.Battery);
            r.Water = GetDouble(resObj, "water", r.Water);
            r.Oxygen = GetDouble(resObj, "oxygen", r.Oxygen);
            r.Wastewater = GetDouble(resObj, "wastewater", r.Wastewater);
            r.UnmetWater = GetDouble(resObj, "unmetWater", 0);
            r.UnmetOxygen = GetDouble(resObj, "unmetOxygen", 0);
            r.ResearchEnabled = GetBool(resObj, "researchEnabled", true);
            r.ResearchProgress = GetDouble(resObj, "researchProgress", 0);
            r.ScienceBalance = GetDouble(resObj, "scienceBalance", 0);
            r.PanelFouling = GetDouble(resObj, "panelFouling", 0);
            r.SystemWear = GetDouble(resObj, "systemWear", 0);
            r.AtmosphericDustLoss = GetDouble(resObj, "atmosphericDustLoss", 0);
            r.LastResearchRateFactor = GetDouble(resObj, "lastResearchRateFactor", 0);
            r.ContinuousOxygenOutageSeconds = GetDouble(resObj, "continuousOxygenOutageSeconds", 0);
            r.WaterExtractorEnabled = GetBool(resObj, "waterExtractorEnabled", true);
            r.WaterExtractorFaulted = GetBool(resObj, "waterExtractorFaulted", false);
            r.OxygenProducerFaulted = GetBool(resObj, "oxygenProducerFaulted", false);
            r.LastWaterExtractionRate = GetDouble(resObj, "lastWaterExtractionRate", 0);
            r.LastLegacyWaterExtractionRate = GetDouble(resObj, "lastLegacyWaterExtractionRate", r.LastWaterExtractionRate);
            r.LastSiteWaterExtractionRate = GetDouble(resObj, "lastSiteWaterExtractionRate", 0);
            r.LastRecycledWaterRate = GetDouble(resObj, "lastRecycledWaterRate", 0);
            r.TotalExtractedWater = GetDouble(resObj, "totalExtractedWater", 0);
            r.TotalLegacyExtractedWater = GetDouble(resObj, "totalLegacyExtractedWater", 0);
            r.TotalSiteExtractedWater = GetDouble(resObj, "totalSiteExtractedWater", 0);
            r.TotalRecycledWater = GetDouble(resObj, "totalRecycledWater", 0);
            r.BatteryOverflowLostTotal = GetDouble(resObj, "batteryOverflowLostTotal", 0);
            r.WaterOverflowLostTotal = GetDouble(resObj, "waterOverflowLostTotal", 0);

            var panelsArr = GetArray(resObj, "panels");
            if (panelsArr != null)
            {
                var panels = new List<PanelUnitState>(panelsArr.Count);
                for (int i = 0; i < panelsArr.Count; i++)
                {
                    var pObj = (Dictionary<string, object>)panelsArr[i];
                    panels.Add(new PanelUnitState(
                        GetString(pObj, "id", $"panel-{i + 1}"),
                        GetDouble(pObj, "outputWeight", PanelUnitState.DefaultUnitWeight),
                        GetBool(pObj, "faulted", false),
                        GetNullableString(pObj, "facilityId"),
                        GetDouble(pObj, "solarFactor", 1.0),
                        GetDouble(pObj, "repairProgressSeconds", 0)));
                }
                r.Panels = panels;
            }

            // Crew
            var crewArr = GetArray(root, "crew");
            if (crewArr == null || crewArr.Count != sim.Crew.Count)
                throw new InvalidDataException("Invalid crew array.");
            for (int i = 0; i < crewArr.Count; i++)
            {
                var cObj = (Dictionary<string, object>)crewArr[i];
                string cid = GetString(cObj, "id", null);
                CrewState target = null;
                for (int j = 0; j < sim.Crew.Count; j++)
                    if (sim.Crew[j].Id == cid) { target = sim.Crew[j]; break; }
                if (target == null) throw new InvalidDataException($"Unknown crew id '{cid}'.");
                target.Location = (CrewLocation)GetInt(cObj, "location", (int)CrewLocation.Inside);
                target.Status = (CrewStatus)GetInt(cObj, "status", (int)CrewStatus.Idle);
                target.Stamina = GetDouble(cObj, "stamina", target.Stamina);
                target.RiskScore = GetDouble(cObj, "riskScore", 0);
                target.ActiveTaskId = GetNullableString(cObj, "activeTaskId");
                target.ReturnRemainingSeconds = GetDouble(cObj, "returnRemainingSeconds", 0);
                target.ActiveSurfaceJobId = GetNullableString(cObj, "activeSurfaceJobId");
                target.SurfacePosX = GetDouble(cObj, "surfacePosX", 0);
                target.SurfacePosZ = GetDouble(cObj, "surfacePosZ", 0);
                target.HasSurfacePosition = GetBool(cObj, "hasSurfacePosition", false);
            }

            // Tasks
            var tasksArr = GetArray(root, "tasks");
            if (tasksArr != null)
            {
                for (int i = 0; i < tasksArr.Count; i++)
                {
                    var tObj = (Dictionary<string, object>)tasksArr[i];
                    string tid = GetString(tObj, "id", null);
                    for (int j = 0; j < sim.Tasks.Count; j++)
                    {
                        if (sim.Tasks[j].Id == tid)
                        {
                            sim.Tasks[j].ProgressSeconds = GetDouble(tObj, "progressSeconds", 0);
                            sim.Tasks[j].IsCompleted = GetBool(tObj, "isCompleted", false);
                            sim.Tasks[j].RewardApplied = GetBool(tObj, "rewardApplied", sim.Tasks[j].IsCompleted);
                            break;
                        }
                    }
                }
            }

            // SolTracker
            var stObj = GetObject(root, "solTracker");
            if (stObj != null)
            {
                sim.SolTracker.SolNumber = GetLong(stObj, "solNumber", 1);
                sim.SolTracker.StartBattery = GetDouble(stObj, "startBattery", 0);
                sim.SolTracker.StartWater = GetDouble(stObj, "startWater", 0);
                sim.SolTracker.StartOxygen = GetDouble(stObj, "startOxygen", 0);
                sim.SolTracker.StartResearch = GetDouble(stObj, "startResearch", 0);
                sim.SolTracker.StartStamina.Clear();
                var stamObj = GetObject(stObj, "startStamina");
                if (stamObj != null)
                {
                    foreach (var kv in stamObj)
                        sim.SolTracker.StartStamina[kv.Key] = ToDouble(kv.Value, 0);
                }
                sim.SolTracker.CompletedTasks.Clear();
                var compArr = GetArray(stObj, "completedTasks");
                if (compArr != null)
                    for (int i = 0; i < compArr.Count; i++)
                        sim.SolTracker.CompletedTasks.Add(Convert.ToString(compArr[i], CultureInfo.InvariantCulture));
                sim.SolTracker.EventsExperienced.Clear();
                var evArr = GetArray(stObj, "eventsExperienced");
                if (evArr != null)
                    for (int i = 0; i < evArr.Count; i++)
                        sim.SolTracker.EventsExperienced.Add(Convert.ToString(evArr[i], CultureInfo.InvariantCulture));
            }

            // ChannelSchedules
            var chArr = GetArray(root, "channelSchedules");
            if (chArr != null)
            {
                var list = new List<ChannelScheduleState>(chArr.Count);
                for (int i = 0; i < chArr.Count; i++)
                {
                    var chObj = (Dictionary<string, object>)chArr[i];
                    list.Add(new ChannelScheduleState(
                        (OperationsChannelId)GetInt(chObj, "channelId", 0),
                        GetULong(chObj, "rngState", 1UL),
                        GetDouble(chObj, "nextTriggerAtSeconds", double.PositiveInfinity),
                        GetDouble(chObj, "warningLeadSeconds", 0),
                        GetBool(chObj, "warningIssued", false),
                        GetBool(chObj, "isActive", false),
                        GetDouble(chObj, "activeUntilSeconds", double.PositiveInfinity),
                        GetNullableString(chObj, "activeTargetId")));
                }
                sim.ChannelSchedules = list;
            }

            // EarthOrderBag
            var bagObj = GetObject(root, "earthOrderBag");
            if (bagObj != null)
            {
                ulong bagRng = GetULong(bagObj, "rngState", 1UL);
                int issued = GetInt(bagObj, "issuedCount", 0);
                var remArr = GetArray(bagObj, "remainingBag");
                var bagList = new List<EarthOrderKind>();
                if (remArr != null)
                    for (int i = 0; i < remArr.Count; i++)
                        bagList.Add((EarthOrderKind)Convert.ToInt32(remArr[i], CultureInfo.InvariantCulture));
                sim.EarthOrderBag = new EarthOrderBagState(bagRng, bagList, issued);
            }

            // EarthOrders
            var ordersArr = GetArray(root, "earthOrders");
            if (ordersArr != null)
            {
                var orders = new List<EarthOrderState>(ordersArr.Count);
                for (int i = 0; i < ordersArr.Count; i++)
                {
                    var oObj = (Dictionary<string, object>)ordersArr[i];
                    orders.Add(new EarthOrderState(
                        GetString(oObj, "orderId", ""),
                        (EarthOrderKind)GetInt(oObj, "kind", 0),
                        GetString(oObj, "code", ""),
                        GetString(oObj, "title", ""),
                        (EarthOrderStatus)GetInt(oObj, "status", 0),
                        GetLong(oObj, "arrivalSol", 3),
                        GetLong(oObj, "advisoryDeadlineSol", 5),
                        GetInt(oObj, "currentStepIndex", 0),
                        GetDouble(oObj, "stepProgressSeconds", 0),
                        GetDouble(oObj, "stepRequiredSeconds", 60),
                        GetNullableString(oObj, "assignedCrewId"),
                        GetString(oObj, "completionSummary", ""),
                        GetBool(oObj, "upfrontWaterPaid", false)));
                }
                sim.EarthOrders = orders;
            }

            // OperationsLog
            var logArr = GetArray(root, "operationsLog");
            if (logArr != null)
            {
                var logs = new List<string>(logArr.Count);
                for (int i = 0; i < logArr.Count; i++)
                    logs.Add(Convert.ToString(logArr[i], CultureInfo.InvariantCulture) ?? "");
                sim.OperationsLog = logs;
            }
            if (eventArray == null && GetInt(root, "schemaVersion", 1) == 1)
            {
                foreach (var ch in sim.ChannelSchedules)
                {
                    if (!OperationsScheduler.IsTimedChannel(ch.ChannelId) || (!ch.IsActive && !ch.WarningIssued)) continue;
                    bool dust = ch.ChannelId == OperationsChannelId.DustStorm;
                    double start = ch.IsActive ? elapsed : ch.NextTriggerAtSeconds;
                    var ev = new EventState(dust ? "ops-dust" : "ops-radiation", dust ? "Regional Dust Storm" : "Solar Radiation Surge",
                        dust ? EventKind.Dust : EventKind.Radiation, Math.Max(0, start - 60), start,
                        ch.IsActive ? ch.ActiveUntilSeconds : start + 120, dust ? .8 : 1);
                    ev.WarningTriggered = true; ev.StartTriggered = ch.IsActive;
                    ev.Phase = ch.IsActive ? EventPhase.Active : EventPhase.Warning;
                    sim.Events.Add(ev);
                }
            }

            int schemaVer = GetInt(root, "schemaVersion", 1);
            if (schemaVer >= 3)
            {
                sim.Materials = GetInt(root, "materials", config.InitialMaterials);
                sim.LastMaterialSupplySol = GetLong(root, "lastMaterialSupplySol", sim.SolTracker.SolNumber);
                sim.NextEntityId = GetInt(root, "nextEntityId", 1);
                sim.MaterialsRefundedTotal = GetInt(root, "materialsRefundedTotal", 0);
                sim.MaterialsForfeitedTotal = GetInt(root, "materialsForfeitedTotal", 0);
                sim.BuildEnergyLostTotal = GetDouble(root, "buildEnergyLostTotal", 0);
                var mapObj = GetObject(root, "map") ?? throw new InvalidDataException("Missing parcel map in v3 save.");
                sim.Map = ReadParcelMap(mapObj);
                int rev = GetInt(root, "expansionRevision", 1);
                var jobsArr = GetArray(root, "surfaceJobs");
                sim.SurfaceJobs = rev >= 2 && jobsArr != null
                    ? ReadSurfaceJobs(jobsArr)
                    : new List<SurfaceJobState>();
                var facsArr = GetArray(root, "facilities");
                sim.Facilities = rev >= 3 && facsArr != null
                    ? ReadFacilities(facsArr)
                    : FacilityState.CreateStarterFacilities();
            }
            else
            {
                // Legacy v1/v2 migration: derive map independently, preserve V2 channels/resources/reports, grant initial materials once on current Sol.
                sim.Map = ParcelMap.Generate(ParcelMap.DeriveMapSeed(config.OperationsSeed));
                sim.Materials = config.InitialMaterials;
                sim.LastMaterialSupplySol = sim.SolTracker.SolNumber;
                sim.NextEntityId = 1;
                sim.SurfaceJobs = new List<SurfaceJobState>();
                sim.Facilities = FacilityState.CreateStarterFacilities();
            }

            NormalizePanelsAfterLoad(sim);
            UtilityNetwork.RefreshTopology(sim);
            ValidateSnapshot(sim, config, elapsed);
        }

        private static void NormalizePanelsAfterLoad(SimulationMutableState sim)
        {
            var panels = sim?.Resources?.Panels;
            if (panels == null)
                return;

            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                if (!string.IsNullOrEmpty(p.FacilityId))
                    continue;

                if (p.Id == "panel-1" || p.Id == "panel-2" || p.Id == "panel-3")
                {
                    p.FacilityId = ParcelMap.StarterSolarSpec.FacilityId;
                    p.SolarFactor = 1.0;
                    continue;
                }

                var owner = SolarProduction.FindOwningFacility(p, sim.Facilities);
                if (owner != null)
                {
                    p.FacilityId = owner.FacilityId;
                    if (sim.Map != null && sim.Map.TryGetCell(owner.Origin, out var cell))
                        p.SolarFactor = cell.SolarFactor;
                }
            }
        }

        private static void WriteConfig(JsonWriter w, MissionConfig c)
        {
            w.Key("config"); w.BeginObject();
            w.Prop("solSeconds", c.SolSeconds);
            w.Prop("daySeconds", c.DaySeconds);
            w.Prop("returnToShelterSeconds", c.ReturnToShelterSeconds);
            w.Prop("returnStaminaRate", c.ReturnStaminaRate);
            w.Prop("restStaminaRate", c.RestStaminaRate);
            w.Prop("lowStaminaWarningThreshold", c.LowStaminaWarningThreshold);
            w.Prop("maxStamina", c.MaxStamina);
            w.Prop("researchGoal", c.ResearchGoal);
            w.Prop("targetSols", c.TargetSols);
            w.Prop("insideRadiationRiskRate", c.InsideRadiationRiskRate);
            w.Prop("outsideRadiationRiskRate", c.OutsideRadiationRiskRate);
            w.Prop("maxCrewRiskScore", c.MaxCrewRiskScore);
            w.Prop("oxygenOutageFailureSeconds", c.OxygenOutageFailureSeconds);
            w.Prop("sleepStaminaRate", c.SleepStaminaRate);
            w.Prop("sleepTimeScale", c.SleepTimeScale);
            w.Prop("lowOxygenWarningThreshold", c.LowOxygenWarningThreshold);
            w.Prop("unpreppedSleepFactor", c.UnpreppedSleepFactor);
            w.Prop("commsDelaySeconds", c.CommsDelaySeconds);
            w.Prop("initialFoodRations", c.InitialFoodRations);
            w.Prop("maxReserveMeals", c.MaxReserveMeals);
            w.Prop("dailyReserveMealSupply", c.DailyReserveMealSupply);
            w.Prop("operationsSeed", c.OperationsSeed);
            w.Prop("enableOperationsRandomEvents", c.EnableOperationsRandomEvents);
            w.Prop("acuteRiskRecoveryRate", c.AcuteRiskRecoveryRate);
            w.Prop("earthOrderArrivalIntervalSols", c.EarthOrderArrivalIntervalSols);
            w.Prop("mode", (int)c.Mode);
            w.Prop("initialMaterials", c.InitialMaterials);
            w.Prop("materialCapacity", c.MaterialCapacity);
            w.Prop("dailyMaterialSupply", c.DailyMaterialSupply);
            w.Key("resources"); w.BeginObject();
            w.Prop("batteryCapacity", c.Resources.BatteryCapacity);
            w.Prop("waterCapacity", c.Resources.WaterCapacity);
            w.Prop("oxygenCapacity", c.Resources.OxygenCapacity);
            w.Prop("wastewaterCapacity", c.Resources.WastewaterCapacity);
            w.Prop("initialBattery", c.Resources.InitialBattery);
            w.Prop("initialWater", c.Resources.InitialWater);
            w.Prop("initialOxygen", c.Resources.InitialOxygen);
            w.Prop("initialWastewater", c.Resources.InitialWastewater);
            w.Prop("solarRate", c.Resources.SolarRate);
            w.Prop("oxygenProductionRate", c.Resources.OxygenProductionRate);
            w.Prop("oxygenEnergyPerUnit", c.Resources.OxygenEnergyPerUnit);
            w.Prop("oxygenWaterPerUnit", c.Resources.OxygenWaterPerUnit);
            w.Prop("recyclingRate", c.Resources.RecyclingRate);
            w.Prop("recyclingEnergyPerUnit", c.Resources.RecyclingEnergyPerUnit);
            w.Prop("waterUsePerCrew", c.Resources.WaterUsePerCrew);
            w.Prop("oxygenUsePerCrew", c.Resources.OxygenUsePerCrew);
            w.Prop("researchEnergyRate", c.Resources.ResearchEnergyRate);
            w.Prop("researchProgressRate", c.Resources.ResearchProgressRate);
            w.Prop("initialPanelFouling", c.Resources.InitialPanelFouling);
            w.Prop("panelMaintenanceReduction", c.Resources.PanelMaintenanceReduction);
            w.Prop("initialSystemWear", c.Resources.InitialSystemWear);
            w.Prop("systemMaintenanceReduction", c.Resources.SystemMaintenanceReduction);
            w.Prop("waterExtractionRate", c.Resources.WaterExtractionRate);
            w.Prop("waterExtractorEnergyRate", c.Resources.WaterExtractorEnergyRate);
            w.Prop("oxygenRepairEnergyRate", c.Resources.OxygenRepairEnergyRate);
            w.EndObject(); w.EndObject();
        }

        private static MissionConfig ReadConfig(Dictionary<string, object> o)
        {
            var defaults = MissionConfig.CreateOperationsDefault();
            var r = GetObject(o, "resources") ?? throw new InvalidDataException("Missing resource config.");
            var rd = defaults.Resources;
            var resources = new ResourceConfig(
                batteryCapacity: GetDouble(r, "batteryCapacity", rd.BatteryCapacity),
                waterCapacity: GetDouble(r, "waterCapacity", rd.WaterCapacity),
                oxygenCapacity: GetDouble(r, "oxygenCapacity", rd.OxygenCapacity),
                wastewaterCapacity: GetDouble(r, "wastewaterCapacity", rd.WastewaterCapacity),
                initialBattery: GetDouble(r, "initialBattery", rd.InitialBattery),
                initialWater: GetDouble(r, "initialWater", rd.InitialWater),
                initialOxygen: GetDouble(r, "initialOxygen", rd.InitialOxygen),
                initialWastewater: GetDouble(r, "initialWastewater", rd.InitialWastewater),
                solarRate: GetDouble(r, "solarRate", rd.SolarRate),
                oxygenProductionRate: GetDouble(r, "oxygenProductionRate", rd.OxygenProductionRate),
                oxygenEnergyPerUnit: GetDouble(r, "oxygenEnergyPerUnit", rd.OxygenEnergyPerUnit),
                oxygenWaterPerUnit: GetDouble(r, "oxygenWaterPerUnit", rd.OxygenWaterPerUnit),
                recyclingRate: GetDouble(r, "recyclingRate", rd.RecyclingRate),
                recyclingEnergyPerUnit: GetDouble(r, "recyclingEnergyPerUnit", rd.RecyclingEnergyPerUnit),
                waterUsePerCrew: GetDouble(r, "waterUsePerCrew", rd.WaterUsePerCrew),
                oxygenUsePerCrew: GetDouble(r, "oxygenUsePerCrew", rd.OxygenUsePerCrew),
                researchEnergyRate: GetDouble(r, "researchEnergyRate", rd.ResearchEnergyRate),
                researchProgressRate: GetDouble(r, "researchProgressRate", rd.ResearchProgressRate),
                initialPanelFouling: GetDouble(r, "initialPanelFouling", rd.InitialPanelFouling),
                panelMaintenanceReduction: GetDouble(r, "panelMaintenanceReduction", rd.PanelMaintenanceReduction),
                initialSystemWear: GetDouble(r, "initialSystemWear", rd.InitialSystemWear),
                systemMaintenanceReduction: GetDouble(r, "systemMaintenanceReduction", rd.SystemMaintenanceReduction),
                waterExtractionRate: GetDouble(r, "waterExtractionRate", rd.WaterExtractionRate),
                waterExtractorEnergyRate: GetDouble(r, "waterExtractorEnergyRate", rd.WaterExtractorEnergyRate),
                oxygenRepairEnergyRate: GetDouble(r, "oxygenRepairEnergyRate", rd.OxygenRepairEnergyRate));
            return new MissionConfig(
                solSeconds: GetDouble(o, "solSeconds", defaults.SolSeconds),
                daySeconds: GetDouble(o, "daySeconds", defaults.DaySeconds),
                resources: resources,
                returnToShelterSeconds: GetDouble(o, "returnToShelterSeconds", defaults.ReturnToShelterSeconds),
                returnStaminaRate: GetDouble(o, "returnStaminaRate", defaults.ReturnStaminaRate),
                restStaminaRate: GetDouble(o, "restStaminaRate", defaults.RestStaminaRate),
                lowStaminaWarningThreshold: GetDouble(o, "lowStaminaWarningThreshold", defaults.LowStaminaWarningThreshold),
                maxStamina: GetDouble(o, "maxStamina", defaults.MaxStamina),
                researchGoal: GetDouble(o, "researchGoal", defaults.ResearchGoal),
                targetSols: GetInt(o, "targetSols", defaults.TargetSols),
                insideRadiationRiskRate: GetDouble(o, "insideRadiationRiskRate", defaults.InsideRadiationRiskRate),
                outsideRadiationRiskRate: GetDouble(o, "outsideRadiationRiskRate", defaults.OutsideRadiationRiskRate),
                maxCrewRiskScore: GetDouble(o, "maxCrewRiskScore", defaults.MaxCrewRiskScore),
                oxygenOutageFailureSeconds: GetDouble(o, "oxygenOutageFailureSeconds", defaults.OxygenOutageFailureSeconds),
                sleepStaminaRate: GetDouble(o, "sleepStaminaRate", defaults.SleepStaminaRate),
                sleepTimeScale: GetDouble(o, "sleepTimeScale", defaults.SleepTimeScale),
                lowOxygenWarningThreshold: GetDouble(o, "lowOxygenWarningThreshold", defaults.LowOxygenWarningThreshold),
                unpreppedSleepFactor: GetDouble(o, "unpreppedSleepFactor", defaults.UnpreppedSleepFactor),
                commsDelaySeconds: GetDouble(o, "commsDelaySeconds", defaults.CommsDelaySeconds),
                initialFoodRations: GetInt(o, "initialFoodRations", defaults.InitialFoodRations),
                maxReserveMeals: GetInt(o, "maxReserveMeals", defaults.MaxReserveMeals),
                dailyReserveMealSupply: GetInt(o, "dailyReserveMealSupply", defaults.DailyReserveMealSupply),
                operationsSeed: GetInt(o, "operationsSeed", defaults.OperationsSeed),
                enableOperationsRandomEvents: GetBool(o, "enableOperationsRandomEvents", defaults.EnableOperationsRandomEvents),
                acuteRiskRecoveryRate: GetDouble(o, "acuteRiskRecoveryRate", defaults.AcuteRiskRecoveryRate),
                earthOrderArrivalIntervalSols: GetInt(o, "earthOrderArrivalIntervalSols", defaults.EarthOrderArrivalIntervalSols),
                mode: (MissionMode)GetInt(o, "mode", (int)defaults.Mode),
                initialMaterials: GetInt(o, "initialMaterials", defaults.InitialMaterials),
                materialCapacity: GetInt(o, "materialCapacity", defaults.MaterialCapacity),
                dailyMaterialSupply: GetInt(o, "dailyMaterialSupply", defaults.DailyMaterialSupply));
        }

        private static void WriteReport(JsonWriter w, SolReport r)
        {
            w.BeginObject();
            w.Prop("SolNumber", r.SolNumber);
            w.Prop("StartBattery", r.StartBattery);
            w.Prop("EndBattery", r.EndBattery);
            w.Prop("StartWater", r.StartWater);
            w.Prop("EndWater", r.EndWater);
            w.Prop("StartOxygen", r.StartOxygen);
            w.Prop("EndOxygen", r.EndOxygen);
            w.Prop("ResearchGained", r.ResearchGained);
            w.Prop("CauseEffectSummary", r.CauseEffectSummary);
            w.Key("CrewStaminaDeltas"); w.BeginObject();
            foreach (var kv in r.CrewStaminaDeltas) w.Prop(kv.Key, kv.Value);
            w.EndObject();
            w.Key("CompletedTasks"); w.BeginArray(); foreach (var text in r.CompletedTasks) w.Value(text); w.EndArray();
            w.Key("EventsExperienced"); w.BeginArray(); foreach (var text in r.EventsExperienced) w.Value(text); w.EndArray();
            w.EndObject();
        }

        private static SolReport ReadReport(Dictionary<string, object> o)
        {
            var deltas = new Dictionary<string, double>();
            var deltaObj = GetObject(o, "CrewStaminaDeltas");
            if (deltaObj != null) foreach (var kv in deltaObj) deltas[kv.Key] = ToDouble(kv.Value, 0);
            return new SolReport(GetLong(o, "SolNumber", 0),
                GetDouble(o, "StartBattery", 0), GetDouble(o, "EndBattery", 0),
                GetDouble(o, "StartWater", 0), GetDouble(o, "EndWater", 0),
                GetDouble(o, "StartOxygen", 0), GetDouble(o, "EndOxygen", 0), GetDouble(o, "ResearchGained", 0),
                deltas, ReadStrings(o, "CompletedTasks"), ReadStrings(o, "EventsExperienced"), GetString(o, "CauseEffectSummary", ""));
        }

        private static List<string> ReadStrings(Dictionary<string, object> o, string key)
        {
            var result = new List<string>();
            var items = GetArray(o, key);
            if (items != null) foreach (var item in items) result.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
            return result;
        }

        private static void CheckRange(double value, double max, string name)
        {
            if (!MissionConfig.IsFinite(value) || value < 0 || value > max)
                throw new InvalidDataException($"Invalid {name} in save.");
        }

        private static void ValidateSnapshot(SimulationMutableState sim, MissionConfig c, double elapsed)
        {
            var r = sim.Resources;
            double effectiveBatteryCap = ColonyStorage.GetEffectiveBatteryCapacity(c.Resources, sim.Facilities);
            CheckRange(r.Battery, effectiveBatteryCap, "battery");
            CheckRange(r.BatteryOverflowLostTotal, double.MaxValue, "battery overflow ledger");
            CheckRange(r.WaterOverflowLostTotal, double.MaxValue, "water overflow ledger");
            CheckRange(r.LastWaterExtractionRate, double.MaxValue, "water extraction rate");
            CheckRange(r.LastLegacyWaterExtractionRate, double.MaxValue, "legacy water extraction rate");
            CheckRange(r.LastSiteWaterExtractionRate, double.MaxValue, "site water extraction rate");
            CheckRange(r.LastRecycledWaterRate, double.MaxValue, "recycled water rate");
            CheckRange(r.TotalExtractedWater, double.MaxValue, "total extracted water");
            CheckRange(r.TotalLegacyExtractedWater, double.MaxValue, "total legacy extracted water");
            CheckRange(r.TotalSiteExtractedWater, double.MaxValue, "total site extracted water");
            CheckRange(r.TotalRecycledWater, double.MaxValue, "total recycled water");
            CheckRange(r.Water, c.Resources.WaterCapacity, "water");
            CheckRange(r.Oxygen, c.Resources.OxygenCapacity, "oxygen");
            CheckRange(r.Wastewater, c.Resources.WastewaterCapacity, "wastewater");
            CheckRange(r.ResearchProgress, double.MaxValue, "research");
            CheckRange(r.ScienceBalance, r.ResearchProgress, "science balance");
            CheckRange(r.UnmetWater, double.MaxValue, "unmet water");
            CheckRange(r.UnmetOxygen, double.MaxValue, "unmet oxygen");
            CheckRange(r.ContinuousOxygenOutageSeconds, double.MaxValue, "oxygen outage");
            CheckRange(r.PanelFouling, 1, "panel fouling"); CheckRange(r.SystemWear, 1, "system wear");
            CheckRange(r.AtmosphericDustLoss, 1, "dust loss");
            if (c.Mode != MissionMode.Operations || !Enum.IsDefined(typeof(MissionOutcome), sim.Outcome) ||
                !Enum.IsDefined(typeof(MissionFailureReason), sim.FailureReason)) throw new InvalidDataException("Invalid mission state.");
            if (sim.FoodRationsRemaining < 0 || sim.SolTracker.SolNumber != (long)Math.Floor(elapsed / c.SolSeconds) + 1)
                throw new InvalidDataException("Invalid calendar or food state.");
            if (sim.SolReportPauseRequested && sim.PendingSolReport == null) throw new InvalidDataException("Missing pending Sol report.");
            var ids = new HashSet<string>();
            foreach (var member in sim.Crew)
            {
                if (!ids.Add(member.Id) || !Enum.IsDefined(typeof(CrewStatus), member.Status) ||
                    !Enum.IsDefined(typeof(CrewLocation), member.Location)) throw new InvalidDataException("Invalid crew state.");
                CheckRange(member.Stamina, c.MaxStamina, "crew stamina");
                CheckRange(member.RiskScore, c.MaxCrewRiskScore, "crew risk");
                CheckRange(member.ReturnRemainingSeconds, c.ReturnToShelterSeconds, "return time");
            }
            ids.Clear();
            foreach (var panel in r.Panels)
            {
                if (string.IsNullOrEmpty(panel.Id) || !ids.Add(panel.Id)) throw new InvalidDataException("Invalid panel IDs.");
                CheckRange(panel.Weight, 1, "panel weight");
                CheckRange(panel.SolarFactor, 2.0, "panel solar factor");
                CheckRange(panel.RepairProgressSeconds, PanelUnitState.DefaultRepairSeconds, "panel repair progress");
            }
            if (r.Panels.Count < 3 || r.Panels.Count > 12 ||
                !ids.Contains("panel-1") || !ids.Contains("panel-2") || !ids.Contains("panel-3"))
            {
                throw new InvalidDataException("Invalid panel set (starter panels panel-1..3 required, max 12).");
            }
            if (sim.Materials < 0 || sim.Materials > c.MaterialCapacity)
                throw new InvalidDataException("Invalid materials balance.");
            if (sim.LastMaterialSupplySol < 1 || sim.LastMaterialSupplySol > sim.SolTracker.SolNumber)
                throw new InvalidDataException("Invalid material supply Sol marker.");
            if (sim.NextEntityId < 1)
                throw new InvalidDataException("Invalid nextEntityId.");
            if (sim.MaterialsRefundedTotal < 0 || sim.MaterialsForfeitedTotal < 0)
                throw new InvalidDataException("Invalid material refund/forfeit ledgers.");
            CheckRange(sim.BuildEnergyLostTotal, double.MaxValue, "build energy lost ledger");
            ValidateParcelMap(sim.Map);
            ValidateSurfaceJobs(sim);
            ValidateFacilities(sim);
            var channelIds = new HashSet<OperationsChannelId>();
            foreach (var channel in sim.ChannelSchedules)
            {
                if (!Enum.IsDefined(typeof(OperationsChannelId), channel.ChannelId) || !channelIds.Add(channel.ChannelId))
                    throw new InvalidDataException("Invalid event channels.");
                if (!channel.IsActive) CheckRange(channel.NextTriggerAtSeconds, double.MaxValue, "next event time");
                else if (OperationsScheduler.IsTimedChannel(channel.ChannelId))
                    CheckRange(channel.ActiveUntilSeconds, double.MaxValue, "active event end");
            }
            if (sim.ChannelSchedules.Count != (c.EnableOperationsRandomEvents ? 5 : 0)) throw new InvalidDataException("Missing event channels.");
            ids.Clear();
            foreach (var order in sim.EarthOrders)
            {
                if (string.IsNullOrEmpty(order.OrderId) || !ids.Add(order.OrderId) ||
                    !Enum.IsDefined(typeof(EarthOrderKind), order.Kind) || !Enum.IsDefined(typeof(EarthOrderStatus), order.Status) ||
                    order.CurrentStepIndex < 0 || order.CurrentStepIndex >= EarthOrderBagState.GetStepCount(order.Kind))
                    throw new InvalidDataException("Invalid Earth order.");
                double required = EarthOrderBagState.GetStepSpec(order.Kind, order.CurrentStepIndex).DurationSeconds;
                if (order.StepRequiredSeconds != required) throw new InvalidDataException("Invalid order duration.");
                CheckRange(order.StepProgressSeconds, required, "order progress");
            }
            foreach (var task in sim.Tasks) CheckRange(task.ProgressSeconds, task.RequiredSeconds, "task progress");
        }

        private static void WriteParcelMap(JsonWriter w, ParcelMap map)
        {
            w.Key("map");
            if (map == null)
            {
                w.NullValue();
                return;
            }
            w.BeginObject();
            w.Prop("mapSeed", map.MapSeed);
            w.Prop("width", map.Config.Width);
            w.Prop("height", map.Config.Height);
            w.Prop("cellSize", map.Config.CellSize);
            w.Prop("originX", map.Config.OriginX);
            w.Prop("originZ", map.Config.OriginZ);
            w.Key("cells");
            w.BeginArray();
            for (int i = 0; i < map.Cells.Count; i++)
            {
                var c = map.Cells[i];
                w.BeginObject();
                w.Prop("x", c.Coord.X);
                w.Prop("z", c.Coord.Z);
                w.Prop("t", (int)c.Terrain);
                w.Prop("p", c.IsPassable);
                w.Prop("b", c.IsBuildable);
                w.Prop("pr", (int)c.Protection);
                w.Prop("sf", c.SolarFactor);
                w.Prop("ex", c.IsExplored);
                w.Prop("sc", c.IsScanned);
                w.Prop("cl", c.IsClaimed);
                w.Prop("ln", c.HasUtilityLine);
                w.Prop("ubid", c.UtilityBlueprintId);
                w.Prop("fid", c.FacilityId);
                w.Prop("sfid", c.StarterFacilityId);
                w.Prop("rk", (int)c.ResourceKind);
                w.Prop("yf", c.YieldFactor);
                w.Prop("rw", c.RemainingWater);
                w.EndObject();
            }
            w.EndArray();
            w.EndObject();
        }

        private static ParcelMap ReadParcelMap(Dictionary<string, object> o)
        {
            int seed = GetInt(o, "mapSeed", 0);
            int width = GetInt(o, "width", ParcelMapConfig.DefaultWidth);
            int height = GetInt(o, "height", ParcelMapConfig.DefaultHeight);
            double cellSize = GetDouble(o, "cellSize", ParcelMapConfig.DefaultCellSize);
            double originX = GetDouble(o, "originX", ParcelMapConfig.DefaultOriginX);
            double originZ = GetDouble(o, "originZ", ParcelMapConfig.DefaultOriginZ);
            var cfg = new ParcelMapConfig(width, height, cellSize, originX, originZ);
            var cellsArr = GetArray(o, "cells") ?? throw new InvalidDataException("Missing map cells array.");
            if (cellsArr.Count != width * height)
                throw new InvalidDataException("Map cell count mismatch.");

            var grid = new ParcelState[width, height];
            for (int i = 0; i < cellsArr.Count; i++)
            {
                var cObj = cellsArr[i] as Dictionary<string, object> ?? throw new InvalidDataException("Invalid cell entry.");
                int x = GetInt(cObj, "x", -1);
                int z = GetInt(cObj, "z", -1);
                var coord = new GridCoord(x, z);
                if (!cfg.IsInBounds(coord) || grid[x, z] != null)
                    throw new InvalidDataException($"Duplicate or out-of-bounds parcel {coord}.");

                grid[x, z] = new ParcelState(
                    coord,
                    (ParcelTerrainKind)GetInt(cObj, "t", 0),
                    GetBool(cObj, "p", false),
                    GetBool(cObj, "b", false),
                    (StarterProtectionKind)GetInt(cObj, "pr", 0),
                    GetDouble(cObj, "sf", 1.0),
                    GetBool(cObj, "ex", false),
                    GetBool(cObj, "sc", false),
                    GetBool(cObj, "cl", false),
                    GetBool(cObj, "ln", false),
                    GetNullableString(cObj, "fid"),
                    GetNullableString(cObj, "sfid"),
                    (ParcelResourceKind)GetInt(cObj, "rk", 0),
                    GetDouble(cObj, "yf", 0),
                    GetDouble(cObj, "rw", 0),
                    GetNullableString(cObj, "ubid"));
            }

            return new ParcelMap(seed, cfg, grid);
        }

        private static void ValidateParcelMap(ParcelMap map)
        {
            if (map == null || map.Cells == null || map.Cells.Count != map.Config.Width * map.Config.Height)
                throw new InvalidDataException("Missing or incomplete parcel map.");
            for (int i = 0; i < map.Cells.Count; i++)
            {
                var c = map.Cells[i];
                if (!Enum.IsDefined(typeof(ParcelTerrainKind), c.Terrain) ||
                    !Enum.IsDefined(typeof(ParcelResourceKind), c.ResourceKind) ||
                    !Enum.IsDefined(typeof(StarterProtectionKind), c.Protection))
                    throw new InvalidDataException("Invalid parcel enum value.");
                CheckRange(c.SolarFactor, 1.0, "parcel solar factor");
                CheckRange(c.YieldFactor, 100.0, "parcel yield factor");
                CheckRange(c.RemainingWater, 1000000.0, "parcel remaining water");
            }
            foreach (var habCoord in ParcelMap.CoreHabitatSpec.Footprint)
            {
                var hab = map.GetCell(habCoord);
                if (!hab.IsClaimed || hab.Protection != StarterProtectionKind.CoreHabitat)
                    throw new InvalidDataException("Invalid core-habitat footprint in save.");
            }
            var airlock = map.GetCell(ParcelMap.AirlockCorridorCoord);
            if (!airlock.IsClaimed || !airlock.IsPassable || airlock.Protection != StarterProtectionKind.AirlockCorridor)
                throw new InvalidDataException("Invalid airlock corridor in save.");
            var solar = map.GetCell(ParcelMap.StarterSolarCoord);
            if (!solar.IsClaimed || solar.Protection != StarterProtectionKind.StarterSolar)
                throw new InvalidDataException("Invalid starter-solar footprint in save.");
        }

        private static void WriteSurfaceJobs(JsonWriter w, List<SurfaceJobState> jobs)
        {
            w.Key("surfaceJobs");
            w.BeginArray();
            if (jobs != null)
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    var j = jobs[i];
                    w.BeginObject();
                    w.Prop("jobId", j.JobId);
                    w.Prop("kind", (int)j.Kind);
                    w.Prop("targetId", j.TargetId);
                    w.Prop("targetX", j.TargetCoord.X);
                    w.Prop("targetZ", j.TargetCoord.Z);
                    w.Prop("workX", j.WorkCoord.X);
                    w.Prop("workZ", j.WorkCoord.Z);
                    w.Prop("assignedCrewId", j.AssignedCrewId);
                    w.Prop("phase", (int)j.Phase);
                    w.Prop("travelSpeed", j.TravelSpeed);
                    w.Prop("workRequiredSeconds", j.WorkRequiredSeconds);
                    w.Prop("workProgressSeconds", j.WorkProgressSeconds);
                    w.Prop("workStaminaRate", j.WorkStaminaRate);
                    w.Prop("workEnergyTotal", j.WorkEnergyTotal);
                    w.Prop("energyConsumed", j.EnergyConsumed);
                    w.Prop("legTotalDistance", j.LegTotalDistance);
                    w.Prop("legDistanceTraveled", j.LegDistanceTraveled);
                    w.Prop("airlockRemainingSeconds", j.AirlockRemainingSeconds);
                    w.Key("waypoints");
                    w.BeginArray();
                    for (int p = 0; p < j.WaypointsInternal.Count; p++)
                    {
                        w.BeginObject();
                        w.Prop("x", j.WaypointsInternal[p].X);
                        w.Prop("z", j.WaypointsInternal[p].Z);
                        w.EndObject();
                    }
                    w.EndArray();
                    w.EndObject();
                }
            }
            w.EndArray();
        }

        private static List<SurfaceJobState> ReadSurfaceJobs(List<object> jobsArr)
        {
            var result = new List<SurfaceJobState>(jobsArr.Count);
            for (int i = 0; i < jobsArr.Count; i++)
            {
                var jObj = jobsArr[i] as Dictionary<string, object> ?? throw new InvalidDataException("Invalid surface job entry.");
                var wpArr = GetArray(jObj, "waypoints");
                var waypoints = new List<WorldPos2D>(wpArr != null ? wpArr.Count : 0);
                if (wpArr != null)
                {
                    for (int p = 0; p < wpArr.Count; p++)
                    {
                        var pObj = wpArr[p] as Dictionary<string, object> ?? throw new InvalidDataException("Invalid surface job waypoint.");
                        waypoints.Add(new WorldPos2D(GetDouble(pObj, "x", 0), GetDouble(pObj, "z", 0)));
                    }
                }

                result.Add(new SurfaceJobState(
                    GetString(jObj, "jobId", $"sjob-load-{i}"),
                    (SurfaceJobKind)GetInt(jObj, "kind", 0),
                    GetString(jObj, "targetId", string.Empty),
                    new GridCoord(GetInt(jObj, "targetX", 0), GetInt(jObj, "targetZ", 0)),
                    new GridCoord(GetInt(jObj, "workX", 0), GetInt(jObj, "workZ", 0)),
                    GetNullableString(jObj, "assignedCrewId"),
                    (SurfaceJobPhase)GetInt(jObj, "phase", 0),
                    GetDouble(jObj, "travelSpeed", GridNav.DefaultWalkSpeed),
                    GetDouble(jObj, "workRequiredSeconds", 0),
                    GetDouble(jObj, "workStaminaRate", 0),
                    GetDouble(jObj, "workEnergyTotal", 0),
                    waypoints,
                    GetDouble(jObj, "legTotalDistance", 0),
                    GetDouble(jObj, "legDistanceTraveled", 0),
                    GetDouble(jObj, "airlockRemainingSeconds", 0),
                    GetDouble(jObj, "workProgressSeconds", 0),
                    GetDouble(jObj, "energyConsumed", 0)));
            }
            return result;
        }

        private static void ValidateSurfaceJobs(SimulationMutableState sim)
        {
            var jobLookup = new Dictionary<string, SurfaceJobState>();
            if (sim.SurfaceJobs != null)
            {
                for (int i = 0; i < sim.SurfaceJobs.Count; i++)
                {
                    var j = sim.SurfaceJobs[i];
                    if (string.IsNullOrEmpty(j.JobId) || jobLookup.ContainsKey(j.JobId))
                        throw new InvalidDataException("Invalid or duplicate surface job ID.");
                    if (!Enum.IsDefined(typeof(SurfaceJobKind), j.Kind) || !Enum.IsDefined(typeof(SurfaceJobPhase), j.Phase))
                        throw new InvalidDataException("Invalid surface job enum.");
                    if (!sim.Map.Config.IsInBounds(j.TargetCoord) || !sim.Map.Config.IsInBounds(j.WorkCoord))
                        throw new InvalidDataException("Surface job coordinates out of map bounds.");
                    if (!MissionConfig.IsFinite(j.TravelSpeed) || j.TravelSpeed <= 0)
                        throw new InvalidDataException("Invalid surface job travel speed.");
                    CheckRange(j.WorkRequiredSeconds, double.MaxValue, "surface job duration");
                    CheckRange(j.WorkProgressSeconds, j.WorkRequiredSeconds, "surface job progress");
                    CheckRange(j.LegTotalDistance, double.MaxValue, "surface job distance");
                    CheckRange(j.LegDistanceTraveled, j.LegTotalDistance, "surface job traveled");
                    CheckRange(j.AirlockRemainingSeconds, 120.0, "surface job airlock timer");
                    for (int p = 0; p < j.WaypointsInternal.Count; p++)
                    {
                        if (!MissionConfig.IsFinite(j.WaypointsInternal[p].X) || !MissionConfig.IsFinite(j.WaypointsInternal[p].Z))
                            throw new InvalidDataException("Non-finite surface waypoint.");
                    }
                    jobLookup[j.JobId] = j;
                }
            }

            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var c = sim.Crew[i];
                if (!MissionConfig.IsFinite(c.SurfacePosX) || !MissionConfig.IsFinite(c.SurfacePosZ))
                    throw new InvalidDataException("Invalid crew surface coordinates.");
                if (c.ActiveSurfaceJobId != null)
                {
                    if (!jobLookup.TryGetValue(c.ActiveSurfaceJobId, out var linked) || linked.AssignedCrewId != c.Id || linked.Phase == SurfaceJobPhase.Completed)
                        throw new InvalidDataException("Invalid crew activeSurfaceJobId reference.");
                }
            }
        }

        private static void WriteFacilities(JsonWriter w, List<FacilityState> facilities)
        {
            w.Key("facilities");
            w.BeginArray();
            if (facilities != null)
            {
                for (int i = 0; i < facilities.Count; i++)
                {
                    var f = facilities[i];
                    w.BeginObject();
                    w.Prop("facilityId", f.FacilityId);
                    w.Prop("kind", (int)f.Kind);
                    w.Prop("originX", f.Origin.X);
                    w.Prop("originZ", f.Origin.Z);
                    w.Prop("rotation", (int)f.Rotation);
                    w.Prop("lifecycleStatus", (int)f.LifecycleStatus);
                    w.Prop("workProgressSeconds", f.WorkProgressSeconds);
                    w.Prop("workRequiredSeconds", f.WorkRequiredSeconds);
                    w.Prop("energySpentTotal", f.EnergySpentTotal);
                    w.Prop("energyRequiredTotal", f.EnergyRequiredTotal);
                    w.Prop("paidMaterials", f.PaidMaterials);
                    w.Prop("isProtectedStarter", f.IsProtectedStarter);
                    w.Prop("isConnectedToCore", f.IsConnectedToCore);
                    w.Prop("isProductionApplied", f.IsProductionApplied);
                    w.Prop("isEnabled", f.IsEnabled);
                    w.Prop("lastWaterExtractionRate", f.LastWaterExtractionRate);
                    w.Prop("lastEnergyDrawRate", f.LastEnergyDrawRate);
                    w.Key("footprint");
                    w.BeginArray();
                    for (int c = 0; c < f.FootprintCells.Count; c++)
                    {
                        w.BeginObject();
                        w.Prop("x", f.FootprintCells[c].X);
                        w.Prop("z", f.FootprintCells[c].Z);
                        w.EndObject();
                    }
                    w.EndArray();
                    w.Key("linkedPanelIds");
                    w.BeginArray();
                    if (f.LinkedPanelIds != null)
                    {
                        for (int p = 0; p < f.LinkedPanelIds.Count; p++)
                            w.Value(f.LinkedPanelIds[p]);
                    }
                    w.EndArray();
                    w.EndObject();
                }
            }
            w.EndArray();
        }

        private static List<FacilityState> ReadFacilities(List<object> facsArr)
        {
            var result = new List<FacilityState>(facsArr.Count);
            for (int i = 0; i < facsArr.Count; i++)
            {
                var fObj = facsArr[i] as Dictionary<string, object> ?? throw new InvalidDataException("Invalid facility entry.");
                var fpArr = GetArray(fObj, "footprint") ?? throw new InvalidDataException("Missing facility footprint.");
                var footprint = new List<GridCoord>(fpArr.Count);
                for (int c = 0; c < fpArr.Count; c++)
                {
                    var cObj = fpArr[c] as Dictionary<string, object> ?? throw new InvalidDataException("Invalid facility footprint coord.");
                    footprint.Add(new GridCoord(GetInt(cObj, "x", -1), GetInt(cObj, "z", -1)));
                }

                var panelArr = GetArray(fObj, "linkedPanelIds");
                var linkedPanels = new List<string>(panelArr != null ? panelArr.Count : 0);
                if (panelArr != null)
                {
                    for (int p = 0; p < panelArr.Count; p++)
                    {
                        if (panelArr[p] is string panelId && !string.IsNullOrWhiteSpace(panelId))
                            linkedPanels.Add(panelId);
                    }
                }

                result.Add(new FacilityState(
                    GetString(fObj, "facilityId", string.Empty),
                    (FacilityKind)GetInt(fObj, "kind", -1),
                    new GridCoord(GetInt(fObj, "originX", -1), GetInt(fObj, "originZ", -1)),
                    (FacilityRotation)GetInt(fObj, "rotation", -1),
                    footprint,
                    (FacilityLifecycleStatus)GetInt(fObj, "lifecycleStatus", -1),
                    GetDouble(fObj, "workProgressSeconds", 0),
                    GetDouble(fObj, "workRequiredSeconds", 0),
                    GetDouble(fObj, "energySpentTotal", 0),
                    GetDouble(fObj, "energyRequiredTotal", 0),
                    GetInt(fObj, "paidMaterials", 0),
                    GetBool(fObj, "isProtectedStarter", false),
                    GetBool(fObj, "isConnectedToCore", false),
                    GetBool(fObj, "isProductionApplied", false),
                    linkedPanels,
                    GetBool(fObj, "isEnabled", true),
                    GetDouble(fObj, "lastWaterExtractionRate", 0),
                    GetDouble(fObj, "lastEnergyDrawRate", 0)));
            }
            return result;
        }

        private static void ValidateFacilities(SimulationMutableState sim)
        {
            if (sim.Facilities == null)
                throw new InvalidDataException("Missing facilities list in save.");
            if (sim.Map == null)
                return;

            var lookup = new Dictionary<string, FacilityState>(StringComparer.Ordinal);
            for (int i = 0; i < sim.Facilities.Count; i++)
            {
                var f = sim.Facilities[i];
                if (string.IsNullOrWhiteSpace(f.FacilityId) || !lookup.TryAdd(f.FacilityId, f))
                    throw new InvalidDataException("Duplicate or empty facilityId in save.");
                if (!Enum.IsDefined(typeof(FacilityKind), f.Kind) || !FacilityCatalog.TryGetSpec(f.Kind, out var spec))
                    throw new InvalidDataException("Invalid facility kind in save.");
                if (!Enum.IsDefined(typeof(FacilityRotation), f.Rotation))
                    throw new InvalidDataException("Invalid facility rotation in save.");
                if (!Enum.IsDefined(typeof(FacilityLifecycleStatus), f.LifecycleStatus))
                    throw new InvalidDataException("Invalid facility lifecycleStatus in save.");
                if (!sim.Map.Config.IsInBounds(f.Origin) || f.FootprintCells.Count == 0)
                    throw new InvalidDataException("Invalid facility origin or empty footprint in save.");
                if (!MissionConfig.IsFinite(f.WorkProgressSeconds) || !MissionConfig.IsFinite(f.WorkRequiredSeconds) ||
                    !MissionConfig.IsFinite(f.EnergySpentTotal) || !MissionConfig.IsFinite(f.EnergyRequiredTotal))
                    throw new InvalidDataException("Non-finite facility counters in save.");
                if (f.WorkProgressSeconds < -1e-6 || f.WorkProgressSeconds > f.WorkRequiredSeconds + 1e-6)
                    throw new InvalidDataException("Invalid facility workProgressSeconds in save.");
                if (f.EnergySpentTotal < -1e-6 || f.EnergySpentTotal > f.EnergyRequiredTotal + 1e-6)
                    throw new InvalidDataException("Invalid facility energySpentTotal in save.");
                if (f.PaidMaterials < 0)
                    throw new InvalidDataException("Negative facility paidMaterials in save.");
                if (f.LifecycleStatus == FacilityLifecycleStatus.Blueprint && f.IsProductionApplied)
                    throw new InvalidDataException("Blueprint facility cannot have production applied.");

                for (int c = 0; c < f.FootprintCells.Count; c++)
                {
                    var coord = f.FootprintCells[c];
                    if (!sim.Map.Config.IsInBounds(coord))
                        throw new InvalidDataException("Facility footprint out of bounds in save.");
                    var cell = sim.Map.GetCell(coord);
                    if (spec.UsesUtilityLayerOnly)
                    {
                        if (f.LifecycleStatus == FacilityLifecycleStatus.Blueprint && cell.UtilityBlueprintId != f.FacilityId)
                            throw new InvalidDataException("Utility blueprint missing map cell reference.");
                        if (f.LifecycleStatus == FacilityLifecycleStatus.Commissioned && !cell.HasUtilityLine)
                            throw new InvalidDataException("Commissioned utility line missing HasUtilityLine on map cell.");
                    }
                    else
                    {
                        bool matchesEmbeddedStarter = f.IsProtectedStarter &&
                            cell.StarterFacilityId == f.FacilityId &&
                            cell.FacilityId == ParcelMap.CoreHabitatSpec.FacilityId;
                        if (cell.FacilityId != f.FacilityId && !matchesEmbeddedStarter)
                            throw new InvalidDataException("Facility footprint missing map cell FacilityId reference.");
                    }
                }
            }

            if (!lookup.TryGetValue(ParcelMap.CoreHabitatSpec.FacilityId, out var core) || !core.IsProtectedStarter ||
                !lookup.TryGetValue(ParcelMap.StarterSolarSpec.FacilityId, out var solar) || !solar.IsProtectedStarter)
                throw new InvalidDataException("Missing protected starter facility in save.");

            foreach (var cell in sim.Map.Cells)
            {
                if (cell.FacilityId != null)
                {
                    if (!lookup.TryGetValue(cell.FacilityId, out var fac) || FacilityCatalog.GetSpec(fac.Kind).UsesUtilityLayerOnly)
                        throw new InvalidDataException("Map cell FacilityId points to missing or invalid facility.");
                }
                if (cell.UtilityBlueprintId != null)
                {
                    if (!lookup.TryGetValue(cell.UtilityBlueprintId, out var ub) || ub.Kind != FacilityKind.UtilityLine || ub.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                        throw new InvalidDataException("Map cell UtilityBlueprintId points to missing or invalid utility blueprint.");
                }
            }
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> obj, string key)
            => obj.TryGetValue(key, out var val) ? val as Dictionary<string, object> : null;

        private static List<object> GetArray(Dictionary<string, object> obj, string key)
            => obj.TryGetValue(key, out var val) ? val as List<object> : null;

        private static string GetString(Dictionary<string, object> obj, string key, string fallback)
            => obj.TryGetValue(key, out var val) && val != null ? Convert.ToString(val, CultureInfo.InvariantCulture) : fallback;

        private static string GetNullableString(Dictionary<string, object> obj, string key)
            => obj.TryGetValue(key, out var val) && val != null ? Convert.ToString(val, CultureInfo.InvariantCulture) : null;

        private static bool GetBool(Dictionary<string, object> obj, string key, bool fallback)
            => obj.TryGetValue(key, out var val) && val != null ? Convert.ToBoolean(val, CultureInfo.InvariantCulture) : fallback;

        private static int GetInt(Dictionary<string, object> obj, string key, int fallback)
            => obj.TryGetValue(key, out var val) && val != null ? Convert.ToInt32(val, CultureInfo.InvariantCulture) : fallback;

        private static long GetLong(Dictionary<string, object> obj, string key, long fallback)
            => obj.TryGetValue(key, out var val) && val != null ? Convert.ToInt64(val, CultureInfo.InvariantCulture) : fallback;

        private static ulong GetULong(Dictionary<string, object> obj, string key, ulong fallback)
        {
            if (!obj.TryGetValue(key, out var val) || val == null) return fallback;
            if (val is string s && ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
                return parsed;
            return Convert.ToUInt64(val, CultureInfo.InvariantCulture);
        }

        private static double GetDouble(Dictionary<string, object> obj, string key, double fallback)
            => obj.TryGetValue(key, out var val) ? ToDouble(val, fallback) : fallback;

        private static double ToDouble(object val, double fallback)
        {
            if (val == null) return fallback;
            if (val is string s)
            {
                if (s == "Infinity") return double.PositiveInfinity;
                if (s == "-Infinity") return double.NegativeInfinity;
                if (s == "NaN") return double.NaN;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                return fallback;
            }
            return Convert.ToDouble(val, CultureInfo.InvariantCulture);
        }

        private sealed class JsonWriter
        {
            private readonly StringBuilder sb = new StringBuilder(4096);
            private readonly Stack<bool> needsComma = new Stack<bool>();

            private void Comma()
            {
                if (needsComma.Count > 0)
                {
                    if (needsComma.Peek()) sb.Append(',');
                    else
                    {
                        needsComma.Pop();
                        needsComma.Push(true);
                    }
                }
            }

            public void BeginObject() { Comma(); sb.Append('{'); needsComma.Push(false); }
            public void EndObject() { needsComma.Pop(); sb.Append('}'); }
            public void BeginArray() { Comma(); sb.Append('['); needsComma.Push(false); }
            public void EndArray() { needsComma.Pop(); sb.Append(']'); }

            public void Key(string name)
            {
                Comma();
                WriteEscaped(name);
                sb.Append(':');
                needsComma.Pop();
                needsComma.Push(false);
            }

            public void Prop(string name, string val) { Key(name); if (val == null) NullValue(); else Value(val); }
            public void Prop(string name, bool val) { Key(name); sb.Append(val ? "true" : "false"); SetComma(); }
            public void Prop(string name, int val) { Key(name); sb.Append(val.ToString(CultureInfo.InvariantCulture)); SetComma(); }
            public void Prop(string name, long val) { Key(name); sb.Append(val.ToString(CultureInfo.InvariantCulture)); SetComma(); }
            public void Prop(string name, double val)
            {
                Key(name);
                if (double.IsPositiveInfinity(val)) WriteEscaped("Infinity");
                else if (double.IsNegativeInfinity(val)) WriteEscaped("-Infinity");
                else if (double.IsNaN(val)) WriteEscaped("NaN");
                else sb.Append(val.ToString("R", CultureInfo.InvariantCulture));
                SetComma();
            }

            public void NullValue() { Comma(); sb.Append("null"); }
            public void Value(string s) { Comma(); if (s == null) sb.Append("null"); else WriteEscaped(s); }
            public void Value(int v) { Comma(); sb.Append(v.ToString(CultureInfo.InvariantCulture)); }

            private void SetComma()
            {
                if (needsComma.Count > 0)
                {
                    needsComma.Pop();
                    needsComma.Push(true);
                }
            }

            private void WriteEscaped(string s)
            {
                sb.Append('"');
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ')
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }

            public override string ToString() => sb.ToString();
        }

        private sealed class JsonReader
        {
            private readonly string text;
            private int pos;

            private JsonReader(string text) { this.text = text; }

            public static object Parse(string text)
            {
                var r = new JsonReader(text);
                var val = r.ReadValue();
                r.SkipWs();
                if (r.pos != r.text.Length)
                    throw new FormatException("Trailing characters after JSON root.");
                return val;
            }

            private void SkipWs()
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
            }

            private object ReadValue()
            {
                SkipWs();
                if (pos >= text.Length) throw new FormatException("Unexpected end of JSON.");
                char c = text[pos];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                throw new FormatException($"Unexpected character '{c}' at position {pos}.");
            }

            private Dictionary<string, object> ReadObject()
            {
                pos++; // '{'
                var dict = new Dictionary<string, object>();
                SkipWs();
                if (pos < text.Length && text[pos] == '}') { pos++; return dict; }
                while (true)
                {
                    SkipWs();
                    if (pos >= text.Length || text[pos] != '"') throw new FormatException($"Expected string key at {pos}.");
                    string key = ReadString();
                    SkipWs();
                    if (pos >= text.Length || text[pos] != ':') throw new FormatException($"Expected ':' at {pos}.");
                    pos++;
                    dict[key] = ReadValue();
                    SkipWs();
                    if (pos >= text.Length) throw new FormatException("Unterminated object.");
                    if (text[pos] == '}') { pos++; return dict; }
                    if (text[pos] != ',') throw new FormatException($"Expected ',' or '}}' at {pos}.");
                    pos++;
                }
            }

            private List<object> ReadArray()
            {
                pos++; // '['
                var list = new List<object>();
                SkipWs();
                if (pos < text.Length && text[pos] == ']') { pos++; return list; }
                while (true)
                {
                    list.Add(ReadValue());
                    SkipWs();
                    if (pos >= text.Length) throw new FormatException("Unterminated array.");
                    if (text[pos] == ']') { pos++; return list; }
                    if (text[pos] != ',') throw new FormatException($"Expected ',' or ']' at {pos}.");
                    pos++;
                }
            }

            private string ReadString()
            {
                pos++; // '"'
                var sb = new StringBuilder();
                while (pos < text.Length)
                {
                    char c = text[pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (pos >= text.Length) throw new FormatException("Unterminated escape.");
                        char esc = text[pos++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (pos + 4 > text.Length) throw new FormatException("Invalid unicode escape.");
                                sb.Append((char)Convert.ToInt32(text.Substring(pos, 4), 16));
                                pos += 4;
                                break;
                            default: throw new FormatException($"Invalid escape '\\{esc}'.");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string.");
            }

            private object ReadNumber()
            {
                int start = pos;
                bool isFloat = false;
                if (text[pos] == '-') pos++;
                while (pos < text.Length && text[pos] >= '0' && text[pos] <= '9') pos++;
                if (pos < text.Length && text[pos] == '.') { isFloat = true; pos++; while (pos < text.Length && text[pos] >= '0' && text[pos] <= '9') pos++; }
                if (pos < text.Length && (text[pos] == 'e' || text[pos] == 'E'))
                {
                    isFloat = true;
                    pos++;
                    if (pos < text.Length && (text[pos] == '+' || text[pos] == '-')) pos++;
                    while (pos < text.Length && text[pos] >= '0' && text[pos] <= '9') pos++;
                }
                string numStr = text.Substring(start, pos - start);
                if (!isFloat && long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;
                throw new FormatException($"Invalid number '{numStr}'.");
            }

            private void Expect(string lit)
            {
                if (pos + lit.Length > text.Length || string.CompareOrdinal(text, pos, lit, 0, lit.Length) != 0)
                    throw new FormatException($"Expected '{lit}' at {pos}.");
                pos += lit.Length;
            }
        }
    }
}
