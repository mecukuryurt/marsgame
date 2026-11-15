using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MarsOutpost.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MarsOutpost.Tests.PlayMode
{
    public sealed class SettingsMenuTests
    {
        private const string Prefix = "MarsOutpost.Settings.";
        private readonly Dictionary<string, object> saved = new Dictionary<string, object>();
        private readonly string[] keys = { "Muted", "Volume", "Ambience", "Music", "Sfx", "Quality", "Fullscreen", "Width", "Height" };
        private SimulationRunner runner;
        private MarsOutpostSettingsMenu menu;
        private bool previousLanguage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            saved.Clear();
            foreach (var key in keys)
            {
                saved[key] = !PlayerPrefs.HasKey(Prefix + key) ? null : (key == "Volume" || key == "Ambience" || key == "Music" || key == "Sfx")
                    ? (object)PlayerPrefs.GetFloat(Prefix + key) : PlayerPrefs.GetInt(Prefix + key);
                PlayerPrefs.DeleteKey(Prefix + key);
            }
            previousLanguage = OutpostStrings.IsTurkish;
            OutpostStrings.IsTurkish = true;
            SceneManager.LoadScene("MarsOutpost", LoadSceneMode.Single);
            yield return null;
            yield return null;
            runner = Object.FindFirstObjectByType<SimulationRunner>();
            menu = Object.FindFirstObjectByType<MarsOutpostSettingsMenu>();
            Assert.That(menu, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (runner != null) Object.Destroy(runner.gameObject);
            yield return null;
            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(Prefix + key);
                if (saved[key] is float f) PlayerPrefs.SetFloat(Prefix + key, f);
                else if (saved[key] is int i) PlayerPrefs.SetInt(Prefix + key, i);
            }
            PlayerPrefs.Save();
            OutpostStrings.IsTurkish = previousLanguage;
        }

        private T Field<T>(string name) => (T)typeof(MarsOutpostSettingsMenu)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);

        [UnityTest]
        public IEnumerator MixerSettingsPersistAndOwnedLoopsAreReleased()
        {
            var audio = Object.FindFirstObjectByType<MarsOutpostAudio>();
            Assert.That(audio, Is.Not.Null);
            var owner = audio.gameObject;
            int initialSources = owner.GetComponents<AudioSource>().Length;
            menu.Open();
            Field<Slider>("ambienceSlider").value = 0.21f;
            Field<Slider>("musicSlider").value = 0.12f;
            Field<Slider>("sfxSlider").value = 0.73f;
            typeof(MarsOutpostSettingsMenu).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
            Assert.That(audio.AmbienceVolume, Is.EqualTo(0.21f).Within(0.001f));
            Assert.That(audio.MusicVolume, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(audio.SfxVolume, Is.EqualTo(0.73f).Within(0.001f));
            Assert.That(PlayerPrefs.GetFloat(Prefix + "Music"), Is.EqualTo(0.12f).Within(0.001f));
            menu.Close();
            Object.Destroy(audio);
            yield return null;
            Assert.That(owner.GetComponents<AudioSource>().Length, Is.EqualTo(initialSources - 2));
        }

        [UnityTest]
        public IEnumerator OpenPausesCloseRestoresAndReopenDoesNotDuplicateEntry()
        {
            var entry = menu.transform.Find("BtnSettings");
            Assert.That(entry, Is.Not.Null);
            menu.Open();
            Assert.That(runner.Stepper.UserPaused, Is.True);
            double elapsed = runner.State.ElapsedSeconds;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(runner.State.ElapsedSeconds, Is.EqualTo(elapsed));
            menu.Close();
            Assert.That(runner.Stepper.UserPaused, Is.False);
            menu.Open(); menu.Close();
            Assert.That(menu.transform.Find("BtnSettings"), Is.SameAs(entry));
            Assert.That(Object.FindObjectsByType<MarsOutpostSettingsMenu>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ClosingSettingsKeepsPriorPauseAndNewFocusPause()
        {
            runner.SetPaused(true); menu.Open(); menu.Close();
            Assert.That(runner.Stepper.UserPaused, Is.True);
            runner.SetPaused(false); menu.Open();
            runner.Stepper.NotifyApplicationFocus(false);
            yield return null;
            runner.Stepper.NotifyApplicationFocus(true);
            menu.Close();
            Assert.That(runner.Stepper.UserPaused, Is.True);
        }

        [UnityTest]
        public IEnumerator QualityDropdownOpensAndApplySurvivesCloseAndRecreation()
        {
            menu.Open();
            yield return null; // Dropdown.Start initializes its popup tween after activation.
            var quality = Field<Dropdown>("qualityDropdown");
            Assert.That(quality.options.Count, Is.EqualTo(3));
            Assert.That(quality.value, Is.EqualTo(2));
            quality.Show(); yield return null;
            Assert.That(menu.GetComponentsInChildren<Toggle>(true).Length, Is.GreaterThan(2));
            quality.Hide();
            quality.value = 1;
            Field<Slider>("volumeSlider").value = .35f;
            typeof(MarsOutpostSettingsMenu).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
            menu.Close();
            Assert.That(PlayerPrefs.GetInt(Prefix + "Quality"), Is.EqualTo(1));
            Assert.That(runner.GetComponent<MarsOutpostAudio>().Volume, Is.EqualTo(.35f).Within(.001f));
            var canvas = menu.gameObject;
            Object.Destroy(menu); yield return null;
            menu = canvas.AddComponent<MarsOutpostSettingsMenu>();
            menu.Open();
            Assert.That(Field<Dropdown>("qualityDropdown").value, Is.EqualTo(1));
            Assert.That(Field<Slider>("volumeSlider").value, Is.EqualTo(.35f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator CancelDoesNotSaveDraftChanges()
        {
            menu.Open();
            Field<Slider>("volumeSlider").value = .05f;
            Field<Dropdown>("qualityDropdown").value = 0;
            menu.Close();
            Assert.That(PlayerPrefs.HasKey(Prefix + "Quality"), Is.False);
            Assert.That(runner.GetComponent<MarsOutpostAudio>().Volume, Is.EqualTo(.6f).Within(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AspectFramingMenuCameraGuardAndResolutionValidationWorkCleanly()
        {
            // 1. 16:10 (1440x900) pulls camera back further than 16:9 (1920x1080) so base + panels stay framed
            float dist169 = MarsOutpostCameraController.ComputeAspectAdjustedDefaultDistance(16f / 9f);
            float dist1610 = MarsOutpostCameraController.ComputeAspectAdjustedDefaultDistance(16f / 10f);
            Assert.That(dist1610, Is.GreaterThan(dist169));
            Assert.That(dist1610, Is.LessThanOrEqualTo(MarsOutpostCameraController.MaxDistance));

            // 2. Menu open blocks camera WASD/pan input; close restores it
            Assert.That(MarsOutpostCameraController.IsMenuBlockingCameraInput, Is.False);
            menu.Open();
            Assert.That(MarsOutpostCameraController.IsMenuBlockingCameraInput, Is.True);
            menu.Close();
            Assert.That(MarsOutpostCameraController.IsMenuBlockingCameraInput, Is.False);

            // 3. Invalid saved resolution falls back safely to native supported resolution
            Assert.That(MarsOutpostSettingsMenu.IsValidResolution(0, 0), Is.False);
            Assert.That(MarsOutpostSettingsMenu.IsValidResolution(320, 240), Is.False);
            Assert.That(MarsOutpostSettingsMenu.IsValidResolution(-1440, 900), Is.False);
            var nativeRes = MarsOutpostSettingsMenu.GetNativeSupportedResolution();
            Assert.That(nativeRes.width, Is.GreaterThanOrEqualTo(640));
            Assert.That(nativeRes.height, Is.GreaterThanOrEqualTo(480));

            PlayerPrefs.SetInt(Prefix + "Width", 0);
            PlayerPrefs.SetInt(Prefix + "Height", -10);
            PlayerPrefs.Save();

            var canvas = menu.gameObject;
            Object.Destroy(menu);
            yield return null;
            menu = canvas.AddComponent<MarsOutpostSettingsMenu>();
            yield return null;

            Assert.That(PlayerPrefs.GetInt(Prefix + "Width"), Is.EqualTo(nativeRes.width));
            Assert.That(PlayerPrefs.GetInt(Prefix + "Height"), Is.EqualTo(nativeRes.height));
        }
    }
}
