using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarsOutpost
{
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public int X { get; }
        public int Z { get; }

        public GridCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        // Fixed N / E / S / W order per F04 contract.
        public static readonly GridCoord[] CardinalOffsets =
        {
            new GridCoord(0, 1),   // North
            new GridCoord(1, 0),   // East
            new GridCoord(0, -1),  // South
            new GridCoord(-1, 0)   // West
        };

        public GridCoord North => new GridCoord(X, Z + 1);
        public GridCoord East => new GridCoord(X + 1, Z);
        public GridCoord South => new GridCoord(X, Z - 1);
        public GridCoord West => new GridCoord(X - 1, Z);

        public int ManhattanDistance(GridCoord other) => Math.Abs(X - other.X) + Math.Abs(Z - other.Z);

        public bool Equals(GridCoord other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);

        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Z})";
    }

    public enum ParcelTerrainKind
    {
        Plain = 0,
        Dune = 1,
        Crater = 2,
        Ridge = 3,
        OutpostPad = 4
    }

    public enum ParcelResourceKind
    {
        None = 0,
        Ice = 1,
        HydratedRegolith = 2
    }

    public enum StarterProtectionKind
    {
        None = 0,
        CoreHabitat = 1,
        AirlockCorridor = 2,
        StarterSolar = 3
    }

    public sealed class ParcelMapConfig
    {
        public int Width { get; }
        public int Height { get; }
        public double CellSize { get; }
        public double OriginX { get; }
        public double OriginZ { get; }

        // Matches MarsOutpostWorldView terrain mesh bounds x[-36, 36], z[-28, 44].
        public const int DefaultWidth = 16;
        public const int DefaultHeight = 16;
        public const double DefaultCellSize = 4.0;
        public const double DefaultOriginX = -32.0;
        public const double DefaultOriginZ = -24.0;

        public static ParcelMapConfig Default { get; } = new ParcelMapConfig(
            DefaultWidth, DefaultHeight, DefaultCellSize, DefaultOriginX, DefaultOriginZ);

        public ParcelMapConfig(int width, int height, double cellSize, double originX, double originZ)
        {
            if (width <= 0 || width > 128) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || height > 128) throw new ArgumentOutOfRangeException(nameof(height));
            if (!MissionConfig.IsFinite(cellSize) || cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (!MissionConfig.IsFinite(originX) || !MissionConfig.IsFinite(originZ)) throw new ArgumentOutOfRangeException(nameof(originX));

            Width = width;
            Height = height;
            CellSize = cellSize;
            OriginX = originX;
            OriginZ = originZ;
        }

        public double MaxWorldX => OriginX + Width * CellSize;
        public double MaxWorldZ => OriginZ + Height * CellSize;

        public bool IsInBounds(GridCoord coord)
            => coord.X >= 0 && coord.X < Width && coord.Z >= 0 && coord.Z < Height;

        public GridCoord WorldToCellUnclamped(double worldX, double worldZ)
        {
            if (!MissionConfig.IsFinite(worldX) || !MissionConfig.IsFinite(worldZ))
                throw new ArgumentOutOfRangeException(nameof(worldX), "World coordinates must be finite.");
            int cx = (int)Math.Floor((worldX - OriginX) / CellSize);
            int cz = (int)Math.Floor((worldZ - OriginZ) / CellSize);
            return new GridCoord(cx, cz);
        }

        public bool TryWorldToCell(double worldX, double worldZ, out GridCoord coord)
        {
            if (!MissionConfig.IsFinite(worldX) || !MissionConfig.IsFinite(worldZ))
            {
                coord = default;
                return false;
            }
            coord = WorldToCellUnclamped(worldX, worldZ);
            return IsInBounds(coord);
        }

        public void CellToWorldCenter(GridCoord coord, out double worldX, out double worldZ)
        {
            worldX = OriginX + (coord.X + 0.5) * CellSize;
            worldZ = OriginZ + (coord.Z + 0.5) * CellSize;
        }

        public void GetCellBounds(GridCoord coord, out double minX, out double minZ, out double maxX, out double maxZ)
        {
            minX = OriginX + coord.X * CellSize;
            minZ = OriginZ + coord.Z * CellSize;
            maxX = minX + CellSize;
            maxZ = minZ + CellSize;
        }

        public IEnumerable<GridCoord> GetCardinalNeighbors(GridCoord coord)
        {
            for (int i = 0; i < GridCoord.CardinalOffsets.Length; i++)
            {
                var next = new GridCoord(coord.X + GridCoord.CardinalOffsets[i].X, coord.Z + GridCoord.CardinalOffsets[i].Z);
                if (IsInBounds(next))
                    yield return next;
            }
        }
    }

    public sealed class StarterFacilitySpec
    {
        public string FacilityId { get; }
        public IReadOnlyList<GridCoord> Footprint { get; }
        public GridCoord WorkCell { get; }
        public IReadOnlyList<string> OwnedPanelIds { get; }

        public StarterFacilitySpec(string facilityId, GridCoord[] footprint, GridCoord workCell, string[] ownedPanelIds = null)
        {
            FacilityId = facilityId;
            Footprint = Array.AsReadOnly((GridCoord[])footprint.Clone());
            WorkCell = workCell;
            OwnedPanelIds = ownedPanelIds != null
                ? Array.AsReadOnly((string[])ownedPanelIds.Clone())
                : Array.AsReadOnly(Array.Empty<string>());
        }
    }

    public sealed class ParcelState
    {
        public GridCoord Coord { get; }
        public ParcelTerrainKind Terrain { get; internal set; }
        public bool IsPassable { get; internal set; }
        public bool IsBuildable { get; internal set; }
        public StarterProtectionKind Protection { get; internal set; }
        public double SolarFactor { get; internal set; }
        public bool IsExplored { get; internal set; }
        public bool IsScanned { get; internal set; }
        public bool IsClaimed { get; internal set; }
        public bool HasUtilityLine { get; internal set; }
        public string UtilityBlueprintId { get; internal set; }
        public string FacilityId { get; internal set; }
        public string StarterFacilityId { get; internal set; }
        public ParcelResourceKind ResourceKind { get; internal set; }
        public double YieldFactor { get; internal set; }
        public double RemainingWater { get; internal set; }

        public ParcelState(
            GridCoord coord,
            ParcelTerrainKind terrain,
            bool isPassable,
            bool isBuildable,
            StarterProtectionKind protection,
            double solarFactor,
            bool isExplored,
            bool isScanned,
            bool isClaimed,
            bool hasUtilityLine,
            string facilityId,
            string starterFacilityId,
            ParcelResourceKind resourceKind,
            double yieldFactor,
            double remainingWater,
            string utilityBlueprintId = null)
        {
            Coord = coord;
            Terrain = terrain;
            IsPassable = isPassable;
            IsBuildable = isBuildable;
            Protection = protection;
            SolarFactor = solarFactor;
            IsExplored = isExplored;
            IsScanned = isScanned;
            IsClaimed = isClaimed;
            HasUtilityLine = hasUtilityLine;
            UtilityBlueprintId = utilityBlueprintId;
            FacilityId = facilityId;
            StarterFacilityId = starterFacilityId;
            ResourceKind = resourceKind;
            YieldFactor = yieldFactor;
            RemainingWater = remainingWater;
        }

        public ParcelState Clone()
        {
            return new ParcelState(
                Coord,
                Terrain,
                IsPassable,
                IsBuildable,
                Protection,
                SolarFactor,
                IsExplored,
                IsScanned,
                IsClaimed,
                HasUtilityLine,
                FacilityId,
                StarterFacilityId,
                ResourceKind,
                YieldFactor,
                RemainingWater,
                UtilityBlueprintId);
        }
    }

    public sealed class ParcelMap
    {
        public const ulong MapRngStreamId = 0x4D41505F56335F31UL;

        // Starter layout aligned with MarsOutpostWorldView:
        // HabitatCenter (-2, 0), AirlockInner (1.20, -0.35), AirlockOuter (2.45, -0.35),
        // SolarArray (4.65, 0.25), SolarWork (4.35, -1.15).
        public static readonly GridCoord AirlockCorridorCoord = new GridCoord(8, 5);
        public static readonly GridCoord SolarWorkCoord = new GridCoord(9, 5);
        public static readonly GridCoord StarterSolarCoord = new GridCoord(9, 6);
        public static readonly GridCoord SystemBayCoord = new GridCoord(6, 6);

        public static readonly StarterFacilitySpec CoreHabitatSpec = new StarterFacilitySpec(
            "core-habitat",
            new[]
            {
                new GridCoord(6, 5),
                new GridCoord(7, 5),
                new GridCoord(6, 6),
                new GridCoord(7, 6),
                new GridCoord(8, 6)
            },
            AirlockCorridorCoord);

        public static readonly StarterFacilitySpec StarterSolarSpec = new StarterFacilitySpec(
            "starter-solar",
            new[] { StarterSolarCoord },
            SolarWorkCoord,
            new[] { "panel-1", "panel-2", "panel-3" });

        public static readonly StarterFacilitySpec LegacyExtractorSpec = new StarterFacilitySpec(
            "legacy-extractor",
            new[] { SystemBayCoord },
            AirlockCorridorCoord);

        public int MapSeed { get; }
        public ParcelMapConfig Config { get; }
        public IReadOnlyList<ParcelState> Cells { get; }

        private readonly ParcelState[,] grid;

        internal ParcelMap(int mapSeed, ParcelMapConfig config, ParcelState[,] grid)
        {
            MapSeed = mapSeed;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));

            var flat = new List<ParcelState>(config.Width * config.Height);
            for (int z = 0; z < config.Height; z++)
                for (int x = 0; x < config.Width; x++)
                    flat.Add(grid[x, z]);
            Cells = new ReadOnlyCollection<ParcelState>(flat);
        }

        public ParcelState GetCell(GridCoord coord)
        {
            if (!Config.IsInBounds(coord))
                throw new ArgumentOutOfRangeException(nameof(coord), $"Coordinate {coord} is outside map bounds.");
            return grid[coord.X, coord.Z];
        }

        public bool TryGetCell(GridCoord coord, out ParcelState cell)
        {
            if (!Config.IsInBounds(coord))
            {
                cell = null;
                return false;
            }
            cell = grid[coord.X, coord.Z];
            return true;
        }

        public ParcelMap Clone()
        {
            var clonedGrid = new ParcelState[Config.Width, Config.Height];
            for (int x = 0; x < Config.Width; x++)
                for (int z = 0; z < Config.Height; z++)
                    clonedGrid[x, z] = grid[x, z].Clone();
            return new ParcelMap(MapSeed, Config, clonedGrid);
        }

        public bool HasClaimedNeighbor(GridCoord coord)
        {
            foreach (var n in Config.GetCardinalNeighbors(coord))
            {
                if (grid[n.X, n.Z].IsClaimed)
                    return true;
            }
            return false;
        }

        public static int DeriveMapSeed(int operationsSeed)
        {
            unchecked
            {
                uint x = (uint)operationsSeed ^ 0xA53C9E21u;
                x ^= x >> 16;
                x *= 0x85EBCA6Bu;
                x ^= x >> 13;
                x *= 0xC2B2AE35u;
                x ^= x >> 16;
                return (int)(x & 0x7FFFFFFFu);
            }
        }

        public static ParcelMap Generate(int mapSeed, ParcelMapConfig config = null)
        {
            config ??= ParcelMapConfig.Default;
            var rng = new DeterministicRng(mapSeed, MapRngStreamId);
            var grid = new ParcelState[config.Width, config.Height];

            // Build lookup sets for starter layout
            var habitatFootprint = new HashSet<GridCoord>(CoreHabitatSpec.Footprint);

            for (int z = 0; z < config.Height; z++)
            {
                for (int x = 0; x < config.Width; x++)
                {
                    var coord = new GridCoord(x, z);
                    config.CellToWorldCenter(coord, out double wx, out double wz);

                    // Determine starter protections first
                    if (habitatFootprint.Contains(coord))
                    {
                        string starterId = coord == SystemBayCoord ? LegacyExtractorSpec.FacilityId : CoreHabitatSpec.FacilityId;
                        grid[x, z] = new ParcelState(
                            coord,
                            ParcelTerrainKind.OutpostPad,
                            isPassable: false,
                            isBuildable: false,
                            protection: StarterProtectionKind.CoreHabitat,
                            solarFactor: 1.0,
                            isExplored: true,
                            isScanned: true,
                            isClaimed: true,
                            hasUtilityLine: true,
                            facilityId: CoreHabitatSpec.FacilityId,
                            starterFacilityId: starterId,
                            resourceKind: ParcelResourceKind.None,
                            yieldFactor: 0,
                            remainingWater: 0);
                        // Advance RNG uniformly per cell so layout tweaks keep deterministic sampling structure
                        rng.NextDouble();
                        rng.NextDouble();
                        rng.NextDouble();
                        continue;
                    }

                    if (coord == AirlockCorridorCoord)
                    {
                        grid[x, z] = new ParcelState(
                            coord,
                            ParcelTerrainKind.OutpostPad,
                            isPassable: true,
                            isBuildable: false,
                            protection: StarterProtectionKind.AirlockCorridor,
                            solarFactor: 1.0,
                            isExplored: true,
                            isScanned: true,
                            isClaimed: true,
                            hasUtilityLine: true,
                            facilityId: null,
                            starterFacilityId: CoreHabitatSpec.FacilityId,
                            resourceKind: ParcelResourceKind.None,
                            yieldFactor: 0,
                            remainingWater: 0);
                        rng.NextDouble();
                        rng.NextDouble();
                        rng.NextDouble();
                        continue;
                    }

                    if (coord == StarterSolarCoord)
                    {
                        grid[x, z] = new ParcelState(
                            coord,
                            ParcelTerrainKind.OutpostPad,
                            isPassable: false,
                            isBuildable: false,
                            protection: StarterProtectionKind.StarterSolar,
                            solarFactor: 1.0,
                            isExplored: true,
                            isScanned: true,
                            isClaimed: true,
                            hasUtilityLine: true,
                            facilityId: StarterSolarSpec.FacilityId,
                            starterFacilityId: StarterSolarSpec.FacilityId,
                            resourceKind: ParcelResourceKind.None,
                            yieldFactor: 0,
                            remainingWater: 0);
                        rng.NextDouble();
                        rng.NextDouble();
                        rng.NextDouble();
                        continue;
                    }

                    if (coord == SolarWorkCoord)
                    {
                        grid[x, z] = new ParcelState(
                            coord,
                            ParcelTerrainKind.OutpostPad,
                            isPassable: true,
                            isBuildable: true,
                            protection: StarterProtectionKind.None,
                            solarFactor: 1.0,
                            isExplored: true,
                            isScanned: true,
                            isClaimed: true,
                            hasUtilityLine: false,
                            facilityId: null,
                            starterFacilityId: null,
                            resourceKind: ParcelResourceKind.None,
                            yieldFactor: 0,
                            remainingWater: 0);
                        rng.NextDouble();
                        rng.NextDouble();
                        rng.NextDouble();
                        continue;
                    }

                    double terrainRoll = rng.NextDouble();
                    double solarRoll = rng.NextDouble();
                    double depositRoll = rng.NextDouble();

                    ParcelTerrainKind terrain = ClassifyTerrain(coord, wx, wz, terrainRoll);
                    bool isPassable = terrain != ParcelTerrainKind.Ridge || z < 13;
                    bool isBuildable = terrain == ParcelTerrainKind.Plain || terrain == ParcelTerrainKind.Dune || terrain == ParcelTerrainKind.OutpostPad;

                    // Starter exploration ring around the outpost (x in [5..11], z in [3..8])
                    bool isInitialExplored = x >= 5 && x <= 11 && z >= 3 && z <= 8;

                    // Keep starter apron around airlock/solar work cell guaranteed passable and buildable
                    if (x >= 7 && x <= 10 && z >= 4 && z <= 7)
                    {
                        terrain = ParcelTerrainKind.Plain;
                        isPassable = true;
                        isBuildable = true;
                    }

                    // Solar factor between 0.65 and 1.00 (rounded to 2 decimal places)
                    double solarFactor = Math.Round(0.65 + solarRoll * 0.35, 2);

                    ParcelResourceKind resKind = ParcelResourceKind.None;
                    double yieldFactor = 0;
                    double remainingWater = 0;

                    if (isPassable && isBuildable)
                    {
                        if (depositRoll < 0.22)
                        {
                            resKind = ParcelResourceKind.Ice;
                            yieldFactor = Math.Round(0.80 + (depositRoll / 0.22) * 0.45, 2);
                            remainingWater = Math.Round(250.0 + ((depositRoll * 7.3) % 1.0) * 350.0, 0);
                        }
                        else if (depositRoll < 0.48)
                        {
                            resKind = ParcelResourceKind.HydratedRegolith;
                            yieldFactor = Math.Round(0.75 + ((depositRoll - 0.22) / 0.26) * 0.45, 2);
                            remainingWater = Math.Round(250.0 + (((depositRoll - 0.22) * 5.9) % 1.0) * 350.0, 0);
                        }
                    }

                    grid[x, z] = new ParcelState(
                        coord,
                        terrain,
                        isPassable,
                        isBuildable,
                        StarterProtectionKind.None,
                        solarFactor,
                        isInitialExplored,
                        isScanned: false,
                        isClaimed: false,
                        hasUtilityLine: false,
                        facilityId: null,
                        starterFacilityId: null,
                        resourceKind: resKind,
                        yieldFactor: yieldFactor,
                        remainingWater: remainingWater);
                }
            }

            EnsureStarterScannedIceSites(grid, config, ref rng);
            return new ParcelMap(mapSeed, config, grid);
        }

        private static ParcelTerrainKind ClassifyTerrain(GridCoord coord, double wx, double wz, double roll)
        {
            // Match MarsOutpostWorldView northern mountain ridge (z > 12..16+)
            if (coord.Z >= 11)
                return ParcelTerrainKind.Ridge;

            // Match the 4 micro-craters from MarsOutpostWorldView.GetTerrainHeight
            if (IsNearCrater(wx, wz, -7.2, -2.5) ||
                IsNearCrater(wx, wz, 7.8, -2.0) ||
                IsNearCrater(wx, wz, 7.8, 3.8) ||
                IsNearCrater(wx, wz, -7.5, 4.2))
            {
                return ParcelTerrainKind.Crater;
            }

            if (roll < 0.10) return ParcelTerrainKind.Crater;
            if (roll < 0.40) return ParcelTerrainKind.Dune;
            return ParcelTerrainKind.Plain;
        }

        private static bool IsNearCrater(double wx, double wz, double cx, double cz)
        {
            double dx = wx - cx;
            double dz = wz - cz;
            return (dx * dx + dz * dz) <= 4.0;
        }

        private static void EnsureStarterScannedIceSites(ParcelState[,] grid, ParcelMapConfig config, ref DeterministicRng rng)
        {
            // Collect all reachable passable & buildable cells in the initial explored ring via 4-way BFS from AirlockCorridorCoord.
            var reachable = new List<ParcelState>();
            var visited = new HashSet<GridCoord> { AirlockCorridorCoord };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(AirlockCorridorCoord);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var next in config.GetCardinalNeighbors(cur))
                {
                    if (!visited.Add(next)) continue;
                    var cell = grid[next.X, next.Z];
                    if (!cell.IsPassable || !cell.IsExplored) continue;
                    queue.Enqueue(next);

                    if (cell.IsBuildable && cell.Protection == StarterProtectionKind.None && cell.Coord != SolarWorkCoord)
                        reachable.Add(cell);
                }
            }

            // Pick/promote at least 2 reachable explored buildable cells to scanned Ice deposits.
            var scannedIce = new List<ParcelState>();
            for (int i = 0; i < reachable.Count; i++)
            {
                if (reachable[i].ResourceKind == ParcelResourceKind.Ice)
                    scannedIce.Add(reachable[i]);
            }

            while (scannedIce.Count < 2 && reachable.Count > scannedIce.Count)
            {
                int pickIdx = rng.NextInt(0, reachable.Count);
                var candidate = reachable[pickIdx];
                if (candidate.ResourceKind == ParcelResourceKind.Ice)
                    continue;

                candidate.ResourceKind = ParcelResourceKind.Ice;
                candidate.YieldFactor = Math.Round(0.90 + rng.NextDouble() * 0.25, 2);
                candidate.RemainingWater = Math.Round(300.0 + rng.NextDouble() * 250.0, 0);
                scannedIce.Add(candidate);
            }

            for (int i = 0; i < Math.Min(2, scannedIce.Count); i++)
                scannedIce[i].IsScanned = true;
        }
    }
}
