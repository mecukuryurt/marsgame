using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsNetworkPlayModeTests
    {
        private static void AdvanceRunnerSim(SimulationRunner runner, double seconds)
        {
            runner.Simulation.Tick(seconds);
            runner.SetPaused(runner.Stepper.UserPaused);
        }

        private static List<GridCoord> FindTwoStepChainFromCore(SimulationRunner runner)
        {
            var map = runner.State.Map;
            var seeds = new List<GridCoord>();
            foreach (var c in map.Cells)
            {
                if (c.Protection != StarterProtectionKind.None && c.HasUtilityLine)
                    seeds.Add(c.Coord);
            }

            foreach (var seed in seeds)
            {
                foreach (var n1 in map.Config.GetCardinalNeighbors(seed))
                {
                    if (!map.TryGetCell(n1, out var cell1) || !cell1.IsExplored || !cell1.IsBuildable || cell1.Protection != StarterProtectionKind.None)
                        continue;
                    foreach (var n2 in map.Config.GetCardinalNeighbors(n1))
                    {
                        if (n2 == seed) continue;
                        if (!map.TryGetCell(n2, out var cell2) || !cell2.IsExplored || !cell2.IsBuildable || cell2.Protection != StarterProtectionKind.None)
                            continue;
                        if (UtilityNetwork.IsCoordConnectedPreview(map, n2))
                            continue;
                        return new List<GridCoord> { n1, n2 };
                    }
                }
            }

            Assert.Fail("Could not find 2-step chain from core on default Operations map.");
            return null;
        }

        [UnityTest]
        public IEnumerator NetworkVisualsAndHudShowDisconnectedReasonUntilUtilityBridgeCommissioned()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var world = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(world, Is.Not.Null);

            runner.InitializeOperationsMission();
            yield return null;

            var chain = FindTwoStepChainFromCore(runner);
            GridCoord bridgeCell = chain[0];
            GridCoord farCell = chain[1];

            if (!runner.State.Map.GetCell(bridgeCell).IsClaimed)
                Assert.That(runner.ClaimParcel(bridgeCell).Success, Is.True);
            if (!runner.State.Map.GetCell(farCell).IsClaimed)
                Assert.That(runner.ClaimParcel(farCell).Success, Is.True);

            // Build SolarPanel on disconnected farCell
            Assert.That(runner.PlaceBlueprint(FacilityKind.SolarPanel, farCell).Success, Is.True);
            string solarId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            Assert.That(runner.AssignBuildBlueprint("crew-1", solarId).Success, Is.True);
            AdvanceRunnerSim(runner, 80.0);
            yield return null;

            // Verify disconnected status in WorldView visual color & HUD reason text
            var solarGo = GameObject.Find($"FacilityVisual_SolarPanel_{solarId}");
            Assert.That(solarGo, Is.Not.Null);
            var junction = solarGo.transform.Find("CommissionedSolarJunction")?.GetComponent<Renderer>();
            Assert.That(junction, Is.Not.Null);
            Assert.That(junction.sharedMaterial.color.r, Is.GreaterThan(junction.sharedMaterial.color.b),
                "Disconnected facility junction should render amber/orange.");

            world.SelectParcel(farCell);
            yield return null;
            var reasonText = GameObject.Find("ParcelReason")?.GetComponent<UnityEngine.UI.Text>();
            Assert.That(reasonText, Is.Not.Null);
            Assert.That(reasonText.text, Does.Contain("Bağlantı Yok").Or.Contain("Not Connected"));

            // Now build UtilityLine bridge at bridgeCell (crew-2 builds it)
            Assert.That(runner.PlaceBlueprint(FacilityKind.UtilityLine, bridgeCell).Success, Is.True);
            string lineId = runner.State.Facilities[runner.State.Facilities.Count - 1].FacilityId;
            Assert.That(runner.AssignBuildBlueprint("crew-2", lineId).Success, Is.True);
            AdvanceRunnerSim(runner, 40.0);
            yield return null;

            // Verify connected solar flips color to cyan & HUD updates cleanly
            solarGo = GameObject.Find($"FacilityVisual_SolarPanel_{solarId}");
            junction = solarGo.transform.Find("CommissionedSolarJunction")?.GetComponent<Renderer>();
            Assert.That(junction, Is.Not.Null);
            Assert.That(junction.sharedMaterial.color.b, Is.GreaterThan(junction.sharedMaterial.color.r),
                "Connected facility junction should render bright cyan.");

            var lineGo = GameObject.Find($"FacilityVisual_UtilityLine_{lineId}");
            Assert.That(lineGo, Is.Not.Null);
            int mask = UtilityNetwork.GetCardinalLineMask(runner.State.Map, bridgeCell);
            Assert.That(mask, Is.GreaterThan(0));
            if ((mask & UtilityNetwork.MaskNorth) != 0) Assert.That(lineGo.transform.Find("CableArmNorth"), Is.Not.Null);
            if ((mask & UtilityNetwork.MaskEast) != 0) Assert.That(lineGo.transform.Find("CableArmEast"), Is.Not.Null);
            if ((mask & UtilityNetwork.MaskSouth) != 0) Assert.That(lineGo.transform.Find("CableArmSouth"), Is.Not.Null);
            if ((mask & UtilityNetwork.MaskWest) != 0) Assert.That(lineGo.transform.Find("CableArmWest"), Is.Not.Null);

            var detailsText = GameObject.Find("ParcelDetails")?.GetComponent<UnityEngine.UI.Text>();
            Assert.That(detailsText, Is.Not.Null);
            Assert.That(detailsText.text, Does.Contain("Bağlı").Or.Contain("Connected"));
        }
    }
}
