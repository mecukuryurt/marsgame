using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsSurfaceMotionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SurfaceWalkAndReturnSmoothAcross15_30_60Fps_And_InteriorTaskWaitsForArrival()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);

            runner.InitializeOperationsMission();
            runner.Stepper.NotifyApplicationFocus(true);
            runner.SetPaused(false);
            world.SetGridVisible(true);

            // Dispatch crew-1 to a distant passable parcel at (11, 8)
            var res = runner.AssignSurfaceWork("crew-1", new GridCoord(11, 8), workRequiredSeconds: 15.0);
            Assert.That(res.Success, Is.True, res.Reason);
            Assert.That(runner.State.SurfaceJobs.Count, Is.EqualTo(1));
            Assert.That(runner.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Travel));

            var routeLine = GameObject.Find("ActiveSurfaceRouteLine");
            Assert.That(routeLine, Is.Not.Null);
            Assert.That(routeLine.activeInHierarchy, Is.True);

            var crew1 = GameObject.Find("Crew_crew-1").transform;
            var pivot1 = crew1.Find("VisualPivot");
            Assert.That(crew1, Is.Not.Null);
            Assert.That(pivot1, Is.Not.Null);

            // Mixed 15/30/60 FPS travel verification (single render owner, forward heading, no >0.55m jump)
            float[] frameDts = { 1f / 15f, 1f / 30f, 1f / 60f };
            double simulated = 0.0;
            Vector3 prevPos = crew1.position;
            float maxStepJump = 0f;
            int stepIdx = 0;

            while (simulated < 3.0)
            {
                float dt = frameDts[stepIdx % frameDts.Length];
                stepIdx++;
                runner.Stepper.AdvanceFrame(dt);
                world.ApplyState(runner.State);
                yield return null;
                simulated += dt;

                Vector3 curPos = crew1.position;
                Vector3 move = curPos - prevPos;
                float flatDist = new Vector2(move.x, move.z).magnitude;
                if (flatDist > maxStepJump) maxStepJump = flatDist;

                if (flatDist > 0.005f)
                {
                    Vector3 flatMove = new Vector3(move.x, 0f, move.z).normalized;
                    Vector3 flatFwd = new Vector3(pivot1.forward.x, 0f, pivot1.forward.z).normalized;
                    Assert.That(Vector3.Dot(flatMove, flatFwd), Is.GreaterThan(0.15f),
                        $"Astronaut walked backward or misaligned during surface travel: move={flatMove}, fwd={flatFwd}");
                }
                prevPos = curPos;
            }
            Assert.That(maxStepJump, Is.LessThan(0.55f), $"Teleport jump detected during surface travel: {maxStepJump:F3}m");

            // Recall/Return via route + airlock
            var retRes = runner.ReturnToShelter("crew-1");
            Assert.That(retRes.Success, Is.True, retRes.Reason);
            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Returning));
            Assert.That(runner.State.Crew[0].HasSurfacePosition, Is.True);

            // Interior task MUST reject until astronaut finishes airlock transition!
            var earlyInsideRes = runner.AssignTask("crew-1", "research");
            Assert.That(earlyInsideRes.Success, Is.False);

            prevPos = crew1.position;
            maxStepJump = 0f;
            int guard = 0;
            while (runner.State.Crew[0].Status == CrewStatus.Returning && guard++ < 600)
            {
                runner.Stepper.AdvanceFrame(1f / 30f);
                world.ApplyState(runner.State);
                yield return null;

                // Still returning -> interior task still blocked
                if (runner.State.Crew[0].Status == CrewStatus.Returning)
                    Assert.That(runner.AssignTask("crew-1", "research").Success, Is.False);

                Vector3 curPos = crew1.position;
                Vector3 move = curPos - prevPos;
                float flatDist = new Vector2(move.x, move.z).magnitude;
                if (flatDist > maxStepJump) maxStepJump = flatDist;
                if (flatDist > 0.005f)
                {
                    Vector3 flatMove = new Vector3(move.x, 0f, move.z).normalized;
                    Vector3 flatFwd = new Vector3(pivot1.forward.x, 0f, pivot1.forward.z).normalized;
                    Assert.That(Vector3.Dot(flatMove, flatFwd), Is.GreaterThan(0.15f),
                        $"Astronaut walked backward during airlock return: move={flatMove}, fwd={flatFwd}");
                }
                prevPos = curPos;
            }

            Assert.That(maxStepJump, Is.LessThan(0.55f), $"Teleport jump detected during return: {maxStepJump:F3}m");
            Assert.That(runner.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(runner.State.Crew[0].HasSurfacePosition, Is.False);

            // Now arrived inside -> interior research task starts cleanly!
            var insideRes = runner.AssignTask("crew-1", "research");
            Assert.That(insideRes.Success, Is.True, insideRes.Reason);
            yield return null;
            Assert.That(Vector3.Dot(pivot1.up, Vector3.up), Is.GreaterThan(0.95f));
        }

        [UnityTest]
        public IEnumerator SurfaceWalkRespectsPauseAnd8xSpeed_AndCancelReleasesJob()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.InitializeOperationsMission();
            runner.Stepper.NotifyApplicationFocus(true);
            runner.SetPaused(false);

            Assert.That(runner.AssignSurfaceWork("crew-1", new GridCoord(12, 6), workRequiredSeconds: 15.0).Success, Is.True);
            var crew1 = GameObject.Find("Crew_crew-1").transform;

            // Advance slightly into Travel
            for (int i = 0; i < 6; i++)
            {
                runner.Stepper.AdvanceFrame(0.25);
                world.ApplyState(runner.State);
                yield return null;
            }
            Assert.That(runner.State.SurfaceJobs[0].Phase, Is.EqualTo(SurfaceJobPhase.Travel));

            // Pause freezes both visual position and domain route progress
            runner.SetPaused(true);
            Vector3 pausedPos = crew1.position;
            double pausedLeg = runner.State.SurfaceJobs[0].LegDistanceTraveled;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Vector3.Distance(crew1.position, pausedPos), Is.LessThan(0.001f));
            Assert.That(runner.State.SurfaceJobs[0].LegDistanceTraveled, Is.EqualTo(pausedLeg));

            // Resume at 8x speed (advance 0.25s per frame = 8x of 30fps) -> continues monotonically
            runner.SetPaused(false);
            double distBefore8x = runner.State.SurfaceJobs[0].LegDistanceTraveled;
            for (int i = 0; i < 8; i++)
            {
                runner.Stepper.AdvanceFrame(0.25);
                world.ApplyState(runner.State);
                yield return null;
            }
            Assert.That(runner.State.SurfaceJobs[0].LegDistanceTraveled, Is.GreaterThan(distBefore8x));
            Assert.That(Vector2.Distance(new Vector2(crew1.position.x, crew1.position.z), new Vector2(pausedPos.x, pausedPos.z)), Is.GreaterThan(0.5f));

            // CancelTask cleanly releases SurfaceJob
            var cancelRes = runner.CancelTask("crew-1");
            Assert.That(cancelRes.Success, Is.True, cancelRes.Reason);
            Assert.That(runner.State.Crew[0].ActiveSurfaceJobId, Is.Null);
            Assert.That(runner.State.SurfaceJobs[0].AssignedCrewId, Is.Null);
        }

        [UnityTest]
        public IEnumerator CameraPansAcrossFullOperationsGridWithoutChangingLockedPitchOrYaw()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var cam = Object.FindFirstObjectByType<MarsOutpostCameraController>();
            Assert.That(cam, Is.Not.Null);

            // Pan to far negative & positive grid edges while keeping locked 43 deg pitch / 0 yaw
            cam.FocusWorldPoint(new Vector3(-30f, 0.2f, -22f), immediate: true);
            Assert.That(cam.CurrentPivot.x, Is.LessThanOrEqualTo(-29.5f));
            Assert.That(cam.CurrentPivot.z, Is.LessThanOrEqualTo(-21.5f));
            Assert.That(cam.CurrentPitch, Is.EqualTo(MarsOutpostCameraController.DefaultPitch).Within(0.01f));
            Assert.That(cam.CurrentYaw, Is.EqualTo(MarsOutpostCameraController.DefaultYaw).Within(0.01f));

            cam.FocusWorldPoint(new Vector3(30f, 0.2f, 38f), immediate: true);
            Assert.That(cam.CurrentPivot.x, Is.GreaterThanOrEqualTo(29.5f));
            Assert.That(cam.CurrentPivot.z, Is.GreaterThanOrEqualTo(37.5f));
            Assert.That(cam.CurrentPitch, Is.EqualTo(MarsOutpostCameraController.DefaultPitch).Within(0.01f));

            cam.ResetCameraView();
            cam.SetCameraPose(MarsOutpostCameraController.DefaultPivot, MarsOutpostCameraController.DefaultDistance, MarsOutpostCameraController.DefaultPitch, MarsOutpostCameraController.DefaultYaw, immediate: true);
            Assert.That(Vector3.Distance(cam.CurrentPivot, MarsOutpostCameraController.DefaultPivot), Is.LessThan(0.05f));
        }
    }
}
