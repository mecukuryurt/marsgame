using NUnit.Framework;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests
{
    public class SimulationStepperTests
    {
        private static void Close(double actual, double expected) =>
            Assert.That(actual, Is.EqualTo(expected).Within(1e-7));

        [Test]
        public void MidFrameReportStopsQueuedStepsAndPreventsEightXJumpAfterResume()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var stepper = new SimulationStepper(sim);

            // Advance to t=480 (night) -> BeginSleep -> advance to t=714 (6s before Sol 1 end).
            sim.Tick(480);
            Assert.That(sim.BeginSleep().Success, Is.True);
            sim.Tick(234);
            Close(sim.State.ElapsedSeconds, 714);

            // Feed 2.0 real seconds (enough for 8 steps = 16s at 8x).
            // Must stop cleanly at step 3 (t=720) when SolReportPauseRequested triggers.
            int executed = stepper.AdvanceFrame(2.0);
            Assert.That(executed, Is.EqualTo(3));
            Close(sim.State.ElapsedSeconds, 720);
            Assert.That(sim.State.SolReportPauseRequested, Is.True);
            Assert.That(stepper.IsPaused, Is.True);
            Close(stepper.RealAccumulator, 0);

            // Dismiss report and feed 0.25s -> must advance only 0.25s at 1x, no leftover 8x jump.
            Assert.That(sim.AcknowledgeSolReport().Success, Is.True);
            Assert.That(stepper.IsPaused, Is.False);
            int nextExecuted = stepper.AdvanceFrame(0.25);
            Assert.That(nextExecuted, Is.EqualTo(1));
            Close(sim.State.ElapsedSeconds, 720.25);
        }

        [Test]
        public void MaxStepsCapRetainsUnpausedAccumulatorWithoutDroppingTime()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var stepper = new SimulationStepper(sim, stepSeconds: 0.25, maxStepsPerFrame: 4);

            int first = stepper.AdvanceFrame(1.5); // 6 steps worth, capped at 4 (1.0s)
            Assert.That(first, Is.EqualTo(4));
            Close(sim.State.ElapsedSeconds, 1.0);
            Close(stepper.RealAccumulator, 0.5);

            int second = stepper.AdvanceFrame(0.0); // drains remaining 2 steps (0.5s)
            Assert.That(second, Is.EqualTo(2));
            Close(sim.State.ElapsedSeconds, 1.5);
            Close(stepper.RealAccumulator, 0);
        }

        [Test]
        public void FocusLossAndPauseDoNotCatchUpRealTimeOrAutoResume()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var stepper = new SimulationStepper(sim);

            stepper.AdvanceFrame(1.0);
            Close(sim.State.ElapsedSeconds, 1.0);

            stepper.NotifyApplicationFocus(false);
            Assert.That(stepper.IsPaused, Is.True);
            Assert.That(stepper.AdvanceFrame(50.0), Is.EqualTo(0));
            Close(sim.State.ElapsedSeconds, 1.0);

            // Regaining focus does NOT auto-resume.
            stepper.NotifyApplicationFocus(true);
            Assert.That(stepper.IsPaused, Is.True);
            Assert.That(stepper.AdvanceFrame(1.0), Is.EqualTo(0));
            Close(sim.State.ElapsedSeconds, 1.0);

            // Manual resume advances only newly elapsed time, never the paused 51s.
            stepper.SetUserPaused(false);
            Assert.That(stepper.AdvanceFrame(0.5), Is.EqualTo(2));
            Close(sim.State.ElapsedSeconds, 1.5);
        }

        [Test]
        public void RestartClearsPauseFlagsAndAccumulator()
        {
            var sim = new MissionSimulation(new MissionConfig(), new MissionScenario());
            var stepper = new SimulationStepper(sim);
            stepper.AdvanceFrame(2.0);
            stepper.NotifyApplicationFocus(false);

            stepper.Restart();
            Assert.That(stepper.IsPaused, Is.False);
            Assert.That(stepper.UserPaused, Is.False);
            Assert.That(stepper.FocusPaused, Is.False);
            Close(stepper.RealAccumulator, 0);
            Close(sim.State.ElapsedSeconds, 0);
        }
    }
}
