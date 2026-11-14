using System.Collections.Generic;
using System.IO;
using MarsOutpost.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MarsOutpost.Editor
{
    /// <summary>
    /// Idempotently creates or updates Assets/MarsOutpost/Scenes/MarsOutpost.unity
    /// without altering SampleScene or any user-created scenes.
    /// </summary>
    public static class MarsOutpostSceneBuilder
    {
        public const string ScenePath = "Assets/MarsOutpost/Scenes/MarsOutpost.unity";

        [MenuItem("MarsOutpost/Build or Update Playable Scene")]
        public static void BuildSceneMenu()
        {
            BuildScene(interactive: true);
        }

        public static void BuildSceneFromCommandLine()
        {
            BuildScene(interactive: false);
        }

        public static void BuildScene(bool interactive)
        {
            EnsureFolder("Assets/MarsOutpost", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main Camera configured for 2.5D perspective with camera controller
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0.4f, 10.566f, -11.416f);
            camGo.transform.rotation = Quaternion.Euler(43.0f, 0.0f, 0.0f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 160f;
            cam.backgroundColor = new Color(0.28f, 0.14f, 0.10f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<MarsOutpostCameraController>();

            // 3D Directional Sunlight (MarsSunLight) casting soft 3D shadows
            var sunGo = new GameObject("MarsSunLight");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.84f, 0.68f, 1f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(46f, -32f, 0f);

            // Subtle cool-slate fill light for balanced 3-point 360° PBR lighting
            var fillGo = new GameObject("MarsFillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.48f, 0.58f, 0.72f, 1f);
            fill.intensity = 0.32f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(35f, 148f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.30f, 0.34f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.38f, 0.28f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.14f, 0.10f, 1f);

            // Subtle atmospheric Mars horizon fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.32f, 0.16f, 0.11f, 1f);
            RenderSettings.fogStartDistance = 36f;
            RenderSettings.fogEndDistance = 78f;

            // Input System EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // Single SimulationRoot owning runner, world view, audio, and HUD
            var rootGo = new GameObject("MarsOutpostRoot");
            var runner = rootGo.AddComponent<SimulationRunner>();
            var worldView = rootGo.AddComponent<MarsOutpostWorldView>();
            var audio = rootGo.AddComponent<MarsOutpostAudio>();
            var hud = rootGo.AddComponent<MarsOutpostHud>();

            var worldSo = new SerializedObject(worldView);
            worldSo.FindProperty("terrainSprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/mars_terrain.png");
            worldSo.FindProperty("habitatSprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/habitat_shelter.png");
            worldSo.FindProperty("lifeSupportSprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/station_lifesupport.png");
            worldSo.FindProperty("galleySprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/station_galley.png");
            worldSo.FindProperty("labSprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/station_lab.png");
            worldSo.FindProperty("solarArraySprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/solar_array.png");
            worldSo.FindProperty("astronautSuitSprite").objectReferenceValue = LoadSpriteAsset("Assets/MarsOutpost/Art/astronaut_suit.png");
            worldSo.ApplyModifiedPropertiesWithoutUndo();

            var audioSo = new SerializedObject(audio);
            audioSo.FindProperty("runner").objectReferenceValue = runner;
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(hud);
            so.FindProperty("runner").objectReferenceValue = runner;
            so.FindProperty("worldView").objectReferenceValue = worldView;
            so.FindProperty("audioController").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
                throw new IOException($"Failed to save scene at {ScenePath}");

            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (interactive)
                Debug.Log($"MarsOutpost playable scene saved to {ScenePath}");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string full = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Sprite LoadSpriteAsset(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (assetPath.EndsWith("mars_terrain.png", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (importer.wrapMode != TextureWrapMode.Repeat || !importer.isReadable || !importer.mipmapEnabled)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        importer.isReadable = true;
                        importer.mipmapEnabled = true;
                        changed = true;
                    }
                }
                else
                {
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath && !string.IsNullOrEmpty(existing.path))
                    scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("MarsOutpost/Build Linux Desktop")]
        public static void BuildLinuxDesktopMenu()
        {
            BuildLinuxDesktop();
        }

        public static void BuildLinuxDesktop()
        {
            BuildScene(interactive: false);
            Directory.CreateDirectory("Build/Linux");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/Linux/MarsOutpost.x86_64",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new IOException($"Linux desktop build failed: {report.summary.result} (errors: {report.summary.totalErrors})");
        }
    }
}
