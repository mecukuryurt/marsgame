using System;
using System.Collections.Generic;

namespace MarsOutpost
{
    // Values are game units and rates are game units per simulation second.
    public sealed class ResourceConfig
    {
        public double BatteryCapacity { get; }
        public double WaterCapacity { get; }
        public double OxygenCapacity { get; }
        public double WastewaterCapacity { get; }
        public double InitialBattery { get; }
        public double InitialWater { get; }
        public double InitialOxygen { get; }
        public double InitialWastewater { get; }
        public double SolarRate { get; }
        public double OxygenProductionRate { get; }
        public double OxygenEnergyPerUnit { get; }
        public double OxygenWaterPerUnit { get; }
        public double RecyclingRate { get; }
        public double RecyclingEnergyPerUnit { get; }
        public double WaterUsePerCrew { get; }
        public double OxygenUsePerCrew { get; }
        public double ResearchEnergyRate { get; }
        public double ResearchProgressRate { get; }
        public double InitialPanelFouling { get; }
        public double PanelMaintenanceReduction { get; }
        public double InitialSystemWear { get; }
        public double SystemMaintenanceReduction { get; }
        public double WaterExtractionRate { get; }
        public double WaterExtractorEnergyRate { get; }
        public double OxygenRepairEnergyRate { get; }

        public ResourceConfig(double batteryCapacity = 100, double waterCapacity = 100, double oxygenCapacity = 100,
            double wastewaterCapacity = 100, double initialBattery = 80, double initialWater = 80,
            double initialOxygen = 80, double initialWastewater = 0, double solarRate = 0.12,
            double oxygenProductionRate = 0.05, double oxygenEnergyPerUnit = 1, double oxygenWaterPerUnit = 0.2,
            double recyclingRate = 0.02, double recyclingEnergyPerUnit = 0.5,
            double waterUsePerCrew = 0.004, double oxygenUsePerCrew = 0.01,
            double researchEnergyRate = 0.03, double researchProgressRate = 1.0,
            double initialPanelFouling = 0, double panelMaintenanceReduction = 0.25,
            double initialSystemWear = 0, double systemMaintenanceReduction = 0.25,
            double waterExtractionRate = 0, double waterExtractorEnergyRate = 0,
            double oxygenRepairEnergyRate = 0.5)
        {
            double[] values = { batteryCapacity, waterCapacity, oxygenCapacity, wastewaterCapacity, initialBattery,
                initialWater, initialOxygen, initialWastewater, solarRate, oxygenProductionRate, oxygenEnergyPerUnit,
                oxygenWaterPerUnit, recyclingRate, recyclingEnergyPerUnit, waterUsePerCrew, oxygenUsePerCrew,
                researchEnergyRate, researchProgressRate, initialPanelFouling, panelMaintenanceReduction,
                initialSystemWear, systemMaintenanceReduction, waterExtractionRate, waterExtractorEnergyRate,
                oxygenRepairEnergyRate };
            foreach (double value in values)
                if (!MissionConfig.IsFinite(value) || value < 0 || value > 1000000)
                    throw new ArgumentOutOfRangeException(nameof(values), "Resource values must be finite and between 0 and 1,000,000.");
            if (initialBattery > batteryCapacity || initialWater > waterCapacity || initialOxygen > oxygenCapacity || initialWastewater > wastewaterCapacity)
                throw new ArgumentException("Initial reserves cannot exceed capacity.");
            if (initialPanelFouling > 1 || panelMaintenanceReduction > 1 || initialSystemWear > 1 || systemMaintenanceReduction > 1)
                throw new ArgumentOutOfRangeException(nameof(values), "Fouling and wear fractions must be between 0 and 1.");
            BatteryCapacity = batteryCapacity; WaterCapacity = waterCapacity; OxygenCapacity = oxygenCapacity; WastewaterCapacity = wastewaterCapacity;
            InitialBattery = initialBattery; InitialWater = initialWater; InitialOxygen = initialOxygen; InitialWastewater = initialWastewater;
            SolarRate = solarRate; OxygenProductionRate = oxygenProductionRate; OxygenEnergyPerUnit = oxygenEnergyPerUnit;
            OxygenWaterPerUnit = oxygenWaterPerUnit; RecyclingRate = recyclingRate; RecyclingEnergyPerUnit = recyclingEnergyPerUnit;
            WaterUsePerCrew = waterUsePerCrew; OxygenUsePerCrew = oxygenUsePerCrew;
            ResearchEnergyRate = researchEnergyRate; ResearchProgressRate = researchProgressRate;
            InitialPanelFouling = initialPanelFouling; PanelMaintenanceReduction = panelMaintenanceReduction;
            InitialSystemWear = initialSystemWear; SystemMaintenanceReduction = systemMaintenanceReduction;
            WaterExtractionRate = waterExtractionRate; WaterExtractorEnergyRate = waterExtractorEnergyRate;
            OxygenRepairEnergyRate = oxygenRepairEnergyRate;
        }
    }

    public sealed class ResourceState
    {
        public double Battery { get; internal set; }
        public double Water { get; internal set; }
        public double Oxygen { get; internal set; }
        public double Wastewater { get; internal set; }
        public double UnmetWater { get; internal set; }
        public double UnmetOxygen { get; internal set; }
        public bool ResearchEnabled { get; internal set; }
        public double ResearchProgress { get; internal set; }
        public double ScienceBalance { get; internal set; }
        public double PanelFouling { get; internal set; }
        public double SystemWear { get; internal set; }
        public double AtmosphericDustLoss { get; internal set; }
        public double LastResearchRateFactor { get; internal set; }
        public double ContinuousOxygenOutageSeconds { get; internal set; }
        public bool WaterExtractorEnabled { get; internal set; }
        public bool WaterExtractorFaulted { get; internal set; }
        public bool OxygenProducerFaulted { get; internal set; }
        public double LastWaterExtractionRate { get; internal set; }
        public double LastLegacyWaterExtractionRate { get; internal set; }
        public double LastSiteWaterExtractionRate { get; internal set; }
        public double LastRecycledWaterRate { get; internal set; }
        public double TotalExtractedWater { get; internal set; }
        public double TotalLegacyExtractedWater { get; internal set; }
        public double TotalSiteExtractedWater { get; internal set; }
        public double TotalRecycledWater { get; internal set; }
        public double BatteryOverflowLostTotal { get; internal set; }
        public double WaterOverflowLostTotal { get; internal set; }
        public List<PanelUnitState> Panels { get; internal set; }

        internal ResourceState Copy()
        {
            var copy = (ResourceState)MemberwiseClone();
            if (Panels != null)
            {
                copy.Panels = new List<PanelUnitState>(Panels.Count);
                for (int i = 0; i < Panels.Count; i++)
                    copy.Panels.Add(Panels[i].Clone());
            }
            return copy;
        }

        internal static ResourceState Initial(ResourceConfig c) => new ResourceState
        {
            Battery = c.InitialBattery,
            Water = c.InitialWater,
            Oxygen = c.InitialOxygen,
            Wastewater = c.InitialWastewater,
            ResearchEnabled = true,
            ResearchProgress = 0,
            ScienceBalance = 0,
            PanelFouling = c.InitialPanelFouling,
            SystemWear = c.InitialSystemWear,
            AtmosphericDustLoss = 0,
            LastResearchRateFactor = 0,
            ContinuousOxygenOutageSeconds = 0,
            WaterExtractorEnabled = true,
            WaterExtractorFaulted = false,
            OxygenProducerFaulted = false,
            LastWaterExtractionRate = 0
        };
    }

    internal sealed class SolTracker
    {
        public long SolNumber;
        public double StartBattery;
        public double StartWater;
        public double StartOxygen;
        public double StartResearch;
        public readonly Dictionary<string, double> StartStamina = new Dictionary<string, double>();
        public readonly List<string> CompletedTasks = new List<string>();
        public readonly List<string> EventsExperienced = new List<string>();

        public SolTracker Clone()
        {
            var copy = new SolTracker
            {
                SolNumber = SolNumber,
                StartBattery = StartBattery,
                StartWater = StartWater,
                StartOxygen = StartOxygen,
                StartResearch = StartResearch
            };
            foreach (var kv in StartStamina) copy.StartStamina[kv.Key] = kv.Value;
            copy.CompletedTasks.AddRange(CompletedTasks);
            copy.EventsExperienced.AddRange(EventsExperienced);
            return copy;
        }

        public void ResetForSol(long solNumber, ResourceState res, List<CrewState> crew)
        {
            SolNumber = solNumber;
            StartBattery = res.Battery;
            StartWater = res.Water;
            StartOxygen = res.Oxygen;
            StartResearch = res.ResearchProgress;
            StartStamina.Clear();
            for (int i = 0; i < crew.Count; i++)
                StartStamina[crew[i].Id] = crew[i].Stamina;
            CompletedTasks.Clear();
            EventsExperienced.Clear();
        }
    }

    internal sealed class SimulationMutableState
    {
        public ResourceState Resources;
        public List<CrewState> Crew;
        public List<TaskState> Tasks;
        public List<EventState> Events;
        public MissionOutcome Outcome;
        public MissionFailureReason FailureReason;
        public string StatusMessage;
        public bool IsSleeping;
        public bool CanTriggerLowOxygenAlert;
        public bool CriticalAlertPauseRequested;
        public string ActiveAlertMessage;
        public int LowOxygenAlertCount;
        public bool SolReportPauseRequested;
        public SolReport PendingSolReport;
        public List<SolReport> SolReports;
        public SolTracker SolTracker;
        public int FoodRationsRemaining;
        public bool MealPreparedThisSol;
        public long NightMealSettledSol;
        public bool NightRationAvailable;
        public bool SleepStartedThisSol;
        public double NightSleepRecoveryFactor;
        public EarthAdviceStatus EarthAdviceStatus;
        public double EarthAdviceRequestedAtSeconds;
        public double EarthAdviceExpectedAtSeconds;
        public double EarthAdviceDeliveredAtSeconds;
        public string EarthAdviceMessage;
        public List<ChannelScheduleState> ChannelSchedules;
        public List<string> OperationsLog;
        public List<EarthOrderState> EarthOrders;
        public EarthOrderBagState EarthOrderBag;
        public ParcelMap Map;
        public int Materials;
        public long LastMaterialSupplySol;
        public int NextEntityId;
        public int MaterialsRefundedTotal;
        public int MaterialsForfeitedTotal;
        public double BuildEnergyLostTotal;
        public List<SurfaceJobState> SurfaceJobs;
        public List<FacilityState> Facilities;

        public SimulationMutableState Clone()
        {
            var copy = new SimulationMutableState
            {
                Resources = Resources.Copy(),
                Crew = new List<CrewState>(Crew.Count),
                Tasks = new List<TaskState>(Tasks.Count),
                Events = new List<EventState>(Events.Count),
                Outcome = Outcome,
                FailureReason = FailureReason,
                StatusMessage = StatusMessage,
                IsSleeping = IsSleeping,
                CanTriggerLowOxygenAlert = CanTriggerLowOxygenAlert,
                CriticalAlertPauseRequested = CriticalAlertPauseRequested,
                ActiveAlertMessage = ActiveAlertMessage,
                LowOxygenAlertCount = LowOxygenAlertCount,
                SolReportPauseRequested = SolReportPauseRequested,
                PendingSolReport = PendingSolReport,
                SolReports = new List<SolReport>(SolReports),
                SolTracker = SolTracker.Clone(),
                FoodRationsRemaining = FoodRationsRemaining,
                MealPreparedThisSol = MealPreparedThisSol,
                NightMealSettledSol = NightMealSettledSol,
                NightRationAvailable = NightRationAvailable,
                SleepStartedThisSol = SleepStartedThisSol,
                NightSleepRecoveryFactor = NightSleepRecoveryFactor,
                EarthAdviceStatus = EarthAdviceStatus,
                EarthAdviceRequestedAtSeconds = EarthAdviceRequestedAtSeconds,
                EarthAdviceExpectedAtSeconds = EarthAdviceExpectedAtSeconds,
                EarthAdviceDeliveredAtSeconds = EarthAdviceDeliveredAtSeconds,
                EarthAdviceMessage = EarthAdviceMessage,
                ChannelSchedules = ChannelSchedules != null ? new List<ChannelScheduleState>(ChannelSchedules.Count) : null,
                OperationsLog = OperationsLog != null ? new List<string>(OperationsLog) : null,
                EarthOrders = EarthOrders != null ? new List<EarthOrderState>(EarthOrders.Count) : null,
                EarthOrderBag = EarthOrderBag?.Clone(),
                Map = Map?.Clone(),
                Materials = Materials,
                LastMaterialSupplySol = LastMaterialSupplySol,
                NextEntityId = NextEntityId,
                MaterialsRefundedTotal = MaterialsRefundedTotal,
                MaterialsForfeitedTotal = MaterialsForfeitedTotal,
                BuildEnergyLostTotal = BuildEnergyLostTotal,
                SurfaceJobs = SurfaceJobs != null ? new List<SurfaceJobState>(SurfaceJobs.Count) : null,
                Facilities = Facilities != null ? new List<FacilityState>(Facilities.Count) : null
            };
            if (Facilities != null)
            {
                for (int i = 0; i < Facilities.Count; i++)
                    copy.Facilities.Add(Facilities[i].Clone());
            }
            if (SurfaceJobs != null)
            {
                for (int i = 0; i < SurfaceJobs.Count; i++)
                    copy.SurfaceJobs.Add(SurfaceJobs[i].Clone());
            }
            if (EarthOrders != null)
            {
                for (int i = 0; i < EarthOrders.Count; i++)
                    copy.EarthOrders.Add(EarthOrders[i].Clone());
            }
            if (ChannelSchedules != null)
            {
                for (int i = 0; i < ChannelSchedules.Count; i++)
                    copy.ChannelSchedules.Add(ChannelSchedules[i].Clone());
            }
            for (int i = 0; i < Crew.Count; i++) copy.Crew.Add(Crew[i].Copy());
            for (int i = 0; i < Tasks.Count; i++) copy.Tasks.Add(Tasks[i].Copy());
            for (int i = 0; i < Events.Count; i++) copy.Events.Add(Events[i].Copy());
            return copy;
        }
    }

    public static class SolarProduction
    {
        public static FacilityState FindOwningFacility(PanelUnitState panel, IReadOnlyList<FacilityState> facilities)
        {
            if (panel == null || facilities == null || facilities.Count == 0)
                return null;

            if (!string.IsNullOrEmpty(panel.FacilityId))
            {
                for (int i = 0; i < facilities.Count; i++)
                {
                    if (facilities[i].FacilityId == panel.FacilityId)
                        return facilities[i];
                }
            }

            for (int i = 0; i < facilities.Count; i++)
            {
                var linked = facilities[i].LinkedPanelIds;
                if (linked != null)
                {
                    for (int j = 0; j < linked.Count; j++)
                    {
                        if (linked[j] == panel.Id)
                            return facilities[i];
                    }
                }
            }

            if (panel.Id == "panel-1" || panel.Id == "panel-2" || panel.Id == "panel-3")
            {
                for (int i = 0; i < facilities.Count; i++)
                {
                    if (facilities[i].FacilityId == ParcelMap.StarterSolarSpec.FacilityId)
                        return facilities[i];
                }
            }

            return null;
        }

        public static bool IsPanelOnline(PanelUnitState panel, IReadOnlyList<FacilityState> facilities)
        {
            if (panel == null || panel.IsFaulted)
                return false;

            if (facilities == null || facilities.Count == 0)
                return true;

            var fac = FindOwningFacility(panel, facilities);
            if (fac == null)
                return string.IsNullOrEmpty(panel.FacilityId);

            return fac.LifecycleStatus == FacilityLifecycleStatus.Commissioned && fac.IsConnectedToCore;
        }

        public static double GetPanelHealthFactor(ResourceState s, IReadOnlyList<FacilityState> facilities = null)
        {
            if (s == null || s.Panels == null || s.Panels.Count == 0)
                return 1.0;

            double sum = 0;
            for (int i = 0; i < s.Panels.Count; i++)
            {
                var panel = s.Panels[i];
                if (IsPanelOnline(panel, facilities))
                    sum += panel.Weight * panel.SolarFactor;
            }
            return sum;
        }

        public static double GetFacilityNominalFactor(ResourceState s, FacilityState fac)
        {
            if (s == null || s.Panels == null || fac == null)
                return 0;

            double sum = 0;
            for (int i = 0; i < s.Panels.Count; i++)
            {
                var panel = s.Panels[i];
                bool owns = panel.FacilityId == fac.FacilityId ||
                    (fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(panel.Id));
                if (owns && !panel.IsFaulted)
                    sum += panel.Weight * panel.SolarFactor;
            }
            return sum;
        }

        public static double ComputeFacilitySolarOutput(double solarRate, MissionState state, FacilityState fac, out double baseDayOutput)
        {
            baseDayOutput = 0;
            if (state == null || state.Resources == null || fac == null || solarRate <= 0)
                return 0;

            double nominalFactor = GetFacilityNominalFactor(state.Resources, fac);
            baseDayOutput = solarRate * nominalFactor;
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned || !fac.IsConnectedToCore || state.IsNight)
                return 0;

            double foulingFactor = Math.Max(0, 1.0 - state.Resources.PanelFouling);
            double dustFactor = Math.Max(0, 1.0 - state.Resources.AtmosphericDustLoss);
            return baseDayOutput * foulingFactor * dustFactor;
        }
    }

    public static class ColonyStorage
    {
        public static double GetEffectiveBatteryCapacity(ResourceConfig config, IReadOnlyList<FacilityState> facilities)
        {
            double cap = config != null ? config.BatteryCapacity : 100.0;
            if (facilities == null)
                return cap;

            for (int i = 0; i < facilities.Count; i++)
            {
                var fac = facilities[i];
                if (fac.LifecycleStatus == FacilityLifecycleStatus.Commissioned &&
                    fac.IsConnectedToCore &&
                    FacilityCatalog.TryGetSpec(fac.Kind, out var spec))
                {
                    cap += spec.BatteryCapacityDelta;
                }
            }
            return cap;
        }

        internal static double ReconcileStorageOverflow(SimulationMutableState simState, ResourceConfig config)
        {
            if (simState?.Resources == null || config == null)
                return 0;

            double effBatteryCap = GetEffectiveBatteryCapacity(config, simState.Facilities);
            double lostBattery = 0;
            if (simState.Resources.Battery > effBatteryCap)
            {
                lostBattery = simState.Resources.Battery - effBatteryCap;
                simState.Resources.Battery = effBatteryCap;
                simState.Resources.BatteryOverflowLostTotal += lostBattery;
            }
            return lostBattery;
        }

        public static double PreviewBatteryLossIfFacilityDisconnected(
            ResourceConfig config,
            MissionState state,
            FacilityState targetFac)
        {
            if (config == null || state?.Resources == null || targetFac == null)
                return 0;

            double currentCap = GetEffectiveBatteryCapacity(config, state.Facilities);
            if (targetFac.Kind == FacilityKind.Battery)
            {
                if (targetFac.LifecycleStatus != FacilityLifecycleStatus.Commissioned || !targetFac.IsConnectedToCore)
                    return 0;
                double nextCap = Math.Max(config.BatteryCapacity, currentCap - FacilityCatalog.GetSpec(FacilityKind.Battery).BatteryCapacityDelta);
                return Math.Max(0, state.Resources.Battery - nextCap);
            }

            if (targetFac.Kind == FacilityKind.UtilityLine && state.Map != null && targetFac.LifecycleStatus == FacilityLifecycleStatus.Commissioned)
            {
                var simMap = state.Map.Clone();
                if (targetFac.FootprintCells != null)
                {
                    for (int i = 0; i < targetFac.FootprintCells.Count; i++)
                    {
                        if (simMap.TryGetCell(targetFac.FootprintCells[i], out var cell))
                            cell.HasUtilityLine = false;
                    }
                }
                var connected = UtilityNetwork.ComputeConnectedLineCells(simMap);
                double nextCap = config.BatteryCapacity;
                if (state.Facilities != null)
                {
                    for (int i = 0; i < state.Facilities.Count; i++)
                    {
                        var f = state.Facilities[i];
                        if (f.FacilityId == targetFac.FacilityId ||
                            f.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                            continue;
                        if (FacilityCatalog.TryGetSpec(f.Kind, out var spec) && spec.BatteryCapacityDelta > 0 &&
                            UtilityNetwork.IsFacilityConnectedToCore(simMap, connected, f))
                        {
                            nextCap += spec.BatteryCapacityDelta;
                        }
                    }
                }
                return Math.Max(0, state.Resources.Battery - nextCap);
            }

            return 0;
        }
    }

    internal static class ResourceSimulation
    {
        // Each call is atomic and capped to prevent pathological calendar lengths monopolizing the runner.
        internal const int MaxTransitions = 100000;

        internal static double AdvanceAll(
            SimulationMutableState simState,
            MissionConfig config,
            double elapsed,
            double duration)
        {
            if (simState.Outcome != MissionOutcome.InProgress)
                return elapsed;

            var s = simState.Resources;
            var crew = simState.Crew;
            var tasks = simState.Tasks;
            var events = simState.Events;
            var c = config.Resources;
            double target = elapsed + duration;
            int steps = 0;

            var taskLookup = new Dictionary<string, TaskState>(tasks.Count);
            for (int i = 0; i < tasks.Count; i++)
                taskLookup[tasks[i].Id] = tasks[i];

            UpdateEventPhases(events, elapsed, simState.SolTracker);
            ComputeEnvironment(events, elapsed, out double initialDustLoss, out double _);
            s.AtmosphericDustLoss = initialDustLoss;

            if (CheckTerminal(simState, config, elapsed))
                return elapsed;

            var crewSurfaceWorkFactors = new double[crew.Count];
            var crewSurfaceEnergyRates = new double[crew.Count];

            while (elapsed < target)
            {
                if (++steps > MaxTransitions)
                    throw new InvalidOperationException("Tick exceeds the resource transition limit; use smaller ticks.");

                UpdateEventPhases(events, elapsed, simState.SolTracker);
                SettleNightMealIfDue(simState, config, elapsed);
                CheckEarthAdviceDelivery(simState, config, elapsed);
                if (config.EnableOperationsRandomEvents && simState.ChannelSchedules != null)
                    ProcessChannelBoundaries(simState, elapsed);
                ComputeEnvironment(events, elapsed, out double dustLoss, out double radiationIntensity);
                s.AtmosphericDustLoss = dustLoss;

                double phase = elapsed % config.SolSeconds;
                bool day = phase < config.DaySeconds;
                double boundary = day ? config.DaySeconds - phase : config.SolSeconds - phase;
                double dt = Math.Min(target - elapsed, boundary);

                double nextSolEnd = simState.SolTracker.SolNumber * config.SolSeconds;
                if (nextSolEnd > elapsed)
                    dt = Math.Min(dt, nextSolEnd - elapsed);

                double missionEnd = config.MissionDurationSeconds;
                if (!double.IsInfinity(missionEnd) && missionEnd > elapsed)
                    dt = Math.Min(dt, missionEnd - elapsed);

                if (simState.EarthAdviceStatus == EarthAdviceStatus.Pending && simState.EarthAdviceExpectedAtSeconds > elapsed)
                    dt = Math.Min(dt, simState.EarthAdviceExpectedAtSeconds - elapsed);

                for (int i = 0; i < events.Count; i++)
                {
                    var ev = events[i];
                    if (!ev.WarningTriggered && ev.WarningSeconds > elapsed)
                        dt = Math.Min(dt, ev.WarningSeconds - elapsed);
                    if (!ev.StartTriggered && ev.StartSeconds > elapsed)
                        dt = Math.Min(dt, ev.StartSeconds - elapsed);
                    if (!ev.EndTriggered && ev.EndSeconds > elapsed)
                        dt = Math.Min(dt, ev.EndSeconds - elapsed);
                }

                if (config.EnableOperationsRandomEvents && simState.ChannelSchedules != null)
                {
                    double nextChannelBoundary = OperationsScheduler.GetNextChannelBoundary(simState.ChannelSchedules);
                    if (nextChannelBoundary > elapsed && nextChannelBoundary < elapsed + dt)
                        dt = nextChannelBoundary - elapsed;
                }

                if (dt <= 0)
                    throw new InvalidOperationException("Tick cannot advance at this calendar precision.");

                double foulingFactor = Math.Max(0, 1.0 - s.PanelFouling);
                double dustFactor = Math.Max(0, 1.0 - dustLoss);
                double panelFactor = SolarProduction.GetPanelHealthFactor(s, simState.Facilities);
                double solar = day ? c.SolarRate * panelFactor * foulingFactor * dustFactor : 0;
                double systemFactor = Math.Max(0, 1.0 - s.SystemWear);
                double waterDemand = c.WaterUsePerCrew * crew.Count;
                double oxygenDemand = c.OxygenUsePerCrew * crew.Count;
                double availableEnergy = s.Battery > 0 ? double.PositiveInfinity : solar;

                EarthOrderState activeOrder = null;
                double orderWorkRate = 0;
                double orderEnergy = 0;
                bool orderUsesOxygenSplit = false;
                if (simState.EarthOrders != null)
                {
                    for (int i = 0; i < simState.EarthOrders.Count; i++)
                    {
                        var candidateOrder = simState.EarthOrders[i];
                        if (candidateOrder.Status != EarthOrderStatus.InProgress || candidateOrder.AssignedCrewId == null ||
                            candidateOrder.StepProgressSeconds >= candidateOrder.StepRequiredSeconds)
                            continue;
                        for (int j = 0; j < crew.Count; j++)
                        {
                            if (crew[j].Id == candidateOrder.AssignedCrewId && crew[j].Status == CrewStatus.Working && crew[j].Stamina > 0 && crew[j].ActiveTaskId == candidateOrder.OrderId)
                            {
                                activeOrder = candidateOrder;
                                break;
                            }
                        }
                        if (activeOrder != null) break;
                    }
                }
                bool oxygenExperimentWorking = activeOrder != null &&
                    EarthOrderBagState.GetStepSpec(activeOrder.Kind, activeOrder.CurrentStepIndex).UsesOxygenSplit;
                double tankShare = oxygenExperimentWorking ? 0.90 : 1.0;
                double o2HealthFactor = s.OxygenProducerFaulted ? 0.5 : 1.0;
                double maxO2Prod = c.OxygenProductionRate * systemFactor * o2HealthFactor;
                double production = Math.Min(maxO2Prod, s.Oxygen >= c.OxygenCapacity ? oxygenDemand / tankShare : double.PositiveInfinity);
                if (c.OxygenEnergyPerUnit > 0)
                    production = Math.Min(production, availableEnergy / c.OxygenEnergyPerUnit);

                double recycling;
                double consumedWater;
                if (s.Water <= 0)
                {
                    if (s.Wastewater <= 0)
                    {
                        if (c.OxygenWaterPerUnit > 0) production = 0;
                        recycling = 0;
                        consumedWater = 0;
                    }
                    else
                    {
                        double low = 0, high = production;
                        for (int i = 0; i < 64 && c.OxygenWaterPerUnit > 0; i++)
                        {
                            double mid = (low + high) / 2;
                            double r = RecycleRate(c, s, systemFactor, waterDemand, mid, availableEnergy);
                            if (mid * c.OxygenWaterPerUnit + waterDemand <= r) low = mid; else high = mid;
                        }
                        if (c.OxygenWaterPerUnit > 0) production = low;
                        recycling = RecycleRate(c, s, systemFactor, waterDemand, production, availableEnergy);
                        consumedWater = Math.Min(waterDemand, recycling);
                        if (c.OxygenWaterPerUnit > 0)
                            production = Math.Min(production, Math.Max(0, recycling - consumedWater) / c.OxygenWaterPerUnit);
                    }
                }
                else
                {
                    consumedWater = waterDemand;
                    recycling = RecycleRate(c, s, systemFactor, consumedWater, production, availableEnergy);
                }

                double consumedOxygen = s.Oxygen > 0 ? oxygenDemand : Math.Min(oxygenDemand, production * tankShare);
                bool inOxygenOutage = oxygenDemand > 0 && s.Oxygen <= 0 && consumedOxygen < oxygenDemand - 1e-12;
                if (!inOxygenOutage)
                    s.ContinuousOxygenOutageSeconds = 0;

                double o2Energy = production * c.OxygenEnergyPerUnit;
                double recEnergy = recycling * c.RecyclingEnergyPerUnit;

                if (simState.Facilities != null)
                {
                    for (int i = 0; i < simState.Facilities.Count; i++)
                    {
                        simState.Facilities[i].LastWaterExtractionRate = 0;
                        simState.Facilities[i].LastEnergyDrawRate = 0;
                    }
                }

                double extraction = 0;
                double extractorEnergy = 0;
                double totalNetDemandForWater = Math.Max(0, consumedWater + production * c.OxygenWaterPerUnit - recycling);
                if (s.WaterExtractorEnabled && c.WaterExtractionRate > 0)
                {
                    double remainingAfterLifeSupport = s.Battery > 0
                        ? double.PositiveInfinity
                        : Math.Max(0, solar - o2Energy - recEnergy);
                    double faultFactor = s.WaterExtractorFaulted ? 0.5 : 1.0;
                    double maxExtract = c.WaterExtractionRate * faultFactor;
                    double desiredExtract = s.Water >= c.WaterCapacity
                        ? Math.Min(maxExtract, totalNetDemandForWater)
                        : maxExtract;
                    if (c.WaterExtractorEnergyRate > 0)
                    {
                        double energyPerUnit = c.WaterExtractorEnergyRate / c.WaterExtractionRate;
                        extraction = Math.Min(desiredExtract, remainingAfterLifeSupport / energyPerUnit);
                        extractorEnergy = extraction * energyPerUnit;
                    }
                    else
                    {
                        extraction = desiredExtract;
                    }
                }

                // Player water extraction sites (after legacy-extractor, fixed FacilityId order, never double-counting legacy).
                double siteExtractionTotal = 0;
                double allocatedNewExtractorEnergy = 0;
                var activeSiteFlows = new List<(FacilityState Fac, ParcelState Cell, double Rate)>();
                if (config.Mode == MissionMode.Operations && simState.Map != null && simState.Facilities != null)
                {
                    var siteExtractors = new List<FacilityState>();
                    for (int i = 0; i < simState.Facilities.Count; i++)
                    {
                        var fac = simState.Facilities[i];
                        if (fac == null || fac.IsProtectedStarter || fac.FacilityId == ParcelMap.LegacyExtractorSpec.FacilityId ||
                            fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                            continue;
                        if (FacilityCatalog.TryGetSpec(fac.Kind, out var exSpec) && exSpec.WaterProductionRate > 0 &&
                            exSpec.RequiredResource != ParcelResourceKind.None)
                        {
                            siteExtractors.Add(fac);
                        }
                    }
                    if (siteExtractors.Count > 1)
                        siteExtractors.Sort((a, b) => string.CompareOrdinal(a.FacilityId, b.FacilityId));

                    double remainingDemandToHoldFull = Math.Max(0, totalNetDemandForWater - extraction);
                    double globalFaultFactor = s.WaterExtractorFaulted ? 0.5 : 1.0;
                    for (int i = 0; i < siteExtractors.Count; i++)
                    {
                        var fac = siteExtractors[i];
                        if (!s.WaterExtractorEnabled || !fac.IsEnabled || !fac.IsConnectedToCore)
                            continue;
                        var exSpec = FacilityCatalog.GetSpec(fac.Kind);
                        if (!simState.Map.TryGetCell(fac.Origin, out var siteCell) ||
                            siteCell.ResourceKind != exSpec.RequiredResource ||
                            siteCell.RemainingWater <= 0)
                        {
                            continue;
                        }

                        double yieldFactor = siteCell.YieldFactor > 0 ? siteCell.YieldFactor : 1.0;
                        double siteMaxRate = exSpec.WaterProductionRate * yieldFactor * globalFaultFactor;
                        double desiredSiteRate = s.Water >= c.WaterCapacity
                            ? Math.Min(siteMaxRate, remainingDemandToHoldFull)
                            : siteMaxRate;
                        if (desiredSiteRate <= 0)
                            continue;

                        double energyPerUnit = exSpec.WaterEnergyPerUnit;
                        double remainingPower = s.Battery > 0
                            ? double.PositiveInfinity
                            : Math.Max(0, solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy);
                        double maxAffordableRate = energyPerUnit > 0 ? remainingPower / energyPerUnit : desiredSiteRate;
                        double actualSiteRate = Math.Min(desiredSiteRate, maxAffordableRate);
                        if (actualSiteRate <= 0)
                            continue;

                        double actualSiteEnergy = actualSiteRate * energyPerUnit;
                        fac.LastWaterExtractionRate = actualSiteRate;
                        fac.LastEnergyDrawRate = actualSiteEnergy;
                        siteExtractionTotal += actualSiteRate;
                        allocatedNewExtractorEnergy += actualSiteEnergy;
                        if (s.Water >= c.WaterCapacity)
                            remainingDemandToHoldFull = Math.Max(0, remainingDemandToHoldFull - actualSiteRate);
                        activeSiteFlows.Add((fac, siteCell, actualSiteRate));
                    }
                }
                s.LastLegacyWaterExtractionRate = extraction;
                s.LastSiteWaterExtractionRate = siteExtractionTotal;
                s.LastWaterExtractionRate = extraction + siteExtractionTotal;
                s.LastRecycledWaterRate = recycling;

                bool hasResearchCrew = false;
                bool hasO2RepairCrew = false;
                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    if (member.Status == CrewStatus.Working && member.Stamina > 0 && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var activeTask))
                    {
                        if (activeTask.Kind == TaskKind.Research)
                            hasResearchCrew = true;
                        else if (activeTask.Kind == TaskKind.OxygenProducerRepair)
                            hasO2RepairCrew = true;
                    }
                }

                double o2RepairEnergy = 0;
                double o2RepairFactor = 0;
                if (hasO2RepairCrew && s.OxygenProducerFaulted)
                {
                    if (s.Battery > 0 || c.OxygenRepairEnergyRate <= 0)
                    {
                        o2RepairEnergy = c.OxygenRepairEnergyRate;
                        o2RepairFactor = 1.0;
                    }
                    else
                    {
                        double remainingForRepair = Math.Max(0, solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy);
                        o2RepairEnergy = Math.Min(c.OxygenRepairEnergyRate, remainingForRepair);
                        o2RepairFactor = c.OxygenRepairEnergyRate > 0 ? o2RepairEnergy / c.OxygenRepairEnergyRate : 1.0;
                    }
                }

                double researchEnergy = 0;
                double researchFactor = 0;
                if (s.ResearchEnabled && hasResearchCrew)
                {
                    if (s.Battery > 0 || c.ResearchEnergyRate <= 0)
                    {
                        researchEnergy = c.ResearchEnergyRate;
                        researchFactor = 1.0;
                    }
                    else
                    {
                        double remainingSolar = Math.Max(0, solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy - o2RepairEnergy);
                        researchEnergy = Math.Min(c.ResearchEnergyRate, remainingSolar);
                        researchFactor = researchEnergy / c.ResearchEnergyRate;
                    }
                }
                s.LastResearchRateFactor = researchFactor;

                if (activeOrder != null)
                {
                    var orderSpec = EarthOrderBagState.GetStepSpec(activeOrder.Kind, activeOrder.CurrentStepIndex);
                    if (orderSpec.UsesOxygenSplit)
                    {
                        orderUsesOxygenSplit = true;
                        orderWorkRate = production > 0 ? 1.0 : 0.0;
                    }
                    else if (orderSpec.EnergyRate > 0)
                    {
                        if (s.Battery > 0)
                        {
                            orderEnergy = orderSpec.EnergyRate;
                            orderWorkRate = 1.0;
                        }
                        else
                        {
                            double remainingForOrder = Math.Max(0, solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy - o2RepairEnergy - researchEnergy);
                            orderEnergy = Math.Min(orderSpec.EnergyRate, remainingForOrder);
                            orderWorkRate = orderEnergy / orderSpec.EnergyRate;
                        }
                    }
                    else
                    {
                        orderWorkRate = 1.0;
                    }
                }

                double allocatedBuildEnergy = 0;
                for (int i = 0; i < crew.Count; i++)
                {
                    crewSurfaceWorkFactors[i] = 1.0;
                    crewSurfaceEnergyRates[i] = 0;
                    var member = crew[i];
                    if (member.Status != CrewStatus.Working || member.Stamina <= 0) continue;
                    var sJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                    if (sJob == null || sJob.Phase != SurfaceJobPhase.Work || sJob.WorkProgressSeconds >= sJob.WorkRequiredSeconds) continue;

                    if (sJob.Kind == SurfaceJobKind.Construct)
                    {
                        var fac = FindFacility(simState.Facilities, sJob.TargetId);
                        if (fac == null || fac.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                        {
                            crewSurfaceWorkFactors[i] = 0;
                            continue;
                        }
                        double remEnergy = Math.Max(0, fac.EnergyRequiredTotal - fac.EnergySpentTotal);
                        double remWork = Math.Max(0, fac.WorkRequiredSeconds - fac.WorkProgressSeconds);
                        double energyRate = remEnergy > 0 && remWork > 0 ? remEnergy / remWork : 0;
                        if (energyRate > 0)
                        {
                            double remainingPower = s.Battery > 0
                                ? double.PositiveInfinity
                                : Math.Max(0, solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy - o2RepairEnergy - researchEnergy - orderEnergy - allocatedBuildEnergy);
                            double draw = Math.Min(energyRate, remainingPower);
                            crewSurfaceEnergyRates[i] = draw;
                            crewSurfaceWorkFactors[i] = draw / energyRate;
                            allocatedBuildEnergy += draw;
                        }
                    }
                }

                ColonyStorage.ReconcileStorageOverflow(simState, c);
                double effectiveBatteryCap = ColonyStorage.GetEffectiveBatteryCapacity(c, simState.Facilities);
                double batteryRate = solar - o2Energy - recEnergy - extractorEnergy - allocatedNewExtractorEnergy - o2RepairEnergy - researchEnergy - orderEnergy - allocatedBuildEnergy;
                double waterRate = recycling + extraction + siteExtractionTotal - consumedWater - production * c.OxygenWaterPerUnit;
                double waterFlowScale = Math.Max(recycling + extraction + siteExtractionTotal, consumedWater + production * c.OxygenWaterPerUnit);
                if (s.Water == 0 && Math.Abs(waterRate) <= 1e-14 * waterFlowScale) waterRate = 0;
                double effectiveProduction = (orderUsesOxygenSplit && orderWorkRate > 0) ? production * 0.90 : production;
                double oxygenRate = effectiveProduction - consumedOxygen;
                double wasteRate = consumedWater - recycling;
                if (s.Battery >= effectiveBatteryCap && batteryRate > 0) batteryRate = 0;
                if (s.Wastewater >= c.WastewaterCapacity && wasteRate > 0) wasteRate = 0;

                dt = Limit(dt, s.Battery, effectiveBatteryCap, batteryRate);
                dt = Limit(dt, s.Water, c.WaterCapacity, waterRate);
                dt = Limit(dt, s.Oxygen, c.OxygenCapacity, oxygenRate);
                dt = Limit(dt, s.Wastewater, c.WastewaterCapacity, wasteRate);

                for (int i = 0; i < activeSiteFlows.Count; i++)
                {
                    var flow = activeSiteFlows[i];
                    if (flow.Rate > 0 && flow.Cell.RemainingWater > 0)
                        dt = Math.Min(dt, flow.Cell.RemainingWater / flow.Rate);
                }

                if (simState.CanTriggerLowOxygenAlert && oxygenRate < 0 && s.Oxygen > config.LowOxygenWarningThreshold)
                    dt = Math.Min(dt, (s.Oxygen - config.LowOxygenWarningThreshold) / -oxygenRate);

                if (inOxygenOutage && config.OxygenOutageFailureSeconds > 0 && config.OxygenOutageFailureSeconds > s.ContinuousOxygenOutageSeconds)
                    dt = Math.Min(dt, config.OxygenOutageFailureSeconds - s.ContinuousOxygenOutageSeconds);

                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor, crewSurfaceWorkFactors[i]);
                    if (staminaRate < 0 && member.Stamina > 0)
                        dt = Math.Min(dt, member.Stamina / -staminaRate);
                    else if (staminaRate > 0 && member.Stamina < config.MaxStamina)
                        dt = Math.Min(dt, (config.MaxStamina - member.Stamina) / staminaRate);

                    if (member.ActiveSurfaceJobId == null && member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds > 0)
                        dt = Math.Min(dt, member.ReturnRemainingSeconds);

                    SurfaceJobState sJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                    if (sJob != null)
                    {
                        if (sJob.Phase == SurfaceJobPhase.Travel && sJob.LegDistanceTraveled < sJob.LegTotalDistance)
                        {
                            double remTravel = (sJob.LegTotalDistance - sJob.LegDistanceTraveled) / sJob.TravelSpeed;
                            if (remTravel > 0) dt = Math.Min(dt, remTravel);
                        }
                        else if (sJob.Phase == SurfaceJobPhase.Return)
                        {
                            if (sJob.LegDistanceTraveled < sJob.LegTotalDistance)
                            {
                                double remWalk = (sJob.LegTotalDistance - sJob.LegDistanceTraveled) / sJob.TravelSpeed;
                                if (remWalk > 0) dt = Math.Min(dt, remWalk);
                            }
                            else if (sJob.AirlockRemainingSeconds > 0)
                            {
                                dt = Math.Min(dt, sJob.AirlockRemainingSeconds);
                            }
                        }
                        else if (sJob.Phase == SurfaceJobPhase.Work && member.Status == CrewStatus.Working &&
                                 sJob.WorkProgressSeconds < sJob.WorkRequiredSeconds)
                        {
                            double remWork = sJob.WorkRequiredSeconds - sJob.WorkProgressSeconds;
                            double wf = crewSurfaceWorkFactors[i];
                            if (remWork > 0 && wf > 0) dt = Math.Min(dt, remWork / wf);
                        }
                    }

                    if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                        !task.IsContinuous && !task.IsCompleted && task.ProgressSeconds < task.RequiredSeconds)
                    {
                        double workRate = task.Kind == TaskKind.Research
                            ? researchFactor
                            : (task.Kind == TaskKind.OxygenProducerRepair ? o2RepairFactor : 1.0);
                        if (workRate > 0)
                            dt = Math.Min(dt, (task.RequiredSeconds - task.ProgressSeconds) / workRate);
                    }

                    double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                    if (riskRate > 0 && member.RiskScore < config.MaxCrewRiskScore)
                        dt = Math.Min(dt, (config.MaxCrewRiskScore - member.RiskScore) / riskRate);
                    else if (riskRate < 0 && member.RiskScore > 0)
                        dt = Math.Min(dt, member.RiskScore / -riskRate);
                }

                if (activeOrder != null && orderWorkRate > 0)
                    dt = Math.Min(dt, (activeOrder.StepRequiredSeconds - activeOrder.StepProgressSeconds) / orderWorkRate);

                if (dt <= 0)
                    throw new InvalidOperationException("Tick cannot advance at this resource precision.");

                if (elapsed + dt == elapsed)
                {
                    bool snapped = false;
                    double value = s.Battery;
                    if (SnapBoundary(ref value, effectiveBatteryCap, batteryRate, dt)) { s.Battery = value; snapped = true; }
                    value = s.Water;
                    if (SnapBoundary(ref value, c.WaterCapacity, waterRate, dt)) { s.Water = value; snapped = true; }
                    value = s.Oxygen;
                    if (SnapBoundary(ref value, c.OxygenCapacity, oxygenRate, dt)) { s.Oxygen = value; snapped = true; }
                    value = s.Wastewater;
                    if (SnapBoundary(ref value, c.WastewaterCapacity, wasteRate, dt)) { s.Wastewater = value; snapped = true; }

                    if (inOxygenOutage && config.OxygenOutageFailureSeconds > 0 && config.OxygenOutageFailureSeconds - s.ContinuousOxygenOutageSeconds <= dt)
                    {
                        s.ContinuousOxygenOutageSeconds = config.OxygenOutageFailureSeconds;
                        snapped = true;
                    }

                    for (int i = 0; i < crew.Count; i++)
                    {
                        var member = crew[i];
                        double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor, crewSurfaceWorkFactors[i]);
                        double stam = member.Stamina;
                        if (SnapBoundary(ref stam, config.MaxStamina, staminaRate, dt)) { member.Stamina = stam; snapped = true; }
                        if (member.ActiveSurfaceJobId == null && member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds > 0 && member.ReturnRemainingSeconds <= dt)
                        {
                            member.ReturnRemainingSeconds = 0;
                            snapped = true;
                        }
                        SurfaceJobState sJobSnap = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                        if (sJobSnap != null)
                        {
                            if (sJobSnap.Phase == SurfaceJobPhase.Travel && sJobSnap.LegDistanceTraveled < sJobSnap.LegTotalDistance &&
                                (sJobSnap.LegTotalDistance - sJobSnap.LegDistanceTraveled) / sJobSnap.TravelSpeed <= dt)
                            {
                                sJobSnap.LegDistanceTraveled = sJobSnap.LegTotalDistance;
                                sJobSnap.EvaluatePositionAndHeading(out var snapPos, out _);
                                member.SurfacePosX = snapPos.X;
                                member.SurfacePosZ = snapPos.Z;
                                snapped = true;
                            }
                            else if (sJobSnap.Phase == SurfaceJobPhase.Return)
                            {
                                if (sJobSnap.LegDistanceTraveled < sJobSnap.LegTotalDistance &&
                                    (sJobSnap.LegTotalDistance - sJobSnap.LegDistanceTraveled) / sJobSnap.TravelSpeed <= dt)
                                {
                                    sJobSnap.LegDistanceTraveled = sJobSnap.LegTotalDistance;
                                    sJobSnap.EvaluatePositionAndHeading(out var snapPos, out _);
                                    member.SurfacePosX = snapPos.X;
                                    member.SurfacePosZ = snapPos.Z;
                                    member.ReturnRemainingSeconds = sJobSnap.GetRemainingLegSeconds();
                                    snapped = true;
                                }
                                else if (sJobSnap.AirlockRemainingSeconds > 0 && sJobSnap.AirlockRemainingSeconds <= dt)
                                {
                                    sJobSnap.AirlockRemainingSeconds = 0;
                                    member.ReturnRemainingSeconds = 0;
                                    snapped = true;
                                }
                            }
                            else if (sJobSnap.Phase == SurfaceJobPhase.Work && member.Status == CrewStatus.Working &&
                                     sJobSnap.WorkProgressSeconds < sJobSnap.WorkRequiredSeconds &&
                                     crewSurfaceWorkFactors[i] > 0 &&
                                     sJobSnap.WorkRequiredSeconds - sJobSnap.WorkProgressSeconds <= crewSurfaceWorkFactors[i] * dt)
                            {
                                double prevWorkSnap = sJobSnap.WorkProgressSeconds;
                                double prevEnergySnap = sJobSnap.EnergyConsumed;
                                sJobSnap.WorkProgressSeconds = sJobSnap.WorkRequiredSeconds;
                                if (crewSurfaceEnergyRates[i] > 0)
                                    sJobSnap.EnergyConsumed = Apply(sJobSnap.EnergyConsumed, crewSurfaceEnergyRates[i], dt, sJobSnap.WorkEnergyTotal);
                                if (sJobSnap.Kind == SurfaceJobKind.Construct)
                                {
                                    var facSnap = FindFacility(simState.Facilities, sJobSnap.TargetId);
                                    if (facSnap != null && facSnap.LifecycleStatus == FacilityLifecycleStatus.Blueprint)
                                    {
                                        facSnap.WorkProgressSeconds = Math.Min(facSnap.WorkRequiredSeconds, facSnap.WorkProgressSeconds + Math.Max(0, sJobSnap.WorkProgressSeconds - prevWorkSnap));
                                        facSnap.EnergySpentTotal = Math.Min(facSnap.EnergyRequiredTotal, facSnap.EnergySpentTotal + Math.Max(0, sJobSnap.EnergyConsumed - prevEnergySnap));
                                    }
                                }
                                snapped = true;
                            }
                        }
                        if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                            taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                            !task.IsContinuous && !task.IsCompleted)
                        {
                            double workRate = task.Kind == TaskKind.Research
                                ? researchFactor
                                : (task.Kind == TaskKind.OxygenProducerRepair ? o2RepairFactor : 1.0);
                            double prog = task.ProgressSeconds;
                            if (SnapBoundary(ref prog, task.RequiredSeconds, workRate, dt)) { task.ProgressSeconds = prog; snapped = true; }
                        }
                        double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                        double risk = member.RiskScore;
                        if (SnapBoundary(ref risk, config.MaxCrewRiskScore, riskRate, dt)) { member.RiskScore = risk; snapped = true; }
                    }
                    if (activeOrder != null && orderWorkRate > 0)
                    {
                        double op = activeOrder.StepProgressSeconds;
                        if (SnapBoundary(ref op, activeOrder.StepRequiredSeconds, orderWorkRate, dt)) { activeOrder.StepProgressSeconds = op; snapped = true; }
                    }
                    for (int i = 0; i < activeSiteFlows.Count; i++)
                    {
                        var flow = activeSiteFlows[i];
                        if (flow.Rate > 0 && flow.Cell.RemainingWater > 0)
                        {
                            double drawn = Math.Min(flow.Cell.RemainingWater, flow.Rate * dt);
                            if (flow.Cell.RemainingWater <= flow.Rate * dt + 1e-12)
                            {
                                drawn = flow.Cell.RemainingWater;
                                flow.Cell.RemainingWater = 0;
                                snapped = true;
                            }
                            else
                            {
                                flow.Cell.RemainingWater = Math.Max(0, flow.Cell.RemainingWater - drawn);
                            }
                            s.TotalSiteExtractedWater += drawn;
                            s.TotalExtractedWater += drawn;
                        }
                    }

                    if (!snapped)
                        throw new InvalidOperationException("Tick cannot advance at this resource precision.");
                    ApplyTransitions(simState, taskLookup, config, c, elapsed);
                    HandleCriticalOxygenCheck(simState, config, c, oxygenRate);
                    if (CheckTerminal(simState, config, elapsed))
                        return elapsed;
                    continue;
                }

                s.Battery = Apply(s.Battery, batteryRate, dt, effectiveBatteryCap);
                s.Water = Apply(s.Water, waterRate, dt, c.WaterCapacity);
                s.Oxygen = Apply(s.Oxygen, oxygenRate, dt, c.OxygenCapacity);
                s.Wastewater = Apply(s.Wastewater, wasteRate, dt, c.WastewaterCapacity);
                for (int i = 0; i < activeSiteFlows.Count; i++)
                {
                    var flow = activeSiteFlows[i];
                    if (flow.Rate > 0 && flow.Cell.RemainingWater > 0)
                    {
                        double drawn = Math.Min(flow.Cell.RemainingWater, flow.Rate * dt);
                        flow.Cell.RemainingWater = Math.Max(0, flow.Cell.RemainingWater - drawn);
                        if (flow.Cell.RemainingWater <= 1e-12)
                            flow.Cell.RemainingWater = 0;
                        s.TotalSiteExtractedWater += drawn;
                        s.TotalExtractedWater += drawn;
                    }
                }
                s.TotalLegacyExtractedWater += extraction * dt;
                s.TotalExtractedWater += extraction * dt;
                s.TotalRecycledWater += recycling * dt;
                s.UnmetWater += (waterDemand - consumedWater) * dt;
                s.UnmetOxygen += (oxygenDemand - consumedOxygen) * dt;
                double researchGained = researchFactor * c.ResearchProgressRate * dt;
                s.ResearchProgress += researchGained;
                if (config.Mode == MissionMode.Operations)
                    s.ScienceBalance += researchGained;

                if (inOxygenOutage)
                {
                    double cap = config.OxygenOutageFailureSeconds > 0 ? config.OxygenOutageFailureSeconds : double.MaxValue;
                    s.ContinuousOxygenOutageSeconds = Apply(s.ContinuousOxygenOutageSeconds, 1.0, dt, cap);
                }

                if (!MissionConfig.IsFinite(s.UnmetWater) || !MissionConfig.IsFinite(s.UnmetOxygen) || !MissionConfig.IsFinite(s.ResearchProgress) || !MissionConfig.IsFinite(s.ScienceBalance))
                    throw new InvalidOperationException("Resource totals overflowed.");

                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor, crewSurfaceWorkFactors[i]);
                    if (staminaRate != 0)
                        member.Stamina = Apply(member.Stamina, staminaRate, dt, config.MaxStamina);

                    if (member.ActiveSurfaceJobId == null && member.Status == CrewStatus.Returning)
                    {
                        double rem = member.ReturnRemainingSeconds - dt;
                        double tol = 1e-12 * Math.Max(1.0, config.ReturnToShelterSeconds);
                        member.ReturnRemainingSeconds = rem < tol ? 0 : Math.Max(0, rem);
                    }

                    SurfaceJobState activeSJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                    if (activeSJob != null)
                    {
                        if (activeSJob.Phase == SurfaceJobPhase.Travel)
                        {
                            activeSJob.LegDistanceTraveled = Apply(activeSJob.LegDistanceTraveled, activeSJob.TravelSpeed, dt, activeSJob.LegTotalDistance);
                            activeSJob.EvaluatePositionAndHeading(out var pos, out _);
                            member.SurfacePosX = pos.X;
                            member.SurfacePosZ = pos.Z;
                            member.HasSurfacePosition = true;
                        }
                        else if (activeSJob.Phase == SurfaceJobPhase.Work && member.Status == CrewStatus.Working)
                        {
                            double wf = crewSurfaceWorkFactors[i];
                            double eRate = crewSurfaceEnergyRates[i];
                            double prevWork = activeSJob.WorkProgressSeconds;
                            double prevEnergy = activeSJob.EnergyConsumed;
                            activeSJob.WorkProgressSeconds = Apply(activeSJob.WorkProgressSeconds, wf, dt, activeSJob.WorkRequiredSeconds);
                            if (eRate > 0)
                                activeSJob.EnergyConsumed = Apply(activeSJob.EnergyConsumed, eRate, dt, activeSJob.WorkEnergyTotal);
                            if (activeSJob.Kind == SurfaceJobKind.Construct)
                            {
                                var fac = FindFacility(simState.Facilities, activeSJob.TargetId);
                                if (fac != null && fac.LifecycleStatus == FacilityLifecycleStatus.Blueprint)
                                {
                                    fac.WorkProgressSeconds = Math.Min(fac.WorkRequiredSeconds, fac.WorkProgressSeconds + Math.Max(0, activeSJob.WorkProgressSeconds - prevWork));
                                    fac.EnergySpentTotal = Math.Min(fac.EnergyRequiredTotal, fac.EnergySpentTotal + Math.Max(0, activeSJob.EnergyConsumed - prevEnergy));
                                }
                            }
                            else if (activeSJob.Kind == SurfaceJobKind.Repair && simState.Resources?.Panels != null)
                            {
                                double deltaWork = Math.Max(0, activeSJob.WorkProgressSeconds - prevWork);
                                if (deltaWork > 0)
                                {
                                    for (int p = 0; p < simState.Resources.Panels.Count; p++)
                                    {
                                        var panel = simState.Resources.Panels[p];
                                        if (panel.Id == activeSJob.TargetId && panel.IsFaulted)
                                        {
                                            panel.RepairProgressSeconds = Math.Min(PanelUnitState.DefaultRepairSeconds, panel.RepairProgressSeconds + deltaWork);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (activeSJob.Phase == SurfaceJobPhase.Return && member.Status == CrewStatus.Returning)
                        {
                            if (activeSJob.LegDistanceTraveled < activeSJob.LegTotalDistance)
                            {
                                activeSJob.LegDistanceTraveled = Apply(activeSJob.LegDistanceTraveled, activeSJob.TravelSpeed, dt, activeSJob.LegTotalDistance);
                            }
                            else if (activeSJob.AirlockRemainingSeconds > 0)
                            {
                                double airRem = activeSJob.AirlockRemainingSeconds - dt;
                                activeSJob.AirlockRemainingSeconds = airRem < 1e-12 ? 0 : Math.Max(0, airRem);
                            }
                            activeSJob.EvaluatePositionAndHeading(out var pos, out _);
                            member.SurfacePosX = pos.X;
                            member.SurfacePosZ = pos.Z;
                            member.HasSurfacePosition = true;
                            member.ReturnRemainingSeconds = activeSJob.GetRemainingLegSeconds();
                        }
                    }

                    if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                        !task.IsContinuous && !task.IsCompleted)
                    {
                        double workRate = task.Kind == TaskKind.Research
                            ? researchFactor
                            : (task.Kind == TaskKind.OxygenProducerRepair ? o2RepairFactor : 1.0);
                        if (workRate > 0)
                            task.ProgressSeconds = Apply(task.ProgressSeconds, workRate, dt, task.RequiredSeconds);
                    }

                    double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                    if (riskRate != 0)
                        member.RiskScore = Apply(member.RiskScore, riskRate, dt, config.MaxCrewRiskScore);
                }

                if (activeOrder != null && orderWorkRate > 0)
                    activeOrder.StepProgressSeconds = Apply(activeOrder.StepProgressSeconds, orderWorkRate, dt, activeOrder.StepRequiredSeconds);

                elapsed += dt;
                ApplyTransitions(simState, taskLookup, config, c, elapsed);
                UpdateEventPhases(events, elapsed, simState.SolTracker);
                SettleNightMealIfDue(simState, config, elapsed);
                CheckEarthAdviceDelivery(simState, config, elapsed);
                if (config.EnableOperationsRandomEvents && simState.ChannelSchedules != null)
                    ProcessChannelBoundaries(simState, elapsed);
                ComputeEnvironment(events, elapsed, out double nextDust, out double _);
                s.AtmosphericDustLoss = nextDust;

                HandleCriticalOxygenCheck(simState, config, c, oxygenRate);

                double solTol = 1e-12 * Math.Max(1.0, nextSolEnd);
                if (elapsed + solTol >= nextSolEnd)
                    FinalizeSol(simState, config);

                if (CheckTerminal(simState, config, elapsed))
                    return elapsed;
            }

            return elapsed;
        }

        private static void HandleCriticalOxygenCheck(
            SimulationMutableState simState,
            MissionConfig config,
            ResourceConfig c,
            double oxygenRate)
        {
            var s = simState.Resources;
            double threshold = config.LowOxygenWarningThreshold;
            if (threshold <= 0) return;

            bool crossingDown = s.Oxygen < threshold || (s.Oxygen <= threshold + 1e-9 && oxygenRate < 0);
            if (simState.CanTriggerLowOxygenAlert && crossingDown)
            {
                simState.CanTriggerLowOxygenAlert = false;
                simState.LowOxygenAlertCount++;
                simState.CriticalAlertPauseRequested = true;
                simState.ActiveAlertMessage = $"Critical oxygen reserve: dropped below {threshold:0.#} units.";
                if (simState.IsSleeping)
                    WakeCrewFromSleep(simState);
            }
            else if (!simState.CanTriggerLowOxygenAlert)
            {
                double rearm = Math.Min(c.OxygenCapacity, threshold + 1.0);
                if (s.Oxygen >= rearm)
                    simState.CanTriggerLowOxygenAlert = true;
            }
        }

        internal static void WakeCrewFromSleep(SimulationMutableState simState)
        {
            simState.IsSleeping = false;
            for (int i = 0; i < simState.Crew.Count; i++)
            {
                var member = simState.Crew[i];
                if (member.Status == CrewStatus.Sleeping)
                    member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
            }
        }

        private static void FinalizeSol(SimulationMutableState simState, MissionConfig config)
        {
            if (simState.IsSleeping)
                WakeCrewFromSleep(simState);

            var tracker = simState.SolTracker;
            var s = simState.Resources;
            var staminaDeltas = new Dictionary<string, double>(simState.Crew.Count);
            for (int i = 0; i < simState.Crew.Count; i++)
            {
                var member = simState.Crew[i];
                double startStam = tracker.StartStamina.TryGetValue(member.Id, out double v) ? v : member.Stamina;
                staminaDeltas[member.Id] = member.Stamina - startStam;
            }

            double researchGained = s.ResearchProgress - tracker.StartResearch;
            string summary = BuildSolSummary(tracker, s, researchGained);

            var report = new SolReport(
                tracker.SolNumber,
                tracker.StartBattery, s.Battery,
                tracker.StartWater, s.Water,
                tracker.StartOxygen, s.Oxygen,
                researchGained,
                staminaDeltas,
                tracker.CompletedTasks,
                tracker.EventsExperienced,
                summary);

            simState.SolReports.Add(report);
            simState.PendingSolReport = report;
            simState.SolReportPauseRequested = true;

            simState.MealPreparedThisSol = false;
            simState.SleepStartedThisSol = false;
            simState.NightRationAvailable = false;
            simState.NightSleepRecoveryFactor = config.UnpreppedSleepFactor;
            for (int i = 0; i < simState.Tasks.Count; i++)
            {
                var task = simState.Tasks[i];
                if (task.Kind == TaskKind.MealPreparation)
                {
                    task.IsCompleted = false;
                    task.RewardApplied = false;
                    task.ProgressSeconds = 0;
                    task.AssignedCrewId = null;
                }
            }

            if (config.Mode == MissionMode.Operations && config.DailyReserveMealSupply > 0)
            {
                simState.FoodRationsRemaining = Math.Min(
                    config.MaxReserveMeals,
                    simState.FoodRationsRemaining + config.DailyReserveMealSupply);
            }

            tracker.ResetForSol(tracker.SolNumber + 1, s, simState.Crew);

            long newSol = tracker.SolNumber;
            if (config.Mode == MissionMode.Operations &&
                config.DailyMaterialSupply > 0 &&
                simState.LastMaterialSupplySol < newSol)
            {
                simState.Materials = Math.Min(config.MaterialCapacity, simState.Materials + config.DailyMaterialSupply);
                simState.LastMaterialSupplySol = newSol;
            }
            if (config.Mode == MissionMode.Operations &&
                config.EarthOrderArrivalIntervalSols > 0 &&
                newSol >= 3 &&
                (newSol - 3) % config.EarthOrderArrivalIntervalSols == 0 &&
                simState.EarthOrderBag != null)
            {
                var order = simState.EarthOrderBag.DrawNextOrder(newSol);
                simState.EarthOrders?.Add(order);
                simState.OperationsLog?.Add($"[Sol {newSol}] Earth order arrived: {order.Code} ({order.OrderId})");
            }
        }

        private static string BuildSolSummary(SolTracker tracker, ResourceState s, double researchGained)
        {
            var parts = new List<string>();
            if (tracker.EventsExperienced.Count > 0)
                parts.Add($"Events: {string.Join(", ", tracker.EventsExperienced)}.");
            else
                parts.Add("No hazard events occurred this sol.");

            parts.Add($"Research gained: {researchGained:0.#}.");
            if (s.Battery < tracker.StartBattery - 5)
                parts.Add("Battery reserve ended lower than start; watch daytime solar load.");
            if (s.Oxygen < tracker.StartOxygen - 5)
                parts.Add("Oxygen reserve declined; check life-support power and water.");
            return string.Join(" ", parts);
        }

        private static void UpdateEventPhases(List<EventState> events, double elapsed, SolTracker tracker)
        {
            double tol = 1e-12 * Math.Max(1.0, elapsed);
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                if (!ev.WarningTriggered && elapsed + tol >= ev.WarningSeconds)
                    ev.WarningTriggered = true;
                if (!ev.StartTriggered && elapsed + tol >= ev.StartSeconds)
                {
                    ev.StartTriggered = true;
                    if (!tracker.EventsExperienced.Contains(ev.Title))
                        tracker.EventsExperienced.Add(ev.Title);
                }
                if (!ev.EndTriggered && elapsed + tol >= ev.EndSeconds)
                    ev.EndTriggered = true;

                if (ev.EndTriggered)
                    ev.Phase = EventPhase.Completed;
                else if (ev.StartTriggered)
                    ev.Phase = EventPhase.Active;
                else if (ev.WarningTriggered)
                    ev.Phase = EventPhase.Warning;
                else
                    ev.Phase = EventPhase.Upcoming;
            }
        }

        private static void ComputeEnvironment(List<EventState> events, double elapsed, out double dustLoss, out double radiationIntensity)
        {
            dustLoss = 0;
            radiationIntensity = 0;
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                if (ev.Phase == EventPhase.Active)
                {
                    if (ev.Kind == EventKind.Dust)
                        dustLoss = Math.Min(1.0, dustLoss + ev.Intensity);
                    else if (ev.Kind == EventKind.Radiation)
                        radiationIntensity += ev.Intensity;
                }
            }
        }

        private static double GetCrewRiskRate(CrewState member, MissionConfig config, double radiationIntensity)
        {
            if (radiationIntensity <= 0)
            {
                if (config.Mode == MissionMode.Operations &&
                    config.AcuteRiskRecoveryRate > 0 &&
                    member.Location == CrewLocation.Inside &&
                    member.RiskScore > 0)
                    return -config.AcuteRiskRecoveryRate;
                return 0;
            }
            double baseRate = member.Location == CrewLocation.Outside
                ? config.OutsideRadiationRiskRate
                : config.InsideRadiationRiskRate;
            return baseRate * radiationIntensity;
        }

        private static bool CheckTerminal(SimulationMutableState simState, MissionConfig config, double elapsed)
        {
            var s = simState.Resources;
            var crew = simState.Crew;
            double tol = 1e-9;

            // Failure checks strictly precede success checks.
            if (config.OxygenOutageFailureSeconds > 0 && s.ContinuousOxygenOutageSeconds >= config.OxygenOutageFailureSeconds - tol)
            {
                s.ContinuousOxygenOutageSeconds = config.OxygenOutageFailureSeconds;
                simState.Outcome = MissionOutcome.Failed;
                simState.FailureReason = MissionFailureReason.OxygenDepleted;
                simState.StatusMessage = "Mission ended: oxygen remained depleted for too long.";
                return true;
            }

            for (int i = 0; i < crew.Count; i++)
            {
                if (crew[i].RiskScore >= config.MaxCrewRiskScore - tol)
                {
                    crew[i].RiskScore = config.MaxCrewRiskScore;
                    simState.Outcome = MissionOutcome.Failed;
                    simState.FailureReason = MissionFailureReason.CrewRiskExceeded;
                    simState.StatusMessage = $"Mission ended for safety: {crew[i].Name} exceeded the radiation risk limit.";
                    return true;
                }
            }

            if (config.TargetSols > 0 && elapsed >= config.MissionDurationSeconds - 1e-12 * Math.Max(1.0, config.MissionDurationSeconds))
            {
                if (s.ResearchProgress >= config.ResearchGoal - tol)
                {
                    simState.Outcome = MissionOutcome.Succeeded;
                    simState.FailureReason = MissionFailureReason.None;
                    simState.StatusMessage = "Mission succeeded: crew stayed safe and met the two-sol research target.";
                }
                else
                {
                    simState.Outcome = MissionOutcome.Failed;
                    simState.FailureReason = MissionFailureReason.ResearchGoalMissed;
                    simState.StatusMessage = $"Mission ended: research target not reached ({s.ResearchProgress:0.#} / {config.ResearchGoal:0.#}).";
                }
                return true;
            }

            return false;
        }

        private static double GetCrewStaminaRate(
            CrewState member,
            MissionConfig config,
            SimulationMutableState simState,
            Dictionary<string, TaskState> taskLookup,
            double researchFactor,
            double surfaceWorkFactor = 1.0)
        {
            switch (member.Status)
            {
                case CrewStatus.Working:
                    if (member.ActiveTaskId != null && taskLookup.TryGetValue(member.ActiveTaskId, out var task))
                    {
                        double workFactor = task.Kind == TaskKind.Research ? researchFactor : 1.0;
                        return task.StaminaRatePerSecond * workFactor;
                    }
                    if (member.ActiveTaskId != null && simState.EarthOrders != null)
                    {
                        for (int i = 0; i < simState.EarthOrders.Count; i++)
                        {
                            var order = simState.EarthOrders[i];
                            if (order.OrderId == member.ActiveTaskId && order.Status == EarthOrderStatus.InProgress)
                                return EarthOrderBagState.GetStepSpec(order.Kind, order.CurrentStepIndex).StaminaRate;
                        }
                    }
                    var crewSurfaceJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                    if (crewSurfaceJob != null)
                    {
                        return crewSurfaceJob.Phase == SurfaceJobPhase.Travel
                            ? config.ReturnStaminaRate
                            : crewSurfaceJob.WorkStaminaRate * surfaceWorkFactor;
                    }
                    return 0;
                case CrewStatus.Resting:
                    return config.RestStaminaRate;
                case CrewStatus.Sleeping:
                    return config.SleepStaminaRate * simState.NightSleepRecoveryFactor;
                case CrewStatus.Returning:
                    return config.ReturnStaminaRate;
                default:
                    return 0;
            }
        }

        internal static void SettleNightMealIfDue(SimulationMutableState simState, MissionConfig config, double elapsed)
        {
            double phase = elapsed % config.SolSeconds;
            double tol = 1e-12 * Math.Max(1.0, config.SolSeconds);
            long sol = simState.SolTracker.SolNumber;
            if (phase + tol < config.DaySeconds || simState.NightMealSettledSol >= sol)
                return;

            bool hadRation = simState.FoodRationsRemaining > 0;
            if (hadRation)
                simState.FoodRationsRemaining--;

            simState.NightMealSettledSol = sol;
            simState.NightRationAvailable = hadRation;
            simState.NightSleepRecoveryFactor = (hadRation && simState.MealPreparedThisSol)
                ? 1.0
                : config.UnpreppedSleepFactor;
        }

        private static void ProcessChannelBoundaries(SimulationMutableState simState, double elapsed)
        {
            double tol = 1e-12 * Math.Max(1.0, elapsed);
            for (int i = 0; i < simState.ChannelSchedules.Count; i++)
            {
                var ch = simState.ChannelSchedules[i];
                if (!ch.IsActive && !double.IsPositiveInfinity(ch.NextTriggerAtSeconds))
                {
                    if (ch.WarningLeadSeconds > 0 && !ch.WarningIssued)
                    {
                        double warnAt = Math.Max(0, ch.NextTriggerAtSeconds - ch.WarningLeadSeconds);
                        if (elapsed + tol >= warnAt)
                        {
                            ch.WarningIssued = true;
                            simState.OperationsLog?.Add($"[{elapsed:0.#}s] {ch.ChannelId} warning -> starts at {ch.NextTriggerAtSeconds:0.#}s");
                            if (ch.ChannelId == OperationsChannelId.Radiation)
                                OnRadiationWarning(simState, elapsed, ch.NextTriggerAtSeconds);
                            else if (ch.ChannelId == OperationsChannelId.DustStorm)
                                OnDustStormWarning(simState, elapsed, ch.NextTriggerAtSeconds);
                        }
                    }

                    if (elapsed + tol >= ch.NextTriggerAtSeconds)
                    {
                        ch.IsActive = true;
                        ch.WarningIssued = false;
                        ch.NextTriggerAtSeconds = double.PositiveInfinity;
                        if (OperationsScheduler.IsTimedChannel(ch.ChannelId))
                        {
                            var rng = new DeterministicRng(ch.RngState);
                            double dur = OperationsScheduler.SampleActiveDurationSeconds(ch.ChannelId, ref rng);
                            ch.RngState = rng.State;
                            ch.ActiveUntilSeconds = elapsed + dur;
                            simState.OperationsLog?.Add($"[{elapsed:0.#}s] {ch.ChannelId} active until {ch.ActiveUntilSeconds:0.#}s");
                            if (ch.ChannelId == OperationsChannelId.Radiation)
                                OnRadiationStarted(simState, elapsed, ch.ActiveUntilSeconds);
                            else if (ch.ChannelId == OperationsChannelId.DustStorm)
                                OnDustStormStarted(simState, elapsed, ch.ActiveUntilSeconds);
                        }
                        else
                        {
                            ch.ActiveUntilSeconds = double.PositiveInfinity;
                            simState.OperationsLog?.Add($"[{elapsed:0.#}s] {ch.ChannelId} fault triggered");
                            if (simState.IsSleeping)
                            {
                                WakeCrewFromSleep(simState);
                                simState.CriticalAlertPauseRequested = true;
                                simState.ActiveAlertMessage = $"Equipment fault during sleep: {ch.ChannelId}. Crew awakened; inspect equipment.";
                            }
                            if (ch.ChannelId == OperationsChannelId.SolarPanel)
                                OnSolarPanelFaultTriggered(simState, ch, elapsed);
                            else if (ch.ChannelId == OperationsChannelId.OxygenProducer)
                            {
                                simState.Resources.OxygenProducerFaulted = true;
                                simState.StatusMessage = "Oxygen producer fault: capacity reduced to 50% until repaired (20 energy / 40s).";
                            }
                            else if (ch.ChannelId == OperationsChannelId.WaterExtractor)
                            {
                                simState.Resources.WaterExtractorFaulted = true;
                                simState.StatusMessage = "Water extractor fault: extraction reduced to 50% until repaired (10 science).";
                            }
                        }
                    }
                }

                if (ch.IsActive && !double.IsPositiveInfinity(ch.ActiveUntilSeconds) && elapsed + tol >= ch.ActiveUntilSeconds)
                {
                    OperationsScheduler.RescheduleAfterResolution(ch, elapsed);
                    simState.OperationsLog?.Add($"[{elapsed:0.#}s] {ch.ChannelId} ended -> next at {ch.NextTriggerAtSeconds:0.#}s");
                    if (ch.ChannelId == OperationsChannelId.Radiation)
                        RemoveDynamicEvent(simState.Events, "ops-radiation");
                    else if (ch.ChannelId == OperationsChannelId.DustStorm)
                        RemoveDynamicEvent(simState.Events, "ops-dust");
                }
            }
        }

        private static void OnSolarPanelFaultTriggered(SimulationMutableState simState, ChannelScheduleState ch, double elapsed)
        {
            var panels = simState.Resources.Panels;
            if (panels == null || panels.Count == 0) return;

            if (!string.IsNullOrEmpty(ch.ActiveTargetId))
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    if (panels[i].Id == ch.ActiveTargetId && panels[i].IsFaulted)
                        return;
                }
            }

            var healthy = new List<PanelUnitState>();
            for (int i = 0; i < panels.Count; i++)
            {
                if (!panels[i].IsFaulted && SolarProduction.IsPanelOnline(panels[i], simState.Facilities))
                    healthy.Add(panels[i]);
            }
            if (healthy.Count == 0) return;

            var rng = new DeterministicRng(ch.RngState);
            int idx = rng.NextInt(0, healthy.Count);
            ch.RngState = rng.State;

            var target = healthy[idx];
            target.IsFaulted = true;
            target.RepairProgressSeconds = 0;
            ch.ActiveTargetId = target.Id;
            simState.StatusMessage = $"Solar panel fault: {target.Id} offline until exterior repair.";
            simState.OperationsLog?.Add($"[{elapsed:0.#}s] SolarPanel target={target.Id}");
        }

        internal static void CompleteSolarPanelRepair(SimulationMutableState simState, string requestedPanelId, double elapsed)
        {
            var s = simState?.Resources;
            if (s?.Panels == null || s.Panels.Count == 0) return;

            string activeChannelTargetId = null;
            ChannelScheduleState solarChannel = null;
            if (simState.ChannelSchedules != null)
            {
                for (int cIdx = 0; cIdx < simState.ChannelSchedules.Count; cIdx++)
                {
                    var ch = simState.ChannelSchedules[cIdx];
                    if (ch.ChannelId == OperationsChannelId.SolarPanel)
                    {
                        solarChannel = ch;
                        if (ch.IsActive)
                            activeChannelTargetId = ch.ActiveTargetId;
                        break;
                    }
                }
            }

            PanelUnitState target = null;
            if (!string.IsNullOrEmpty(requestedPanelId))
            {
                for (int i = 0; i < s.Panels.Count; i++)
                {
                    if (s.Panels[i].Id == requestedPanelId)
                    {
                        target = s.Panels[i];
                        break;
                    }
                }
            }

            if (target == null && !string.IsNullOrEmpty(activeChannelTargetId))
            {
                for (int i = 0; i < s.Panels.Count; i++)
                {
                    if (s.Panels[i].Id == activeChannelTargetId && s.Panels[i].IsFaulted)
                    {
                        target = s.Panels[i];
                        break;
                    }
                }
            }

            if (target == null)
            {
                for (int i = 0; i < s.Panels.Count; i++)
                {
                    if (s.Panels[i].IsFaulted)
                    {
                        target = s.Panels[i];
                        break;
                    }
                }
            }

            if (target == null)
                return;

            target.IsFaulted = false;
            target.RepairProgressSeconds = 0;

            bool anyRemainingFault = false;
            for (int i = 0; i < s.Panels.Count; i++)
            {
                if (s.Panels[i].IsFaulted)
                {
                    anyRemainingFault = true;
                    break;
                }
            }

            if (solarChannel != null && solarChannel.IsActive &&
                (solarChannel.ActiveTargetId == target.Id || !anyRemainingFault))
            {
                OperationsScheduler.RescheduleAfterResolution(solarChannel, elapsed);
                simState.OperationsLog?.Add($"[{elapsed:0.#}s] SolarPanel repaired ({target.Id}) -> next at {solarChannel.NextTriggerAtSeconds:0.#}s");
            }

            simState.StatusMessage = $"Solar panel {target.Id} repaired and back online.";
        }

        private static void OnDustStormWarning(SimulationMutableState simState, double elapsed, double startAt)
        {
            if (simState.IsSleeping)
            {
                WakeCrewFromSleep(simState);
                simState.CriticalAlertPauseRequested = true;
                simState.ActiveAlertMessage = "Dust storm warning during sleep: crew awakened. Solar output will drop by 80%.";
            }
            var ev = new EventState(
                "ops-dust",
                "Regional Dust Storm",
                EventKind.Dust,
                elapsed,
                startAt,
                startAt + 120.0,
                0.80,
                "OPS-DUST-CHANNEL",
                "Operations dust storm warning: solar output will drop by 80%.");
            ev.WarningTriggered = true;
            ev.Phase = EventPhase.Warning;
            UpsertDynamicEvent(simState.Events, ev);
        }

        private static void OnDustStormStarted(SimulationMutableState simState, double elapsed, double endAt)
        {
            var ev = new EventState(
                "ops-dust",
                "Regional Dust Storm",
                EventKind.Dust,
                Math.Max(0, elapsed - 60.0),
                elapsed,
                endAt,
                0.80,
                "OPS-DUST-CHANNEL",
                "Active dust storm: solar output reduced by 80% (x0.20).");
            ev.WarningTriggered = true;
            ev.StartTriggered = true;
            ev.Phase = EventPhase.Active;
            UpsertDynamicEvent(simState.Events, ev);
            if (!simState.SolTracker.EventsExperienced.Contains(ev.Title))
                simState.SolTracker.EventsExperienced.Add(ev.Title);
        }

        private static void OnRadiationWarning(SimulationMutableState simState, double elapsed, double startAt)
        {
            var ev = new EventState(
                "ops-radiation",
                "Solar Radiation Surge",
                EventKind.Radiation,
                elapsed,
                startAt,
                startAt + 90.0,
                1.0,
                "OPS-RAD-CHANNEL",
                "Operations radiation warning: move exterior crew inside before surge.");
            ev.WarningTriggered = true;
            ev.Phase = EventPhase.Warning;
            UpsertDynamicEvent(simState.Events, ev);

            if (simState.IsSleeping)
            {
                simState.IsSleeping = false;
                for (int i = 0; i < simState.Crew.Count; i++)
                {
                    var member = simState.Crew[i];
                    if (member.Status == CrewStatus.Sleeping)
                        member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                }
                simState.CriticalAlertPauseRequested = true;
                simState.ActiveAlertMessage = "Radiation warning during sleep: crew awakened. Bring any exterior crew inside.";
            }
        }

        private static void OnRadiationStarted(SimulationMutableState simState, double elapsed, double endAt)
        {
            var ev = new EventState(
                "ops-radiation",
                "Solar Radiation Surge",
                EventKind.Radiation,
                Math.Max(0, elapsed - 60.0),
                elapsed,
                endAt,
                1.0,
                "OPS-RAD-CHANNEL",
                "Active radiation surge: keep crew inside shelter.");
            ev.WarningTriggered = true;
            ev.StartTriggered = true;
            ev.Phase = EventPhase.Active;
            UpsertDynamicEvent(simState.Events, ev);
            if (!simState.SolTracker.EventsExperienced.Contains(ev.Title))
                simState.SolTracker.EventsExperienced.Add(ev.Title);
        }

        private static void UpsertDynamicEvent(List<EventState> events, EventState updated)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Id == updated.Id)
                {
                    events[i] = updated;
                    return;
                }
            }
            events.Add(updated);
        }

        private static void RemoveDynamicEvent(List<EventState> events, string eventId)
        {
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i].Id == eventId)
                    events.RemoveAt(i);
            }
        }

        private static void CheckEarthAdviceDelivery(SimulationMutableState simState, MissionConfig config, double elapsed)
        {
            if (simState.EarthAdviceStatus != EarthAdviceStatus.Pending)
                return;

            double tol = 1e-12 * Math.Max(1.0, simState.EarthAdviceExpectedAtSeconds);
            if (elapsed + tol >= simState.EarthAdviceExpectedAtSeconds)
                DeliverEarthAdvice(simState, config, simState.EarthAdviceExpectedAtSeconds);
        }

        internal static void DeliverEarthAdvice(SimulationMutableState simState, MissionConfig config, double deliveryElapsed)
        {
            simState.EarthAdviceStatus = EarthAdviceStatus.Delivered;
            simState.EarthAdviceDeliveredAtSeconds = deliveryElapsed;
            simState.EarthAdviceMessage = BuildEarthAdviceMessage(simState, config, deliveryElapsed);
        }

        private static string FormatEventClock(MissionConfig config, double seconds)
        {
            long sol = (long)Math.Floor(seconds / config.SolSeconds) + 1;
            int inSol = (int)Math.Round(seconds % config.SolSeconds);
            if (inSol >= (int)Math.Round(config.SolSeconds))
            {
                inSol = 0;
                sol++;
            }
            return $"Sol {sol} {inSol / 60:00}:{inSol % 60:00}";
        }

        private static string BuildEarthAdviceMessage(SimulationMutableState simState, MissionConfig config, double deliveryElapsed)
        {
            string relayHeader =
                $"[Earth Relay - Sent {FormatEventClock(config, simState.EarthAdviceRequestedAtSeconds)}, Received {FormatEventClock(config, deliveryElapsed)}]\n" +
                $"Note: Real Earth-Mars one-way radio delay varies (~3 to 22 min by orbital position); this 45s delay is compressed for training.\n";

            EventState active = null;
            EventState warningOrUpcoming = null;
            EventState latestPastRelevant = null;

            for (int i = 0; i < simState.Events.Count; i++)
            {
                var ev = simState.Events[i];
                if (ev.Phase == EventPhase.Active && active == null)
                    active = ev;
                else if ((ev.Phase == EventPhase.Warning || ev.Phase == EventPhase.Upcoming) && warningOrUpcoming == null)
                    warningOrUpcoming = ev;
                else if (ev.Phase == EventPhase.Completed)
                    latestPastRelevant = ev;
            }

            if (active != null)
            {
                if (active.Kind == EventKind.Radiation)
                {
                    bool anyOutside = false;
                    for (int i = 0; i < simState.Crew.Count; i++)
                    {
                        if (simState.Crew[i].Location == CrewLocation.Outside) { anyOutside = true; break; }
                    }
                    string action = anyOutside
                        ? "ORDER: Recall outside crew inside immediately; outside risk (+1.20/s) is 8x higher than inside (+0.15/s)."
                        : "CONFIRMATION: All crew are inside habitat shelter; hold indoor duties until radiation clears.";
                    return relayHeader + $"ACTIVE {active.Title} (started {FormatEventClock(config, active.StartSeconds)}, ends {FormatEventClock(config, active.EndSeconds)}): {action}";
                }
                return relayHeader + $"ACTIVE {active.Title} (started {FormatEventClock(config, active.StartSeconds)}, ends {FormatEventClock(config, active.EndSeconds)}): Solar reduced; watch battery and pause research if O2 drops.";
            }

            if (latestPastRelevant != null && latestPastRelevant.EndSeconds >= simState.EarthAdviceRequestedAtSeconds - 60.0)
            {
                string pastStamp = $"{FormatEventClock(config, latestPastRelevant.StartSeconds)}-{FormatEventClock(config, latestPastRelevant.EndSeconds)}";
                string nextTip = warningOrUpcoming != null
                    ? $"Next scheduled hazard: {warningOrUpcoming.Title} at {FormatEventClock(config, warningOrUpcoming.StartSeconds)}."
                    : "Window clear: keep Oxygen above 20, prepare reserve meal before night, and advance Research.";
                return relayHeader + $"POST-EVENT NOTE ({latestPastRelevant.Title} already ended at {pastStamp}, before reply arrived at {FormatEventClock(config, deliveryElapsed)}): Check crew risk & panel fouling now. {nextTip}";
            }

            if (warningOrUpcoming != null)
            {
                string targetStamp = FormatEventClock(config, warningOrUpcoming.StartSeconds);
                if (warningOrUpcoming.Kind == EventKind.Radiation)
                    return relayHeader + $"UPCOMING {warningOrUpcoming.Title} at {targetStamp}: Finish exterior panel work early and return inside before warning expires.";
                return relayHeader + $"UPCOMING {warningOrUpcoming.Title} at {targetStamp}: Clean solar panels if fouled and build battery buffer before dust reduces sunlight.";
            }

            var s = simState.Resources;
            if (s.Oxygen < 40)
                return relayHeader + "STATUS ADVICE: Oxygen reserve is tightening; check Interior System Maint. and keep Life Support powered ahead of Research.";
            if (s.Battery < 40)
                return relayHeader + "STATUS ADVICE: Battery is low; schedule Exterior Panel Maint. in clear daylight or toggle Research Power OFF temporarily.";
            if (!simState.MealPreparedThisSol && simState.FoodRationsRemaining > 0)
                return relayHeader + "STATUS ADVICE: Systems stable; assign 30s Reserve Meal Prep inside before night so crew sleep stays at 1.0x recovery.";
            return relayHeader + "STATUS ADVICE: Reserves nominal; keep indoor Research active while rotating tired crew into Rest Inside.";
        }

        private static void ApplyTransitions(
            SimulationMutableState simState,
            Dictionary<string, TaskState> taskLookup,
            MissionConfig config,
            ResourceConfig c,
            double elapsed)
        {
            var s = simState.Resources;
            var crew = simState.Crew;
            var tasks = simState.Tasks;

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (!task.IsContinuous && !task.IsCompleted && task.ProgressSeconds >= task.RequiredSeconds)
                {
                    task.ProgressSeconds = task.RequiredSeconds;
                    task.IsCompleted = true;
                    if (!task.RewardApplied)
                    {
                        if (task.Kind == TaskKind.PanelMaintenance)
                            s.PanelFouling = Math.Max(0, s.PanelFouling - c.PanelMaintenanceReduction);
                        else if (task.Kind == TaskKind.SystemMaintenance)
                            s.SystemWear = Math.Max(0, s.SystemWear - c.SystemMaintenanceReduction);
                        else if (task.Kind == TaskKind.MealPreparation)
                        {
                            simState.MealPreparedThisSol = true;
                            if (simState.NightMealSettledSol == simState.SolTracker.SolNumber &&
                                simState.NightRationAvailable &&
                                !simState.SleepStartedThisSol)
                            {
                                simState.NightSleepRecoveryFactor = 1.0;
                            }
                        }
                        else if (task.Kind == TaskKind.SolarPanelRepair)
                        {
                            CompleteSolarPanelRepair(simState, null, elapsed);
                        }
                        else if (task.Kind == TaskKind.OxygenProducerRepair)
                        {
                            s.OxygenProducerFaulted = false;
                            if (simState.ChannelSchedules != null)
                            {
                                for (int cIdx = 0; cIdx < simState.ChannelSchedules.Count; cIdx++)
                                {
                                    var ch = simState.ChannelSchedules[cIdx];
                                    if (ch.ChannelId == OperationsChannelId.OxygenProducer && ch.IsActive)
                                    {
                                        OperationsScheduler.RescheduleAfterResolution(ch, elapsed);
                                        simState.OperationsLog?.Add($"[{elapsed:0.#}s] OxygenProducer repaired -> next at {ch.NextTriggerAtSeconds:0.#}s");
                                        break;
                                    }
                                }
                            }
                            simState.StatusMessage = "Oxygen producer repaired and capacity restored.";
                        }
                        task.RewardApplied = true;
                        simState.SolTracker.CompletedTasks.Add(task.Title);
                    }

                    if (task.Kind == TaskKind.SolarPanelRepair || task.Kind == TaskKind.OxygenProducerRepair)
                    {
                        task.IsCompleted = false;
                        task.RewardApplied = false;
                        task.ProgressSeconds = 0;
                    }

                    if (task.AssignedCrewId != null)
                    {
                        for (int j = 0; j < crew.Count; j++)
                        {
                            if (crew[j].Id == task.AssignedCrewId)
                            {
                                crew[j].ActiveTaskId = null;
                                crew[j].Status = crew[j].Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                                break;
                            }
                        }
                        task.AssignedCrewId = null;
                    }
                }
            }

            if (simState.EarthOrders != null)
            {
                for (int i = 0; i < simState.EarthOrders.Count; i++)
                {
                    var order = simState.EarthOrders[i];
                    if (order.Status == EarthOrderStatus.InProgress &&
                        order.StepProgressSeconds >= order.StepRequiredSeconds - 1e-9)
                    {
                        if (order.AssignedCrewId != null)
                        {
                            for (int j = 0; j < crew.Count; j++)
                            {
                                if (crew[j].Id == order.AssignedCrewId)
                                {
                                    crew[j].ActiveTaskId = null;
                                    crew[j].Status = crew[j].Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                                    break;
                                }
                            }
                            order.AssignedCrewId = null;
                        }

                        int totalSteps = EarthOrderBagState.GetStepCount(order.Kind);
                        if (order.CurrentStepIndex + 1 < totalSteps)
                        {
                            order.CurrentStepIndex++;
                            order.StepProgressSeconds = 0;
                            order.StepRequiredSeconds = EarthOrderBagState.GetStepSpec(order.Kind, order.CurrentStepIndex).DurationSeconds;
                            simState.StatusMessage = $"{order.Code} step {order.CurrentStepIndex} complete; ready for next stage.";
                            simState.OperationsLog?.Add($"[{elapsed:0.#}s] Earth order {order.Code} advanced to step {order.CurrentStepIndex + 1}/{totalSteps}");
                        }
                        else
                        {
                            order.StepProgressSeconds = order.StepRequiredSeconds;
                            order.Status = EarthOrderStatus.Completed;
                            order.CompletionSummary = $"{order.Code} completed on Sol {simState.SolTracker.SolNumber} (0 science).";
                            simState.SolTracker.CompletedTasks.Add($"{order.Code}: {order.Title}");
                            simState.OperationsLog?.Add($"[{elapsed:0.#}s] Earth order completed: {order.Code} ({order.OrderId})");
                        }
                    }
                }
            }

            for (int i = 0; i < crew.Count; i++)
            {
                var member = crew[i];
                var sJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                if (sJob != null)
                {
                    if (sJob.Phase == SurfaceJobPhase.Travel && sJob.LegDistanceTraveled >= sJob.LegTotalDistance - 1e-9)
                    {
                        sJob.LegDistanceTraveled = sJob.LegTotalDistance;
                        sJob.EvaluatePositionAndHeading(out var arrivePos, out _);
                        member.SurfacePosX = arrivePos.X;
                        member.SurfacePosZ = arrivePos.Z;
                        member.HasSurfacePosition = true;
                        if (sJob.Kind == SurfaceJobKind.Construct)
                        {
                            var trFac = FindFacility(simState.Facilities, sJob.TargetId);
                            if (trFac == null || trFac.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                            {
                                sJob.Phase = SurfaceJobPhase.Completed;
                                sJob.AssignedCrewId = null;
                                member.ActiveSurfaceJobId = null;
                                member.ActiveTaskId = null;
                                member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                                continue;
                            }
                        }

                        if (sJob.WorkRequiredSeconds <= 0)
                        {
                            if (sJob.Kind == SurfaceJobKind.Construct)
                                CompleteBlueprintFacility(simState, sJob.TargetId);
                            else if (sJob.Kind == SurfaceJobKind.Repair)
                                CompleteSolarPanelRepair(simState, sJob.TargetId, elapsed);
                            sJob.Phase = SurfaceJobPhase.Completed;
                            sJob.AssignedCrewId = null;
                            member.ActiveSurfaceJobId = null;
                            member.ActiveTaskId = null;
                            member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                        }
                        else
                        {
                            sJob.Phase = SurfaceJobPhase.Work;
                        }
                    }
                    else if (sJob.Phase == SurfaceJobPhase.Work && member.Status == CrewStatus.Working &&
                             sJob.WorkProgressSeconds >= sJob.WorkRequiredSeconds - 1e-9)
                    {
                        sJob.WorkProgressSeconds = sJob.WorkRequiredSeconds;
                        if (sJob.Kind == SurfaceJobKind.Construct)
                            CompleteBlueprintFacility(simState, sJob.TargetId);
                        else if (sJob.Kind == SurfaceJobKind.Repair)
                            CompleteSolarPanelRepair(simState, sJob.TargetId, elapsed);
                        sJob.Phase = SurfaceJobPhase.Completed;
                        sJob.AssignedCrewId = null;
                        member.ActiveSurfaceJobId = null;
                        member.ActiveTaskId = null;
                        member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                    }
                    else if (sJob.Phase == SurfaceJobPhase.Return && member.Status == CrewStatus.Returning &&
                             sJob.LegDistanceTraveled >= sJob.LegTotalDistance - 1e-9 &&
                             sJob.AirlockRemainingSeconds <= 1e-9)
                    {
                        sJob.LegDistanceTraveled = sJob.LegTotalDistance;
                        sJob.AirlockRemainingSeconds = 0;
                        sJob.Phase = SurfaceJobPhase.Completed;
                        sJob.AssignedCrewId = null;
                        member.ActiveSurfaceJobId = null;
                        member.ActiveTaskId = null;
                        member.ReturnRemainingSeconds = 0;
                        member.Location = CrewLocation.Inside;
                        member.HasSurfacePosition = false;
                        member.SurfacePosX = 0;
                        member.SurfacePosZ = 0;
                        member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Sheltered;
                    }
                }

                if (member.Status == CrewStatus.Working && member.Stamina <= 0)
                {
                    member.Stamina = 0;
                    if (member.ActiveTaskId != null && taskLookup.TryGetValue(member.ActiveTaskId, out var task))
                        task.AssignedCrewId = null;
                    if (simState.EarthOrders != null)
                    {
                        for (int o = 0; o < simState.EarthOrders.Count; o++)
                        {
                            if (simState.EarthOrders[o].AssignedCrewId == member.Id)
                                simState.EarthOrders[o].AssignedCrewId = null;
                        }
                    }
                    var exhaustedJob = FindSurfaceJob(simState.SurfaceJobs, member.ActiveSurfaceJobId);
                    if (exhaustedJob != null)
                        exhaustedJob.AssignedCrewId = null;
                    member.ActiveSurfaceJobId = null;
                    member.ActiveTaskId = null;
                    member.Status = CrewStatus.Exhausted;
                }
                else if (member.Status == CrewStatus.Resting && member.Stamina >= config.MaxStamina)
                {
                    member.Stamina = config.MaxStamina;
                    member.Status = CrewStatus.Idle;
                }
                else if (member.ActiveSurfaceJobId == null && member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds <= 0)
                {
                    member.ReturnRemainingSeconds = 0;
                    member.Location = CrewLocation.Inside;
                    member.HasSurfacePosition = false;
                    member.SurfacePosX = 0;
                    member.SurfacePosZ = 0;
                    member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Sheltered;
                }
            }
        }

        private static SurfaceJobState FindSurfaceJob(List<SurfaceJobState> jobs, string jobId)
        {
            if (jobs == null || string.IsNullOrEmpty(jobId)) return null;
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i].JobId == jobId)
                    return jobs[i];
            return null;
        }

        private static FacilityState FindFacility(List<FacilityState> facilities, string facilityId)
        {
            if (facilities == null || string.IsNullOrEmpty(facilityId)) return null;
            for (int i = 0; i < facilities.Count; i++)
                if (facilities[i].FacilityId == facilityId)
                    return facilities[i];
            return null;
        }

        internal static void CompleteBlueprintFacility(SimulationMutableState simState, string facilityId)
        {
            var fac = FindFacility(simState?.Facilities, facilityId);
            if (fac == null) return;

            fac.WorkProgressSeconds = fac.WorkRequiredSeconds;
            if (fac.EnergyRequiredTotal > 0 && fac.EnergyRequiredTotal - fac.EnergySpentTotal <= 1e-6)
                fac.EnergySpentTotal = fac.EnergyRequiredTotal;
            else
                fac.EnergySpentTotal = Math.Min(fac.EnergyRequiredTotal, fac.EnergySpentTotal);

            bool newlyCommissioned = !(fac.IsProductionApplied && fac.LifecycleStatus == FacilityLifecycleStatus.Commissioned);
            fac.LifecycleStatus = FacilityLifecycleStatus.Commissioned;
            fac.IsProductionApplied = true;

            if (fac.Kind == FacilityKind.SolarPanel)
                EnsureSolarPanelUnit(simState, fac);

            if (!newlyCommissioned)
                return;

            if (FacilityCatalog.TryGetSpec(fac.Kind, out var spec) && spec.UsesUtilityLayerOnly && simState.Map != null)
            {
                for (int c = 0; c < fac.FootprintCells.Count; c++)
                {
                    if (simState.Map.TryGetCell(fac.FootprintCells[c], out var cell))
                    {
                        cell.HasUtilityLine = true;
                        cell.UtilityBlueprintId = null;
                    }
                }
            }

            UtilityNetwork.RefreshTopology(simState);
            simState.StatusMessage = $"Completed construction of {fac.Kind} ({fac.FacilityId}).";
        }

        private static void EnsureSolarPanelUnit(SimulationMutableState simState, FacilityState fac)
        {
            if (simState?.Resources == null || fac == null)
                return;

            if (simState.Resources.Panels == null)
                simState.Resources.Panels = new List<PanelUnitState>();

            var panels = simState.Resources.Panels;
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].FacilityId == fac.FacilityId || fac.LinkedPanelIds.Contains(panels[i].Id))
                {
                    if (!fac.LinkedPanelIds.Contains(panels[i].Id))
                        fac.LinkedPanelIds.Add(panels[i].Id);
                    return;
                }
            }

            double solarFactor = 1.0;
            if (simState.Map != null && simState.Map.TryGetCell(fac.Origin, out var cell))
                solarFactor = cell.SolarFactor;

            string candidateId = $"panel-{fac.FacilityId}";
            bool exists = false;
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].Id == candidateId)
                {
                    exists = true;
                    break;
                }
            }
            while (exists)
            {
                candidateId = $"panel-{simState.NextEntityId++}";
                exists = false;
                for (int i = 0; i < panels.Count; i++)
                {
                    if (panels[i].Id == candidateId)
                    {
                        exists = true;
                        break;
                    }
                }
            }

            fac.LinkedPanelIds.Add(candidateId);
            panels.Add(new PanelUnitState(
                candidateId,
                PanelUnitState.DefaultUnitWeight,
                isFaulted: false,
                facilityId: fac.FacilityId,
                solarFactor: solarFactor));
        }

        private static double RecycleRate(ResourceConfig c, ResourceState s, double systemFactor, double consumedWater, double production, double energy)
        {
            double rate = c.RecyclingRate * systemFactor;
            if (s.Wastewater <= 0) rate = Math.Min(rate, consumedWater);
            if (s.Water >= c.WaterCapacity) rate = Math.Min(rate, consumedWater + production * c.OxygenWaterPerUnit);
            if (c.RecyclingEnergyPerUnit > 0) rate = Math.Min(rate, Math.Max(0, energy - production * c.OxygenEnergyPerUnit) / c.RecyclingEnergyPerUnit);
            return rate;
        }

        private static double Limit(double dt, double value, double capacity, double rate)
        {
            if (rate < 0 && value > 0) return Math.Min(dt, value / -rate);
            if (rate > 0 && value < capacity) return Math.Min(dt, (capacity - value) / rate);
            return dt;
        }

        private static bool SnapBoundary(ref double value, double capacity, double rate, double dt)
        {
            if (rate < 0 && value > 0 && value / -rate <= dt) { value = 0; return true; }
            if (rate > 0 && value < capacity && (capacity - value) / rate <= dt) { value = capacity; return true; }
            return false;
        }

        private static double Apply(double value, double rate, double dt, double capacity)
        {
            double next = value + rate * dt;
            double tolerance = 1e-12 * Math.Max(1, Math.Max(value, capacity));
            if (rate < 0 && next < tolerance) return 0;
            if (rate > 0 && capacity - next < tolerance) return capacity;
            return Math.Max(0, Math.Min(capacity, next));
        }
    }
}
