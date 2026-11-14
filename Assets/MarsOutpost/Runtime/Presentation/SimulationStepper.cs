using System;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Deterministic fixed-step simulation stepper with pause/focus isolation and mid-frame interruption.
    /// </summary>
    public sealed class SimulationStepper
    {
        public const double DefaultStepSeconds = 0.25;
        public const int DefaultMaxStepsPerFrame = 16;

        public MissionSimulation Simulation { get; }
        public double StepSeconds { get; }
        public int MaxStepsPerFrame { get; }
        public bool UserPaused { get; private set; }
        public bool FocusPaused { get; private set; }
        public double RealAccumulator => realAccumulator;

        private double realAccumulator;

        public bool IsPaused =>
            UserPaused ||
            FocusPaused ||
            Simulation.State.PauseRequested ||
            Simulation.State.Outcome != MissionOutcome.InProgress;

        public double EffectiveTimeScale =>
            IsPaused ? 0.0 : Simulation.State.RecommendedTimeScale;

        public SimulationStepper(
            MissionSimulation simulation,
            double stepSeconds = DefaultStepSeconds,
            int maxStepsPerFrame = DefaultMaxStepsPerFrame)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            if (double.IsNaN(stepSeconds) || double.IsInfinity(stepSeconds) || stepSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            if (maxStepsPerFrame <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStepsPerFrame));

            StepSeconds = stepSeconds;
            MaxStepsPerFrame = maxStepsPerFrame;
        }

        public void SetUserPaused(bool paused)
        {
            UserPaused = paused;
            if (paused)
                realAccumulator = 0;
        }

        public void ToggleUserPause()
        {
            SetUserPaused(!UserPaused);
        }

        public void NotifyApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FocusPaused = true;
                UserPaused = true;
                realAccumulator = 0;
            }
            else
            {
                // Returning focus clears the focus flag, but never auto-resumes UserPaused.
                FocusPaused = false;
            }
        }

        public void Restart()
        {
            Simulation.Restart();
            UserPaused = false;
            FocusPaused = false;
            realAccumulator = 0;
        }

        public int AdvanceFrame(double realDeltaSeconds)
        {
            if (double.IsNaN(realDeltaSeconds) || double.IsInfinity(realDeltaSeconds) || realDeltaSeconds < 0)
                return 0;

            if (IsPaused)
            {
                realAccumulator = 0;
                return 0;
            }

            realAccumulator += realDeltaSeconds;
            int executed = 0;

            while (executed < MaxStepsPerFrame && realAccumulator >= StepSeconds)
            {
                if (IsPaused)
                {
                    realAccumulator = 0;
                    break;
                }

                double scale = Simulation.State.RecommendedTimeScale;
                realAccumulator -= StepSeconds;
                Simulation.Tick(StepSeconds * scale);
                executed++;

                if (Simulation.State.PauseRequested || Simulation.State.Outcome != MissionOutcome.InProgress)
                {
                    // Drop leftover queue so pre-report 8x time cannot jump after resuming.
                    realAccumulator = 0;
                    break;
                }
            }

            return executed;
        }
    }
}
