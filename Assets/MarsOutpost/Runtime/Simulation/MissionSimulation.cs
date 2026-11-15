using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarsOutpost
{
    public enum CrewLocation
    {
        Inside,
        Outside
    }

    public enum CrewStatus
    {
        Idle,
        Working,
        Resting,
        Returning,
        Sheltered,
        Sleeping,
        Exhausted
    }

    public enum TaskKind
    {
        PanelMaintenance,
        SystemMaintenance,
        Research,
        MealPreparation,
        SolarPanelRepair,
        OxygenProducerRepair
    }

    public enum EarthAdviceStatus
    {
        NotRequested,
        Pending,
        Delivered
    }

    public enum EventKind
    {
        Radiation,
        Dust
    }

    public enum EventPhase
    {
        Upcoming,
        Warning,
        Active,
        Completed
    }

    public enum MissionMode
    {
        Training,
        Demo,
        Operations
    }

    public enum MissionOutcome
    {
        InProgress,
        Succeeded,
        Failed
    }

    public enum MissionFailureReason
    {
        None,
        OxygenDepleted,
        CrewRiskExceeded,
        ResearchGoalMissed
    }

    public sealed class SolReport
    {
        public long SolNumber { get; }
        public double StartBattery { get; }
        public double EndBattery { get; }
        public double StartWater { get; }
        public double EndWater { get; }
        public double StartOxygen { get; }
        public double EndOxygen { get; }
        public double ResearchGained { get; }
        public IReadOnlyDictionary<string, double> CrewStaminaDeltas { get; }
        public IReadOnlyList<string> CompletedTasks { get; }
        public IReadOnlyList<string> EventsExperienced { get; }
        public string CauseEffectSummary { get; }

        internal SolReport(
            long solNumber,
            double startBattery, double endBattery,
            double startWater, double endWater,
            double startOxygen, double endOxygen,
            double researchGained,
            Dictionary<string, double> crewStaminaDeltas,
            List<string> completedTasks,
            List<string> eventsExperienced,
            string causeEffectSummary)
        {
            SolNumber = solNumber;
            StartBattery = startBattery;
            EndBattery = endBattery;
            StartWater = startWater;
            EndWater = endWater;
            StartOxygen = startOxygen;
            EndOxygen = endOxygen;
            ResearchGained = researchGained;
            CrewStaminaDeltas = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(crewStaminaDeltas));
            CompletedTasks = new ReadOnlyCollection<string>(new List<string>(completedTasks));
            EventsExperienced = new ReadOnlyCollection<string>(new List<string>(eventsExperienced));
            CauseEffectSummary = causeEffectSummary ?? "";
        }
    }

    public sealed class MissionConfig
    {
        public double SolSeconds { get; }
        public double DaySeconds { get; }
        public ResourceConfig Resources { get; }
        public double ReturnToShelterSeconds { get; }
        public double ReturnStaminaRate { get; }
        public double RestStaminaRate { get; }
        public double LowStaminaWarningThreshold { get; }
        public double MaxStamina { get; }
        public double ResearchGoal { get; }
        public int TargetSols { get; }
        public double InsideRadiationRiskRate { get; }
        public double OutsideRadiationRiskRate { get; }
        public double AcuteRiskRecoveryRate { get; }
        public double MaxCrewRiskScore { get; }
        public double OxygenOutageFailureSeconds { get; }
        public double SleepStaminaRate { get; }
        public double SleepTimeScale { get; }
        public double LowOxygenWarningThreshold { get; }
        public double UnpreppedSleepFactor { get; }
        public double CommsDelaySeconds { get; }
        public int InitialFoodRations { get; }
        public int MaxReserveMeals { get; }
        public int DailyReserveMealSupply { get; }
        public int OperationsSeed { get; }
        public bool EnableOperationsRandomEvents { get; }
        public int EarthOrderArrivalIntervalSols { get; }
        public MissionMode Mode { get; }
        public int InitialMaterials { get; }
        public int MaterialCapacity { get; }
        public int DailyMaterialSupply { get; }

        public double MissionDurationSeconds => TargetSols > 0 ? SolSeconds * TargetSols : double.PositiveInfinity;

        public static MissionConfig CreateOperationsDefault(
            ResourceConfig resources = null,
            int operationsSeed = 424242,
            bool enableOperationsRandomEvents = true)
        {
            return new MissionConfig(
                solSeconds: 720,
                daySeconds: 480,
                resources: resources ?? new ResourceConfig(
                    initialBattery: 50,
                    researchEnergyRate: 0.09,
                    researchProgressRate: 0.15,
                    initialPanelFouling: 0.20,
                    panelMaintenanceReduction: 0.20,
                    initialSystemWear: 0.15,
                    systemMaintenanceReduction: 0.15,
                    waterExtractionRate: 0.012,
                    waterExtractorEnergyRate: 0.015),
                researchGoal: 0,
                targetSols: 0,
                maxReserveMeals: 2,
                dailyReserveMealSupply: 1,
                operationsSeed: operationsSeed,
                enableOperationsRandomEvents: enableOperationsRandomEvents,
                acuteRiskRecoveryRate: 0.03,
                earthOrderArrivalIntervalSols: 2,
                mode: MissionMode.Operations);
        }

        public MissionConfig(
            double solSeconds = 720,
            double daySeconds = 480,
            ResourceConfig resources = null,
            double returnToShelterSeconds = 10,
            double returnStaminaRate = -0.10,
            double restStaminaRate = 0.20,
            double lowStaminaWarningThreshold = 20,
            double maxStamina = 100,
            double researchGoal = 120,
            int targetSols = 2,
            double insideRadiationRiskRate = 0.15,
            double outsideRadiationRiskRate = 1.20,
            double maxCrewRiskScore = 100,
            double oxygenOutageFailureSeconds = 20,
            double sleepStaminaRate = 0.40,
            double sleepTimeScale = 8.0,
            double lowOxygenWarningThreshold = 20,
            double unpreppedSleepFactor = 0.75,
            double commsDelaySeconds = 45,
            int initialFoodRations = 2,
            int maxReserveMeals = 2,
            int dailyReserveMealSupply = 0,
            int operationsSeed = 424242,
            bool enableOperationsRandomEvents = false,
            double acuteRiskRecoveryRate = 0,
            int earthOrderArrivalIntervalSols = 0,
            MissionMode mode = MissionMode.Training,
            int initialMaterials = 40,
            int materialCapacity = 80,
            int dailyMaterialSupply = 8)
        {
            if (!IsFinite(solSeconds) || solSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(solSeconds));
            if (!IsFinite(daySeconds) || daySeconds < 0 || daySeconds > solSeconds)
                throw new ArgumentOutOfRangeException(nameof(daySeconds));
            if (!IsFinite(returnToShelterSeconds) || returnToShelterSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(returnToShelterSeconds));
            if (!IsFinite(returnStaminaRate) || returnStaminaRate > 0)
                throw new ArgumentOutOfRangeException(nameof(returnStaminaRate));
            if (!IsFinite(restStaminaRate) || restStaminaRate < 0)
                throw new ArgumentOutOfRangeException(nameof(restStaminaRate));
            if (!IsFinite(lowStaminaWarningThreshold) || lowStaminaWarningThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(lowStaminaWarningThreshold));
            if (!IsFinite(maxStamina) || maxStamina <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStamina));
            if (!IsFinite(researchGoal) || researchGoal < 0)
                throw new ArgumentOutOfRangeException(nameof(researchGoal));
            if (targetSols < 0)
                throw new ArgumentOutOfRangeException(nameof(targetSols));
            if (!IsFinite(insideRadiationRiskRate) || insideRadiationRiskRate < 0)
                throw new ArgumentOutOfRangeException(nameof(insideRadiationRiskRate));
            if (!IsFinite(outsideRadiationRiskRate) || outsideRadiationRiskRate < 0)
                throw new ArgumentOutOfRangeException(nameof(outsideRadiationRiskRate));
            if (!IsFinite(acuteRiskRecoveryRate) || acuteRiskRecoveryRate < 0)
                throw new ArgumentOutOfRangeException(nameof(acuteRiskRecoveryRate));
            if (!IsFinite(maxCrewRiskScore) || maxCrewRiskScore <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCrewRiskScore));
            if (!IsFinite(oxygenOutageFailureSeconds) || oxygenOutageFailureSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(oxygenOutageFailureSeconds));
            if (!IsFinite(sleepStaminaRate) || sleepStaminaRate < 0)
                throw new ArgumentOutOfRangeException(nameof(sleepStaminaRate));
            if (!IsFinite(sleepTimeScale) || sleepTimeScale <= 0)
                throw new ArgumentOutOfRangeException(nameof(sleepTimeScale));
            if (!IsFinite(lowOxygenWarningThreshold) || lowOxygenWarningThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(lowOxygenWarningThreshold));
            if (!IsFinite(unpreppedSleepFactor) || unpreppedSleepFactor < 0 || unpreppedSleepFactor > 1)
                throw new ArgumentOutOfRangeException(nameof(unpreppedSleepFactor));
            if (!IsFinite(commsDelaySeconds) || commsDelaySeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(commsDelaySeconds));
            if (initialFoodRations < 0)
                throw new ArgumentOutOfRangeException(nameof(initialFoodRations));
            if (maxReserveMeals < 0)
                throw new ArgumentOutOfRangeException(nameof(maxReserveMeals));
            if (dailyReserveMealSupply < 0)
                throw new ArgumentOutOfRangeException(nameof(dailyReserveMealSupply));
            if (earthOrderArrivalIntervalSols < 0)
                throw new ArgumentOutOfRangeException(nameof(earthOrderArrivalIntervalSols));
            if (initialMaterials < 0)
                throw new ArgumentOutOfRangeException(nameof(initialMaterials));
            if (materialCapacity < initialMaterials)
                throw new ArgumentOutOfRangeException(nameof(materialCapacity));
            if (dailyMaterialSupply < 0)
                throw new ArgumentOutOfRangeException(nameof(dailyMaterialSupply));

            SolSeconds = solSeconds;
            DaySeconds = daySeconds;
            Resources = resources ?? new ResourceConfig();
            ReturnToShelterSeconds = returnToShelterSeconds;
            ReturnStaminaRate = returnStaminaRate;
            RestStaminaRate = restStaminaRate;
            LowStaminaWarningThreshold = lowStaminaWarningThreshold;
            MaxStamina = maxStamina;
            ResearchGoal = researchGoal;
            TargetSols = targetSols;
            InsideRadiationRiskRate = insideRadiationRiskRate;
            OutsideRadiationRiskRate = outsideRadiationRiskRate;
            AcuteRiskRecoveryRate = acuteRiskRecoveryRate;
            MaxCrewRiskScore = maxCrewRiskScore;
            OxygenOutageFailureSeconds = oxygenOutageFailureSeconds;
            SleepStaminaRate = sleepStaminaRate;
            SleepTimeScale = sleepTimeScale;
            LowOxygenWarningThreshold = lowOxygenWarningThreshold;
            UnpreppedSleepFactor = unpreppedSleepFactor;
            CommsDelaySeconds = commsDelaySeconds;
            InitialFoodRations = initialFoodRations;
            MaxReserveMeals = maxReserveMeals;
            DailyReserveMealSupply = dailyReserveMealSupply;
            OperationsSeed = operationsSeed;
            EnableOperationsRandomEvents = enableOperationsRandomEvents;
            EarthOrderArrivalIntervalSols = earthOrderArrivalIntervalSols;
            Mode = mode;
            InitialMaterials = initialMaterials;
            MaterialCapacity = materialCapacity;
            DailyMaterialSupply = dailyMaterialSupply;
        }

        internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class CrewState
    {
        public string Id { get; }
        public string Name { get; }
        public double Stamina { get; internal set; }
        public CrewLocation Location { get; internal set; }
        public CrewStatus Status { get; internal set; }
        public string ActiveTaskId { get; internal set; }
        public double ReturnRemainingSeconds { get; internal set; }
        public double RiskScore { get; internal set; }
        public string ActiveSurfaceJobId { get; internal set; }
        public double SurfacePosX { get; internal set; }
        public double SurfacePosZ { get; internal set; }
        public bool HasSurfacePosition { get; internal set; }

        public CrewState(string id, string name, double initialStamina = 100, CrewLocation initialLocation = CrewLocation.Inside)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Crew ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Crew name is required.", nameof(name));
            if (!MissionConfig.IsFinite(initialStamina) || initialStamina < 0 || initialStamina > 1000)
                throw new ArgumentOutOfRangeException(nameof(initialStamina));
            Id = id;
            Name = name;
            Stamina = initialStamina;
            Location = initialLocation;
            Status = initialStamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
        }

        internal CrewState Copy() => (CrewState)MemberwiseClone();
    }

    public sealed class TaskState
    {
        public string Id { get; }
        public string Title { get; }
        public TaskKind Kind { get; }
        public CrewLocation Location { get; }
        public bool IsContinuous { get; }
        public double RequiredSeconds { get; }
        public double StaminaRatePerSecond { get; }
        public double ProgressSeconds { get; internal set; }
        public string AssignedCrewId { get; internal set; }
        public bool IsCompleted { get; internal set; }
        public bool RewardApplied { get; internal set; }

        public double NormalizedProgress =>
            IsContinuous ? 0 : (RequiredSeconds > 0 ? Math.Min(1.0, ProgressSeconds / RequiredSeconds) : 1.0);

        public TaskState(
            string id,
            string title,
            TaskKind kind,
            CrewLocation location,
            bool isContinuous,
            double requiredSeconds,
            double staminaRatePerSecond)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Task ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Task title is required.", nameof(title));
            if (!MissionConfig.IsFinite(requiredSeconds) || requiredSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredSeconds));
            if (!isContinuous && requiredSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(requiredSeconds), "One-time tasks require a positive duration.");
            if (!MissionConfig.IsFinite(staminaRatePerSecond) || staminaRatePerSecond > 0)
                throw new ArgumentOutOfRangeException(nameof(staminaRatePerSecond));

            Id = id;
            Title = title;
            Kind = kind;
            Location = location;
            IsContinuous = isContinuous;
            RequiredSeconds = isContinuous ? 0 : requiredSeconds;
            StaminaRatePerSecond = staminaRatePerSecond;
        }

        internal TaskState Copy() => (TaskState)MemberwiseClone();
    }

    public sealed class EventState
    {
        public string Id { get; }
        public string Title { get; }
        public EventKind Kind { get; }
        public double WarningSeconds { get; }
        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public double Intensity { get; }
        public string SourceRecordId { get; }
        public string SourceNote { get; }
        public EventPhase Phase { get; internal set; }
        public bool WarningTriggered { get; internal set; }
        public bool StartTriggered { get; internal set; }
        public bool EndTriggered { get; internal set; }

        public double SecondsUntilStart(double elapsed) => Math.Max(0, StartSeconds - elapsed);
        public double SecondsRemaining(double elapsed) => Math.Max(0, EndSeconds - elapsed);

        public EventState(
            string id,
            string title,
            EventKind kind,
            double warningSeconds,
            double startSeconds,
            double endSeconds,
            double intensity,
            string sourceRecordId = "",
            string sourceNote = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Event ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Event title is required.", nameof(title));
            if (!MissionConfig.IsFinite(warningSeconds) || warningSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(warningSeconds));
            if (!MissionConfig.IsFinite(startSeconds) || startSeconds < warningSeconds)
                throw new ArgumentOutOfRangeException(nameof(startSeconds));
            if (!MissionConfig.IsFinite(endSeconds) || endSeconds <= startSeconds)
                throw new ArgumentOutOfRangeException(nameof(endSeconds));
            if (!MissionConfig.IsFinite(intensity) || intensity < 0 || (kind == EventKind.Dust && intensity > 1))
                throw new ArgumentOutOfRangeException(nameof(intensity));

            Id = id;
            Title = title;
            Kind = kind;
            WarningSeconds = warningSeconds;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Intensity = intensity;
            SourceRecordId = sourceRecordId ?? "";
            SourceNote = sourceNote ?? "";
            Phase = EventPhase.Upcoming;
        }

        internal EventState Copy() => (EventState)MemberwiseClone();
    }

    public sealed class MissionScenario
    {
        private readonly ReadOnlyCollection<CrewState> crew;
        private readonly ReadOnlyCollection<TaskState> tasks;
        private readonly ReadOnlyCollection<EventState> events;

        public IReadOnlyList<CrewState> Crew => crew;
        public IReadOnlyList<TaskState> Tasks => tasks;
        public IReadOnlyList<EventState> Events => events;

        public MissionScenario(
            IEnumerable<CrewState> crew = null,
            IEnumerable<TaskState> tasks = null,
            IEnumerable<EventState> events = null)
        {
            var crewCopy = new List<CrewState>();
            var crewIds = new HashSet<string>();
            foreach (var member in crew ?? DefaultCrew())
            {
                if (member == null || !crewIds.Add(member.Id))
                    throw new ArgumentException("Crew must have unique IDs and no null members.", nameof(crew));
                crewCopy.Add(member.Copy());
            }
            this.crew = crewCopy.AsReadOnly();

            var taskCopy = new List<TaskState>();
            var taskIds = new HashSet<string>();
            foreach (var task in tasks ?? DefaultTasks())
            {
                if (task == null || !taskIds.Add(task.Id))
                    throw new ArgumentException("Tasks must have unique IDs and no null entries.", nameof(tasks));
                taskCopy.Add(task.Copy());
            }
            this.tasks = taskCopy.AsReadOnly();

            var eventCopy = new List<EventState>();
            var eventIds = new HashSet<string>();
            foreach (var ev in events ?? DefaultEvents())
            {
                if (ev == null || !eventIds.Add(ev.Id))
                    throw new ArgumentException("Events must have unique IDs and no null entries.", nameof(events));
                eventCopy.Add(ev.Copy());
            }
            this.events = eventCopy.AsReadOnly();
        }

        private static IEnumerable<CrewState> DefaultCrew()
        {
            return new[]
            {
                new CrewState("crew-1", "Astronaut 1"),
                new CrewState("crew-2", "Astronaut 2"),
                new CrewState("crew-3", "Astronaut 3")
            };
        }

        private static IEnumerable<TaskState> DefaultTasks()
        {
            return new[]
            {
                new TaskState("panel-maintenance", "Exterior Panel Maintenance", TaskKind.PanelMaintenance, CrewLocation.Outside, false, 60, -0.35),
                new TaskState("system-maintenance", "Interior System Maintenance", TaskKind.SystemMaintenance, CrewLocation.Inside, false, 45, -0.20),
                new TaskState("meal-prep", "Reserve Meal Preparation", TaskKind.MealPreparation, CrewLocation.Inside, false, 30, -0.10),
                new TaskState("research", "Research Station", TaskKind.Research, CrewLocation.Inside, true, 0, -0.15)
            };
        }

        private static IEnumerable<EventState> DefaultEvents()
        {
            return new[]
            {
                new EventState(
                    "rad-1",
                    "Solar Particle Event #1",
                    EventKind.Radiation,
                    60, 360, 420, 1.0,
                    "2024-05-09T13:59:00-SEP-001",
                    "NASA DONKI SEP (ACE/EPAM) compressed ~5h flare-to-SEP lead into 300s warning."),
                new EventState(
                    "rad-2",
                    "Solar Particle Event #2",
                    EventKind.Radiation,
                    600, 660, 720, 1.0,
                    "2024-05-10T13:35:00-SEP-001",
                    "NASA DONKI SEP (GOES-P SEISS) ~24h follow-up wave mapped to Sol 1 night."),
                new EventState(
                    "dust-1",
                    "Regional Dust Storm",
                    EventKind.Dust,
                    750, 780, 960, 0.50,
                    "2018-MARS-DUST-CONTEXT",
                    "Educational 0.50 solar attenuation inspired by 2018 NASA Opportunity/Curiosity dust records.")
            };
        }
    }

    public sealed class MissionState
    {
        public double ElapsedSeconds { get; }
        public MissionMode Mode { get; }
        public long SolNumber { get; }
        public double SecondsIntoSol { get; }
        public bool IsNight { get; }
        public IReadOnlyList<CrewState> Crew { get; }
        public IReadOnlyList<TaskState> Tasks { get; }
        public IReadOnlyList<EventState> Events { get; }
        public ResourceState Resources { get; }
        public MissionOutcome Outcome { get; }
        public MissionFailureReason FailureReason { get; }
        public string StatusMessage { get; }
        public bool IsSleeping { get; }
        public double RecommendedTimeScale { get; }
        public bool SolReportPauseRequested { get; }
        public bool CriticalAlertPauseRequested { get; }
        public bool PauseRequested => SolReportPauseRequested || CriticalAlertPauseRequested;
        public string ActiveAlertMessage { get; }
        public int LowOxygenAlertCount { get; }
        public SolReport PendingSolReport { get; }
        public IReadOnlyList<SolReport> SolReports { get; }
        public int FoodRationsRemaining { get; }
        public bool MealPreparedThisSol { get; }
        public bool NightMealSettledForCurrentSol { get; }
        public double CurrentNightSleepFactor { get; }
        public EarthAdviceStatus EarthAdviceStatus { get; }
        public double EarthAdviceRequestedAtSeconds { get; }
        public double EarthAdviceExpectedAtSeconds { get; }
        public double EarthAdviceDeliveredAtSeconds { get; }
        public string EarthAdviceMessage { get; }
        public IReadOnlyList<ChannelScheduleState> ChannelSchedules { get; }
        public IReadOnlyList<string> OperationsLog { get; }
        public IReadOnlyList<EarthOrderState> EarthOrders { get; }
        public ParcelMap Map { get; }
        public int Materials { get; }
        public int MaterialCapacity { get; }
        public long LastMaterialSupplySol { get; }
        public IReadOnlyList<SurfaceJobState> SurfaceJobs { get; }
        public IReadOnlyList<FacilityState> Facilities { get; }
        public double EffectiveBatteryCapacity { get; }
        public int MaterialsRefundedTotal { get; }
        public int MaterialsForfeitedTotal { get; }
        public double BuildEnergyLostTotal { get; }
        public double EarthAdviceRemainingSeconds =>
            EarthAdviceStatus == EarthAdviceStatus.Pending ? Math.Max(0, EarthAdviceExpectedAtSeconds - ElapsedSeconds) : 0;

        public bool ShouldSuggestEarthAdvice
        {
            get
            {
                if (EarthAdviceStatus != EarthAdviceStatus.NotRequested || Outcome != MissionOutcome.InProgress)
                    return false;
                for (int i = 0; i < Events.Count; i++)
                {
                    var ev = Events[i];
                    if ((ev.Phase == EventPhase.Upcoming || ev.Phase == EventPhase.Warning) &&
                        ev.SecondsUntilStart(ElapsedSeconds) <= 30.0)
                        return true;
                }
                return false;
            }
        }

        internal MissionState(
            double elapsed,
            MissionConfig config,
            IReadOnlyList<CrewState> crew,
            IReadOnlyList<TaskState> tasks,
            IReadOnlyList<EventState> events,
            ResourceState resources,
            MissionOutcome outcome,
            MissionFailureReason failureReason,
            string statusMessage,
            bool isSleeping,
            bool solReportPauseRequested,
            bool criticalAlertPauseRequested,
            string activeAlertMessage,
            int lowOxygenAlertCount,
            SolReport pendingSolReport,
            IReadOnlyList<SolReport> solReports,
            int foodRationsRemaining,
            bool mealPreparedThisSol,
            bool nightMealSettledForCurrentSol,
            double currentNightSleepFactor,
            EarthAdviceStatus earthAdviceStatus,
            double earthAdviceRequestedAtSeconds,
            double earthAdviceExpectedAtSeconds,
            double earthAdviceDeliveredAtSeconds,
            string earthAdviceMessage,
            IReadOnlyList<ChannelScheduleState> channelSchedules,
            IReadOnlyList<string> operationsLog,
            IReadOnlyList<EarthOrderState> earthOrders,
            ParcelMap map,
            int materials,
            int materialCapacity,
            long lastMaterialSupplySol,
            IReadOnlyList<SurfaceJobState> surfaceJobs = null,
            IReadOnlyList<FacilityState> facilities = null,
            int materialsRefundedTotal = 0,
            int materialsForfeitedTotal = 0,
            double buildEnergyLostTotal = 0)
        {
            ElapsedSeconds = elapsed;
            Mode = config.Mode;
            SolNumber = (long)Math.Floor(elapsed / config.SolSeconds) + 1;
            SecondsIntoSol = elapsed % config.SolSeconds;
            IsNight = SecondsIntoSol >= config.DaySeconds;
            Crew = crew;
            Tasks = tasks;
            Events = events;
            Resources = resources;
            Outcome = outcome;
            FailureReason = failureReason;
            StatusMessage = statusMessage ?? "";
            IsSleeping = isSleeping;
            RecommendedTimeScale = isSleeping ? config.SleepTimeScale : 1.0;
            SolReportPauseRequested = solReportPauseRequested;
            CriticalAlertPauseRequested = criticalAlertPauseRequested;
            ActiveAlertMessage = activeAlertMessage ?? "";
            LowOxygenAlertCount = lowOxygenAlertCount;
            PendingSolReport = pendingSolReport;
            SolReports = solReports;
            FoodRationsRemaining = foodRationsRemaining;
            MealPreparedThisSol = mealPreparedThisSol;
            NightMealSettledForCurrentSol = nightMealSettledForCurrentSol;
            CurrentNightSleepFactor = currentNightSleepFactor;
            EarthAdviceStatus = earthAdviceStatus;
            EarthAdviceRequestedAtSeconds = earthAdviceRequestedAtSeconds;
            EarthAdviceExpectedAtSeconds = earthAdviceExpectedAtSeconds;
            EarthAdviceDeliveredAtSeconds = earthAdviceDeliveredAtSeconds;
            EarthAdviceMessage = earthAdviceMessage ?? "";
            ChannelSchedules = channelSchedules;
            OperationsLog = operationsLog;
            EarthOrders = earthOrders;
            Map = map;
            Materials = materials;
            MaterialCapacity = materialCapacity;
            LastMaterialSupplySol = lastMaterialSupplySol;
            SurfaceJobs = surfaceJobs ?? Array.AsReadOnly(Array.Empty<SurfaceJobState>());
            Facilities = facilities ?? Array.AsReadOnly(Array.Empty<FacilityState>());
            EffectiveBatteryCapacity = ColonyStorage.GetEffectiveBatteryCapacity(config.Resources, Facilities);
            MaterialsRefundedTotal = materialsRefundedTotal;
            MaterialsForfeitedTotal = materialsForfeitedTotal;
            BuildEnergyLostTotal = buildEnergyLostTotal;
        }
    }

    public sealed class CommandResult
    {
        public bool Success { get; }
        public string Reason { get; }
        private CommandResult(bool success, string reason) { Success = success; Reason = reason; }
        internal static CommandResult Unsupported() => new CommandResult(false, "This command is not implemented yet.");
        public static CommandResult Rejected(string reason) => new CommandResult(false, reason);
        public static CommandResult Accepted(string reason = "") => new CommandResult(true, reason);
    }

    public sealed class MissionSimulation
    {
        private readonly MissionConfig config;
        private readonly MissionScenario scenario;
        private SimulationMutableState sim;
        private double timeCompensation;

        public MissionConfig Config => config;
        public MissionState State { get; private set; }

        public MissionSimulation(MissionConfig config, MissionScenario scenario)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            InitializeFromScenario();
        }

        private void InitializeFromScenario()
        {
            var crew = new List<CrewState>(scenario.Crew.Count);
            for (int i = 0; i < scenario.Crew.Count; i++)
            {
                var c = scenario.Crew[i].Copy();
                c.Stamina = Math.Min(config.MaxStamina, c.Stamina);
                c.Status = c.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                c.ActiveTaskId = null;
                c.ReturnRemainingSeconds = 0;
                c.RiskScore = 0;
                c.ActiveSurfaceJobId = null;
                c.SurfacePosX = 0;
                c.SurfacePosZ = 0;
                c.HasSurfacePosition = false;
                crew.Add(c);
            }

            var tasks = new List<TaskState>(scenario.Tasks.Count);
            for (int i = 0; i < scenario.Tasks.Count; i++)
                tasks.Add(scenario.Tasks[i].Copy());

            var events = new List<EventState>(scenario.Events.Count);
            for (int i = 0; i < scenario.Events.Count; i++)
            {
                var ev = scenario.Events[i].Copy();
                ev.Phase = ev.WarningSeconds <= 0 ? EventPhase.Warning : EventPhase.Upcoming;
                ev.WarningTriggered = ev.WarningSeconds <= 0;
                ev.StartTriggered = false;
                ev.EndTriggered = false;
                events.Add(ev);
            }

            var resources = ResourceState.Initial(config.Resources);
            if (config.Mode == MissionMode.Operations)
            {
                resources.Panels = new List<PanelUnitState>
                {
                    new PanelUnitState("panel-1", PanelUnitState.DefaultUnitWeight, facilityId: ParcelMap.StarterSolarSpec.FacilityId, solarFactor: 1.0),
                    new PanelUnitState("panel-2", PanelUnitState.DefaultUnitWeight, facilityId: ParcelMap.StarterSolarSpec.FacilityId, solarFactor: 1.0),
                    new PanelUnitState("panel-3", PanelUnitState.DefaultUnitWeight, facilityId: ParcelMap.StarterSolarSpec.FacilityId, solarFactor: 1.0)
                };
                bool hasPanelRepairTask = false;
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].Kind == TaskKind.SolarPanelRepair)
                    {
                        hasPanelRepairTask = true;
                        break;
                    }
                }
                if (!hasPanelRepairTask)
                {
                    tasks.Add(new TaskState(
                        "panel-repair",
                        "Exterior Solar Panel Repair",
                        TaskKind.SolarPanelRepair,
                        CrewLocation.Outside,
                        false,
                        60,
                        -0.35));
                }
                bool hasO2RepairTask = false;
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].Kind == TaskKind.OxygenProducerRepair)
                    {
                        hasO2RepairTask = true;
                        break;
                    }
                }
                if (!hasO2RepairTask)
                {
                    tasks.Add(new TaskState(
                        "o2-repair",
                        "Interior O2 Producer Repair",
                        TaskKind.OxygenProducerRepair,
                        CrewLocation.Inside,
                        false,
                        40,
                        -0.20));
                }
            }
            var tracker = new SolTracker();
            tracker.ResetForSol(1, resources, crew);

            sim = new SimulationMutableState
            {
                Resources = resources,
                Crew = crew,
                Tasks = tasks,
                Events = events,
                Outcome = MissionOutcome.InProgress,
                FailureReason = MissionFailureReason.None,
                StatusMessage = "",
                IsSleeping = false,
                CanTriggerLowOxygenAlert = resources.Oxygen >= config.LowOxygenWarningThreshold,
                CriticalAlertPauseRequested = false,
                ActiveAlertMessage = "",
                LowOxygenAlertCount = 0,
                SolReportPauseRequested = false,
                PendingSolReport = null,
                SolReports = new List<SolReport>(),
                SolTracker = tracker,
                FoodRationsRemaining = config.InitialFoodRations,
                MealPreparedThisSol = false,
                NightMealSettledSol = 0,
                NightRationAvailable = false,
                SleepStartedThisSol = false,
                NightSleepRecoveryFactor = config.UnpreppedSleepFactor,
                EarthAdviceStatus = EarthAdviceStatus.NotRequested,
                EarthAdviceRequestedAtSeconds = -1,
                EarthAdviceExpectedAtSeconds = -1,
                EarthAdviceDeliveredAtSeconds = -1,
                EarthAdviceMessage = "",
                ChannelSchedules = config.EnableOperationsRandomEvents
                    ? OperationsScheduler.CreateInitialChannels(config.OperationsSeed)
                    : new List<ChannelScheduleState>(),
                OperationsLog = new List<string>(),
                EarthOrders = new List<EarthOrderState>(),
                EarthOrderBag = config.Mode == MissionMode.Operations ? new EarthOrderBagState(config.OperationsSeed) : null,
                Map = config.Mode == MissionMode.Operations ? ParcelMap.Generate(ParcelMap.DeriveMapSeed(config.OperationsSeed)) : null,
                Materials = config.Mode == MissionMode.Operations ? config.InitialMaterials : 0,
                LastMaterialSupplySol = config.Mode == MissionMode.Operations ? 1 : 0,
                NextEntityId = 1,
                SurfaceJobs = new List<SurfaceJobState>(),
                Facilities = config.Mode == MissionMode.Operations
                    ? FacilityState.CreateStarterFacilities()
                    : new List<FacilityState>()
            };

            if (config.Mode == MissionMode.Operations)
                UtilityNetwork.RefreshTopology(sim);

            timeCompensation = 0;
            PublishState(0);
        }

        private void PublishState(double elapsed)
        {
            var crewSnap = new List<CrewState>(sim.Crew.Count);
            for (int i = 0; i < sim.Crew.Count; i++)
                crewSnap.Add(sim.Crew[i].Copy());

            var taskSnap = new List<TaskState>(sim.Tasks.Count);
            for (int i = 0; i < sim.Tasks.Count; i++)
                taskSnap.Add(sim.Tasks[i].Copy());

            var eventSnap = new List<EventState>(sim.Events.Count);
            for (int i = 0; i < sim.Events.Count; i++)
                eventSnap.Add(sim.Events[i].Copy());

            var channelSnap = new List<ChannelScheduleState>(sim.ChannelSchedules.Count);
            for (int i = 0; i < sim.ChannelSchedules.Count; i++)
                channelSnap.Add(sim.ChannelSchedules[i].Clone());

            var logSnap = new List<string>(sim.OperationsLog);
            var orderSnap = new List<EarthOrderState>(sim.EarthOrders != null ? sim.EarthOrders.Count : 0);
            if (sim.EarthOrders != null)
            {
                for (int i = 0; i < sim.EarthOrders.Count; i++)
                    orderSnap.Add(sim.EarthOrders[i].Clone());
            }

            var sJobSnap = new List<SurfaceJobState>(sim.SurfaceJobs != null ? sim.SurfaceJobs.Count : 0);
            if (config.Mode == MissionMode.Operations && sim.SurfaceJobs != null)
            {
                for (int i = 0; i < sim.SurfaceJobs.Count; i++)
                    sJobSnap.Add(sim.SurfaceJobs[i].Clone());
            }

            var facSnap = new List<FacilityState>(sim.Facilities != null ? sim.Facilities.Count : 0);
            if (config.Mode == MissionMode.Operations && sim.Facilities != null)
            {
                for (int i = 0; i < sim.Facilities.Count; i++)
                    facSnap.Add(sim.Facilities[i].Clone());
            }

            State = new MissionState(
                elapsed,
                config,
                crewSnap.AsReadOnly(),
                taskSnap.AsReadOnly(),
                eventSnap.AsReadOnly(),
                sim.Resources.Copy(),
                sim.Outcome,
                sim.FailureReason,
                sim.StatusMessage,
                sim.IsSleeping,
                sim.SolReportPauseRequested,
                sim.CriticalAlertPauseRequested,
                sim.ActiveAlertMessage,
                sim.LowOxygenAlertCount,
                sim.PendingSolReport,
                new ReadOnlyCollection<SolReport>(new List<SolReport>(sim.SolReports)),
                sim.FoodRationsRemaining,
                sim.MealPreparedThisSol,
                sim.NightMealSettledSol == trackerSol(elapsed),
                sim.NightSleepRecoveryFactor,
                sim.EarthAdviceStatus,
                sim.EarthAdviceRequestedAtSeconds,
                sim.EarthAdviceExpectedAtSeconds,
                sim.EarthAdviceDeliveredAtSeconds,
                sim.EarthAdviceMessage,
                channelSnap.AsReadOnly(),
                logSnap.AsReadOnly(),
                orderSnap.AsReadOnly(),
                sim.Map?.Clone(),
                sim.Materials,
                config.Mode == MissionMode.Operations ? config.MaterialCapacity : 0,
                sim.LastMaterialSupplySol,
                sJobSnap.AsReadOnly(),
                facSnap.AsReadOnly(),
                sim.MaterialsRefundedTotal,
                sim.MaterialsForfeitedTotal,
                sim.BuildEnergyLostTotal);
        }

        private long trackerSol(double elapsed) => (long)Math.Floor(elapsed / config.SolSeconds) + 1;

        public void Tick(double seconds)
        {
            if (!MissionConfig.IsFinite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (seconds == 0 || sim.Outcome != MissionOutcome.InProgress) return;

            double adjusted = seconds - timeCompensation;
            double next = State.ElapsedSeconds + adjusted;
            // 2^63 is the first double at or above long.MaxValue; reserve room for sol 1.
            if (!MissionConfig.IsFinite(next) || next / config.SolSeconds >= 9223372036854775808d)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Elapsed time exceeds the supported calendar.");

            double nextCompensation = (next - State.ElapsedSeconds) - adjusted;

            var nextSim = sim.Clone();
            double finalElapsed = ResourceSimulation.AdvanceAll(nextSim, config, State.ElapsedSeconds, next - State.ElapsedSeconds);

            sim = nextSim;
            timeCompensation = sim.Outcome != MissionOutcome.InProgress ? 0 : nextCompensation;
            PublishState(finalElapsed);
        }

        public void Restart()
        {
            InitializeFromScenario();
        }

        public CommandResult AssignTask(string crewId, string taskId) => AssignTaskInternal(crewId, taskId, false);

        public CommandResult ReplaceTask(string crewId, string taskId) => AssignTaskInternal(crewId, taskId, true);

        private CommandResult AssignTaskInternal(string crewId, string taskId, bool replace)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.IsSleeping)
                return CommandResult.Rejected("Cannot assign tasks while the crew is sleeping.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");
            if (string.IsNullOrWhiteSpace(taskId))
                return CommandResult.Rejected("Task ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");

            var task = FindTask(taskId);
            if (task == null)
                return CommandResult.Rejected($"Unknown task '{taskId}'.");

            if (member.Status == CrewStatus.Sleeping)
                return CommandResult.Rejected("Cannot assign tasks while sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is currently returning to shelter.");
            if (!replace && (member.Status == CrewStatus.Working || member.ActiveTaskId != null || member.ActiveSurfaceJobId != null))
                return CommandResult.Rejected("Crew member is already assigned to a task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest before working.");
            if (task.IsCompleted)
                return CommandResult.Rejected("This one-time task is already completed.");
            if (task.AssignedCrewId != null && !(replace && task.AssignedCrewId == member.Id))
                return CommandResult.Rejected("Task station is already occupied by another crew member.");
            if (task.Location == CrewLocation.Inside && member.Location == CrewLocation.Outside)
                return CommandResult.Rejected("Crew member is outside and must return to shelter before taking an indoor task.");
            if (task.Kind == TaskKind.MealPreparation)
            {
                bool hasSolRation = sim.NightMealSettledSol == State.SolNumber
                    ? sim.NightRationAvailable
                    : sim.FoodRationsRemaining > 0;
                if (!hasSolRation)
                    return CommandResult.Rejected("No reserve food rations remaining for meal preparation.");
            }
            if (task.Kind == TaskKind.SolarPanelRepair)
            {
                bool anyFaulted = false;
                if (sim.Resources.Panels != null)
                {
                    for (int i = 0; i < sim.Resources.Panels.Count; i++)
                    {
                        if (sim.Resources.Panels[i].IsFaulted)
                        {
                            anyFaulted = true;
                            break;
                        }
                    }
                }
                if (!anyFaulted)
                    return CommandResult.Rejected("All solar panels are healthy; no repair needed.");
            }
            if (task.Kind == TaskKind.OxygenProducerRepair && !sim.Resources.OxygenProducerFaulted)
                return CommandResult.Rejected("Oxygen producer is healthy; no repair needed.");
            string station = task.Kind == TaskKind.OxygenProducerRepair ? "system-maintenance" : task.Id;
            foreach (var otherTask in sim.Tasks)
            {
                string otherStation = otherTask.Kind == TaskKind.OxygenProducerRepair ? "system-maintenance" : otherTask.Id;
                if (otherTask.Id != task.Id && otherStation == station && otherTask.AssignedCrewId != null && otherTask.AssignedCrewId != member.Id)
                    return CommandResult.Rejected("Station is occupied by another task.");
            }
            foreach (var order in sim.EarthOrders)
                if (order.Status == EarthOrderStatus.InProgress && order.AssignedCrewId != null &&
                    order.AssignedCrewId != member.Id && order.StationTaskId == station)
                    return CommandResult.Rejected("Station is occupied by an Earth order. Wait or release that crew member first.");

            if (replace)
                ReleaseTask(member);

            if (task.Location == CrewLocation.Outside)
                member.Location = CrewLocation.Outside;

            member.Status = CrewStatus.Working;
            member.ActiveTaskId = task.Id;
            task.AssignedCrewId = member.Id;

            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        internal bool ResolveOperationsFault(OperationsChannelId channelId)
        {
            if (sim.ChannelSchedules == null) return false;
            for (int i = 0; i < sim.ChannelSchedules.Count; i++)
            {
                var ch = sim.ChannelSchedules[i];
                if (ch.ChannelId == channelId && ch.IsActive && !OperationsScheduler.IsTimedChannel(channelId))
                {
                    OperationsScheduler.RescheduleAfterResolution(ch, State.ElapsedSeconds);
                    sim.OperationsLog.Add($"[{State.ElapsedSeconds:0.#}s] {ch.ChannelId} resolved -> next at {ch.NextTriggerAtSeconds:0.#}s");
                    PublishState(State.ElapsedSeconds);
                    return true;
                }
            }
            return false;
        }

        public CommandResult SpendScience(double amount, string reason = null)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (!MissionConfig.IsFinite(amount) || amount <= 0)
                return CommandResult.Rejected("Science spend amount must be positive.");

            const double tol = 1e-9;
            if (sim.Resources.ScienceBalance + tol < amount)
                return CommandResult.Rejected($"Insufficient science balance ({sim.Resources.ScienceBalance:0.#} / {amount:0.#} required).");

            sim.Resources.ScienceBalance = Math.Max(0.0, sim.Resources.ScienceBalance - amount);
            sim.StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? $"Spent {amount:0.#} science."
                : $"Spent {amount:0.#} science: {reason}.";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }


        public const int ClaimParcelCost = 2;

        public CommandResult ClaimParcel(int x, int z) => ClaimParcel(new GridCoord(x, z));

        public CommandResult ClaimParcel(GridCoord coord)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Parcel claiming is only available in Operations mode.");
            if (!sim.Map.TryGetCell(coord, out var cell))
                return CommandResult.Rejected($"Parcel {coord} is outside the map bounds.");
            if (!cell.IsExplored)
                return CommandResult.Rejected($"Parcel {coord} must be explored before claiming.");
            if (cell.IsClaimed)
                return CommandResult.Rejected($"Parcel {coord} is already claimed.");
            if (!sim.Map.HasClaimedNeighbor(coord))
                return CommandResult.Rejected($"Parcel {coord} must border a claimed parcel (N/E/S/W).");
            if (sim.Materials < ClaimParcelCost)
                return CommandResult.Rejected($"Insufficient materials ({sim.Materials} / {ClaimParcelCost} required).");

            sim.Materials -= ClaimParcelCost;
            cell.IsClaimed = true;
            sim.StatusMessage = $"Claimed parcel {coord} (-{ClaimParcelCost} materials).";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult AssignSurfaceWork(
            string crewId,
            GridCoord targetCoord,
            double workRequiredSeconds = 10.0,
            SurfaceJobKind kind = SurfaceJobKind.GenericWork,
            string targetId = null,
            bool allowTargetCell = false,
            double workStaminaRate = -0.18,
            bool replace = false)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.IsSleeping)
                return CommandResult.Rejected("Cannot assign surface work while the crew is sleeping.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Surface jobs are only available in Operations mode.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");
            if (!MissionConfig.IsFinite(workRequiredSeconds) || workRequiredSeconds < 0)
                return CommandResult.Rejected("Invalid surface work duration.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");
            if (member.Status == CrewStatus.Sleeping)
                return CommandResult.Rejected("Cannot assign tasks while sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is currently returning to shelter.");
            if (!replace && (member.Status == CrewStatus.Working || member.ActiveTaskId != null || member.ActiveSurfaceJobId != null))
                return CommandResult.Rejected("Crew member is already assigned to a task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest before working.");
            if (!sim.Map.Config.IsInBounds(targetCoord))
                return CommandResult.Rejected($"Parcel {targetCoord} is outside the map bounds.");

            GridCoord startCell;
            WorldPos2D startPos;
            if (member.Location == CrewLocation.Inside || !member.HasSurfacePosition)
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }
            else if (sim.Map.Config.TryWorldToCell(member.SurfacePosX, member.SurfacePosZ, out var curCell))
            {
                startCell = curCell;
                startPos = new WorldPos2D(member.SurfacePosX, member.SurfacePosZ);
            }
            else
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }

            if (!GridNav.TryFindBestWorkCell(sim.Map, startCell, targetCoord, allowTargetCell, out var workCell, out var cellPath))
                return CommandResult.Rejected($"No reachable surface path to parcel {targetCoord}.");

            if (replace)
                ReleaseTask(member);

            if (sim.SurfaceJobs == null)
                sim.SurfaceJobs = new List<SurfaceJobState>();

            double legLen = GridNav.BuildWorldWaypoints(sim.Map.Config, startPos, cellPath, endAtAirlockOuter: false, out var waypoints);
            string resolvedTargetId = string.IsNullOrWhiteSpace(targetId) ? $"parcel-{targetCoord.X}-{targetCoord.Z}" : targetId;
            string jobId = $"sjob-{sim.NextEntityId++}";

            var phase = legLen <= 1e-6
                ? (workRequiredSeconds <= 0 ? SurfaceJobPhase.Completed : SurfaceJobPhase.Work)
                : SurfaceJobPhase.Travel;

            var job = new SurfaceJobState(
                jobId,
                kind,
                resolvedTargetId,
                targetCoord,
                workCell,
                member.Id,
                phase,
                GridNav.DefaultWalkSpeed,
                workRequiredSeconds,
                workStaminaRate,
                0,
                waypoints,
                legLen);

            if (phase == SurfaceJobPhase.Completed)
            {
                job.AssignedCrewId = null;
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = waypoints[waypoints.Count - 1].X;
                member.SurfacePosZ = waypoints[waypoints.Count - 1].Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = null;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Idle;
            }
            else
            {
                sim.SurfaceJobs.Add(job);
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = startPos.X;
                member.SurfacePosZ = startPos.Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = job.JobId;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Working;
            }

            sim.StatusMessage = $"{member.Name} dispatched to surface work at {targetCoord}.";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult ValidateBlueprintPlacement(FacilityKind kind, GridCoord origin, FacilityRotation rotation, out List<GridCoord> footprint)
            => FacilityPlacementValidator.ValidatePlacement(sim, config, kind, origin, rotation, out footprint);

        public CommandResult PlaceBlueprint(FacilityKind kind, GridCoord origin, FacilityRotation rotation = FacilityRotation.Deg0)
        {
            var check = FacilityPlacementValidator.ValidatePlacement(sim, config, kind, origin, rotation, out var footprint);
            if (!check.Success)
                return check;

            var spec = FacilityCatalog.GetSpec(kind);
            if (sim.Facilities == null)
                sim.Facilities = FacilityState.CreateStarterFacilities();

            string facId = $"fac-{sim.NextEntityId++}";
            sim.Materials -= spec.MaterialCost;

            var facility = new FacilityState(
                facId,
                kind,
                origin,
                rotation,
                footprint,
                FacilityLifecycleStatus.Blueprint,
                workProgressSeconds: 0,
                workRequiredSeconds: spec.NetWorkSeconds,
                energySpentTotal: 0,
                energyRequiredTotal: spec.WorkEnergyCost,
                paidMaterials: spec.MaterialCost,
                isProtectedStarter: false,
                isConnectedToCore: false,
                isProductionApplied: false);

            for (int i = 0; i < footprint.Count; i++)
            {
                var cell = sim.Map.GetCell(footprint[i]);
                if (spec.UsesUtilityLayerOnly)
                {
                    cell.UtilityBlueprintId = facId;
                }
                else
                {
                    cell.FacilityId = facId;
                    if (spec.BlocksPassability)
                        cell.IsPassable = false;
                }
            }

            sim.Facilities.Add(facility);
            sim.StatusMessage = $"Placed {kind} blueprint at {origin} (-{spec.MaterialCost} materials).";
            sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] Blueprint placed: {facId} ({kind}) at {origin}");
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted(facId);
        }

        public CommandResult AssignBuildBlueprint(string crewId, string facilityId, bool replace = false)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.IsSleeping)
                return CommandResult.Rejected("Cannot assign construction while the crew is sleeping.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Construction is only available in Operations mode.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");
            if (string.IsNullOrWhiteSpace(facilityId))
                return CommandResult.Rejected("Facility ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");
            if (member.Status == CrewStatus.Sleeping)
                return CommandResult.Rejected("Cannot assign tasks while sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is currently returning to shelter.");
            if (!replace && (member.Status == CrewStatus.Working || member.ActiveTaskId != null || member.ActiveSurfaceJobId != null))
                return CommandResult.Rejected("Crew member is already assigned to a task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest before working.");

            var fac = FindFacility(facilityId);
            if (fac == null)
                return CommandResult.Rejected($"Unknown facility '{facilityId}'.");
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                return CommandResult.Rejected("Facility is already commissioned or not a blueprint.");

            if (sim.SurfaceJobs != null)
            {
                for (int i = 0; i < sim.SurfaceJobs.Count; i++)
                {
                    var existing = sim.SurfaceJobs[i];
                    if (existing.Kind == SurfaceJobKind.Construct &&
                        existing.TargetId == facilityId &&
                        existing.Phase != SurfaceJobPhase.Completed &&
                        !string.IsNullOrEmpty(existing.AssignedCrewId))
                    {
                        if (!(replace && existing.AssignedCrewId == member.Id))
                            return CommandResult.Rejected("Another crew member is already constructing this facility.");
                    }
                }
            }

            GridCoord startCell;
            WorldPos2D startPos;
            if (member.Location == CrewLocation.Inside || !member.HasSurfacePosition)
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }
            else if (sim.Map.Config.TryWorldToCell(member.SurfacePosX, member.SurfacePosZ, out var curCell))
            {
                startCell = curCell;
                startPos = new WorldPos2D(member.SurfacePosX, member.SurfacePosZ);
            }
            else
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }

            FacilityCatalog.TryGetSpec(fac.Kind, out var spec);
            bool allowTargetCell = spec != null && spec.UsesUtilityLayerOnly;

            GridCoord bestTargetCell = fac.Origin;
            GridCoord bestWorkCell = default;
            List<GridCoord> bestPath = null;
            if (fac.FootprintCells != null && fac.FootprintCells.Count > 0)
            {
                for (int i = 0; i < fac.FootprintCells.Count; i++)
                {
                    var candidateTarget = fac.FootprintCells[i];
                    if (GridNav.TryFindBestWorkCell(sim.Map, startCell, candidateTarget, allowTargetCell, out var candWorkCell, out var candPath))
                    {
                        if (bestPath == null || candPath.Count < bestPath.Count)
                        {
                            bestTargetCell = candidateTarget;
                            bestWorkCell = candWorkCell;
                            bestPath = candPath;
                        }
                    }
                }
            }
            else if (GridNav.TryFindBestWorkCell(sim.Map, startCell, fac.Origin, allowTargetCell, out var origWorkCell, out var origPath))
            {
                bestTargetCell = fac.Origin;
                bestWorkCell = origWorkCell;
                bestPath = origPath;
            }

            if (bestPath == null)
                return CommandResult.Rejected($"No reachable surface path to construct blueprint '{facilityId}'.");

            if (replace)
                ReleaseTask(member);

            if (sim.SurfaceJobs == null)
                sim.SurfaceJobs = new List<SurfaceJobState>();

            double legLen = GridNav.BuildWorldWaypoints(sim.Map.Config, startPos, bestPath, endAtAirlockOuter: false, out var waypoints);
            double remWork = Math.Max(0, fac.WorkRequiredSeconds - fac.WorkProgressSeconds);
            double remEnergy = Math.Max(0, fac.EnergyRequiredTotal - fac.EnergySpentTotal);
            string jobId = $"sjob-{sim.NextEntityId++}";

            var phase = legLen <= 1e-6
                ? (remWork <= 0 ? SurfaceJobPhase.Completed : SurfaceJobPhase.Work)
                : SurfaceJobPhase.Travel;

            var job = new SurfaceJobState(
                jobId,
                SurfaceJobKind.Construct,
                fac.FacilityId,
                bestTargetCell,
                bestWorkCell,
                member.Id,
                phase,
                GridNav.DefaultWalkSpeed,
                remWork,
                -0.20,
                remEnergy,
                waypoints,
                legLen);

            if (phase == SurfaceJobPhase.Completed)
            {
                ResourceSimulation.CompleteBlueprintFacility(sim, fac.FacilityId);
                job.AssignedCrewId = null;
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = waypoints[waypoints.Count - 1].X;
                member.SurfacePosZ = waypoints[waypoints.Count - 1].Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = null;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Idle;
            }
            else
            {
                sim.SurfaceJobs.Add(job);
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = startPos.X;
                member.SurfacePosZ = startPos.Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = job.JobId;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Working;
            }

            sim.StatusMessage = $"{member.Name} dispatched to construct {fac.Kind} ({fac.FacilityId}).";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted(jobId);
        }

        public CommandResult CanDismantleFacility(string facilityId)
        {
            return FacilityPlacementValidator.CanDismantleOrModifyFacility(sim, facilityId);
        }

        public CommandResult AssignRepairPanel(string crewId, string panelOrFacilityId = null, bool replace = false)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.IsSleeping)
                return CommandResult.Rejected("Cannot assign panel repair while the crew is sleeping.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Targeted surface panel repair is only available in Operations mode.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");
            if (member.Status == CrewStatus.Sleeping)
                return CommandResult.Rejected("Cannot assign tasks while sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is currently returning to shelter.");
            if (!replace && (member.Status == CrewStatus.Working || member.ActiveTaskId != null || member.ActiveSurfaceJobId != null))
                return CommandResult.Rejected("Crew member is already assigned to a task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest before working.");

            var panels = sim.Resources?.Panels;
            if (panels == null || panels.Count == 0)
                return CommandResult.Rejected("No solar panels available.");

            PanelUnitState targetPanel = null;
            if (!string.IsNullOrWhiteSpace(panelOrFacilityId))
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    if (panels[i].Id == panelOrFacilityId)
                    {
                        targetPanel = panels[i];
                        break;
                    }
                }

                if (targetPanel == null)
                {
                    var reqFac = FindFacility(panelOrFacilityId);
                    if (reqFac != null)
                    {
                        for (int i = 0; i < panels.Count; i++)
                        {
                            var p = panels[i];
                            bool owns = p.FacilityId == reqFac.FacilityId ||
                                (reqFac.LinkedPanelIds != null && reqFac.LinkedPanelIds.Contains(p.Id));
                            if (owns && p.IsFaulted)
                            {
                                targetPanel = p;
                                break;
                            }
                        }
                        if (targetPanel == null)
                            return CommandResult.Rejected($"No faulted solar panel on {reqFac.FacilityId}.");
                    }
                }

                if (targetPanel == null)
                    return CommandResult.Rejected($"Unknown solar panel or facility '{panelOrFacilityId}'.");
                if (!targetPanel.IsFaulted)
                    return CommandResult.Rejected($"Solar panel '{targetPanel.Id}' is healthy; no repair needed.");
            }
            else
            {
                string activeTargetId = null;
                if (sim.ChannelSchedules != null)
                {
                    for (int i = 0; i < sim.ChannelSchedules.Count; i++)
                    {
                        var ch = sim.ChannelSchedules[i];
                        if (ch.ChannelId == OperationsChannelId.SolarPanel && ch.IsActive)
                        {
                            activeTargetId = ch.ActiveTargetId;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(activeTargetId))
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        if (panels[i].Id == activeTargetId && panels[i].IsFaulted)
                        {
                            targetPanel = panels[i];
                            break;
                        }
                    }
                }

                if (targetPanel == null)
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        if (panels[i].IsFaulted)
                        {
                            targetPanel = panels[i];
                            break;
                        }
                    }
                }

                if (targetPanel == null)
                    return CommandResult.Rejected("All solar panels are healthy; no repair needed.");
            }

            if (sim.SurfaceJobs != null)
            {
                for (int i = 0; i < sim.SurfaceJobs.Count; i++)
                {
                    var sj = sim.SurfaceJobs[i];
                    if (sj.Kind == SurfaceJobKind.Repair && sj.TargetId == targetPanel.Id &&
                        sj.Phase != SurfaceJobPhase.Completed &&
                        !string.IsNullOrEmpty(sj.AssignedCrewId) &&
                        sj.AssignedCrewId != member.Id)
                    {
                        return CommandResult.Rejected("Another crew member is already repairing this solar panel.");
                    }
                }
            }

            var ownerFac = SolarProduction.FindOwningFacility(targetPanel, sim.Facilities);
            GridCoord targetCoord = ownerFac != null ? ownerFac.Origin : ParcelMap.StarterSolarSpec.Footprint[0];

            GridCoord startCell;
            WorldPos2D startPos;
            if (member.Location == CrewLocation.Inside || !member.HasSurfacePosition)
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }
            else if (sim.Map.Config.TryWorldToCell(member.SurfacePosX, member.SurfacePosZ, out var curCell))
            {
                startCell = curCell;
                startPos = new WorldPos2D(member.SurfacePosX, member.SurfacePosZ);
            }
            else
            {
                startCell = ParcelMap.AirlockCorridorCoord;
                startPos = GridNav.AirlockOuterWorldPos;
            }

            if (!GridNav.TryFindBestWorkCell(sim.Map, startCell, targetCoord, allowTargetCell: false, out var workCell, out var path))
                return CommandResult.Rejected($"No reachable surface path to repair solar panel '{targetPanel.Id}' at {targetCoord}.");

            if (replace)
                ReleaseTask(member);

            if (sim.SurfaceJobs == null)
                sim.SurfaceJobs = new List<SurfaceJobState>();

            double legLen = GridNav.BuildWorldWaypoints(sim.Map.Config, startPos, path, endAtAirlockOuter: false, out var waypoints);
            double remWork = Math.Max(0, PanelUnitState.DefaultRepairSeconds - targetPanel.RepairProgressSeconds);
            string jobId = $"sjob-{sim.NextEntityId++}";

            var phase = legLen <= 1e-6
                ? (remWork <= 0 ? SurfaceJobPhase.Completed : SurfaceJobPhase.Work)
                : SurfaceJobPhase.Travel;

            var job = new SurfaceJobState(
                jobId,
                SurfaceJobKind.Repair,
                targetPanel.Id,
                targetCoord,
                workCell,
                member.Id,
                phase,
                GridNav.DefaultWalkSpeed,
                remWork,
                -0.35,
                0,
                waypoints,
                legLen);

            if (phase == SurfaceJobPhase.Completed)
            {
                ResourceSimulation.CompleteSolarPanelRepair(sim, targetPanel.Id, State.ElapsedSeconds);
                job.AssignedCrewId = null;
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = waypoints[waypoints.Count - 1].X;
                member.SurfacePosZ = waypoints[waypoints.Count - 1].Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = null;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Idle;
            }
            else
            {
                sim.SurfaceJobs.Add(job);
                member.Location = CrewLocation.Outside;
                member.SurfacePosX = startPos.X;
                member.SurfacePosZ = startPos.Z;
                member.HasSurfacePosition = true;
                member.ActiveSurfaceJobId = job.JobId;
                member.ActiveTaskId = null;
                member.Status = CrewStatus.Working;
            }

            sim.StatusMessage = $"{member.Name} dispatched to repair solar panel {targetPanel.Id} at {targetCoord}.";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted(jobId);
        }

        private FacilityState FindFacility(string facilityId)
        {
            if (sim.Facilities == null || string.IsNullOrEmpty(facilityId))
                return null;
            for (int i = 0; i < sim.Facilities.Count; i++)
                if (sim.Facilities[i].FacilityId == facilityId)
                    return sim.Facilities[i];
            return null;
        }

        public CommandResult CanCancelBlueprint(string facilityId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Only available in Operations mode.");
            return FacilityPlacementValidator.CanCancelBlueprint(sim, facilityId);
        }

        public CommandResult CancelBlueprint(string facilityId)
        {
            var check = CanCancelBlueprint(facilityId);
            if (!check.Success)
                return check;

            var fac = FindFacility(facilityId);
            if (fac == null)
                return CommandResult.Rejected($"Unknown facility '{facilityId}'.");

            FacilityCatalog.TryGetSpec(fac.Kind, out var spec);
            ClearFacilityFootprintAndJobs(fac, spec, isBlueprintCancel: true);

            sim.MaterialsForfeitedTotal += fac.PaidMaterials;
            sim.BuildEnergyLostTotal += fac.EnergySpentTotal;
            sim.Facilities.Remove(fac);

            UtilityNetwork.RefreshTopology(sim);
            sim.StatusMessage = $"Cancelled {fac.Kind} blueprint {facilityId} (0 materials refunded).";
            sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] Blueprint cancelled: {facilityId} ({fac.Kind}), forfeited {fac.PaidMaterials} mat, lost {fac.EnergySpentTotal:0.#} energy");
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted("0");
        }

        public CommandResult DismantleFacility(string facilityId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Only available in Operations mode.");

            var check = FacilityPlacementValidator.CanDismantleOrModifyFacility(sim, facilityId);
            if (!check.Success)
                return check;

            var fac = FindFacility(facilityId);
            if (fac == null)
                return CommandResult.Rejected($"Unknown facility '{facilityId}'.");

            FacilityCatalog.TryGetSpec(fac.Kind, out var spec);
            ClearFacilityFootprintAndJobs(fac, spec, isBlueprintCancel: false);
            RemoveOwnedPanels(fac);

            FacilityPlacementValidator.PreviewDismantleRefund(
                fac, sim.Materials, config.MaterialCapacity, out int appliedRefund, out int rawRefund, out int cappedLoss);
            sim.Materials += appliedRefund;
            sim.MaterialsRefundedTotal += appliedRefund;
            sim.MaterialsForfeitedTotal += Math.Max(0, fac.PaidMaterials - appliedRefund);
            sim.BuildEnergyLostTotal += fac.EnergySpentTotal;
            sim.Facilities.Remove(fac);

            UtilityNetwork.RefreshTopology(sim);
            double lost = ColonyStorage.ReconcileStorageOverflow(sim, config.Resources);

            string capNote = cappedLoss > 0 ? $" ({cappedLoss} refund capped)" : "";
            string lossNote = lost > 0 ? $", -{lost:0.#} overflow battery lost" : "";
            sim.StatusMessage = $"Dismantled {fac.Kind} {facilityId} (+{appliedRefund} mat{capNote}{lossNote}).";
            sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] Dismantled {facilityId} ({fac.Kind}): +{appliedRefund}/{rawRefund} mat{capNote}{lossNote}");
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted(appliedRefund.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private void ClearFacilityFootprintAndJobs(FacilityState fac, FacilitySpec spec, bool isBlueprintCancel)
        {
            if (fac.FootprintCells != null)
            {
                for (int i = 0; i < fac.FootprintCells.Count; i++)
                {
                    GridCoord coord = fac.FootprintCells[i];
                    if (!sim.Map.TryGetCell(coord, out var cell))
                        continue;

                    if (spec != null && spec.UsesUtilityLayerOnly)
                    {
                        if (!isBlueprintCancel)
                            cell.HasUtilityLine = false;
                        if (cell.UtilityBlueprintId == fac.FacilityId)
                            cell.UtilityBlueprintId = null;
                    }
                    else
                    {
                        if (cell.FacilityId == fac.FacilityId)
                            cell.FacilityId = null;
                        if (spec != null && spec.BlocksPassability)
                            cell.IsPassable = cell.Terrain != ParcelTerrainKind.Ridge || coord.Z < 13;
                    }
                }
            }

            if (sim.SurfaceJobs != null)
            {
                for (int i = sim.SurfaceJobs.Count - 1; i >= 0; i--)
                {
                    var j = sim.SurfaceJobs[i];
                    if (j.Phase == SurfaceJobPhase.Completed)
                        continue;
                    bool matches = j.TargetId == fac.FacilityId ||
                        (fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(j.TargetId));
                    if (matches)
                        sim.SurfaceJobs.RemoveAt(i);
                }
            }
        }

        private void RemoveOwnedPanels(FacilityState fac)
        {
            if (sim.Resources?.Panels == null || fac == null)
                return;

            for (int i = sim.Resources.Panels.Count - 1; i >= 0; i--)
            {
                var p = sim.Resources.Panels[i];
                bool owns = p.FacilityId == fac.FacilityId ||
                    (fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(p.Id));
                if (owns)
                    sim.Resources.Panels.RemoveAt(i);
            }
        }

        internal CommandResult RemoveUtilityLine(string facilityId)
        {
            var fac = FindFacility(facilityId);
            if (fac == null || fac.Kind != FacilityKind.UtilityLine)
                return CommandResult.Rejected($"Unknown utility line '{facilityId}'.");
            return fac.LifecycleStatus == FacilityLifecycleStatus.Blueprint
                ? CancelBlueprint(facilityId)
                : DismantleFacility(facilityId);
        }

        public CommandResult SetWaterExtractorEnabled(bool enabled)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.Resources.WaterExtractorEnabled == enabled)
                return CommandResult.Accepted();

            sim.Resources.WaterExtractorEnabled = enabled;
            if (!enabled)
            {
                sim.Resources.LastWaterExtractionRate = 0;
                sim.Resources.LastLegacyWaterExtractionRate = 0;
                sim.Resources.LastSiteWaterExtractionRate = 0;
                if (sim.Facilities != null)
                {
                    for (int i = 0; i < sim.Facilities.Count; i++)
                    {
                        sim.Facilities[i].LastWaterExtractionRate = 0;
                        sim.Facilities[i].LastEnergyDrawRate = 0;
                    }
                }
            }
            sim.StatusMessage = enabled
                ? "Water extractor enabled (unlimited accessible reserve scenario)."
                : "Water extractor paused.";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult SetFacilityEnabled(string facilityId, bool enabled)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Only available in Operations mode.");
            var fac = FindFacility(facilityId);
            if (fac == null || fac.IsProtectedStarter)
                return CommandResult.Rejected($"Unknown or non-switchable facility '{facilityId}'.");
            if (!FacilityCatalog.TryGetSpec(fac.Kind, out var spec) || spec.WaterProductionRate <= 0)
                return CommandResult.Rejected($"Facility {fac.Kind} does not support per-site enable toggle.");
            if (fac.IsEnabled == enabled)
                return CommandResult.Accepted();

            fac.IsEnabled = enabled;
            if (!enabled)
            {
                fac.LastWaterExtractionRate = 0;
                fac.LastEnergyDrawRate = 0;
                double siteSum = 0;
                if (sim.Facilities != null)
                {
                    for (int i = 0; i < sim.Facilities.Count; i++)
                        siteSum += sim.Facilities[i].LastWaterExtractionRate;
                }
                sim.Resources.LastSiteWaterExtractionRate = siteSum;
                sim.Resources.LastWaterExtractionRate = sim.Resources.LastLegacyWaterExtractionRate + siteSum;
            }
            sim.StatusMessage = enabled
                ? $"{fac.Kind} ({facilityId}) site enabled."
                : $"{fac.Kind} ({facilityId}) site paused.";
            sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] Facility {facilityId} ({fac.Kind}) enabled={enabled}");
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult RepairWaterExtractorWithScience()
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (!sim.Resources.WaterExtractorFaulted)
                return CommandResult.Rejected("Water extractor is already operating normally; no repair needed.");

            const double repairCost = 10.0;
            const double tol = 1e-9;
            if (sim.Resources.ScienceBalance + tol < repairCost)
                return CommandResult.Rejected($"Insufficient science balance ({sim.Resources.ScienceBalance:0.#} / {repairCost:0.#} required).");

            sim.Resources.ScienceBalance = Math.Max(0.0, sim.Resources.ScienceBalance - repairCost);
            sim.Resources.WaterExtractorFaulted = false;
            if (sim.ChannelSchedules != null)
            {
                for (int i = 0; i < sim.ChannelSchedules.Count; i++)
                {
                    var ch = sim.ChannelSchedules[i];
                    if (ch.ChannelId == OperationsChannelId.WaterExtractor && ch.IsActive)
                    {
                        OperationsScheduler.RescheduleAfterResolution(ch, State.ElapsedSeconds);
                        sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] WaterExtractor repaired (10 science) -> next at {ch.NextTriggerAtSeconds:0.#}s");
                        break;
                    }
                }
            }
            sim.StatusMessage = "Water extractor repaired (10 science spent); extraction restored to 100%.";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }


        public CommandResult CancelTask(string crewId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");

            if (member.Status != CrewStatus.Working && member.Status != CrewStatus.Resting)
                return CommandResult.Rejected("Crew member has no active task or rest to cancel.");

            ReleaseTask(member);
            member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;

            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult Rest(string crewId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");

            if (member.Status == CrewStatus.Sleeping || sim.IsSleeping)
                return CommandResult.Rejected("Crew member is already sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is returning to shelter.");
            if (member.Location == CrewLocation.Outside)
                return CommandResult.Rejected("Crew member is outside; return to shelter to rest.");
            if (member.Status == CrewStatus.Resting)
                return CommandResult.Rejected("Crew member is already resting.");
            if (member.Stamina >= config.MaxStamina)
                return CommandResult.Rejected("Crew member already has full stamina.");

            ReleaseTask(member);
            member.Status = CrewStatus.Resting;

            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult ReturnToShelter(string crewId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (string.IsNullOrWhiteSpace(crewId))
                return CommandResult.Rejected("Crew ID is required.");

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");

            if (member.Status == CrewStatus.Sleeping)
                return CommandResult.Rejected("Crew member is already sleeping inside shelter.");
            if (member.Location == CrewLocation.Inside)
                return CommandResult.Rejected("Crew member is already inside shelter.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is already returning to shelter.");

            ReleaseTask(member);
            if (config.Mode == MissionMode.Operations && sim.Map != null && member.HasSurfacePosition)
            {
                GridCoord startCell = sim.Map.Config.TryWorldToCell(member.SurfacePosX, member.SurfacePosZ, out var sCell)
                    ? sCell
                    : ParcelMap.AirlockCorridorCoord;
                var startPos = new WorldPos2D(member.SurfacePosX, member.SurfacePosZ);

                if (!GridNav.TryFindPath(sim.Map, startCell, ParcelMap.AirlockCorridorCoord, out var retPath))
                    return CommandResult.Rejected("No reachable surface path back to the airlock.");

                double retLen = GridNav.BuildWorldWaypoints(sim.Map.Config, startPos, retPath, endAtAirlockOuter: true, out var retWaypoints);
                double airSec = config.ReturnToShelterSeconds > 0 ? GridNav.AirlockPassSeconds : 0;
                if (config.ReturnToShelterSeconds <= 0 && retLen <= 1e-6)
                {
                    member.Location = CrewLocation.Inside;
                    member.HasSurfacePosition = false;
                    member.SurfacePosX = 0;
                    member.SurfacePosZ = 0;
                    member.ReturnRemainingSeconds = 0;
                    member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Sheltered;
                }
                else
                {
                    if (sim.SurfaceJobs == null)
                        sim.SurfaceJobs = new List<SurfaceJobState>();
                    var retJob = new SurfaceJobState(
                        $"sjob-{sim.NextEntityId++}",
                        SurfaceJobKind.GenericWork,
                        "airlock-return",
                        ParcelMap.AirlockCorridorCoord,
                        ParcelMap.AirlockCorridorCoord,
                        member.Id,
                        SurfaceJobPhase.Return,
                        GridNav.DefaultWalkSpeed,
                        0,
                        config.ReturnStaminaRate,
                        0,
                        retWaypoints,
                        retLen,
                        0,
                        airSec);
                    sim.SurfaceJobs.Add(retJob);
                    member.ActiveSurfaceJobId = retJob.JobId;
                    member.Status = CrewStatus.Returning;
                    member.ReturnRemainingSeconds = retJob.GetRemainingLegSeconds();
                }

                PublishState(State.ElapsedSeconds);
                return CommandResult.Accepted();
            }

            if (config.ReturnToShelterSeconds <= 0)
            {
                member.Location = CrewLocation.Inside;
                member.ReturnRemainingSeconds = 0;
                member.HasSurfacePosition = false;
                member.SurfacePosX = 0;
                member.SurfacePosZ = 0;
                member.Status = member.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Sheltered;
            }
            else
            {
                member.Status = CrewStatus.Returning;
                member.ReturnRemainingSeconds = config.ReturnToShelterSeconds;
            }

            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult BeginSleep()
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (!State.IsNight)
                return CommandResult.Rejected("Sleep is only available during nighttime.");
            if (sim.IsSleeping)
                return CommandResult.Rejected("Crew is already sleeping.");

            for (int i = 0; i < sim.Crew.Count; i++)
            {
                if (sim.Crew[i].Location == CrewLocation.Outside || sim.Crew[i].Status == CrewStatus.Returning)
                    return CommandResult.Rejected("All crew members must be inside the shelter before sleeping.");
            }

            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var member = sim.Crew[i];
                ReleaseTask(member);
                member.Status = CrewStatus.Sleeping;
            }

            sim.IsSleeping = true;
            sim.SleepStartedThisSol = true;
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult RequestEarthAdvice()
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (sim.EarthAdviceStatus == EarthAdviceStatus.Pending)
                return CommandResult.Rejected("Earth advice request is already in transit; wait for the 45s compressed reply.");
            if (sim.EarthAdviceStatus == EarthAdviceStatus.Delivered)
                return CommandResult.Rejected("Earth advice can only be requested once per mission.");

            sim.EarthAdviceStatus = EarthAdviceStatus.Pending;
            sim.EarthAdviceRequestedAtSeconds = State.ElapsedSeconds;
            sim.EarthAdviceExpectedAtSeconds = State.ElapsedSeconds + config.CommsDelaySeconds;
            if (config.CommsDelaySeconds <= 0)
                ResourceSimulation.DeliverEarthAdvice(sim, config, State.ElapsedSeconds);
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult CancelSleep()
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (!sim.IsSleeping)
                return CommandResult.Rejected("Crew is not currently sleeping.");

            ResourceSimulation.WakeCrewFromSleep(sim);
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult AcknowledgeSolReport()
        {
            if (!sim.SolReportPauseRequested && sim.PendingSolReport == null)
                return CommandResult.Rejected("No pending sol report to dismiss.");

            sim.SolReportPauseRequested = false;
            sim.PendingSolReport = null;
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult AcknowledgeCriticalAlert()
        {
            if (!sim.CriticalAlertPauseRequested && string.IsNullOrEmpty(sim.ActiveAlertMessage))
                return CommandResult.Rejected("No active critical alert to dismiss.");

            sim.CriticalAlertPauseRequested = false;
            sim.ActiveAlertMessage = "";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult SetResearchEnabled(bool enabled)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");

            sim.Resources.ResearchEnabled = enabled;
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }



        public CommandResult AcceptEarthOrder(string orderId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            var order = FindEarthOrder(orderId);
            if (order == null)
                return CommandResult.Rejected($"Unknown Earth order '{orderId}'.");
            if (order.Status != EarthOrderStatus.Received)
                return CommandResult.Rejected($"Earth order '{order.Code}' is already {order.Status}.");

            order.Status = EarthOrderStatus.Accepted;
            sim.StatusMessage = $"Earth order accepted: {order.Code} ({order.Title}).";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult CancelEarthOrder(string orderId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            var order = FindEarthOrder(orderId);
            if (order == null)
                return CommandResult.Rejected($"Unknown Earth order '{orderId}'.");
            if (order.Status == EarthOrderStatus.Completed || order.Status == EarthOrderStatus.Cancelled)
                return CommandResult.Rejected($"Earth order '{order.Code}' is already {order.Status}.");

            if (order.AssignedCrewId != null)
            {
                var crewMember = FindCrew(order.AssignedCrewId);
                if (crewMember != null)
                {
                    if (crewMember.ActiveTaskId == order.OrderId)
                        crewMember.ActiveTaskId = null;
                    if (crewMember.Status == CrewStatus.Working)
                        crewMember.Status = crewMember.Stamina <= 0 ? CrewStatus.Exhausted : CrewStatus.Idle;
                }
                order.AssignedCrewId = null;
            }

            order.Status = EarthOrderStatus.Cancelled;
            order.CompletionSummary = $"{order.Code} cancelled (0 science, no refund).";
            sim.StatusMessage = $"Earth order cancelled: {order.Code}.";
            sim.OperationsLog?.Add($"[{State.ElapsedSeconds:0.#}s] Earth order cancelled: {order.Code} ({order.OrderId})");
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        public CommandResult AssignEarthOrder(string orderId, string crewId)
        {
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            var order = FindEarthOrder(orderId);
            if (order == null)
                return CommandResult.Rejected($"Unknown Earth order '{orderId}'.");
            if (order.Status != EarthOrderStatus.Accepted && order.Status != EarthOrderStatus.InProgress)
                return CommandResult.Rejected($"Earth order '{order.Code}' must be Accepted before assignment (currently {order.Status}).");

            // Only one Earth order can be InProgress at a time
            if (sim.EarthOrders != null)
            {
                for (int i = 0; i < sim.EarthOrders.Count; i++)
                {
                    var other = sim.EarthOrders[i];
                    if (other.OrderId != order.OrderId && other.Status == EarthOrderStatus.InProgress)
                        return CommandResult.Rejected($"Another Earth order ({other.Code}) is already in progress.");
                }
            }

            var member = FindCrew(crewId);
            if (member == null)
                return CommandResult.Rejected($"Unknown crew member '{crewId}'.");
            if (member.Status == CrewStatus.Sleeping || sim.IsSleeping)
                return CommandResult.Rejected("Cannot assign Earth order while sleeping.");
            if (member.Status == CrewStatus.Returning)
                return CommandResult.Rejected("Crew member is returning to shelter.");
            if (member.Status == CrewStatus.Working || member.ActiveTaskId != null || member.ActiveSurfaceJobId != null)
                return CommandResult.Rejected("Crew member is already assigned to another task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest first.");
            if (order.AssignedCrewId != null && order.AssignedCrewId != member.Id)
                return CommandResult.Rejected("Earth order is already assigned to another crew member.");

            var spec = EarthOrderBagState.GetStepSpec(order.Kind, order.CurrentStepIndex);
            if (spec.Location == CrewLocation.Inside && member.Location == CrewLocation.Outside)
                return CommandResult.Rejected("Crew member is outside and must return to shelter before taking an indoor order step.");
            foreach (var task in sim.Tasks)
            {
                string station = task.Kind == TaskKind.OxygenProducerRepair ? "system-maintenance" : task.Id;
                if (order.StationTaskId != null && order.StationTaskId == station && task.AssignedCrewId != null && task.AssignedCrewId != member.Id)
                    return CommandResult.Rejected("Station is occupied by another crew member. Release it before assigning this order.");
            }

            if (spec.UpfrontWaterCost > 0 && !order.UpfrontWaterPaid)
            {
                if (sim.Resources.Water < spec.UpfrontWaterCost)
                    return CommandResult.Rejected($"Insufficient water for {order.Code}: requires {spec.UpfrontWaterCost:F1} units.");
                sim.Resources.Water -= spec.UpfrontWaterCost;
                order.UpfrontWaterPaid = true;
            }

            if (spec.Location == CrewLocation.Outside)
                member.Location = CrewLocation.Outside;

            order.StepRequiredSeconds = spec.DurationSeconds;
            order.Status = EarthOrderStatus.InProgress;
            order.AssignedCrewId = member.Id;
            member.Status = CrewStatus.Working;
            member.ActiveTaskId = order.OrderId;
            sim.StatusMessage = $"{member.Name} assigned to {order.Code} (Step {order.CurrentStepIndex + 1}).";
            PublishState(State.ElapsedSeconds);
            return CommandResult.Accepted();
        }

        private EarthOrderState FindEarthOrder(string orderId)
        {
            if (sim.EarthOrders == null || string.IsNullOrWhiteSpace(orderId)) return null;
            for (int i = 0; i < sim.EarthOrders.Count; i++)
                if (sim.EarthOrders[i].OrderId == orderId)
                    return sim.EarthOrders[i];
            return null;
        }

        private CrewState FindCrew(string crewId)
        {
            for (int i = 0; i < sim.Crew.Count; i++)
                if (sim.Crew[i].Id == crewId)
                    return sim.Crew[i];
            return null;
        }

        private TaskState FindTask(string taskId)
        {
            for (int i = 0; i < sim.Tasks.Count; i++)
                if (sim.Tasks[i].Id == taskId)
                    return sim.Tasks[i];
            return null;
        }

        private void ReleaseTask(CrewState member)
        {
            if (member.ActiveTaskId != null)
            {
                var task = FindTask(member.ActiveTaskId);
                if (task != null && task.AssignedCrewId == member.Id)
                    task.AssignedCrewId = null;
                var order = FindEarthOrder(member.ActiveTaskId);
                if (order != null && order.AssignedCrewId == member.Id)
                    order.AssignedCrewId = null;
                member.ActiveTaskId = null;
            }
            if (member.ActiveSurfaceJobId != null)
            {
                if (sim.SurfaceJobs != null)
                {
                    for (int i = 0; i < sim.SurfaceJobs.Count; i++)
                    {
                        var job = sim.SurfaceJobs[i];
                        if (job.JobId == member.ActiveSurfaceJobId && job.AssignedCrewId == member.Id)
                        {
                            job.AssignedCrewId = null;
                            break;
                        }
                    }
                }
                member.ActiveSurfaceJobId = null;
            }
        }

        public string SaveToJson()
        {
            return OperationsSaveCodec.Serialize(config, State.ElapsedSeconds, timeCompensation, sim);
        }

        public CommandResult SaveToFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return CommandResult.Rejected("Save path is empty.");
            if (config.Mode != MissionMode.Operations)
                return CommandResult.Rejected("Save is only supported in Operations mode.");
            try
            {
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    System.IO.Directory.CreateDirectory(dir);
                string tmpPath = path + ".tmp";
                string json = SaveToJson();
                System.IO.File.WriteAllText(tmpPath, json, System.Text.Encoding.UTF8);
                if (System.IO.File.Exists(path))
                    System.IO.File.Replace(tmpPath, path, null);
                else
                    System.IO.File.Move(tmpPath, path);
                return CommandResult.Accepted();
            }
            catch (Exception ex)
            {
                return CommandResult.Rejected($"Failed to save file: {ex.Message}");
            }
        }

        public static bool TryLoadFromJson(string json, out MissionSimulation simulation, out string error)
        {
            return OperationsSaveCodec.TryDeserialize(json, out simulation, out error);
        }

        public static bool TryLoadFromFile(string path, out MissionSimulation simulation, out string error)
        {
            simulation = null;
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                error = "Save file not found.";
                return false;
            }
            try
            {
                string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
                return TryLoadFromJson(json, out simulation, out error);
            }
            catch (Exception ex)
            {
                error = $"Failed to read save file: {ex.Message}";
                return false;
            }
        }

        internal void ApplyLoadedSnapshot(Dictionary<string, object> root, double elapsed, double loadedTimeCompensation)
        {
            OperationsSaveCodec.ApplyToState(root, sim, config, elapsed);
            for (int i = 0; i < sim.Tasks.Count; i++)
                sim.Tasks[i].AssignedCrewId = null;
            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var c = sim.Crew[i];
                if (c.ActiveTaskId != null)
                {
                    var t = FindTask(c.ActiveTaskId);
                    if (t != null) t.AssignedCrewId = c.Id;
                }
            }
            timeCompensation = loadedTimeCompensation;
            PublishState(elapsed);
        }
    }
}
