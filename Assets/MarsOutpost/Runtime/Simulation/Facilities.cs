using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarsOutpost
{
    public enum FacilityKind
    {
        UtilityLine = 0,
        SolarPanel = 1,
        Battery = 2,
        WaterExtractor = 3,
        RegolithFurnace = 4,
        WaterTank = 5,
        ExtraShelter = 6,
        RoverDock = 7,
        StarterCoreHabitat = 100,
        StarterAirlockCorridor = 101,
        StarterSolarArray = 102,
        StarterWaterExtractor = 103
    }

    public enum FacilityRotation
    {
        Deg0 = 0,
        Deg90 = 1,
        Deg180 = 2,
        Deg270 = 3
    }

    public enum FacilityLifecycleStatus
    {
        Blueprint = 0,
        Commissioned = 1
    }

    public sealed class FacilitySpec
    {
        public FacilityKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaterialCost { get; }
        public double NetWorkSeconds { get; }
        public double WorkEnergyCost { get; }
        public bool UsesUtilityLayerOnly { get; }
        public bool BlocksPassability { get; }
        public ParcelResourceKind RequiredResource { get; }
        public double BatteryCapacityDelta { get; }
        public double WaterCapacityDelta { get; }
        public double WaterProductionRate { get; }
        public double WaterEnergyPerUnit { get; }
        public bool IsPlayerBuildable { get; }
        public bool UnlockedInPlayerCatalog { get; }

        public FacilitySpec(
            FacilityKind kind,
            int width,
            int height,
            int materialCost,
            double netWorkSeconds,
            double workEnergyCost,
            bool usesUtilityLayerOnly,
            bool blocksPassability,
            ParcelResourceKind requiredResource = ParcelResourceKind.None,
            double batteryCapacityDelta = 0,
            double waterCapacityDelta = 0,
            double waterProductionRate = 0,
            double waterEnergyPerUnit = 0,
            bool isPlayerBuildable = true,
            bool unlockedInPlayerCatalog = false)
        {
            Kind = kind;
            Width = width;
            Height = height;
            MaterialCost = materialCost;
            NetWorkSeconds = netWorkSeconds;
            WorkEnergyCost = workEnergyCost;
            UsesUtilityLayerOnly = usesUtilityLayerOnly;
            BlocksPassability = blocksPassability;
            RequiredResource = requiredResource;
            BatteryCapacityDelta = batteryCapacityDelta;
            WaterCapacityDelta = waterCapacityDelta;
            WaterProductionRate = waterProductionRate;
            WaterEnergyPerUnit = waterEnergyPerUnit;
            IsPlayerBuildable = isPlayerBuildable;
            UnlockedInPlayerCatalog = unlockedInPlayerCatalog;
        }
    }

    public static class FacilityCatalog
    {
        public const int MaxPlayerNonUtilityFacilities = 48;
        public const int MaxTotalPanels = 12;
        public const int MaxNewExtractors = 4;
        public const int MaxExtraShelters = 1;
        public const int MaxRoverDocks = 1;

        private static readonly Dictionary<FacilityKind, FacilitySpec> Specs = new Dictionary<FacilityKind, FacilitySpec>
        {
            { FacilityKind.UtilityLine, new FacilitySpec(FacilityKind.UtilityLine, 1, 1, 1, 5.0, 0.0, usesUtilityLayerOnly: true, blocksPassability: false, unlockedInPlayerCatalog: true) },
            { FacilityKind.SolarPanel, new FacilitySpec(FacilityKind.SolarPanel, 1, 1, 12, 45.0, 2.0, usesUtilityLayerOnly: false, blocksPassability: true, unlockedInPlayerCatalog: true) },
            { FacilityKind.Battery, new FacilitySpec(FacilityKind.Battery, 1, 1, 16, 45.0, 2.0, usesUtilityLayerOnly: false, blocksPassability: true, batteryCapacityDelta: 25.0, unlockedInPlayerCatalog: true) },
            { FacilityKind.WaterExtractor, new FacilitySpec(FacilityKind.WaterExtractor, 1, 1, 18, 60.0, 3.0, usesUtilityLayerOnly: false, blocksPassability: true, requiredResource: ParcelResourceKind.Ice, waterProductionRate: 0.012, waterEnergyPerUnit: 1.5, unlockedInPlayerCatalog: true) },
            { FacilityKind.RegolithFurnace, new FacilitySpec(FacilityKind.RegolithFurnace, 1, 1, 22, 75.0, 4.0, usesUtilityLayerOnly: false, blocksPassability: true, requiredResource: ParcelResourceKind.HydratedRegolith, waterProductionRate: 0.006, waterEnergyPerUnit: 4.0, unlockedInPlayerCatalog: false) },
            { FacilityKind.WaterTank, new FacilitySpec(FacilityKind.WaterTank, 1, 1, 10, 40.0, 1.0, usesUtilityLayerOnly: false, blocksPassability: true, waterCapacityDelta: 50.0, unlockedInPlayerCatalog: false) },
            { FacilityKind.ExtraShelter, new FacilitySpec(FacilityKind.ExtraShelter, 2, 2, 24, 90.0, 5.0, usesUtilityLayerOnly: false, blocksPassability: true, unlockedInPlayerCatalog: false) },
            { FacilityKind.RoverDock, new FacilitySpec(FacilityKind.RoverDock, 2, 1, 20, 75.0, 4.0, usesUtilityLayerOnly: false, blocksPassability: true, unlockedInPlayerCatalog: false) },
            { FacilityKind.StarterCoreHabitat, new FacilitySpec(FacilityKind.StarterCoreHabitat, 1, 1, 0, 0.0, 0.0, usesUtilityLayerOnly: false, blocksPassability: true, isPlayerBuildable: false, unlockedInPlayerCatalog: false) },
            { FacilityKind.StarterAirlockCorridor, new FacilitySpec(FacilityKind.StarterAirlockCorridor, 1, 1, 0, 0.0, 0.0, usesUtilityLayerOnly: false, blocksPassability: false, isPlayerBuildable: false, unlockedInPlayerCatalog: false) },
            { FacilityKind.StarterSolarArray, new FacilitySpec(FacilityKind.StarterSolarArray, 1, 1, 0, 0.0, 0.0, usesUtilityLayerOnly: false, blocksPassability: true, isPlayerBuildable: false, unlockedInPlayerCatalog: false) },
            { FacilityKind.StarterWaterExtractor, new FacilitySpec(FacilityKind.StarterWaterExtractor, 1, 1, 0, 0.0, 0.0, usesUtilityLayerOnly: false, blocksPassability: true, isPlayerBuildable: false, unlockedInPlayerCatalog: false) }
        };

        public static bool TryGetSpec(FacilityKind kind, out FacilitySpec spec)
            => Specs.TryGetValue(kind, out spec);

        public static FacilitySpec GetSpec(FacilityKind kind)
        {
            if (!Specs.TryGetValue(kind, out var spec))
                throw new ArgumentOutOfRangeException(nameof(kind), $"Unknown facility kind: {kind}");
            return spec;
        }

        public static IReadOnlyList<FacilitySpec> GetPlayerCatalogSpecs()
        {
            var list = new List<FacilitySpec>();
            foreach (var kv in Specs)
            {
                if (kv.Value.IsPlayerBuildable && kv.Value.UnlockedInPlayerCatalog)
                    list.Add(kv.Value);
            }
            return list;
        }

        public static void GetRotatedDimensions(FacilitySpec spec, FacilityRotation rotation, out int width, out int height)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (rotation == FacilityRotation.Deg90 || rotation == FacilityRotation.Deg270)
            {
                width = spec.Height;
                height = spec.Width;
            }
            else
            {
                width = spec.Width;
                height = spec.Height;
            }
        }

        public static List<GridCoord> GetFootprintCells(FacilityKind kind, GridCoord origin, FacilityRotation rotation)
        {
            var spec = GetSpec(kind);
            GetRotatedDimensions(spec, rotation, out int w, out int h);
            var cells = new List<GridCoord>(w * h);
            for (int dz = 0; dz < h; dz++)
            {
                for (int dx = 0; dx < w; dx++)
                    cells.Add(new GridCoord(origin.X + dx, origin.Z + dz));
            }
            return cells;
        }
    }

    public sealed class FacilityState
    {
        public string FacilityId { get; }
        public FacilityKind Kind { get; }
        public GridCoord Origin { get; }
        public FacilityRotation Rotation { get; }
        public IReadOnlyList<GridCoord> FootprintCells { get; }
        public FacilityLifecycleStatus LifecycleStatus { get; internal set; }
        public double WorkProgressSeconds { get; internal set; }
        public double WorkRequiredSeconds { get; }
        public double EnergySpentTotal { get; internal set; }
        public double EnergyRequiredTotal { get; }
        public int PaidMaterials { get; }
        public bool IsProtectedStarter { get; }
        public bool IsConnectedToCore { get; internal set; }
        public bool IsProductionApplied { get; internal set; }
        public List<string> LinkedPanelIds { get; internal set; }
        public bool IsEnabled { get; internal set; }
        public double LastWaterExtractionRate { get; internal set; }
        public double LastEnergyDrawRate { get; internal set; }

        public FacilityState(
            string facilityId,
            FacilityKind kind,
            GridCoord origin,
            FacilityRotation rotation,
            IList<GridCoord> footprintCells,
            FacilityLifecycleStatus lifecycleStatus,
            double workProgressSeconds,
            double workRequiredSeconds,
            double energySpentTotal,
            double energyRequiredTotal,
            int paidMaterials,
            bool isProtectedStarter,
            bool isConnectedToCore,
            bool isProductionApplied,
            IList<string> linkedPanelIds = null,
            bool isEnabled = true,
            double lastWaterExtractionRate = 0,
            double lastEnergyDrawRate = 0)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
                throw new ArgumentException("FacilityId is required.", nameof(facilityId));

            FacilityId = facilityId;
            Kind = kind;
            Origin = origin;
            Rotation = rotation;

            var copied = new GridCoord[footprintCells != null ? footprintCells.Count : 0];
            if (footprintCells != null)
            {
                for (int i = 0; i < footprintCells.Count; i++)
                    copied[i] = footprintCells[i];
            }
            FootprintCells = Array.AsReadOnly(copied);

            LifecycleStatus = lifecycleStatus;
            WorkProgressSeconds = Math.Max(0, workProgressSeconds);
            WorkRequiredSeconds = Math.Max(0, workRequiredSeconds);
            EnergySpentTotal = Math.Max(0, energySpentTotal);
            EnergyRequiredTotal = Math.Max(0, energyRequiredTotal);
            PaidMaterials = Math.Max(0, paidMaterials);
            IsProtectedStarter = isProtectedStarter;
            IsConnectedToCore = isConnectedToCore;
            IsProductionApplied = isProductionApplied;
            LinkedPanelIds = linkedPanelIds != null ? new List<string>(linkedPanelIds) : new List<string>();
            IsEnabled = isEnabled;
            LastWaterExtractionRate = Math.Max(0, lastWaterExtractionRate);
            LastEnergyDrawRate = Math.Max(0, lastEnergyDrawRate);
        }

        public FacilityState Clone()
        {
            return new FacilityState(
                FacilityId,
                Kind,
                Origin,
                Rotation,
                new List<GridCoord>(FootprintCells),
                LifecycleStatus,
                WorkProgressSeconds,
                WorkRequiredSeconds,
                EnergySpentTotal,
                EnergyRequiredTotal,
                PaidMaterials,
                IsProtectedStarter,
                IsConnectedToCore,
                IsProductionApplied,
                LinkedPanelIds,
                IsEnabled,
                LastWaterExtractionRate,
                LastEnergyDrawRate);
        }

        public static List<FacilityState> CreateStarterFacilities()
        {
            return new List<FacilityState>
            {
                new FacilityState(
                    ParcelMap.CoreHabitatSpec.FacilityId,
                    FacilityKind.StarterCoreHabitat,
                    ParcelMap.CoreHabitatSpec.Footprint[0],
                    FacilityRotation.Deg0,
                    new List<GridCoord>(ParcelMap.CoreHabitatSpec.Footprint),
                    FacilityLifecycleStatus.Commissioned,
                    0, 0, 0, 0, 0,
                    isProtectedStarter: true,
                    isConnectedToCore: true,
                    isProductionApplied: true),
                new FacilityState(
                    ParcelMap.StarterSolarSpec.FacilityId,
                    FacilityKind.StarterSolarArray,
                    ParcelMap.StarterSolarSpec.Footprint[0],
                    FacilityRotation.Deg0,
                    new List<GridCoord>(ParcelMap.StarterSolarSpec.Footprint),
                    FacilityLifecycleStatus.Commissioned,
                    0, 0, 0, 0, 0,
                    isProtectedStarter: true,
                    isConnectedToCore: true,
                    isProductionApplied: true,
                    ParcelMap.StarterSolarSpec.OwnedPanelIds != null ? new List<string>(ParcelMap.StarterSolarSpec.OwnedPanelIds) : null),
                new FacilityState(
                    ParcelMap.LegacyExtractorSpec.FacilityId,
                    FacilityKind.StarterWaterExtractor,
                    ParcelMap.LegacyExtractorSpec.Footprint[0],
                    FacilityRotation.Deg0,
                    new List<GridCoord>(ParcelMap.LegacyExtractorSpec.Footprint),
                    FacilityLifecycleStatus.Commissioned,
                    0, 0, 0, 0, 0,
                    isProtectedStarter: true,
                    isConnectedToCore: true,
                    isProductionApplied: true)
            };
        }
    }

    public static class FacilityPlacementValidator
    {
        internal static CommandResult ValidatePlacement(
            SimulationMutableState sim,
            MissionConfig config,
            FacilityKind kind,
            GridCoord origin,
            FacilityRotation rotation,
            out List<GridCoord> footprint)
        {
            footprint = null;
            if (sim == null || config == null)
                return CommandResult.Rejected("No active simulation.");
            if (sim.Outcome != MissionOutcome.InProgress)
                return CommandResult.Rejected("Mission has ended; restart to play again.");
            if (config.Mode != MissionMode.Operations || sim.Map == null)
                return CommandResult.Rejected("Construction is only available in Operations mode.");

            if (!FacilityCatalog.TryGetSpec(kind, out var spec) || !spec.IsPlayerBuildable)
                return CommandResult.Rejected($"Structure type '{kind}' is not buildable.");

            footprint = FacilityCatalog.GetFootprintCells(kind, origin, rotation);
            for (int i = 0; i < footprint.Count; i++)
            {
                var c = footprint[i];
                if (!sim.Map.TryGetCell(c, out var cell))
                    return CommandResult.Rejected($"Footprint cell {c} is out of map bounds.");
                if (!cell.IsExplored)
                    return CommandResult.Rejected($"Parcel {c} must be explored first.");
                if (!cell.IsClaimed)
                    return CommandResult.Rejected($"Parcel {c} must be claimed before building.");
                if (cell.Protection != StarterProtectionKind.None)
                    return CommandResult.Rejected($"Parcel {c} belongs to protected starter infrastructure.");

                if (spec.UsesUtilityLayerOnly)
                {
                    if (cell.HasUtilityLine || !string.IsNullOrEmpty(cell.UtilityBlueprintId))
                        return CommandResult.Rejected($"Parcel {c} already has a utility line or blueprint.");
                    if (!cell.IsBuildable && string.IsNullOrEmpty(cell.FacilityId))
                        return CommandResult.Rejected($"Terrain at {c} cannot support a utility line.");
                }
                else
                {
                    if (!cell.IsBuildable)
                        return CommandResult.Rejected($"Terrain at {c} is not buildable ({cell.Terrain}).");
                    if (!string.IsNullOrEmpty(cell.FacilityId))
                        return CommandResult.Rejected($"Parcel {c} already contains a facility or blueprint.");
                }

                if (spec.RequiredResource != ParcelResourceKind.None)
                {
                    if (!cell.IsScanned)
                        return CommandResult.Rejected($"Parcel {c} must be scanned before placing {kind}.");
                    if (cell.ResourceKind != spec.RequiredResource)
                        return CommandResult.Rejected($"Parcel {c} requires {spec.RequiredResource} (found {cell.ResourceKind}).");
                }
            }

            // Limits check (F01.8)
            var facs = sim.Facilities;
            int nonUtilityCount = 0;
            int playerPanels = 0;
            int newExtractors = 0;
            int extraShelters = 0;
            int roverDocks = 0;
            if (facs != null)
            {
                for (int i = 0; i < facs.Count; i++)
                {
                    var f = facs[i];
                    if (f.IsProtectedStarter) continue;
                    var fSpec = FacilityCatalog.GetSpec(f.Kind);
                    if (!fSpec.UsesUtilityLayerOnly)
                        nonUtilityCount++;
                    if (f.Kind == FacilityKind.SolarPanel) playerPanels++;
                    else if (f.Kind == FacilityKind.WaterExtractor) newExtractors++;
                    else if (f.Kind == FacilityKind.ExtraShelter) extraShelters++;
                    else if (f.Kind == FacilityKind.RoverDock) roverDocks++;
                }
            }

            if (!spec.UsesUtilityLayerOnly && nonUtilityCount >= FacilityCatalog.MaxPlayerNonUtilityFacilities)
                return CommandResult.Rejected($"Player facility cap reached ({FacilityCatalog.MaxPlayerNonUtilityFacilities}).");
            int starterPanels = sim.Resources != null && sim.Resources.Panels != null
                ? Math.Min(3, sim.Resources.Panels.Count)
                : 3;
            if (kind == FacilityKind.SolarPanel && starterPanels + playerPanels >= FacilityCatalog.MaxTotalPanels)
                return CommandResult.Rejected($"Solar panel cap reached ({FacilityCatalog.MaxTotalPanels} total panels).");
            if (kind == FacilityKind.WaterExtractor && newExtractors >= FacilityCatalog.MaxNewExtractors)
                return CommandResult.Rejected($"New water extractor cap reached ({FacilityCatalog.MaxNewExtractors}).");
            if (kind == FacilityKind.ExtraShelter && extraShelters >= FacilityCatalog.MaxExtraShelters)
                return CommandResult.Rejected($"Extra shelter cap reached ({FacilityCatalog.MaxExtraShelters}).");
            if (kind == FacilityKind.RoverDock && roverDocks >= FacilityCatalog.MaxRoverDocks)
                return CommandResult.Rejected($"Rover dock cap reached ({FacilityCatalog.MaxRoverDocks}).");

            if (sim.Materials < spec.MaterialCost)
                return CommandResult.Rejected($"Insufficient materials ({sim.Materials}/{spec.MaterialCost} required).");

            // Corridor / access / crew trap check for passability-blocking facilities
            if (spec.BlocksPassability)
            {
                // Cannot place on top of any outside astronaut
                if (sim.Crew != null)
                {
                    for (int i = 0; i < sim.Crew.Count; i++)
                    {
                        var m = sim.Crew[i];
                        if (m.HasSurfacePosition && sim.Map.Config.TryWorldToCell(m.SurfacePosX, m.SurfacePosZ, out var crewCell))
                        {
                            for (int fIdx = 0; fIdx < footprint.Count; fIdx++)
                            {
                                if (footprint[fIdx] == crewCell)
                                    return CommandResult.Rejected($"Cannot place blueprint on parcel {crewCell} while crew is standing there.");
                            }
                        }
                    }
                }

                // Clone map temporarily with footprint cells blocked to ensure work-neighbor reachability AND return corridors for all outside crew.
                var testMap = sim.Map.Clone();
                for (int i = 0; i < footprint.Count; i++)
                    testMap.GetCell(footprint[i]).IsPassable = false;

                bool hasWorkAccess = false;
                for (int i = 0; i < footprint.Count; i++)
                {
                    if (GridNav.TryFindBestWorkCell(testMap, ParcelMap.AirlockCorridorCoord, footprint[i], allowTargetCell: false, out _, out _))
                    {
                        hasWorkAccess = true;
                        break;
                    }
                }
                if (!hasWorkAccess)
                    return CommandResult.Rejected($"No reachable adjacent work cell from airlock to build {kind} at {origin}.");

                if (sim.Crew != null)
                {
                    for (int i = 0; i < sim.Crew.Count; i++)
                    {
                        var m = sim.Crew[i];
                        if (m.HasSurfacePosition && sim.Map.Config.TryWorldToCell(m.SurfacePosX, m.SurfacePosZ, out var crewCell))
                        {
                            if (!GridNav.TryFindPath(testMap, crewCell, ParcelMap.AirlockCorridorCoord, out _))
                                return CommandResult.Rejected($"Placing {kind} at {origin} would block {m.Name}'s return corridor to airlock.");
                        }
                    }
                }
            }
            else
            {
                if (!GridNav.TryFindBestWorkCell(sim.Map, ParcelMap.AirlockCorridorCoord, origin, allowTargetCell: true, out _, out _))
                    return CommandResult.Rejected($"No reachable path from airlock to utility line cell {origin}.");
            }

            return CommandResult.Accepted();
        }

        private static FacilityState FindFacility(SimulationMutableState sim, string facilityId)
        {
            if (sim == null || string.IsNullOrEmpty(facilityId) || sim.Facilities == null)
                return null;
            for (int i = 0; i < sim.Facilities.Count; i++)
            {
                if (sim.Facilities[i].FacilityId == facilityId)
                    return sim.Facilities[i];
            }
            return null;
        }

        private static bool HasAssignedCrewWork(SimulationMutableState sim, FacilityState fac)
        {
            if (sim?.SurfaceJobs == null || fac == null)
                return false;

            for (int i = 0; i < sim.SurfaceJobs.Count; i++)
            {
                var j = sim.SurfaceJobs[i];
                if (j.Phase == SurfaceJobPhase.Completed || string.IsNullOrEmpty(j.AssignedCrewId))
                    continue;
                if (j.TargetId == fac.FacilityId ||
                    (fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(j.TargetId)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasCrewOccupyingFacilityOrParcel(SimulationMutableState sim, FacilityState fac)
        {
            if (sim?.Crew == null || sim.Map == null || fac == null)
                return false;

            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var m = sim.Crew[i];
                if (m.HasSurfacePosition && sim.Map.Config.TryWorldToCell(m.SurfacePosX, m.SurfacePosZ, out var crewCell) &&
                    fac.FootprintCells != null)
                {
                    for (int k = 0; k < fac.FootprintCells.Count; k++)
                    {
                        if (fac.FootprintCells[k] == crewCell)
                            return true;
                    }
                }
            }
            return false;
        }

        private static bool HasShelterOccupants(SimulationMutableState sim, FacilityState fac)
        {
            if (sim?.Crew == null || fac == null || fac.Kind != FacilityKind.ExtraShelter)
                return false;

            // Extra shelter capacity assignment lands in OP24; guard conservatively until shelter IDs are assigned.
            return false;
        }

        private static bool PreservesCrewReturnCorridorsAfterRemoval(SimulationMutableState sim, FacilityState fac, out string blockerName)
        {
            blockerName = null;
            if (sim?.Crew == null || sim.Map == null || fac == null)
                return true;

            var testMap = sim.Map.Clone();
            bool blocks = FacilityCatalog.TryGetSpec(fac.Kind, out var spec) && spec.BlocksPassability;
            if (fac.FootprintCells != null)
            {
                for (int i = 0; i < fac.FootprintCells.Count; i++)
                {
                    if (!testMap.TryGetCell(fac.FootprintCells[i], out var cell))
                        continue;
                    if (spec != null && spec.UsesUtilityLayerOnly)
                    {
                        cell.HasUtilityLine = false;
                        if (cell.UtilityBlueprintId == fac.FacilityId)
                            cell.UtilityBlueprintId = null;
                    }
                    else
                    {
                        if (cell.FacilityId == fac.FacilityId)
                            cell.FacilityId = null;
                        if (blocks)
                            cell.IsPassable = cell.Terrain != ParcelTerrainKind.Ridge || cell.Coord.Z < 13;
                    }
                }
            }

            for (int i = 0; i < sim.Crew.Count; i++)
            {
                var m = sim.Crew[i];
                if (m.HasSurfacePosition && testMap.Config.TryWorldToCell(m.SurfacePosX, m.SurfacePosZ, out var crewCell))
                {
                    if (!GridNav.TryFindPath(testMap, crewCell, ParcelMap.AirlockCorridorCoord, out _))
                    {
                        blockerName = m.Name;
                        return false;
                    }
                }
            }
            return true;
        }

        internal static CommandResult CanCancelBlueprint(SimulationMutableState sim, string facilityId)
        {
            var fac = FindFacility(sim, facilityId);
            if (fac == null)
                return CommandResult.Rejected($"Unknown facility '{facilityId}'.");
            if (fac.IsProtectedStarter)
                return CommandResult.Rejected("Protected starter infrastructure cannot be cancelled.");
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Blueprint)
                return CommandResult.Rejected("Facility is already commissioned; use Dismantle instead.");
            if (HasAssignedCrewWork(sim, fac))
                return CommandResult.Rejected("Cannot cancel blueprint while a crew member is assigned to build it.");
            if (HasCrewOccupyingFacilityOrParcel(sim, fac))
                return CommandResult.Rejected("Cannot cancel blueprint while a crew member is standing on its parcel.");
            if (!PreservesCrewReturnCorridorsAfterRemoval(sim, fac, out string blockerName))
                return CommandResult.Rejected($"Cancelling blueprint {fac.Kind} would block {blockerName}'s return corridor.");
            return CommandResult.Accepted();
        }

        internal static CommandResult CanDismantleOrModifyFacility(SimulationMutableState sim, string facilityId)
        {
            var fac = FindFacility(sim, facilityId);
            if (fac == null)
                return CommandResult.Rejected($"Unknown facility '{facilityId}'.");
            if (fac.IsProtectedStarter)
                return CommandResult.Rejected("Protected starter infrastructure cannot be dismantled.");
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                return CommandResult.Rejected("Blueprint cannot be dismantled; use Cancel Blueprint instead.");

            if (sim.ChannelSchedules != null)
            {
                for (int i = 0; i < sim.ChannelSchedules.Count; i++)
                {
                    var ch = sim.ChannelSchedules[i];
                    if (ch.ChannelId == OperationsChannelId.SolarPanel && ch.IsActive && !string.IsNullOrEmpty(ch.ActiveTargetId))
                    {
                        bool ownsTarget = fac.LinkedPanelIds != null && fac.LinkedPanelIds.Contains(ch.ActiveTargetId);
                        if (!ownsTarget && sim.Resources?.Panels != null)
                        {
                            for (int p = 0; p < sim.Resources.Panels.Count; p++)
                            {
                                var unit = sim.Resources.Panels[p];
                                if (unit.Id == ch.ActiveTargetId && unit.FacilityId == fac.FacilityId)
                                {
                                    ownsTarget = true;
                                    break;
                                }
                            }
                        }
                        if (ownsTarget)
                            return CommandResult.Rejected($"Cannot dismantle {fac.Kind} while panel '{ch.ActiveTargetId}' is the active fault target; repair it first.");
                    }
                }
            }

            if (HasAssignedCrewWork(sim, fac))
                return CommandResult.Rejected("Cannot dismantle facility while a crew member is assigned to work on it.");
            if (HasCrewOccupyingFacilityOrParcel(sim, fac))
                return CommandResult.Rejected("Cannot dismantle facility while a crew member is standing on its parcel.");
            if (HasShelterOccupants(sim, fac))
                return CommandResult.Rejected("Cannot dismantle extra shelter while crew members are occupying it.");
            if (!PreservesCrewReturnCorridorsAfterRemoval(sim, fac, out string blockerName))
                return CommandResult.Rejected($"Dismantling {fac.Kind} would block {blockerName}'s return corridor.");

            return CommandResult.Accepted();
        }

        public static void PreviewDismantleRefund(
            FacilityState fac,
            int currentMaterials,
            int materialCapacity,
            out int appliedRefund,
            out int rawRefund,
            out int cappedLoss)
        {
            rawRefund = fac != null && fac.LifecycleStatus == FacilityLifecycleStatus.Commissioned
                ? Math.Max(0, fac.PaidMaterials) / 2
                : 0;
            int space = Math.Max(0, materialCapacity - currentMaterials);
            appliedRefund = Math.Min(rawRefund, space);
            cappedLoss = rawRefund - appliedRefund;
        }

        public static void PreviewStorageSpillOnDismantle(
            ResourceConfig config,
            MissionState state,
            FacilityState fac,
            out double energySpill,
            out double waterSpill)
        {
            energySpill = ColonyStorage.PreviewBatteryLossIfFacilityDisconnected(config, state, fac);
            waterSpill = 0;
        }
    }
}
