using System;
using System.Collections.Generic;

namespace MarsOutpost
{
    public enum EarthOrderKind
    {
        SoilSample = 0,               // EARTH-01
        OxygenExperiment = 1,         // EARTH-02
        RadiationSensorCalibration = 2, // EARTH-03
        ThermalInsulationCheck = 3,   // EARTH-04
        WaterQualityAnalysis = 4,     // EARTH-05
        AntennaAlignment = 5,         // EARTH-06
        EmergencyStockAudit = 6       // EARTH-07
    }

    public enum EarthOrderStatus
    {
        Received = 0,
        Accepted = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public readonly struct EarthOrderStepSpec
    {
        public CrewLocation Location { get; }
        public double DurationSeconds { get; }
        public double StaminaRate { get; }
        public double EnergyRate { get; }
        public double UpfrontWaterCost { get; }
        public bool UsesOxygenSplit { get; }
        public string StepLabel { get; }

        public EarthOrderStepSpec(
            CrewLocation location,
            double durationSeconds,
            double staminaRate,
            double energyRate = 0,
            double upfrontWaterCost = 0,
            bool usesOxygenSplit = false,
            string stepLabel = "")
        {
            Location = location;
            DurationSeconds = durationSeconds;
            StaminaRate = staminaRate;
            EnergyRate = energyRate;
            UpfrontWaterCost = upfrontWaterCost;
            UsesOxygenSplit = usesOxygenSplit;
            StepLabel = stepLabel ?? "";
        }
    }

    public sealed class EarthOrderState
    {
        public int StepCount => EarthOrderBagState.GetStepCount(Kind);
        public EarthOrderStepSpec CurrentStep => EarthOrderBagState.GetStepSpec(Kind, CurrentStepIndex);
        public string StationTaskId => CurrentStep.Location == CrewLocation.Outside ? null
            : Kind == EarthOrderKind.EmergencyStockAudit ? "meal-prep"
            : Kind == EarthOrderKind.SoilSample || Kind == EarthOrderKind.OxygenExperiment || Kind == EarthOrderKind.WaterQualityAnalysis
                ? "research" : "system-maintenance";
        public string OrderId { get; }
        public EarthOrderKind Kind { get; }
        public string Code { get; }
        public string Title { get; }
        public EarthOrderStatus Status { get; internal set; }
        public long ArrivalSol { get; }
        public long AdvisoryDeadlineSol { get; }
        public int CurrentStepIndex { get; internal set; }
        public double StepProgressSeconds { get; internal set; }
        public double StepRequiredSeconds { get; internal set; }
        public string AssignedCrewId { get; internal set; }
        public string CompletionSummary { get; internal set; }
        public bool UpfrontWaterPaid { get; internal set; }

        public EarthOrderState(
            string orderId,
            EarthOrderKind kind,
            string code,
            string title,
            EarthOrderStatus status,
            long arrivalSol,
            long advisoryDeadlineSol,
            int currentStepIndex = 0,
            double stepProgressSeconds = 0,
            double stepRequiredSeconds = 30,
            string assignedCrewId = null,
            string completionSummary = "",
            bool upfrontWaterPaid = false)
        {
            OrderId = orderId;
            Kind = kind;
            Code = code;
            Title = title;
            Status = status;
            ArrivalSol = arrivalSol;
            AdvisoryDeadlineSol = advisoryDeadlineSol;
            CurrentStepIndex = currentStepIndex;
            StepProgressSeconds = stepProgressSeconds;
            StepRequiredSeconds = stepRequiredSeconds;
            AssignedCrewId = assignedCrewId;
            CompletionSummary = completionSummary ?? "";
            UpfrontWaterPaid = upfrontWaterPaid;
        }

        public EarthOrderState Clone()
        {
            return new EarthOrderState(
                OrderId,
                Kind,
                Code,
                Title,
                Status,
                ArrivalSol,
                AdvisoryDeadlineSol,
                CurrentStepIndex,
                StepProgressSeconds,
                StepRequiredSeconds,
                AssignedCrewId,
                CompletionSummary,
                UpfrontWaterPaid);
        }
    }

    internal sealed class EarthOrderBagState
    {
        private static readonly EarthOrderKind[] AllKinds =
        {
            EarthOrderKind.SoilSample,
            EarthOrderKind.OxygenExperiment,
            EarthOrderKind.RadiationSensorCalibration,
            EarthOrderKind.ThermalInsulationCheck,
            EarthOrderKind.WaterQualityAnalysis,
            EarthOrderKind.AntennaAlignment,
            EarthOrderKind.EmergencyStockAudit
        };

        public ulong RngState;
        public List<EarthOrderKind> RemainingBag;
        public int IssuedCount;

        public EarthOrderBagState(int seed)
        {
            var rng = new DeterministicRng(seed, 101UL);
            RngState = rng.State;
            RemainingBag = new List<EarthOrderKind>(AllKinds.Length);
            IssuedCount = 0;
        }

        private EarthOrderBagState() { }

        internal EarthOrderBagState(ulong rngState, List<EarthOrderKind> remainingBag, int issuedCount)
        {
            RngState = rngState;
            RemainingBag = remainingBag ?? new List<EarthOrderKind>();
            IssuedCount = issuedCount;
        }

        public EarthOrderBagState Clone()
        {
            return new EarthOrderBagState
            {
                RngState = RngState,
                RemainingBag = RemainingBag != null ? new List<EarthOrderKind>(RemainingBag) : new List<EarthOrderKind>(),
                IssuedCount = IssuedCount
            };
        }

        public EarthOrderState DrawNextOrder(long arrivalSol)
        {
            if (RemainingBag == null || RemainingBag.Count == 0)
            {
                RemainingBag = new List<EarthOrderKind>(AllKinds);
                var rng = new DeterministicRng(RngState);
                for (int i = RemainingBag.Count - 1; i > 0; i--)
                {
                    int j = rng.NextInt(0, i + 1);
                    EarthOrderKind tmp = RemainingBag[i];
                    RemainingBag[i] = RemainingBag[j];
                    RemainingBag[j] = tmp;
                }
                RngState = rng.State;
            }

            int lastIdx = RemainingBag.Count - 1;
            EarthOrderKind kind = RemainingBag[lastIdx];
            RemainingBag.RemoveAt(lastIdx);
            IssuedCount++;

            GetOrderMetadata(kind, out string code, out string title, out double initialStepSeconds);
            string orderId = $"CMD-S{arrivalSol}-{IssuedCount:00}";
            return new EarthOrderState(
                orderId,
                kind,
                code,
                title,
                EarthOrderStatus.Received,
                arrivalSol,
                arrivalSol + 2,
                0,
                0,
                initialStepSeconds);
        }

        public static int GetStepCount(EarthOrderKind kind)
        {
            switch (kind)
            {
                case EarthOrderKind.SoilSample:
                case EarthOrderKind.RadiationSensorCalibration:
                case EarthOrderKind.ThermalInsulationCheck:
                case EarthOrderKind.AntennaAlignment:
                case EarthOrderKind.EmergencyStockAudit:
                    return 2;
                default:
                    return 1;
            }
        }

        public static EarthOrderStepSpec GetStepSpec(EarthOrderKind kind, int stepIndex)
        {
            switch (kind)
            {
                case EarthOrderKind.SoilSample:
                    return stepIndex == 0
                        ? new EarthOrderStepSpec(CrewLocation.Outside, 60, -0.25, stepLabel: "Exterior soil collection")
                        : new EarthOrderStepSpec(CrewLocation.Inside, 60, -0.15, stepLabel: "Interior lab analysis & report");
                case EarthOrderKind.OxygenExperiment:
                    return new EarthOrderStepSpec(CrewLocation.Inside, 60, -0.15, usesOxygenSplit: true, stepLabel: "Interior O2 lab experiment");
                case EarthOrderKind.RadiationSensorCalibration:
                    return stepIndex == 0
                        ? new EarthOrderStepSpec(CrewLocation.Outside, 60, -0.25, stepLabel: "Exterior radiation sensor calibration")
                        : new EarthOrderStepSpec(CrewLocation.Inside, 20, -0.15, stepLabel: "Interior terminal calibration report");
                case EarthOrderKind.ThermalInsulationCheck:
                    return stepIndex == 0
                        ? new EarthOrderStepSpec(CrewLocation.Outside, 60, -0.25, energyRate: 3.0 / 90.0, stepLabel: "Exterior thermal insulation scan")
                        : new EarthOrderStepSpec(CrewLocation.Inside, 30, -0.15, energyRate: 3.0 / 90.0, stepLabel: "Interior thermal log & report");
                case EarthOrderKind.WaterQualityAnalysis:
                    return new EarthOrderStepSpec(CrewLocation.Inside, 60, -0.15, energyRate: 2.0 / 60.0, upfrontWaterCost: 2.0, stepLabel: "Interior water quality analysis & report");
                case EarthOrderKind.AntennaAlignment:
                    return stepIndex == 0
                        ? new EarthOrderStepSpec(CrewLocation.Outside, 60, -0.25, energyRate: 5.0 / 60.0, stepLabel: "Exterior antenna alignment & calibration")
                        : new EarthOrderStepSpec(CrewLocation.Inside, 30, -0.15, stepLabel: "Interior link test & report");
                case EarthOrderKind.EmergencyStockAudit:
                    return stepIndex == 0
                        ? new EarthOrderStepSpec(CrewLocation.Inside, 60, -0.15, stepLabel: "Interior emergency stock & supply count")
                        : new EarthOrderStepSpec(CrewLocation.Inside, 30, -0.15, stepLabel: "Interior emergency procedure drill & report");
                default:
                    GetOrderMetadata(kind, out _, out _, out double dur);
                    return new EarthOrderStepSpec(CrewLocation.Inside, dur, -0.15, stepLabel: "Order execution");
            }
        }

        public static void GetOrderMetadata(EarthOrderKind kind, out string code, out string title, out double initialStepSeconds)
        {
            switch (kind)
            {
                case EarthOrderKind.SoilSample:
                    code = "EARTH-01";
                    title = "Collect and analyze soil sample";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.OxygenExperiment:
                    code = "EARTH-02";
                    title = "Special oxygen production experiment";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.RadiationSensorCalibration:
                    code = "EARTH-03";
                    title = "Calibrate exterior radiation sensor";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.ThermalInsulationCheck:
                    code = "EARTH-04";
                    title = "Inspect habitat thermal insulation";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.WaterQualityAnalysis:
                    code = "EARTH-05";
                    title = "Analyze water quality sample";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.AntennaAlignment:
                    code = "EARTH-06";
                    title = "Align communications antenna";
                    initialStepSeconds = 60;
                    return;
                case EarthOrderKind.EmergencyStockAudit:
                    code = "EARTH-07";
                    title = "Audit emergency stock and procedures";
                    initialStepSeconds = 60;
                    return;
                default:
                    code = "EARTH-00";
                    title = "Earth Operations Order";
                    initialStepSeconds = 30;
                    return;
            }
        }
    }
}
