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
        MealPreparation
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
        public double MaxCrewRiskScore { get; }
        public double OxygenOutageFailureSeconds { get; }
        public double SleepStaminaRate { get; }
        public double SleepTimeScale { get; }
        public double LowOxygenWarningThreshold { get; }
        public double UnpreppedSleepFactor { get; }
        public double CommsDelaySeconds { get; }
        public int InitialFoodRations { get; }

        public double MissionDurationSeconds => TargetSols > 0 ? SolSeconds * TargetSols : double.PositiveInfinity;

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
            int initialFoodRations = 2)
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
            MaxCrewRiskScore = maxCrewRiskScore;
            OxygenOutageFailureSeconds = oxygenOutageFailureSeconds;
            SleepStaminaRate = sleepStaminaRate;
            SleepTimeScale = sleepTimeScale;
            LowOxygenWarningThreshold = lowOxygenWarningThreshold;
            UnpreppedSleepFactor = unpreppedSleepFactor;
            CommsDelaySeconds = commsDelaySeconds;
            InitialFoodRations = initialFoodRations;
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
            string earthAdviceMessage)
        {
            ElapsedSeconds = elapsed;
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
        }
    }

    public sealed class CommandResult
    {
        public bool Success { get; }
        public string Reason { get; }
        private CommandResult(bool success, string reason) { Success = success; Reason = reason; }
        internal static CommandResult Unsupported() => new CommandResult(false, "This command is not implemented yet.");
        internal static CommandResult Rejected(string reason) => new CommandResult(false, reason);
        internal static CommandResult Accepted(string reason = "") => new CommandResult(true, reason);
    }

    public sealed class MissionSimulation
    {
        private readonly MissionConfig config;
        private readonly MissionScenario scenario;
        private SimulationMutableState sim;
        private double timeCompensation;

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
                EarthAdviceMessage = ""
            };

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
                sim.EarthAdviceMessage);
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

        public CommandResult AssignTask(string crewId, string taskId)
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
            if (member.Status == CrewStatus.Working || member.ActiveTaskId != null)
                return CommandResult.Rejected("Crew member is already assigned to a task.");
            if (member.Status == CrewStatus.Exhausted || member.Stamina <= 0)
                return CommandResult.Rejected("Crew member is exhausted and must rest before working.");
            if (task.IsCompleted)
                return CommandResult.Rejected("This one-time task is already completed.");
            if (task.AssignedCrewId != null)
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

            if (task.Location == CrewLocation.Outside)
                member.Location = CrewLocation.Outside;

            member.Status = CrewStatus.Working;
            member.ActiveTaskId = task.Id;
            task.AssignedCrewId = member.Id;

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
            if (config.ReturnToShelterSeconds <= 0)
            {
                member.Location = CrewLocation.Inside;
                member.ReturnRemainingSeconds = 0;
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
                member.ActiveTaskId = null;
            }
        }
    }
}
