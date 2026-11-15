using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MarsOutpost.Tests
{
    public class ParcelMapTests
    {
        [Test]
        public void SimulationAssemblyHasNoUnityEngineReferences()
        {
            var refs = typeof(ParcelMap).Assembly.GetReferencedAssemblies();
            foreach (var r in refs)
                Assert.That(r.Name, Does.Not.StartWith("UnityEngine"), $"Unexpected Unity reference: {r.Name}");
        }

        [Test]
        public void WorldToCellUsesFloorForNegativeAndHalfOpenBoundaries()
        {
            var cfg = ParcelMapConfig.Default; // Origin (-32, -24), CellSize 4, 16x16 => X[-32, 32), Z[-24, 40)

            // Half-open lower/upper boundaries
            Assert.That(cfg.TryWorldToCell(-32.0, -24.0, out var c00), Is.True);
            Assert.That(c00, Is.EqualTo(new GridCoord(0, 0)));

            Assert.That(cfg.TryWorldToCell(31.999, 39.999, out var cMax), Is.True);
            Assert.That(cMax, Is.EqualTo(new GridCoord(15, 15)));

            // Upper edges are exclusive
            Assert.That(cfg.TryWorldToCell(32.0, 0.0, out _), Is.False);
            Assert.That(cfg.TryWorldToCell(0.0, 40.0, out _), Is.False);
            Assert.That(cfg.TryWorldToCell(-32.001, 0.0, out _), Is.False);
            Assert.That(cfg.TryWorldToCell(0.0, -24.001, out _), Is.False);
            Assert.That(cfg.TryWorldToCell(double.NaN, 0.0, out _), Is.False);
            Assert.That(cfg.TryWorldToCell(0.0, double.PositiveInfinity, out _), Is.False);

            // Floor vs truncation check near negative coordinates and origin (-32.1 must be -1, not 0)
            var unclampedNeg = cfg.WorldToCellUnclamped(-32.1, -24.1);
            Assert.That(unclampedNeg, Is.EqualTo(new GridCoord(-1, -1)));

            // Around world origin (0,0): x=-0.1 is in cell X=7 ([-4, 0)), x=+0.1 is in cell X=8 ([0, 4))
            Assert.That(cfg.TryWorldToCell(-0.1, -0.1, out var negNearZero), Is.True);
            Assert.That(negNearZero, Is.EqualTo(new GridCoord(7, 5)));
            Assert.That(cfg.TryWorldToCell(0.1, 0.1, out var posNearZero), Is.True);
            Assert.That(posNearZero, Is.EqualTo(new GridCoord(8, 6)));

            // Round-trip from every cell center back to the same cell
            for (int z = 0; z < cfg.Height; z++)
            {
                for (int x = 0; x < cfg.Width; x++)
                {
                    var expected = new GridCoord(x, z);
                    cfg.CellToWorldCenter(expected, out double wx, out double wz);
                    Assert.That(cfg.TryWorldToCell(wx, wz, out var actual), Is.True);
                    Assert.That(actual, Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void CardinalNeighborsFollowFixedOrderAndBounds()
        {
            var cfg = ParcelMapConfig.Default;

            var corner = cfg.GetCardinalNeighbors(new GridCoord(0, 0)).ToArray();
            Assert.That(corner, Is.EqualTo(new[] { new GridCoord(0, 1), new GridCoord(1, 0) }));

            var center = cfg.GetCardinalNeighbors(new GridCoord(5, 5)).ToArray();
            Assert.That(center, Is.EqualTo(new[]
            {
                new GridCoord(5, 6), // N
                new GridCoord(6, 5), // E
                new GridCoord(5, 4), // S
                new GridCoord(4, 5)  // W
            }));
        }

        [Test]
        public void StarterFootprintsAndCorridorMatchWorldViewCoordinates()
        {
            var map = ParcelMap.Generate(ParcelMap.DeriveMapSeed(424242));
            var cfg = map.Config;

            // Map stays strictly inside WorldView terrain mesh x[-36,36], z[-28,44]
            Assert.That(cfg.OriginX, Is.GreaterThanOrEqualTo(-36.0));
            Assert.That(cfg.MaxWorldX, Is.LessThanOrEqualTo(36.0));
            Assert.That(cfg.OriginZ, Is.GreaterThanOrEqualTo(-28.0));
            Assert.That(cfg.MaxWorldZ, Is.LessThanOrEqualTo(44.0));

            // Habitat center (-2, 0) -> CoreHabitat
            Assert.That(cfg.TryWorldToCell(-2.0, 0.0, out var habCoord), Is.True);
            var habCell = map.GetCell(habCoord);
            Assert.That(habCell.Protection, Is.EqualTo(StarterProtectionKind.CoreHabitat));
            Assert.That(habCell.IsBuildable, Is.False);
            Assert.That(habCell.IsClaimed, Is.True);

            // Airlock outer (2.45, -0.35) -> AirlockCorridor (passable, claimed, protected from building)
            Assert.That(cfg.TryWorldToCell(2.45, -0.35, out var airlockCoord), Is.True);
            Assert.That(airlockCoord, Is.EqualTo(ParcelMap.AirlockCorridorCoord));
            var airlockCell = map.GetCell(airlockCoord);
            Assert.That(airlockCell.Protection, Is.EqualTo(StarterProtectionKind.AirlockCorridor));
            Assert.That(airlockCell.IsPassable, Is.True);
            Assert.That(airlockCell.IsBuildable, Is.False);
            Assert.That(airlockCell.IsClaimed, Is.True);

            // Solar array (4.65, 0.25) -> StarterSolar owning panel-1..3 with SolarFactor == 1.0
            Assert.That(cfg.TryWorldToCell(4.65, 0.25, out var solarCoord), Is.True);
            Assert.That(solarCoord, Is.EqualTo(ParcelMap.StarterSolarCoord));
            var solarCell = map.GetCell(solarCoord);
            Assert.That(solarCell.Protection, Is.EqualTo(StarterProtectionKind.StarterSolar));
            Assert.That(solarCell.SolarFactor, Is.EqualTo(1.0));
            Assert.That(solarCell.IsBuildable, Is.False);
            Assert.That(ParcelMap.StarterSolarSpec.OwnedPanelIds, Is.EqualTo(new[] { "panel-1", "panel-2", "panel-3" }));

            // Solar work pos (4.35, -1.15) -> Passable & Claimed work apron
            Assert.That(cfg.TryWorldToCell(4.35, -1.15, out var solarWorkCoord), Is.True);
            Assert.That(solarWorkCoord, Is.EqualTo(ParcelMap.SolarWorkCoord));
            Assert.That(map.GetCell(solarWorkCoord).IsPassable, Is.True);
            Assert.That(map.GetCell(solarWorkCoord).IsClaimed, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(101)]
        [TestCase(202)]
        [TestCase(303)]
        [TestCase(424242)]
        public void SameSeedIsDeterministicAndGuaranteesTwoReachableScannedIceSites(int operationsSeed)
        {
            int mapSeed = ParcelMap.DeriveMapSeed(operationsSeed);
            var mapA = ParcelMap.Generate(mapSeed);
            var mapB = ParcelMap.Generate(mapSeed);

            Assert.That(mapA.Cells.Count, Is.EqualTo(256));
            for (int i = 0; i < mapA.Cells.Count; i++)
            {
                var a = mapA.Cells[i];
                var b = mapB.Cells[i];
                Assert.That(a.Coord, Is.EqualTo(b.Coord));
                Assert.That(a.Terrain, Is.EqualTo(b.Terrain));
                Assert.That(a.SolarFactor, Is.EqualTo(b.SolarFactor));
                Assert.That(a.ResourceKind, Is.EqualTo(b.ResourceKind));
                Assert.That(a.RemainingWater, Is.EqualTo(b.RemainingWater));
                Assert.That(a.IsExplored, Is.EqualTo(b.IsExplored));
                Assert.That(a.IsScanned, Is.EqualTo(b.IsScanned));

                Assert.That(a.SolarFactor, Is.InRange(0.65, 1.0));
                if (a.ResourceKind != ParcelResourceKind.None)
                {
                    Assert.That(a.RemainingWater, Is.InRange(250.0, 600.0));
                    Assert.That(a.YieldFactor, Is.GreaterThan(0));
                }
            }

            // Reachability BFS from airlock over explored passable cells
            var visited = new HashSet<GridCoord> { ParcelMap.AirlockCorridorCoord };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(ParcelMap.AirlockCorridorCoord);
            int scannedIceCount = 0;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var curCell = mapA.GetCell(cur);
                if (curCell.IsExplored && curCell.IsScanned && curCell.IsBuildable &&
                    curCell.Protection == StarterProtectionKind.None && curCell.ResourceKind == ParcelResourceKind.Ice)
                {
                    scannedIceCount++;
                }

                foreach (var n in mapA.Config.GetCardinalNeighbors(cur))
                {
                    var nCell = mapA.GetCell(n);
                    if (nCell.IsPassable && nCell.IsExplored && visited.Add(n))
                        queue.Enqueue(n);
                }
            }

            Assert.That(scannedIceCount, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void MapGenerationAndCloneDoNotAffectOperationsEventChannels()
        {
            var cfg = MissionConfig.CreateOperationsDefault(operationsSeed: 7777, enableOperationsRandomEvents: true);
            var simA = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));
            var simB = new MissionSimulation(cfg, new MissionScenario(events: Array.Empty<EventState>()));

            // Generate map independently on separate stream
            var map = ParcelMap.Generate(ParcelMap.DeriveMapSeed(cfg.OperationsSeed));
            var clone = map.Clone();
            clone.GetCell(new GridCoord(0, 0)).IsClaimed = true;
            Assert.That(map.GetCell(new GridCoord(0, 0)).IsClaimed, Is.False);

            // Advance both simulations and verify all 5 Operations channels remain identical
            simA.Tick(300);
            simB.Tick(300);
            Assert.That(simA.State.ChannelSchedules.Count, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(simA.State.ChannelSchedules[i].RngState, Is.EqualTo(simB.State.ChannelSchedules[i].RngState));
                Assert.That(simA.State.ChannelSchedules[i].NextTriggerAtSeconds, Is.EqualTo(simB.State.ChannelSchedules[i].NextTriggerAtSeconds));
            }
        }
    }
}
