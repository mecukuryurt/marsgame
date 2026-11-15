using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MarsOutpost.Presentation;

namespace MarsOutpost.Tests.PlayMode
{
    public class OperationsParcelSelectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator GridToggleAndMaterialsStayHiddenInTrainingAndAppearInOperations()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var worldView = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(worldView, Is.Not.Null);

            // Switch to Training -> hides Operations grid UI and materials
            runner.InitializeDefaultMission();
            yield return null;
            Assert.That(runner.CurrentMode, Is.EqualTo(MissionMode.Training));
            var gridOverlay = worldView.transform.Find("OperationsGridOverlay");
            Assert.That(gridOverlay, Is.Not.Null);
            Assert.That(gridOverlay.gameObject.activeSelf, Is.False);
            Assert.That(GameObject.Find("BtnGridToggle"), Is.Null);
            Assert.That(GameObject.Find("MaterialsText"), Is.Null);

            // Switch to Demo -> still hidden
            runner.InitializeDemoMission();
            yield return null;
            Assert.That(gridOverlay.gameObject.activeSelf, Is.False);
            Assert.That(GameObject.Find("BtnGridToggle"), Is.Null);

            // Switch to Operations -> Grid button and Materials visible; grid starts toggled OFF
            runner.InitializeOperationsMission();
            runner.SetPaused(true);
            yield return null;

            Assert.That(GameObject.Find("BtnGridToggle"), Is.Null);
            GameObject.Find("BtnBuildMode").GetComponent<Button>().onClick.Invoke();
            var btnGridGo = GameObject.Find("BtnGridToggle");
            var matsGo = GameObject.Find("MaterialsText");
            Assert.That(btnGridGo, Is.Not.Null);
            Assert.That(matsGo, Is.Not.Null);
            Assert.That(matsGo.GetComponent<Text>().text, Does.Contain("40/80"));
            Assert.That(gridOverlay.gameObject.activeSelf, Is.True);
            btnGridGo.GetComponent<Button>().onClick.Invoke();
            Assert.That(worldView.IsGridVisible, Is.False);

            // Toggle Grid ON via HUD button
            btnGridGo.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(worldView.IsGridVisible, Is.True);
            Assert.That(gridOverlay.gameObject.activeSelf, Is.True);
            var tilesMr = gridOverlay.Find("GridTiles")?.GetComponent<MeshRenderer>();
            var linesMr = gridOverlay.Find("GridLines")?.GetComponent<MeshRenderer>();
            Assert.That(tilesMr != null && tilesMr.enabled, Is.True);
            Assert.That(linesMr != null && linesMr.enabled, Is.True);

            // Toggle Grid OFF
            btnGridGo.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(worldView.IsGridVisible, Is.False);
            Assert.That(gridOverlay.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ScreenClickSelectsDomainParcelHidesUnscannedGeologyAndClaimsAtomically()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var worldView = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.InitializeOperationsMission();
            runner.SetPaused(true);
            yield return null;

            var cam = Camera.main;
            Assert.That(cam, Is.Not.Null);

            // World center of (8, 4): Origin (-32,-24), CellSize 4 -> x = 2.0, z = -6.0
            runner.State.Map.Config.CellToWorldCenter(new GridCoord(8, 4), out double cx, out double cz);
            Vector3 targetWorld = new Vector3((float)cx, MarsOutpostWorldView.GetTerrainHeight((float)cx, (float)cz), (float)cz);
            Vector3 screenPos = cam.WorldToScreenPoint(targetWorld);
            Assert.That(screenPos.z, Is.GreaterThan(0f));

            // In grid inspection mode a structure above the terrain must not move the selected cell.
            worldView.SetGridVisible(true);
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.transform.position = Vector3.Lerp(cam.transform.position, targetWorld, 0.5f);
            obstruction.transform.localScale = Vector3.one * 2f;
            Physics.SyncTransforms();
            bool handled = worldView.ProcessScreenClick(new Vector2(screenPos.x, screenPos.y));
            Object.Destroy(obstruction);
            yield return null;

            Assert.That(handled, Is.True);
            Assert.That(worldView.SelectedParcel, Is.EqualTo(new GridCoord(8, 4)));

            var card = GameObject.Find("ParcelSelectionCard");
            Assert.That(card, Is.Not.Null);
            Assert.That(GameObject.Find("ParcelTitle").GetComponent<Text>().text, Does.Contain("(8, 4)"));

            var outlineGo = worldView.transform.Find("SelectedParcelOutline");
            Assert.That(outlineGo != null && outlineGo.gameObject.activeSelf, Is.True);
            var lr = outlineGo.GetComponent<LineRenderer>();
            Vector3 p0 = lr.GetPosition(0);
            Assert.That(p0.x, Is.InRange((float)cx - 2.01f, (float)cx + 2.01f));
            Assert.That(p0.z, Is.InRange((float)cz - 2.01f, (float)cz + 2.01f));

            // Claim via HUD card button -> deducts 2 materials cleanly
            var claimBtn = GameObject.Find("BtnClaimParcel").GetComponent<Button>();
            Assert.That(claimBtn.interactable, Is.True);
            claimBtn.onClick.Invoke();
            yield return null;

            Assert.That(runner.State.Materials, Is.EqualTo(38));
            Assert.That(runner.State.Map.GetCell(new GridCoord(8, 4)).IsClaimed, Is.True);
            Assert.That(GameObject.Find("MaterialsText").GetComponent<Text>().text, Does.Contain("38/80"));
            Assert.That(claimBtn.interactable, Is.False);

            // Find an explored-but-unscanned cell and verify hidden geology never leaks
            GridCoord? unscannedExplored = null;
            GridCoord? unexplored = null;
            for (int z = 0; z < runner.State.Map.Config.Height; z++)
            {
                for (int x = 0; x < runner.State.Map.Config.Width; x++)
                {
                    var gc = new GridCoord(x, z);
                    var cell = runner.State.Map.GetCell(gc);
                    if (cell.IsExplored && !cell.IsScanned && !cell.IsClaimed && !runner.State.Map.HasClaimedNeighbor(gc) && !unscannedExplored.HasValue)
                        unscannedExplored = gc;
                    if (!cell.IsExplored && !unexplored.HasValue)
                        unexplored = gc;
                }
            }

            Assert.That(unscannedExplored.HasValue, Is.True);
            worldView.SelectParcel(unscannedExplored.Value);
            yield return null;
            string details = GameObject.Find("ParcelDetails").GetComponent<Text>().text;
            Assert.That(details, Does.Not.Contain("Yüzey Buzu").And.Not.Contain("Surface Ice"));
            Assert.That(details, Does.Not.Contain("Regolit").And.Not.Contain("Regolith"));
            Assert.That(details, Does.Not.Contain("Çorak").And.Not.Contain("Barren"));

            // Clicking Claim on disconnected explored cell triggers atomic rejection via Runner & FeedbackBanner
            int rejectedCount = 0;
            runner.CommandRejected += _ => rejectedCount++;
            claimBtn.onClick.Invoke();
            yield return null;
            Assert.That(rejectedCount, Is.EqualTo(1));
            Assert.That(runner.State.Materials, Is.EqualTo(38));
            Assert.That(runner.State.Map.GetCell(unscannedExplored.Value).IsClaimed, Is.False);
            Assert.That(GameObject.Find("FeedbackBanner") != null, Is.True);

            // Unexplored cell also hides SolarFactor and Resource
            Assert.That(unexplored.HasValue, Is.True);
            worldView.SelectParcel(unexplored.Value);
            yield return null;
            string unexploredDetails = GameObject.Find("ParcelDetails").GetComponent<Text>().text;
            Assert.That(unexploredDetails, Does.Not.Contain("%"));
            Assert.That(unexploredDetails, Does.Not.Contain("Yüzey Buzu").And.Not.Contain("Surface Ice"));
            Assert.That(unexploredDetails, Does.Not.Contain("Regolit").And.Not.Contain("Regolith"));
        }

        [UnityTest]
        public IEnumerator StationPriorityAndEscapeOrRightClickFilterPreserveCrewCommands()
        {
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            var worldView = Object.FindFirstObjectByType<MarsOutpostWorldView>();
            runner.InitializeOperationsMission();
            runner.SetPaused(true);
            yield return null;

            // Select parcel (8, 4)
            worldView.SelectParcel(new GridCoord(8, 4));
            yield return null;
            Assert.That(worldView.SelectedParcel, Is.EqualTo(new GridCoord(8, 4)));

            // Clicking existing Solar Array world pos must trigger station action without clearing/overwriting with terrain
            string clickedStation = null;
            worldView.StationClicked += id => clickedStation = id;
            Vector3 solarScreen = Camera.main.WorldToScreenPoint(new Vector3(4.65f, 0.3f, 0.25f));
            worldView.ProcessScreenClick(new Vector2(solarScreen.x, solarScreen.y));
            yield return null;
            Assert.That(clickedStation, Is.EqualTo("panel-maintenance"));
            Assert.That(worldView.SelectedParcel, Is.EqualTo(new GridCoord(8, 4)));

            // Escape clears parcel selection cleanly without opening Settings menu
            Assert.That(worldView.HandleEscapeCancel(), Is.True);
            yield return null;
            Assert.That(worldView.SelectedParcel.HasValue, Is.False);
            Assert.That(GameObject.Find("ParcelSelectionCard"), Is.Null);

            // Right-click also clears selection
            worldView.SelectParcel(new GridCoord(8, 4));
            yield return null;
            Assert.That(worldView.SelectedParcel.HasValue, Is.True);
            worldView.ProcessScreenClick(Vector2.zero, isRightClick: true);
            yield return null;
            Assert.That(worldView.SelectedParcel.HasValue, Is.False);
        }
    }
}
