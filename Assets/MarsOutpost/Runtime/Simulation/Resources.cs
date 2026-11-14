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

        public ResourceConfig(double batteryCapacity = 100, double waterCapacity = 100, double oxygenCapacity = 100,
            double wastewaterCapacity = 100, double initialBattery = 80, double initialWater = 80,
            double initialOxygen = 80, double initialWastewater = 0, double solarRate = 0.12,
            double oxygenProductionRate = 0.05, double oxygenEnergyPerUnit = 1, double oxygenWaterPerUnit = 0.2,
            double recyclingRate = 0.02, double recyclingEnergyPerUnit = 0.5,
            double waterUsePerCrew = 0.004, double oxygenUsePerCrew = 0.01,
            double researchEnergyRate = 0.03, double researchProgressRate = 1.0,
            double initialPanelFouling = 0, double panelMaintenanceReduction = 0.25,
            double initialSystemWear = 0, double systemMaintenanceReduction = 0.25)
        {
            double[] values = { batteryCapacity, waterCapacity, oxygenCapacity, wastewaterCapacity, initialBattery,
                initialWater, initialOxygen, initialWastewater, solarRate, oxygenProductionRate, oxygenEnergyPerUnit,
                oxygenWaterPerUnit, recyclingRate, recyclingEnergyPerUnit, waterUsePerCrew, oxygenUsePerCrew,
                researchEnergyRate, researchProgressRate, initialPanelFouling, panelMaintenanceReduction,
                initialSystemWear, systemMaintenanceReduction };
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
        public double PanelFouling { get; internal set; }
        public double SystemWear { get; internal set; }
        public double AtmosphericDustLoss { get; internal set; }
        public double LastResearchRateFactor { get; internal set; }
        public double ContinuousOxygenOutageSeconds { get; internal set; }

        internal ResourceState Copy() => (ResourceState)MemberwiseClone();

        internal static ResourceState Initial(ResourceConfig c) => new ResourceState
        {
            Battery = c.InitialBattery,
            Water = c.InitialWater,
            Oxygen = c.InitialOxygen,
            Wastewater = c.InitialWastewater,
            ResearchEnabled = true,
            ResearchProgress = 0,
            PanelFouling = c.InitialPanelFouling,
            SystemWear = c.InitialSystemWear,
            AtmosphericDustLoss = 0,
            LastResearchRateFactor = 0,
            ContinuousOxygenOutageSeconds = 0
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
                EarthAdviceMessage = EarthAdviceMessage
            };
            for (int i = 0; i < Crew.Count; i++) copy.Crew.Add(Crew[i].Copy());
            for (int i = 0; i < Tasks.Count; i++) copy.Tasks.Add(Tasks[i].Copy());
            for (int i = 0; i < Events.Count; i++) copy.Events.Add(Events[i].Copy());
            return copy;
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

            while (elapsed < target)
            {
                if (++steps > MaxTransitions)
                    throw new InvalidOperationException("Tick exceeds the resource transition limit; use smaller ticks.");

                UpdateEventPhases(events, elapsed, simState.SolTracker);
                SettleNightMealIfDue(simState, config, elapsed);
                CheckEarthAdviceDelivery(simState, config, elapsed);
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

                if (dt <= 0)
                    throw new InvalidOperationException("Tick cannot advance at this calendar precision.");

                double foulingFactor = Math.Max(0, 1.0 - s.PanelFouling);
                double dustFactor = Math.Max(0, 1.0 - dustLoss);
                double solar = day ? c.SolarRate * foulingFactor * dustFactor : 0;
                double systemFactor = Math.Max(0, 1.0 - s.SystemWear);
                double waterDemand = c.WaterUsePerCrew * crew.Count;
                double oxygenDemand = c.OxygenUsePerCrew * crew.Count;
                double availableEnergy = s.Battery > 0 ? double.PositiveInfinity : solar;

                double maxO2Prod = c.OxygenProductionRate * systemFactor;
                double production = Math.Min(maxO2Prod, s.Oxygen >= c.OxygenCapacity ? oxygenDemand : double.PositiveInfinity);
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

                double consumedOxygen = s.Oxygen > 0 ? oxygenDemand : Math.Min(oxygenDemand, production);
                bool inOxygenOutage = oxygenDemand > 0 && s.Oxygen <= 0 && consumedOxygen < oxygenDemand - 1e-12;
                if (!inOxygenOutage)
                    s.ContinuousOxygenOutageSeconds = 0;

                double o2Energy = production * c.OxygenEnergyPerUnit;
                double recEnergy = recycling * c.RecyclingEnergyPerUnit;

                bool hasResearchCrew = false;
                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    if (member.Status == CrewStatus.Working && member.Stamina > 0 && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var activeTask) &&
                        activeTask.Kind == TaskKind.Research)
                    {
                        hasResearchCrew = true;
                        break;
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
                        double remainingSolar = Math.Max(0, solar - o2Energy - recEnergy);
                        researchEnergy = Math.Min(c.ResearchEnergyRate, remainingSolar);
                        researchFactor = researchEnergy / c.ResearchEnergyRate;
                    }
                }
                s.LastResearchRateFactor = researchFactor;

                double batteryRate = solar - o2Energy - recEnergy - researchEnergy;
                double waterRate = recycling - consumedWater - production * c.OxygenWaterPerUnit;
                double waterFlowScale = Math.Max(recycling, consumedWater + production * c.OxygenWaterPerUnit);
                if (s.Water == 0 && Math.Abs(waterRate) <= 1e-14 * waterFlowScale) waterRate = 0;
                double oxygenRate = production - consumedOxygen;
                double wasteRate = consumedWater - recycling;
                if (s.Battery >= c.BatteryCapacity && batteryRate > 0) batteryRate = 0;
                if (s.Wastewater >= c.WastewaterCapacity && wasteRate > 0) wasteRate = 0;

                dt = Limit(dt, s.Battery, c.BatteryCapacity, batteryRate);
                dt = Limit(dt, s.Water, c.WaterCapacity, waterRate);
                dt = Limit(dt, s.Oxygen, c.OxygenCapacity, oxygenRate);
                dt = Limit(dt, s.Wastewater, c.WastewaterCapacity, wasteRate);

                if (simState.CanTriggerLowOxygenAlert && oxygenRate < 0 && s.Oxygen > config.LowOxygenWarningThreshold)
                    dt = Math.Min(dt, (s.Oxygen - config.LowOxygenWarningThreshold) / -oxygenRate);

                if (inOxygenOutage && config.OxygenOutageFailureSeconds > 0 && config.OxygenOutageFailureSeconds > s.ContinuousOxygenOutageSeconds)
                    dt = Math.Min(dt, config.OxygenOutageFailureSeconds - s.ContinuousOxygenOutageSeconds);

                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor);
                    if (staminaRate < 0 && member.Stamina > 0)
                        dt = Math.Min(dt, member.Stamina / -staminaRate);
                    else if (staminaRate > 0 && member.Stamina < config.MaxStamina)
                        dt = Math.Min(dt, (config.MaxStamina - member.Stamina) / staminaRate);

                    if (member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds > 0)
                        dt = Math.Min(dt, member.ReturnRemainingSeconds);

                    if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                        !task.IsContinuous && !task.IsCompleted && task.ProgressSeconds < task.RequiredSeconds)
                    {
                        double workRate = task.Kind == TaskKind.Research ? researchFactor : 1.0;
                        if (workRate > 0)
                            dt = Math.Min(dt, (task.RequiredSeconds - task.ProgressSeconds) / workRate);
                    }

                    double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                    if (riskRate > 0 && member.RiskScore < config.MaxCrewRiskScore)
                        dt = Math.Min(dt, (config.MaxCrewRiskScore - member.RiskScore) / riskRate);
                }

                if (dt <= 0)
                    throw new InvalidOperationException("Tick cannot advance at this resource precision.");

                if (elapsed + dt == elapsed)
                {
                    bool snapped = false;
                    double value = s.Battery;
                    if (SnapBoundary(ref value, c.BatteryCapacity, batteryRate, dt)) { s.Battery = value; snapped = true; }
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
                        double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor);
                        double stam = member.Stamina;
                        if (SnapBoundary(ref stam, config.MaxStamina, staminaRate, dt)) { member.Stamina = stam; snapped = true; }
                        if (member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds > 0 && member.ReturnRemainingSeconds <= dt)
                        {
                            member.ReturnRemainingSeconds = 0;
                            snapped = true;
                        }
                        if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                            taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                            !task.IsContinuous && !task.IsCompleted)
                        {
                            double workRate = task.Kind == TaskKind.Research ? researchFactor : 1.0;
                            double prog = task.ProgressSeconds;
                            if (SnapBoundary(ref prog, task.RequiredSeconds, workRate, dt)) { task.ProgressSeconds = prog; snapped = true; }
                        }
                        double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                        double risk = member.RiskScore;
                        if (SnapBoundary(ref risk, config.MaxCrewRiskScore, riskRate, dt)) { member.RiskScore = risk; snapped = true; }
                    }

                    if (!snapped)
                        throw new InvalidOperationException("Tick cannot advance at this resource precision.");
                    ApplyTransitions(simState, taskLookup, config, c);
                    HandleCriticalOxygenCheck(simState, config, c, oxygenRate);
                    if (CheckTerminal(simState, config, elapsed))
                        return elapsed;
                    continue;
                }

                s.Battery = Apply(s.Battery, batteryRate, dt, c.BatteryCapacity);
                s.Water = Apply(s.Water, waterRate, dt, c.WaterCapacity);
                s.Oxygen = Apply(s.Oxygen, oxygenRate, dt, c.OxygenCapacity);
                s.Wastewater = Apply(s.Wastewater, wasteRate, dt, c.WastewaterCapacity);
                s.UnmetWater += (waterDemand - consumedWater) * dt;
                s.UnmetOxygen += (oxygenDemand - consumedOxygen) * dt;
                s.ResearchProgress += researchFactor * c.ResearchProgressRate * dt;

                if (inOxygenOutage)
                {
                    double cap = config.OxygenOutageFailureSeconds > 0 ? config.OxygenOutageFailureSeconds : double.MaxValue;
                    s.ContinuousOxygenOutageSeconds = Apply(s.ContinuousOxygenOutageSeconds, 1.0, dt, cap);
                }

                if (!MissionConfig.IsFinite(s.UnmetWater) || !MissionConfig.IsFinite(s.UnmetOxygen) || !MissionConfig.IsFinite(s.ResearchProgress))
                    throw new InvalidOperationException("Resource totals overflowed.");

                for (int i = 0; i < crew.Count; i++)
                {
                    var member = crew[i];
                    double staminaRate = GetCrewStaminaRate(member, config, simState, taskLookup, researchFactor);
                    if (staminaRate != 0)
                        member.Stamina = Apply(member.Stamina, staminaRate, dt, config.MaxStamina);

                    if (member.Status == CrewStatus.Returning)
                    {
                        double rem = member.ReturnRemainingSeconds - dt;
                        double tol = 1e-12 * Math.Max(1.0, config.ReturnToShelterSeconds);
                        member.ReturnRemainingSeconds = rem < tol ? 0 : Math.Max(0, rem);
                    }

                    if (member.Status == CrewStatus.Working && member.ActiveTaskId != null &&
                        taskLookup.TryGetValue(member.ActiveTaskId, out var task) &&
                        !task.IsContinuous && !task.IsCompleted)
                    {
                        double workRate = task.Kind == TaskKind.Research ? researchFactor : 1.0;
                        if (workRate > 0)
                            task.ProgressSeconds = Apply(task.ProgressSeconds, workRate, dt, task.RequiredSeconds);
                    }

                    double riskRate = GetCrewRiskRate(member, config, radiationIntensity);
                    if (riskRate > 0)
                        member.RiskScore = Apply(member.RiskScore, riskRate, dt, config.MaxCrewRiskScore);
                }

                elapsed += dt;
                ApplyTransitions(simState, taskLookup, config, c);
                UpdateEventPhases(events, elapsed, simState.SolTracker);
                SettleNightMealIfDue(simState, config, elapsed);
                CheckEarthAdviceDelivery(simState, config, elapsed);
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

            tracker.ResetForSol(tracker.SolNumber + 1, s, simState.Crew);
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
            if (radiationIntensity <= 0) return 0;
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
            double researchFactor)
        {
            switch (member.Status)
            {
                case CrewStatus.Working:
                    if (member.ActiveTaskId != null && taskLookup.TryGetValue(member.ActiveTaskId, out var task))
                    {
                        double workFactor = task.Kind == TaskKind.Research ? researchFactor : 1.0;
                        return task.StaminaRatePerSecond * workFactor;
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
            ResourceConfig c)
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
                        task.RewardApplied = true;
                        simState.SolTracker.CompletedTasks.Add(task.Title);
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

            for (int i = 0; i < crew.Count; i++)
            {
                var member = crew[i];
                if (member.Status == CrewStatus.Working && member.Stamina <= 0)
                {
                    member.Stamina = 0;
                    if (member.ActiveTaskId != null && taskLookup.TryGetValue(member.ActiveTaskId, out var task))
                        task.AssignedCrewId = null;
                    member.ActiveTaskId = null;
                    member.Status = CrewStatus.Exhausted;
                }
                else if (member.Status == CrewStatus.Resting && member.Stamina >= config.MaxStamina)
                {
                    member.Stamina = config.MaxStamina;
                    member.Status = CrewStatus.Idle;
                }
                else if (member.Status == CrewStatus.Returning && member.ReturnRemainingSeconds <= 0)
                {
                    member.ReturnRemainingSeconds = 0;
                    member.Location = CrewLocation.Inside;
                    member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Sheltered;
                }
            }
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
