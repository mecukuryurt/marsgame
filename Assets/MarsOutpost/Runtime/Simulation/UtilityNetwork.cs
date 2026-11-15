using System.Collections.Generic;

namespace MarsOutpost
{
    /// <summary>
    /// Deterministic 4-way cardinal utility connectivity graph rooted at the core habitat ports
    /// and protected starter infrastructure.
    /// </summary>
    public static class UtilityNetwork
    {
        public const int MaskNorth = 1;
        public const int MaskEast = 2;
        public const int MaskSouth = 4;
        public const int MaskWest = 8;

        public static HashSet<GridCoord> ComputeConnectedLineCells(ParcelMap map)
        {
            var connected = new HashSet<GridCoord>();
            if (map == null)
                return connected;

            var queue = new Queue<GridCoord>();
            var cells = map.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell.HasUtilityLine && cell.Protection != StarterProtectionKind.None)
                {
                    if (connected.Add(cell.Coord))
                        queue.Enqueue(cell.Coord);
                }
            }

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var n in map.Config.GetCardinalNeighbors(cur))
                {
                    if (map.TryGetCell(n, out var nCell) && nCell.HasUtilityLine && connected.Add(n))
                        queue.Enqueue(n);
                }
            }

            return connected;
        }

        public static bool IsCellConnectedToCore(ParcelMap map, HashSet<GridCoord> connectedLines, GridCoord coord)
        {
            if (map == null || connectedLines == null || !map.Config.IsInBounds(coord))
                return false;
            if (connectedLines.Contains(coord))
                return true;

            foreach (var n in map.Config.GetCardinalNeighbors(coord))
            {
                if (connectedLines.Contains(n))
                    return true;
            }
            return false;
        }

        public static bool IsCoordConnectedPreview(ParcelMap map, GridCoord coord)
        {
            if (map == null) return false;
            var connected = ComputeConnectedLineCells(map);
            return IsCellConnectedToCore(map, connected, coord);
        }

        public static bool IsFacilityConnectedToCore(ParcelMap map, HashSet<GridCoord> connectedLines, FacilityState fac)
        {
            if (fac == null || map == null)
                return false;
            if (fac.IsProtectedStarter)
                return true;
            if (fac.LifecycleStatus != FacilityLifecycleStatus.Commissioned)
                return false;

            bool isUtilityLine = FacilityCatalog.TryGetSpec(fac.Kind, out var spec) && spec.UsesUtilityLayerOnly;
            if (isUtilityLine)
            {
                if (fac.FootprintCells != null && fac.FootprintCells.Count > 0)
                {
                    for (int i = 0; i < fac.FootprintCells.Count; i++)
                    {
                        if (connectedLines.Contains(fac.FootprintCells[i]))
                            return true;
                    }
                    return false;
                }
                return connectedLines.Contains(fac.Origin);
            }

            if (fac.FootprintCells != null && fac.FootprintCells.Count > 0)
            {
                for (int i = 0; i < fac.FootprintCells.Count; i++)
                {
                    if (IsCellConnectedToCore(map, connectedLines, fac.FootprintCells[i]))
                        return true;
                }
                return false;
            }

            return IsCellConnectedToCore(map, connectedLines, fac.Origin);
        }

        internal static void RefreshTopology(SimulationMutableState sim)
        {
            if (sim == null || sim.Map == null || sim.Facilities == null)
                return;

            var connected = ComputeConnectedLineCells(sim.Map);
            for (int i = 0; i < sim.Facilities.Count; i++)
            {
                var fac = sim.Facilities[i];
                if (fac == null) continue;
                fac.IsConnectedToCore = IsFacilityConnectedToCore(sim.Map, connected, fac);
            }
        }

        public static int GetCardinalLineMask(ParcelMap map, GridCoord coord)
        {
            if (map == null) return 0;
            int mask = 0;
            if (map.TryGetCell(new GridCoord(coord.X, coord.Z + 1), out var nCell) && nCell.HasUtilityLine)
                mask |= MaskNorth;
            if (map.TryGetCell(new GridCoord(coord.X + 1, coord.Z), out var eCell) && eCell.HasUtilityLine)
                mask |= MaskEast;
            if (map.TryGetCell(new GridCoord(coord.X, coord.Z - 1), out var sCell) && sCell.HasUtilityLine)
                mask |= MaskSouth;
            if (map.TryGetCell(new GridCoord(coord.X - 1, coord.Z), out var wCell) && wCell.HasUtilityLine)
                mask |= MaskWest;
            return mask;
        }
    }
}
