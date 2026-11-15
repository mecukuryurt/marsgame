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
        public MissionMode CurrentMode => State != null ? State.Mode : MissionMode.Operations;
        public double ActiveResearchGoal { get; private set; } = 0;

        public event Action<MissionState> StateChanged;
        public event Action<CommandResult> CommandRejected;

        private void Awake()
        {
            if (Stepper == null)
                InitializeOperationsMission();
        }

        public void InitializeMission(MissionConfig config, MissionScenario scenario)
        {
            ActiveResearchGoal = config != null ? config.ResearchGoal : 120;
            IsDemoMode = config != null && config.Mode == MissionMode.Demo;
            var simulation = new MissionSimulation(config, scenario);
            Stepper = new SimulationStepper(simulation);
            StateChanged?.Invoke(State);
        }

        public void InitializeOperationsMission()
        {
            IsDemoMode = false;
            var config = MissionConfig.CreateOperationsDefault();
            InitializeMission(config, new MissionScenario(events: System.Array.Empty<EventState>()));
        }

        public void CycleMode()
        {
            switch (CurrentMode)
            {
                case MissionMode.Operations:
                    InitializeDemoMission();
                    break;
                case MissionMode.Demo:
                    InitializeDefaultMission();
                    break;
                default:
                    InitializeOperationsMission();
                    break;
            }
        }

        public void InitializeDefaultMission()
        {
            IsDemoMode = false;
            var resCfg = new ResourceConfig(
                initialBattery: 50,
                researchEnergyRate: 0.09,
                researchProgressRate: 0.15,
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
                commsDelaySeconds: 10,
                mode: MissionMode.Demo);

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
        public CommandResult ReplaceTask(string crewId, string taskId) => Execute(Simulation.ReplaceTask(crewId, taskId));
        public CommandResult CancelTask(string crewId) => Execute(Simulation.CancelTask(crewId));
        public CommandResult Rest(string crewId) => Execute(Simulation.Rest(crewId));
        public CommandResult ReturnToShelter(string crewId) => Execute(Simulation.ReturnToShelter(crewId));
        public CommandResult BeginSleep() => Execute(Simulation.BeginSleep());
        public CommandResult CancelSleep() => Execute(Simulation.CancelSleep());
        public CommandResult SetResearchEnabled(bool enabled) => Execute(Simulation.SetResearchEnabled(enabled));
        public CommandResult AcknowledgeSolReport() => Execute(Simulation.AcknowledgeSolReport());
        public CommandResult AcknowledgeCriticalAlert() => Execute(Simulation.AcknowledgeCriticalAlert());
        public CommandResult RequestEarthAdvice() => Execute(Simulation.RequestEarthAdvice());
        public CommandResult AcceptEarthOrder(string id) => Execute(Simulation.AcceptEarthOrder(id));
        public CommandResult CancelEarthOrder(string id) => Execute(Simulation.CancelEarthOrder(id));
        public CommandResult AssignEarthOrder(string id, string crewId) => Execute(Simulation.AssignEarthOrder(id, crewId));
        public CommandResult RepairWaterExtractor() => Execute(Simulation.RepairWaterExtractorWithScience());
        public CommandResult SetWaterExtractorEnabled(bool enabled) => Execute(Simulation.SetWaterExtractorEnabled(enabled));
        public CommandResult SetFacilityEnabled(string facilityId, bool enabled) => Execute(Simulation.SetFacilityEnabled(facilityId, enabled));
        public CommandResult ClaimParcel(GridCoord coord) => Execute(Simulation.ClaimParcel(coord));
        public CommandResult AssignSurfaceWork(
            string crewId,
            GridCoord targetCoord,
            double workRequiredSeconds = 10.0,
            SurfaceJobKind kind = SurfaceJobKind.GenericWork,
            string targetId = null,
            bool allowTargetCell = false,
            double workStaminaRate = -0.18,
            bool replace = false)
            => Execute(Simulation.AssignSurfaceWork(crewId, targetCoord, workRequiredSeconds, kind, targetId, allowTargetCell, workStaminaRate, replace));

        public CommandResult ValidateBlueprintPlacement(
            FacilityKind kind,
            GridCoord origin,
            FacilityRotation rotation,
            out System.Collections.Generic.List<GridCoord> footprint)
            => Simulation.ValidateBlueprintPlacement(kind, origin, rotation, out footprint);

        public CommandResult PlaceBlueprint(
            FacilityKind kind,
            GridCoord origin,
            FacilityRotation rotation = FacilityRotation.Deg0)
            => Execute(Simulation.PlaceBlueprint(kind, origin, rotation));

        public CommandResult AssignBuildBlueprint(
            string crewId,
            string facilityId,
            bool replace = false)
            => Execute(Simulation.AssignBuildBlueprint(crewId, facilityId, replace));

        public CommandResult AssignRepairPanel(
            string crewId,
            string panelOrFacilityId = null,
            bool replace = false)
            => Execute(Simulation.AssignRepairPanel(crewId, panelOrFacilityId, replace));

        public CommandResult CanCancelBlueprint(string facilityId)
            => Simulation.CanCancelBlueprint(facilityId);

        public CommandResult CancelBlueprint(string facilityId)
            => Execute(Simulation.CancelBlueprint(facilityId));

        public CommandResult CanDismantleFacility(string facilityId)
            => Simulation.CanDismantleFacility(facilityId);

        public CommandResult DismantleFacility(string facilityId)
            => Execute(Simulation.DismantleFacility(facilityId));

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

        public CommandResult SaveOperationsToFile(string path)
        {
            if (Simulation == null)
                return CommandResult.Rejected("No active simulation.");
            var res = Simulation.SaveToFile(path);
            if (!res.Success) CommandRejected?.Invoke(res);
            return res;
        }

        public CommandResult LoadOperationsFromFile(string path)
        {
            if (!MissionSimulation.TryLoadFromFile(path, out var loaded, out string error))
            {
                var rej = CommandResult.Rejected(error);
                CommandRejected?.Invoke(rej);
                return rej;
            }
            ActiveResearchGoal = 0;
            IsDemoMode = false;
            Stepper = new SimulationStepper(loaded);
            Stepper.SetUserPaused(true);
            StateChanged?.Invoke(State);
            return CommandResult.Accepted();
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
