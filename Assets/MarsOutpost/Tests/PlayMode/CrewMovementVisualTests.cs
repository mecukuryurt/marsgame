using System.Collections;
using MarsOutpost.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MarsOutpost.Tests.PlayMode
{
    public class CrewMovementVisualTests
    {
        [UnityTest]
        public IEnumerator WalkingStopsWhilePausedAndReturnFollowsCountdown()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.InitializeDemoMission();
            runner.AssignTask("crew-1", "panel-maintenance");
            runner.SetPaused(true);
            var crew = GameObject.Find("Crew_crew-1").transform;
            Vector3 pausedAt = crew.position;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Vector3.Distance(crew.position, pausedAt), Is.LessThan(0.001f));

            // Move visually outside, then return. The domain's four-second demo
            // countdown, not a fixed walking speed, determines route progress.
            runner.SetPaused(false);
            yield return new WaitForSecondsRealtime(3f);
            runner.ReturnToShelter("crew-1");
            Assert.That(runner.State.Crew[0].Status, Is.EqualTo(CrewStatus.Returning));
            Vector3 start = crew.position;
            runner.Stepper.AdvanceFrame(2.0);
            world.ApplyState(runner.State);
            Vector3 halfway = crew.position;
            Assert.That(Vector3.Distance(start, halfway), Is.GreaterThan(0.1f));
            Assert.That(runner.State.Crew[0].ReturnRemainingSeconds, Is.GreaterThan(0));
            runner.Stepper.AdvanceFrame(2.0);
            world.ApplyState(runner.State);
            Assert.That(runner.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Assert.That(Vector3.Distance(halfway, crew.position), Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator SleepingAndRestingAlignAlongBunkAnchorsAndWakeCleanly()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            runner.Stepper.NotifyApplicationFocus(true);
            runner.SetPaused(false);

            for (int b = 0; b < 3; b++)
            {
                Assert.That(GameObject.Find($"SleepAnchor_{b}"), Is.Not.Null, $"SleepAnchor_{b} missing");
                Assert.That(GameObject.Find($"BunkFrame_{b}"), Is.Not.Null, $"BunkFrame_{b} missing");
            }

            // 1. Spend enough stamina so crew-1 stays Resting while walking to bunk and lying down
            runner.AssignTask("crew-1", "research");
            runner.Stepper.Simulation.Tick(120.0);
            world.ApplyState(runner.State);
            var restRes = runner.Rest("crew-1");
            Assert.That(restRes.Success, Is.True, restRes.Reason);

            var crew1 = GameObject.Find("Crew_crew-1").transform;
            var pivot1 = crew1.Find("VisualPivot");
            var anchor0 = GameObject.Find("SleepAnchor_0").transform;

            float deadline = Time.realtimeSinceStartup + 5.0f;
            while (Time.realtimeSinceStartup < deadline && Vector3.Dot(pivot1.up, anchor0.forward) < 0.98f)
                yield return null;

            var bunk0 = GameObject.Find("BunkFrame_0").GetComponent<Renderer>().bounds;
            var torso1 = pivot1.Find("Torso").GetComponent<Renderer>().bounds;
            var helmet1 = pivot1.Find("HeadPivot/Helmet").GetComponent<Renderer>().bounds;
            var ring1 = crew1.Find("SelectionRing");
            var name1 = crew1.Find("NameLabel");

            // Head (+Y of VisualPivot) points along anchor.forward (towards pillow -Z), visor (+Z) points up (+Y)
            Assert.That(Vector3.Dot(pivot1.up, anchor0.forward), Is.GreaterThan(0.95f),
                $"Status={runner.State.Crew[0].Status}, pos={crew1.position}, pivotUp={pivot1.up}, anchorFwd={anchor0.forward}, isPaused={runner.Stepper.IsPaused}");
            Assert.That(Vector3.Dot(pivot1.forward, anchor0.up), Is.GreaterThan(0.95f));

            // Torso stays strictly inside its bunk footprint; helmet stays in bunk X range
            Assert.That(torso1.min.x, Is.GreaterThanOrEqualTo(bunk0.min.x - 0.05f));
            Assert.That(torso1.max.x, Is.LessThanOrEqualTo(bunk0.max.x + 0.05f));
            Assert.That(torso1.min.z, Is.GreaterThanOrEqualTo(bunk0.min.z - 0.05f));
            Assert.That(torso1.max.z, Is.LessThanOrEqualTo(bunk0.max.z + 0.05f));
            Assert.That(helmet1.center.x, Is.InRange(bunk0.min.x, bunk0.max.x));
            Assert.That(helmet1.center.z, Is.InRange(bunk0.min.z - 0.08f, bunk0.max.z));

            // Selection ring stays flat on ground, name label hovers above sleeping helmet
            Assert.That(Vector3.Dot(ring1.up, Vector3.up), Is.GreaterThan(0.99f));
            Assert.That(ring1.position.y, Is.LessThan(0.1f));
            Assert.That(name1.position.y, Is.GreaterThan(helmet1.max.y));

            // 2. All three crew sleeping together at 8x (advance to nighttime first)
            while (!runner.State.IsNight)
                runner.Stepper.Simulation.Tick(20.0);
            world.ApplyState(runner.State);
            var sleepRes = runner.BeginSleep();
            Assert.That(sleepRes.Success, Is.True, sleepRes.Reason);
            deadline = Time.realtimeSinceStartup + 5.0f;
            while (Time.realtimeSinceStartup < deadline)
            {
                bool allAligned = true;
                for (int i = 0; i < 3; i++)
                {
                    var p = GameObject.Find($"Crew_crew-{i + 1}").transform.Find("VisualPivot");
                    var a = GameObject.Find($"SleepAnchor_{i}").transform;
                    if (Vector3.Dot(p.up, a.forward) < 0.98f) { allAligned = false; break; }
                }
                if (allAligned) break;
                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                string id = $"crew-{i + 1}";
                var pivot = GameObject.Find($"Crew_{id}").transform.Find("VisualPivot");
                var anchor = GameObject.Find($"SleepAnchor_{i}").transform;
                var bunk = GameObject.Find($"BunkFrame_{i}").GetComponent<Renderer>().bounds;
                var torso = pivot.Find("Torso").GetComponent<Renderer>().bounds;
                Assert.That(Vector3.Dot(pivot.up, anchor.forward), Is.GreaterThan(0.95f), $"{id} not aligned with bunk {i}");
                Assert.That(torso.center.x, Is.InRange(bunk.min.x, bunk.max.x), $"{id} torso outside bunk {i}");
            }

            string reviewCapture = System.Environment.GetEnvironmentVariable("MARS_REVIEW_SLEEP_CAPTURE");
            if (System.Environment.GetEnvironmentVariable("MARS_CAPTURE_CP01") == "1" || !string.IsNullOrEmpty(reviewCapture))
            {
                var cam = Camera.main;
                var rt = new RenderTexture(1440, 900, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1440, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
                tex.Apply();
                cam.targetTexture = prev;
                RenderTexture.active = null;
                Object.Destroy(rt);
                System.IO.File.WriteAllBytes(string.IsNullOrEmpty(reviewCapture)
                    ? "/run/media/senesatka/storage/projects/marsgame/docs/validation/v2-cp01-sleep.png" : reviewCapture, tex.EncodeToPNG());
                Object.Destroy(tex);
            }

            // 3. Pause freezes pose; waking and assigning work restores clean upright orientation
            runner.SetPaused(true);
            runner.CancelSleep();
            Quaternion pausedRot = pivot1.localRotation;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Quaternion.Angle(pivot1.localRotation, pausedRot), Is.LessThan(0.5f));

            runner.SetPaused(false);
            runner.AssignTask("crew-1", "research");
            deadline = Time.realtimeSinceStartup + 5.0f;
            while (Time.realtimeSinceStartup < deadline && Vector3.Dot(pivot1.up, Vector3.up) < 0.98f)
                yield return null;
            Assert.That(Vector3.Dot(pivot1.up, Vector3.up), Is.GreaterThan(0.95f));
        }

        [UnityTest]
        public IEnumerator ReturningFollowsAirlockTangentSmoothlyWithoutBackwardWalkOrSnap()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.Stepper.NotifyApplicationFocus(true);
            runner.SetPaused(false);

            // Send crew-1 outside to solar array and move visually to exterior position
            runner.AssignTask("crew-1", "panel-maintenance");
            var crew1 = GameObject.Find("Crew_crew-1").transform;
            var pivot1 = crew1.Find("VisualPivot");
            float deadline = Time.realtimeSinceStartup + 6.0f;
            while (Time.realtimeSinceStartup < deadline && crew1.position.x < 3.5f)
                yield return null;
            Assert.That(crew1.position.x, Is.GreaterThan(3.5f));

            // Trigger 10s standard return
            var ret = runner.ReturnToShelter("crew-1");
            Assert.That(ret.Success, Is.True, ret.Reason);
            Assert.That(runner.State.Crew[0].ReturnRemainingSeconds, Is.EqualTo(10.0).Within(0.01));

            // Step through first half (5s) at mixed frame rates (15/30/60 FPS)
            float[] frameDts = { 1f / 15f, 1f / 30f, 1f / 60f };
            double simulated = 0.0;
            Vector3 prevPos = crew1.position;
            float maxStepJump = 0f;
            int stepIdx = 0;

            while (simulated < 5.0)
            {
                float dt = frameDts[stepIdx % frameDts.Length];
                stepIdx++;
                runner.Stepper.AdvanceFrame(dt);
                world.ApplyState(runner.State);
                yield return null;
                simulated += dt;

                Vector3 curPos = crew1.position;
                float jump = Vector3.Distance(prevPos, curPos);
                if (jump > maxStepJump) maxStepJump = jump;
                // No backward walk (+X) when returning from exterior (+X) to interior (-X)
                Assert.That(curPos.x, Is.LessThanOrEqualTo(prevPos.x + 0.001f), "Crew moved backward (+X) during return");
                prevPos = curPos;
            }

            // No single-frame teleportation (> 0.6m at 15fps)
            Assert.That(maxStepJump, Is.LessThan(0.6f));

            // Pause freezes position and ReturnRemainingSeconds
            runner.SetPaused(true);
            Vector3 pausedPos = crew1.position;
            double pausedRem = runner.State.Crew[0].ReturnRemainingSeconds;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Vector3.Distance(crew1.position, pausedPos), Is.LessThan(0.001f));
            Assert.That(runner.State.Crew[0].ReturnRemainingSeconds, Is.EqualTo(pausedRem));

            // Resume and complete remaining return; verify facing tangent and final interior slot (< 0.05m)
            runner.SetPaused(false);
            while (runner.State.Crew[0].Status == CrewStatus.Returning)
            {
                Vector3 before = crew1.position;
                runner.Stepper.AdvanceFrame(1f / 30f);
                world.ApplyState(runner.State);
                yield return null;
                Vector3 moveDir = crew1.position - before;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 flatMove = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
                    Vector3 flatFwd = new Vector3(pivot1.forward.x, 0f, pivot1.forward.z).normalized;
                    Assert.That(Vector3.Dot(flatMove, flatFwd), Is.GreaterThan(0.2f), "Crew facing away from movement tangent");
                }
            }

            Assert.That(runner.State.Crew[0].Location, Is.EqualTo(CrewLocation.Inside));
            Vector3 expectedIdleSlot0 = new Vector3(-3.2f, 0f, -0.55f);
            float finalDist = Vector2.Distance(new Vector2(crew1.position.x, crew1.position.z), new Vector2(expectedIdleSlot0.x, expectedIdleSlot0.z));
            Assert.That(finalDist, Is.LessThan(0.05f));

            // Assigning interior task after return keeps upright pose clean
            var assignRes = runner.AssignTask("crew-1", "research");
            Assert.That(assignRes.Success, Is.True, assignRes.Reason);
            yield return null;
            Assert.That(Vector3.Dot(pivot1.up, Vector3.up), Is.GreaterThan(0.95f));
        }

        [UnityTest]
        public IEnumerator EnvironmentalPoolsSeparateAndStayOutsideProtectedInteriorVolume()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.Stepper.NotifyApplicationFocus(true);
            runner.SetPaused(false);

            // Simultaneous active Radiation + Dust events during nighttime
            var cfg = new MissionConfig(solSeconds: 110, daySeconds: 10);
            var events = new[]
            {
                new EventState("rad-1", "Solar Storm", EventKind.Radiation, 5, 10, 100, 1.0),
                new EventState("dust-1", "Dust Storm", EventKind.Dust, 5, 10, 100, 0.5)
            };
            runner.InitializeMission(cfg, new MissionScenario(events: events));
            runner.Stepper.Simulation.Tick(15.0);
            world.ApplyState(runner.State);

            for (int f = 0; f < 10; f++)
                yield return null;

            int activeDust = 0;
            int activeRad = 0;
            int activeStars = 0;
            Vector3 sampleDustPos = Vector3.zero;
            GameObject sampleDustGo = null;

            for (int i = 0; i < 16; i++)
            {
                var go = GameObject.Find($"DustPart_{i}");
                Assert.That(go, Is.Not.Null);
                if (go.GetComponent<MeshRenderer>().enabled)
                {
                    activeDust++;
                    Assert.That(MarsOutpostWorldView.IsInsideProtectedVolume(go.transform.localPosition), Is.False, $"DustPart_{i} inside protected volume");
                    if (sampleDustGo == null)
                    {
                        sampleDustGo = go;
                        sampleDustPos = go.transform.localPosition;
                    }
                }
            }

            for (int i = 0; i < 12; i++)
            {
                var go = GameObject.Find($"RadPart_{i}");
                Assert.That(go, Is.Not.Null);
                if (go.GetComponent<MeshRenderer>().enabled)
                {
                    activeRad++;
                    Assert.That(MarsOutpostWorldView.IsInsideProtectedVolume(go.transform.localPosition), Is.False, $"RadPart_{i} inside protected volume");
                }
            }

            for (int i = 0; i < 20; i++)
            {
                var go = GameObject.Find($"StarPart_{i}");
                Assert.That(go, Is.Not.Null);
                if (go.GetComponent<MeshRenderer>().enabled)
                    activeStars++;
            }

            Assert.That(activeDust, Is.EqualTo(16));
            Assert.That(activeRad, Is.EqualTo(12));
            Assert.That(activeStars, Is.EqualTo(20));

            // Pause freezes particle movement
            runner.SetPaused(true);
            sampleDustPos = sampleDustGo.transform.localPosition;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Vector3.Distance(sampleDustGo.transform.localPosition, sampleDustPos), Is.LessThan(0.001f));

            if (System.Environment.GetEnvironmentVariable("MARS_CAPTURE_CP03") == "1")
            {
                runner.SetPaused(false);
                yield return null;
                var cam = Camera.main;
                var rt = new RenderTexture(1440, 900, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1440, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
                tex.Apply();
                cam.targetTexture = prev;
                RenderTexture.active = null;
                Object.Destroy(rt);
                System.IO.File.WriteAllBytes("/run/media/senesatka/storage/projects/marsgame/docs/validation/v2-cp03-hazards.png", tex.EncodeToPNG());
                Object.Destroy(tex);
            }
        }
    }
}
