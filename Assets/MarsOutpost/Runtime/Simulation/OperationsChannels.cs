using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MarsOutpost.Tests.EditMode")]

namespace MarsOutpost
{
    public enum OperationsChannelId
    {
        Radiation = 0,
        SolarPanel = 1,
        OxygenProducer = 2,
        DustStorm = 3,
        WaterExtractor = 4
    }

    public sealed class PanelUnitState
    {
        public const double DefaultUnitWeight = 1.0 / 3.0;
        public const double DefaultRepairSeconds = 60.0;

        public string Id { get; }
        public double Weight { get; }
        public bool IsFaulted { get; internal set; }
        public string FacilityId { get; internal set; }
        public double SolarFactor { get; internal set; }
        public double RepairProgressSeconds { get; internal set; }

        public PanelUnitState(
            string id,
            double weight,
            bool isFaulted = false,
            string facilityId = null,
            double solarFactor = 1.0,
            double repairProgressSeconds = 0)
        {
            Id = id;
            Weight = weight;
            IsFaulted = isFaulted;
            FacilityId = facilityId;
            SolarFactor = solarFactor >= 0 ? solarFactor : 1.0;
            RepairProgressSeconds = repairProgressSeconds >= 0 ? repairProgressSeconds : 0;
        }

        public PanelUnitState Clone() => new PanelUnitState(Id, Weight, IsFaulted, FacilityId, SolarFactor, RepairProgressSeconds);
    }

    public struct DeterministicRng
    {
        public ulong State { get; private set; }

        public DeterministicRng(int seed, ulong streamId)
        {
            ulong x = (uint)seed ^ (0x9E3779B97F4A7C15UL * (streamId + 1UL));
            // SplitMix64 warm-up so seed=0 and nearby seeds diverge cleanly.
            State = Mix64(x);
            if (State == 0) State = 0xD6E8FEB86659FD93UL;
        }

        public DeterministicRng(ulong state)
        {
            State = state == 0 ? 0xD6E8FEB86659FD93UL : state;
        }

        public ulong NextUInt64()
        {
            ulong z = (State += 0x9E3779B97F4A7C15UL);
            return Mix64(z);
        }

        public double NextDouble()
        {
            // 53-bit uniform double in [0, 1).
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        public double NextRange(double minInclusive, double maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (maxExclusive - minInclusive) * NextDouble();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong span = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt64() % span);
        }

        private static ulong Mix64(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public sealed class ChannelScheduleState
    {
        public OperationsChannelId ChannelId { get; }
        public ulong RngState { get; internal set; }
        public double NextTriggerAtSeconds { get; internal set; }
        public double WarningLeadSeconds { get; }
        public bool WarningIssued { get; internal set; }
        public bool IsActive { get; internal set; }
        public double ActiveUntilSeconds { get; internal set; }
        public string ActiveTargetId { get; internal set; }

        public ChannelScheduleState(
            OperationsChannelId channelId,
            ulong rngState,
            double nextTriggerAtSeconds,
            double warningLeadSeconds,
            bool warningIssued = false,
            bool isActive = false,
            double activeUntilSeconds = double.PositiveInfinity,
            string activeTargetId = null)
        {
            ChannelId = channelId;
            RngState = rngState;
            NextTriggerAtSeconds = nextTriggerAtSeconds;
            WarningLeadSeconds = warningLeadSeconds;
            WarningIssued = warningIssued;
            IsActive = isActive;
            ActiveUntilSeconds = activeUntilSeconds;
            ActiveTargetId = activeTargetId;
        }

        public ChannelScheduleState Clone()
        {
            return new ChannelScheduleState(
                ChannelId,
                RngState,
                NextTriggerAtSeconds,
                WarningLeadSeconds,
                WarningIssued,
                IsActive,
                ActiveUntilSeconds,
                ActiveTargetId);
        }

        internal double NextBoundarySeconds()
        {
            double next = double.PositiveInfinity;
            if (!IsActive && !double.IsPositiveInfinity(NextTriggerAtSeconds))
            {
                if (WarningLeadSeconds > 0 && !WarningIssued)
                {
                    double warnAt = Math.Max(0, NextTriggerAtSeconds - WarningLeadSeconds);
                    if (warnAt < next) next = warnAt;
                }
                if (NextTriggerAtSeconds < next) next = NextTriggerAtSeconds;
            }
            if (IsActive && !double.IsPositiveInfinity(ActiveUntilSeconds))
            {
                if (ActiveUntilSeconds < next) next = ActiveUntilSeconds;
            }
            return next;
        }
    }

    internal static class OperationsScheduler
    {
        public static readonly OperationsChannelId[] AllChannels =
        {
            OperationsChannelId.Radiation,
            OperationsChannelId.SolarPanel,
            OperationsChannelId.OxygenProducer,
            OperationsChannelId.DustStorm,
            OperationsChannelId.WaterExtractor
        };

        public static List<ChannelScheduleState> CreateInitialChannels(int seed)
        {
            var list = new List<ChannelScheduleState>(AllChannels.Length);
            for (int i = 0; i < AllChannels.Length; i++)
            {
                var ch = AllChannels[i];
                var rng = new DeterministicRng(seed, (ulong)ch + 1UL);
                double wait = SampleWaitSeconds(ch, ref rng);
                double lead = GetWarningLeadSeconds(ch);
                list.Add(new ChannelScheduleState(ch, rng.State, wait, lead));
            }
            return list;
        }

        public static void GetWaitBounds(OperationsChannelId channel, out double minSeconds, out double maxSeconds)
        {
            switch (channel)
            {
                case OperationsChannelId.Radiation:
                    minSeconds = 900; maxSeconds = 1200; return;
                case OperationsChannelId.SolarPanel:
                    minSeconds = 1200; maxSeconds = 1800; return;
                case OperationsChannelId.OxygenProducer:
                    minSeconds = 1200; maxSeconds = 1800; return;
                case OperationsChannelId.DustStorm:
                    minSeconds = 600; maxSeconds = 900; return;
                case OperationsChannelId.WaterExtractor:
                    minSeconds = 1200; maxSeconds = 1500; return;
                default:
                    minSeconds = 900; maxSeconds = 1200; return;
            }
        }

        public static double GetWarningLeadSeconds(OperationsChannelId channel)
        {
            return (channel == OperationsChannelId.Radiation || channel == OperationsChannelId.DustStorm) ? 60 : 0;
        }

        public static double SampleWaitSeconds(OperationsChannelId channel, ref DeterministicRng rng)
        {
            GetWaitBounds(channel, out double min, out double max);
            return rng.NextRange(min, max);
        }

        public static double SampleActiveDurationSeconds(OperationsChannelId channel, ref DeterministicRng rng)
        {
            switch (channel)
            {
                case OperationsChannelId.Radiation:
                    return rng.NextRange(60, 120);
                case OperationsChannelId.DustStorm:
                    return rng.NextRange(60, 180);
                default:
                    return double.PositiveInfinity;
            }
        }

        public static bool IsTimedChannel(OperationsChannelId channel)
        {
            return channel == OperationsChannelId.Radiation || channel == OperationsChannelId.DustStorm;
        }

        public static double GetNextChannelBoundary(IReadOnlyList<ChannelScheduleState> channels)
        {
            if (channels == null) return double.PositiveInfinity;
            double next = double.PositiveInfinity;
            for (int i = 0; i < channels.Count; i++)
            {
                double b = channels[i].NextBoundarySeconds();
                if (b < next) next = b;
            }
            return next;
        }

        public static void RescheduleAfterResolution(ChannelScheduleState ch, double currentElapsed)
        {
            var rng = new DeterministicRng(ch.RngState);
            double wait = SampleWaitSeconds(ch.ChannelId, ref rng);
            ch.RngState = rng.State;
            ch.IsActive = false;
            ch.WarningIssued = false;
            ch.ActiveUntilSeconds = double.PositiveInfinity;
            ch.ActiveTargetId = null;
            ch.NextTriggerAtSeconds = currentElapsed + wait;
        }
    }
}
