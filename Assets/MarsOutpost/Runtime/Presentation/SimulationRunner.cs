using System;
using UnityEngine;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Single scene owner of MissionSimulation and SimulationStepper.
    /// </summary>
    public sealed class SimulationRunner : MonoBehaviour
    {
        public SimulationStepper Stepper { get; private set; }
        public MissionSimulation Simulation => Stepper?.Simulation;
        public MissionState State => Simulation?.State;
        public bool IsDemoMode { get; private set; }
        public double ActiveResearchGoal { get; private set; } = 120;

        public event Action<MissionState> StateChanged;
        public event Action<CommandResult> CommandRejected;

        private void Awake()
        {
            if (Stepper == null)
                InitializeDefaultMission();
        }

        public void InitializeMission(MissionConfig config, MissionScenario scenario)
        {
            ActiveResearchGoal = config != null ? config.ResearchGoal : 120;
            var simulation = new MissionSimulation(config, scenario);
            Stepper = new SimulationStepper(simulation);
            StateChanged?.Invoke(State);
        }

        public void InitializeDefaultMission()
        {
            IsDemoMode = false;
            var resCfg = new ResourceConfig(
                initialPanelFouling: 0.20,
                panelMaintenanceReduction: 0.20,
                initialSystemWear: 0.15,
                systemMaintenanceReduction: 0.15);
            var config = new MissionConfig(resources: resCfg);
            InitializeMission(config, new MissionScenario());
        }

        public void InitializeDemoMission()
        {
            IsDemoMode = true;
            var resCfg = new ResourceConfig(
                solarRate: 0.45,
                oxygenProductionRate: 0.16,
                recyclingRate: 0.08,
                waterUsePerCrew: 0.015,
                oxygenUsePerCrew: 0.035,
                researchEnergyRate: 0.08,
                researchProgressRate: 1.0,
                initialPanelFouling: 0.25,
                panelMaintenanceReduction: 0.25,
                initialSystemWear: 0.20,
                systemMaintenanceReduction: 0.20);
            var config = new MissionConfig(
                solSeconds: 45,
                daySeconds: 30,
                resources: resCfg,
                returnToShelterSeconds: 4,
                researchGoal: 24,
                targetSols: 2,
                insideRadiationRiskRate: 0.15,
                outsideRadiationRiskRate: 1.20,
                oxygenOutageFailureSeconds: 8,
                sleepStaminaRate: 1.20,
                sleepTimeScale: 4.0,
                commsDelaySeconds: 10);

            var tasks = new[]
            {
                new TaskState("panel-maintenance", "Exterior Panel Maintenance", TaskKind.PanelMaintenance, CrewLocation.Outside, false, 10, -0.35),
                new TaskState("system-maintenance", "Interior System Maintenance", TaskKind.SystemMaintenance, CrewLocation.Inside, false, 8, -0.20),
                new TaskState("meal-prep", "Reserve Meal Preparation", TaskKind.MealPreparation, CrewLocation.Inside, false, 6, -0.10),
                new TaskState("research", "Research Station", TaskKind.Research, CrewLocation.Inside, true, 0, -0.15)
            };

            var events = new[]
            {
                new EventState(
                    "rad-1",
                    "Solar Particle Event #1",
                    EventKind.Radiation,
                    8, 20, 26, 1.0,
                    "2024-05-09T13:59:00-SEP-001",
                    "Compressed 90s demo window using NASA DONKI SEP sequence."),
                new EventState(
                    "dust-1",
                    "Regional Dust Storm",
                    EventKind.Dust,
                    48, 54, 68, 0.50,
                    "2018-MARS-DUST-CONTEXT",
                    "Compressed 90s demo window using 2018 Mars dust attenuation context.")
            };

            InitializeMission(config, new MissionScenario(tasks: tasks, events: events));
        }

        private void Update()
        {
            if (Stepper == null) return;
            int steps = Stepper.AdvanceFrame(Time.unscaledDeltaTime);
            if (steps > 0)
                StateChanged?.Invoke(State);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (Stepper == null) return;
            Stepper.NotifyApplicationFocus(hasFocus);
            StateChanged?.Invoke(State);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (Stepper == null || !pauseStatus) return;
            Stepper.NotifyApplicationFocus(false);
            StateChanged?.Invoke(State);
        }

        public CommandResult AssignTask(string crewId, string taskId) => Execute(Simulation.AssignTask(crewId, taskId));
        public CommandResult CancelTask(string crewId) => Execute(Simulation.CancelTask(crewId));
        public CommandResult Rest(string crewId) => Execute(Simulation.Rest(crewId));
        public CommandResult ReturnToShelter(string crewId) => Execute(Simulation.ReturnToShelter(crewId));
        public CommandResult BeginSleep() => Execute(Simulation.BeginSleep());
        public CommandResult CancelSleep() => Execute(Simulation.CancelSleep());
        public CommandResult SetResearchEnabled(bool enabled) => Execute(Simulation.SetResearchEnabled(enabled));
        public CommandResult AcknowledgeSolReport() => Execute(Simulation.AcknowledgeSolReport());
        public CommandResult AcknowledgeCriticalAlert() => Execute(Simulation.AcknowledgeCriticalAlert());
        public CommandResult RequestEarthAdvice() => Execute(Simulation.RequestEarthAdvice());

        public void TogglePause()
        {
            if (Stepper == null) return;
            Stepper.ToggleUserPause();
            StateChanged?.Invoke(State);
        }

        public void SetPaused(bool paused)
        {
            if (Stepper == null) return;
            Stepper.SetUserPaused(paused);
            StateChanged?.Invoke(State);
        }

        public void RestartMission()
        {
            if (Stepper == null) return;
            Stepper.Restart();
            StateChanged?.Invoke(State);
        }

        private CommandResult Execute(CommandResult result)
        {
            StateChanged?.Invoke(State);
            if (!result.Success)
                CommandRejected?.Invoke(result);
            return result;
        }
    }
}
