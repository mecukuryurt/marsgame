using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarsOutpost
{
    public readonly struct WorldPos2D : IEquatable<WorldPos2D>
    {
        public double X { get; }
        public double Z { get; }

        public WorldPos2D(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double DistanceTo(WorldPos2D other)
        {
            double dx = other.X - X;
            double dz = other.Z - Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public static WorldPos2D Lerp(WorldPos2D a, WorldPos2D b, double t)
        {
            return new WorldPos2D(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t);
        }

        public bool Equals(WorldPos2D other) => X.Equals(other.X) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is WorldPos2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Z.GetHashCode();
            }
        }

        public static bool operator ==(WorldPos2D left, WorldPos2D right) => left.Equals(right);
        public static bool operator !=(WorldPos2D left, WorldPos2D right) => !left.Equals(right);
    }

    public enum SurfaceJobKind
    {
        GenericWork = 0,
        Construct = 1,
        Explore = 2,
        Scan = 3,
        Repair = 4
    }

    public enum SurfaceJobPhase
    {
        Travel = 0,
        Work = 1,
        Return = 2,
        Completed = 3
    }

    public static class GridNav
    {
        public static readonly WorldPos2D AirlockOuterWorldPos = new WorldPos2D(2.45, -0.35);
        public const double DefaultWalkSpeed = 2.0;
        public const double AirlockPassSeconds = 1.5;

        /// <summary>
        /// Deterministic 4-neighbor shortest-path BFS using fixed N/E/S/W order.
        /// Allows stepping out of <paramref name="start"/>, while all entered cells must be passable.
        /// </summary>
        public static bool TryFindPath(ParcelMap map, GridCoord start, GridCoord goal, out List<GridCoord> path)
        {
            path = null;
            if (map == null || !map.Config.IsInBounds(start) || !map.Config.IsInBounds(goal))
                return false;
            if (!map.TryGetCell(goal, out var goalCell) || !goalCell.IsPassable)
                return false;

            if (start == goal)
            {
                path = new List<GridCoord> { start };
                return true;
            }

            int width = map.Config.Width;
            int height = map.Config.Height;
            int total = width * height;
            var visited = new bool[total];
            var parent = new int[total];
            for (int i = 0; i < total; i++) parent[i] = -1;

            int startIdx = start.Z * width + start.X;
            int goalIdx = goal.Z * width + goal.X;

            var queue = new Queue<GridCoord>();
            visited[startIdx] = true;
            queue.Enqueue(start);

            bool found = false;
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int curIdx = cur.Z * width + cur.X;

                for (int d = 0; d < GridCoord.CardinalOffsets.Length; d++)
                {
                    var next = new GridCoord(
                        cur.X + GridCoord.CardinalOffsets[d].X,
                        cur.Z + GridCoord.CardinalOffsets[d].Z);
                    if (!map.Config.IsInBounds(next)) continue;

                    int nextIdx = next.Z * width + next.X;
                    if (visited[nextIdx]) continue;
                    if (!map.TryGetCell(next, out var nextCell) || !nextCell.IsPassable) continue;

                    visited[nextIdx] = true;
                    parent[nextIdx] = curIdx;
                    if (nextIdx == goalIdx)
                    {
                        found = true;
                        break;
                    }
                    queue.Enqueue(next);
                }

                if (found) break;
            }

            if (!found) return false;

            path = new List<GridCoord>();
            int trace = goalIdx;
            while (trace != -1)
            {
                int x = trace % width;
                int z = trace / width;
                path.Add(new GridCoord(x, z));
                if (trace == startIdx) break;
                trace = parent[trace];
            }
            path.Reverse();
            return true;
        }

        /// <summary>
        /// Chooses a reachable work cell in fixed N/E/S/W preference among adjacent passable neighbors
        /// (or the target cell itself if <paramref name="allowTargetCell"/> is true and it is passable).
        /// Minimizes path length; ties break in N/E/S/W order.
        /// </summary>
        public static bool TryFindBestWorkCell(
            ParcelMap map,
            GridCoord start,
            GridCoord target,
            bool allowTargetCell,
            out GridCoord workCell,
            out List<GridCoord> cellPath)
        {
            workCell = default;
            cellPath = null;
            if (map == null || !map.Config.IsInBounds(start) || !map.Config.IsInBounds(target))
                return false;

            if (allowTargetCell && map.TryGetCell(target, out var targetCell) && targetCell.IsPassable)
            {
                if (TryFindPath(map, start, target, out var directPath))
                {
                    workCell = target;
                    cellPath = directPath;
                    return true;
                }
            }

            List<GridCoord> bestPath = null;
            GridCoord bestCell = default;

            for (int d = 0; d < GridCoord.CardinalOffsets.Length; d++)
            {
                var candidate = new GridCoord(
                    target.X + GridCoord.CardinalOffsets[d].X,
                    target.Z + GridCoord.CardinalOffsets[d].Z);
                if (!map.Config.IsInBounds(candidate)) continue;
                if (!map.TryGetCell(candidate, out var candCell) || !candCell.IsPassable) continue;

                if (TryFindPath(map, start, candidate, out var candidatePath))
                {
                    if (bestPath == null || candidatePath.Count < bestPath.Count)
                    {
                        bestPath = candidatePath;
                        bestCell = candidate;
                    }
                }
            }

            if (bestPath == null) return false;
            workCell = bestCell;
            cellPath = bestPath;
            return true;
        }

        public static double BuildWorldWaypoints(
            ParcelMapConfig config,
            WorldPos2D startPos,
            IReadOnlyList<GridCoord> cellPath,
            bool endAtAirlockOuter,
            out List<WorldPos2D> waypoints)
        {
            waypoints = new List<WorldPos2D>();
            AddDistinctWaypoint(waypoints, startPos);

            if (cellPath != null)
            {
                int startIdx = 0;
                // If already inside first cell, go directly to next cell center to avoid backtracking to cell center.
                if (cellPath.Count > 1 && config.TryWorldToCell(startPos.X, startPos.Z, out var startCell) && startCell == cellPath[0])
                    startIdx = 1;

                for (int i = startIdx; i < cellPath.Count; i++)
                {
                    if (endAtAirlockOuter && i == cellPath.Count - 1 && cellPath[i] == ParcelMap.AirlockCorridorCoord)
                    {
                        AddDistinctWaypoint(waypoints, AirlockOuterWorldPos);
                    }
                    else
                    {
                        config.CellToWorldCenter(cellPath[i], out double cx, out double cz);
                        AddDistinctWaypoint(waypoints, new WorldPos2D(cx, cz));
                    }
                }
            }

            if (endAtAirlockOuter)
                AddDistinctWaypoint(waypoints, AirlockOuterWorldPos);

            double totalLength = 0;
            for (int i = 1; i < waypoints.Count; i++)
                totalLength += waypoints[i - 1].DistanceTo(waypoints[i]);

            return totalLength;
        }

        private static void AddDistinctWaypoint(List<WorldPos2D> list, WorldPos2D pt)
        {
            if (list.Count == 0 || list[list.Count - 1].DistanceTo(pt) > 1e-6)
                list.Add(pt);
        }
    }

    public sealed class SurfaceJobState
    {
        public string JobId { get; }
        public SurfaceJobKind Kind { get; }
        public string TargetId { get; }
        public GridCoord TargetCoord { get; }
        public GridCoord WorkCoord { get; internal set; }
        public string AssignedCrewId { get; internal set; }
        public SurfaceJobPhase Phase { get; internal set; }
        public double TravelSpeed { get; }
        public double WorkRequiredSeconds { get; }
        public double WorkProgressSeconds { get; internal set; }
        public double WorkStaminaRate { get; }
        public double WorkEnergyTotal { get; }
        public double EnergyConsumed { get; internal set; }
        public List<WorldPos2D> WaypointsInternal { get; }
        public ReadOnlyCollection<WorldPos2D> Waypoints { get; }
        public double LegTotalDistance { get; internal set; }
        public double LegDistanceTraveled { get; internal set; }
        public double AirlockRemainingSeconds { get; internal set; }

        public SurfaceJobState(
            string jobId,
            SurfaceJobKind kind,
            string targetId,
            GridCoord targetCoord,
            GridCoord workCoord,
            string assignedCrewId,
            SurfaceJobPhase phase,
            double travelSpeed,
            double workRequiredSeconds,
            double workStaminaRate,
            double workEnergyTotal,
            IEnumerable<WorldPos2D> waypoints,
            double legTotalDistance,
            double legDistanceTraveled = 0,
            double airlockRemainingSeconds = 0,
            double workProgressSeconds = 0,
            double energyConsumed = 0)
        {
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            TargetCoord = targetCoord;
            WorkCoord = workCoord;
            AssignedCrewId = assignedCrewId;
            Phase = phase;
            TravelSpeed = travelSpeed > 0 ? travelSpeed : GridNav.DefaultWalkSpeed;
            WorkRequiredSeconds = Math.Max(0, workRequiredSeconds);
            WorkStaminaRate = workStaminaRate;
            WorkEnergyTotal = Math.Max(0, workEnergyTotal);
            WaypointsInternal = waypoints != null ? new List<WorldPos2D>(waypoints) : new List<WorldPos2D>();
            Waypoints = WaypointsInternal.AsReadOnly();
            LegTotalDistance = Math.Max(0, legTotalDistance);
            LegDistanceTraveled = Math.Max(0, Math.Min(LegTotalDistance, legDistanceTraveled));
            AirlockRemainingSeconds = Math.Max(0, airlockRemainingSeconds);
            WorkProgressSeconds = Math.Max(0, Math.Min(WorkRequiredSeconds, workProgressSeconds));
            EnergyConsumed = Math.Max(0, Math.Min(WorkEnergyTotal, energyConsumed));
        }

        public SurfaceJobState Clone()
        {
            return new SurfaceJobState(
                JobId,
                Kind,
                TargetId,
                TargetCoord,
                WorkCoord,
                AssignedCrewId,
                Phase,
                TravelSpeed,
                WorkRequiredSeconds,
                WorkStaminaRate,
                WorkEnergyTotal,
                WaypointsInternal,
                LegTotalDistance,
                LegDistanceTraveled,
                AirlockRemainingSeconds,
                WorkProgressSeconds,
                EnergyConsumed);
        }

        public double GetRemainingLegSeconds()
        {
            if (Phase != SurfaceJobPhase.Travel && Phase != SurfaceJobPhase.Return) return 0;
            double walkRem = LegTotalDistance > LegDistanceTraveled
                ? (LegTotalDistance - LegDistanceTraveled) / TravelSpeed
                : 0;
            double airRem = Phase == SurfaceJobPhase.Return ? Math.Max(0, AirlockRemainingSeconds) : 0;
            return walkRem + airRem;
        }

        public void EvaluatePositionAndHeading(out WorldPos2D pos, out WorldPos2D heading)
        {
            if (WaypointsInternal.Count == 0)
            {
                pos = GridNav.AirlockOuterWorldPos;
                heading = new WorldPos2D(1, 0);
                return;
            }
            if (WaypointsInternal.Count == 1 || LegTotalDistance <= 1e-9)
            {
                pos = WaypointsInternal[WaypointsInternal.Count - 1];
                heading = new WorldPos2D(1, 0);
                return;
            }

            double remaining = Math.Max(0, Math.Min(LegTotalDistance, LegDistanceTraveled));
            for (int i = 1; i < WaypointsInternal.Count; i++)
            {
                var a = WaypointsInternal[i - 1];
                var b = WaypointsInternal[i];
                double segLen = a.DistanceTo(b);
                if (segLen <= 1e-9) continue;

                double dx = (b.X - a.X) / segLen;
                double dz = (b.Z - a.Z) / segLen;
                heading = new WorldPos2D(dx, dz);

                if (remaining <= segLen || i == WaypointsInternal.Count - 1)
                {
                    double t = Math.Max(0, Math.Min(1, remaining / segLen));
                    pos = WorldPos2D.Lerp(a, b, t);
                    return;
                }
                remaining -= segLen;
            }

            int last = WaypointsInternal.Count - 1;
            pos = WaypointsInternal[last];
            var prev = WaypointsInternal[last - 1];
            double len = prev.DistanceTo(pos);
            heading = len > 1e-9 ? new WorldPos2D((pos.X - prev.X) / len, (pos.Z - prev.Z) / len) : new WorldPos2D(1, 0);
        }
    }
}
