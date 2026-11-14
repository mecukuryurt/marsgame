using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// 2.5D World View driven strictly by read-only MissionState with smooth airlock waypoint pathfinding,
    /// animated multi-part astronauts, emergency beacon, interactive stations, and environmental effects.
    /// </summary>
    public sealed class MarsOutpostWorldView : MonoBehaviour
    {
        private static readonly Vector2 HabitatCenter = new Vector2(-1.6f, 0.0f);
        private static readonly Vector2 SystemBayPos = new Vector2(-2.9f, 0.95f);
        private static readonly Vector2 GalleyPos = new Vector2(-1.6f, 0.95f);
        private static readonly Vector2 ResearchLabPos = new Vector2(-0.3f, 0.95f);
        private static readonly Vector2 SolarArrayPos = new Vector2(2.8f, 0.4f);
        private static readonly Vector2 BunksPos = new Vector2(-2.85f, -1.55f);

        private static readonly Vector2 AirlockInnerWaypoint = new Vector2(-0.35f, -0.35f);
        private static readonly Vector2 AirlockOuterWaypoint = new Vector2(0.95f, -0.35f);
        private const float HabitatWallX = 0.15f;

        private static readonly Vector2[] IdleSlots =
        {
            new Vector2(-2.45f, -0.75f),
            new Vector2(-1.6f, -0.75f),
            new Vector2(-0.75f, -0.75f)
        };

        private static readonly Color DayGroundColor = new Color(0.68f, 0.34f, 0.21f, 1f);
        private static readonly Color NightGroundColor = new Color(0.22f, 0.13f, 0.16f, 1f);
        private static readonly Color DustTintColor = new Color(0.76f, 0.56f, 0.34f, 0.38f);
        private static readonly Color RadiationTintColor = new Color(0.95f, 0.25f, 0.25f, 0.24f);

        private static readonly Color[] CrewVisorAccents =
        {
            new Color(0.96f, 0.72f, 0.22f, 1f),
            new Color(0.28f, 0.80f, 0.96f, 1f),
            new Color(0.42f, 0.88f, 0.54f, 1f)
        };

        [Header("Art Sprite Assets (Optional Fallback to Procedural Shapes)")]
        [SerializeField] private Sprite terrainSprite;
        [SerializeField] private Sprite habitatSprite;
        [SerializeField] private Sprite lifeSupportSprite;
        [SerializeField] private Sprite galleySprite;
        [SerializeField] private Sprite labSprite;
        [SerializeField] private Sprite solarArraySprite;
        [SerializeField] private Sprite astronautSuitSprite;

        private Sprite squareSprite;
        private Sprite circleSprite;
        private SpriteRenderer groundRenderer;
        private SpriteRenderer hazardOverlayRenderer;
        private SpriteRenderer solarPanelRenderer;
        private SpriteRenderer solarDustOverlayRenderer;
        private SpriteRenderer beaconLightRenderer;
        private SpriteRenderer beaconHaloRenderer;
        private Transform innerDoorTransform;
        private Transform outerDoorTransform;
        private float doorOpenProgress = 0f;

        private TextMesh habitatLabel;
        private TextMesh solarLabel;
        private TextMesh systemBayLabel;
        private TextMesh galleyLabel;
        private TextMesh labLabel;
        private TextMesh bunksLabel;
        private TextMesh airlockLabel;
        private bool isTurkish = true;

        private bool hasRadiationHazard;
        private bool hasDustHazard;
        private bool isNightTime;
        private MissionState latestState;

        private readonly Dictionary<string, CrewVisual> crewVisuals = new Dictionary<string, CrewVisual>();
        private readonly List<StationActivityIndicator> stationIndicators = new List<StationActivityIndicator>();
        private readonly List<EnvParticle> envParticles = new List<EnvParticle>();
        private Camera mainCam;

        public string SelectedCrewId { get; private set; }
        public event Action<string> CrewSelected;
        public event Action<string> StationClicked;

        private sealed class DustPuff
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private sealed class CrewVisual
        {
            public string Id;
            public int Index;
            public Transform Root;
            public SpriteRenderer Shadow;
            public SpriteRenderer LeftFoot;
            public SpriteRenderer RightFoot;
            public SpriteRenderer Ring;
            public SpriteRenderer Body;
            public SpriteRenderer Visor;
            public TextMesh StatusBadge;
            public TextMesh NameLabel;
            public Vector2 TargetPos;
            public Vector3 BodyBaseScale;
            public Vector3 VisorBasePos;
            public CrewStatus LastStatus;
            public string LastTaskId;
            public int LastFacing;
            public DustPuff[] DustPuffs;
            public float PuffSpawnTimer;
            public int PuffIndex;
        }

        private sealed class StationActivityIndicator
        {
            public string TaskId;
            public Vector2 StationPos;
            public SpriteRenderer GlowRenderer;
            public GameObject BarRoot;
            public SpriteRenderer BarBg;
            public SpriteRenderer BarFill;
            public Color ThemeColor;
            public float BarWidth;
        }

        private sealed class EnvParticle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Speed;
            public float TwinklePhase;
        }

        private void Awake()
        {
            mainCam = Camera.main;
            CreateSprites();
            BuildWorldGeometry();
        }

        private void CreateSprites()
        {
            var sqTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var whitePixels = new[] { Color.white, Color.white, Color.white, Color.white };
            sqTex.SetPixels(whitePixels);
            sqTex.Apply();
            squareSprite = Sprite.Create(sqTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

            const int size = 64;
            var circTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radiusSq = center * center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    pixels[y * size + x] = (dx * dx + dy * dy <= radiusSq) ? Color.white : Color.clear;
                }
            }
            circTex.SetPixels(pixels);
            circTex.Apply();
            circleSprite = Sprite.Create(circTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void BuildWorldGeometry()
        {
            // Ground and Horizon
            if (terrainSprite != null)
            {
                groundRenderer = CreateCustomSpriteObject("MarsTerrain", new Vector3(0, 0, 10), new Vector2(22f, 13f), Color.white, -20, terrainSprite);
            }
            else
            {
                groundRenderer = CreateSpriteObject("MarsTerrain", new Vector3(0, 0, 10), new Vector2(24, 14), DayGroundColor, -20, false);
                CreateSpriteObject("TerrainRidgeNorth", new Vector3(0.4f, 3.1f, 9.8f), new Vector2(20f, 1.4f), new Color(0.50f, 0.24f, 0.16f, 0.45f), -19, false);
                CreateSpriteObject("CraterRimWest", new Vector3(-4.2f, -2.8f, 9.8f), new Vector2(2.6f, 2.6f), new Color(0.46f, 0.22f, 0.15f, 0.35f), -19, true);
                CreateSpriteObject("CraterRimEast", new Vector3(4.3f, 2.5f, 9.8f), new Vector2(2.0f, 2.0f), new Color(0.46f, 0.22f, 0.15f, 0.35f), -19, true);
            }
            hazardOverlayRenderer = CreateSpriteObject("HazardOverlay", new Vector3(0, 0, 9), new Vector2(24, 14), Color.clear, 15, false);

            // 2.5D Airlock Tunnel between habitat (x = -0.05f) and exterior (x = 0.85f) at y = -0.35f
            CreateSpriteObject("AirlockShadow", new Vector3(0.42f, -0.42f, 0), new Vector2(1.10f, 0.85f), new Color(0f, 0f, 0f, 0.32f), -13, false);
            CreateSpriteObject("AirlockFloor", new Vector3(0.40f, -0.35f, 0), new Vector2(1.00f, 0.75f), new Color(0.24f, 0.26f, 0.30f, 1f), -8, false);
            CreateSpriteObject("AirlockFrameTop", new Vector3(0.40f, 0.06f, 0), new Vector2(1.02f, 0.10f), new Color(0.45f, 0.52f, 0.60f, 1f), -3, false);
            CreateSpriteObject("AirlockFrameBottom", new Vector3(0.40f, -0.76f, 0), new Vector2(1.02f, 0.10f), new Color(0.35f, 0.40f, 0.48f, 1f), -3, false);
            CreateSpriteObject("AirlockPath", new Vector3(1.75f, -0.35f, 0.2f), new Vector2(1.5f, 0.60f), new Color(0.34f, 0.30f, 0.28f, 1f), -12, false);

            innerDoorTransform = CreateSpriteObject("InnerAirlockDoor", new Vector3(-0.05f, -0.35f, 0), new Vector2(0.18f, 0.72f), new Color(0.88f, 0.68f, 0.22f, 1f), -4, false).transform;
            outerDoorTransform = CreateSpriteObject("OuterAirlockDoor", new Vector3(0.85f, -0.35f, 0), new Vector2(0.18f, 0.72f), new Color(0.88f, 0.68f, 0.22f, 1f), -4, false).transform;

            beaconLightRenderer = CreateSpriteObject("BeaconLight", new Vector3(0.15f, 0.12f, 0), new Vector2(0.20f, 0.20f), new Color(0.20f, 0.82f, 0.52f, 0.8f), 10, true);
            beaconHaloRenderer = CreateSpriteObject("BeaconHalo", new Vector3(0.15f, 0.12f, 0), new Vector2(0.42f, 0.42f), new Color(0.20f, 0.82f, 0.52f, 0.25f), 9, true);
            airlockLabel = CreateWorldLabel("AirlockLabel", new Vector3(0.38f, 0.35f, 0), isTurkish ? "HAVA KİLİDİ" : "AIRLOCK", 0.065f, new Color(0.95f, 0.90f, 0.75f));

            // Habitat Shelter (Left/Center)
            CreateSpriteObject("HabitatShadow", new Vector3(HabitatCenter.x + 0.12f, HabitatCenter.y - 0.14f, 0.2f), new Vector2(4.9f, 4.2f), new Color(0f, 0f, 0f, 0.35f), -14, false);
            if (habitatSprite != null)
            {
                CreateCustomSpriteObject("HabitatShelter", new Vector3(HabitatCenter.x, HabitatCenter.y, 0), new Vector2(4.9f, 4.2f), Color.white, -10, habitatSprite);
            }
            else
            {
                CreateSpriteObject("HabitatFloor", new Vector3(HabitatCenter.x, HabitatCenter.y, 0), new Vector2(4.6f, 3.9f), new Color(0.18f, 0.22f, 0.27f, 1f), -10, false);
                CreateSpriteObject("HabitatBorder", new Vector3(HabitatCenter.x, HabitatCenter.y, 0.1f), new Vector2(4.85f, 4.15f), new Color(0.58f, 0.68f, 0.78f, 1f), -11, false);
            }
            habitatLabel = CreateWorldLabel("HabitatLabel", new Vector3(HabitatCenter.x, -2.35f, 0), isTurkish ? "SIĞINAK (İÇERİ - GÜVENLİ)" : "HABITAT SHELTER (INSIDE)", 0.085f, Color.white);

            // Bunks / Rest Area (Bottom Left inside Habitat)
            CreateSpriteObject("BunksShadow", new Vector3(BunksPos.x + 0.06f, BunksPos.y - 0.08f, 0.05f), new Vector2(1.35f, 0.75f), new Color(0f, 0f, 0f, 0.30f), -7, false);
            CreateSpriteObject("BunksFrame", new Vector3(BunksPos.x, BunksPos.y, 0), new Vector2(1.30f, 0.70f), new Color(0.24f, 0.28f, 0.35f, 1f), -5, false);
            CreateSpriteObject("BunkCot1", new Vector3(BunksPos.x - 0.30f, BunksPos.y, 0), new Vector2(0.50f, 0.58f), new Color(0.18f, 0.44f, 0.62f, 1f), -4, false);
            CreateSpriteObject("BunkCot2", new Vector3(BunksPos.x + 0.30f, BunksPos.y, 0), new Vector2(0.50f, 0.58f), new Color(0.18f, 0.44f, 0.62f, 1f), -4, false);
            bunksLabel = CreateWorldLabel("BunksLabel", new Vector3(-2.85f, -2.05f, 0), isTurkish ? "DİNLENME / UYKU" : "REST BUNKS", 0.065f, new Color(0.75f, 0.88f, 1f));

            // Interior Stations: Life Support, Galley, Research Lab
            CreateSpriteObject("SystemBayShadow", new Vector3(SystemBayPos.x + 0.06f, SystemBayPos.y - 0.08f, 0.05f), new Vector2(1.15f, 0.90f), new Color(0f, 0f, 0f, 0.30f), -7, false);
            if (lifeSupportSprite != null)
                CreateCustomSpriteObject("SystemBay", new Vector3(SystemBayPos.x, SystemBayPos.y, 0), new Vector2(1.15f, 0.90f), Color.white, -5, lifeSupportSprite);
            else
                CreateSpriteObject("SystemBay", new Vector3(SystemBayPos.x, SystemBayPos.y, 0), new Vector2(1.1f, 0.85f), new Color(0.24f, 0.56f, 0.74f, 1f), -5, false);
            systemBayLabel = CreateWorldLabel("SystemBayLabel", new Vector3(SystemBayPos.x, SystemBayPos.y + 0.65f, 0), isTurkish ? "O2 & SU" : "O2 / WATER", 0.065f, new Color(0.85f, 0.95f, 1f));

            CreateSpriteObject("GalleyBayShadow", new Vector3(GalleyPos.x + 0.06f, GalleyPos.y - 0.08f, 0.05f), new Vector2(1.15f, 0.90f), new Color(0f, 0f, 0f, 0.30f), -7, false);
            if (galleySprite != null)
                CreateCustomSpriteObject("GalleyBay", new Vector3(GalleyPos.x, GalleyPos.y, 0), new Vector2(1.15f, 0.90f), Color.white, -5, galleySprite);
            else
                CreateSpriteObject("GalleyBay", new Vector3(GalleyPos.x, GalleyPos.y, 0), new Vector2(1.1f, 0.85f), new Color(0.30f, 0.62f, 0.44f, 1f), -5, false);
            galleyLabel = CreateWorldLabel("GalleyBayLabel", new Vector3(GalleyPos.x, GalleyPos.y + 0.65f, 0), isTurkish ? "MUTFAK" : "GALLEY", 0.065f, new Color(0.86f, 1f, 0.90f));

            CreateSpriteObject("LabBayShadow", new Vector3(ResearchLabPos.x + 0.06f, ResearchLabPos.y - 0.08f, 0.05f), new Vector2(1.15f, 0.90f), new Color(0f, 0f, 0f, 0.30f), -7, false);
            if (labSprite != null)
                CreateCustomSpriteObject("ResearchLab", new Vector3(ResearchLabPos.x, ResearchLabPos.y, 0), new Vector2(1.15f, 0.90f), Color.white, -5, labSprite);
            else
                CreateSpriteObject("ResearchLab", new Vector3(ResearchLabPos.x, ResearchLabPos.y, 0), new Vector2(1.1f, 0.85f), new Color(0.50f, 0.42f, 0.80f, 1f), -5, false);
            labLabel = CreateWorldLabel("ResearchLabLabel", new Vector3(ResearchLabPos.x, ResearchLabPos.y + 0.65f, 0), isTurkish ? "BİLİM LAB" : "SCIENCE LAB", 0.065f, new Color(0.92f, 0.88f, 1f));

            // Exterior Solar Array (Right)
            CreateSpriteObject("SolarShadow", new Vector3(SolarArrayPos.x + 0.14f, SolarArrayPos.y - 0.14f, 0.2f), new Vector2(2.55f, 1.80f), new Color(0f, 0f, 0f, 0.30f), -8, false);
            if (solarArraySprite != null)
            {
                solarPanelRenderer = CreateCustomSpriteObject("SolarArray", new Vector3(SolarArrayPos.x, SolarArrayPos.y, 0), new Vector2(2.45f, 1.70f), Color.white, -5, solarArraySprite);
            }
            else
            {
                CreateSpriteObject("SolarFrame", new Vector3(SolarArrayPos.x, SolarArrayPos.y, 0.1f), new Vector2(2.45f, 1.75f), new Color(0.32f, 0.36f, 0.42f, 1f), -6, false);
                solarPanelRenderer = CreateSpriteObject("SolarArray", new Vector3(SolarArrayPos.x, SolarArrayPos.y, 0), new Vector2(2.2f, 1.5f), new Color(0.18f, 0.34f, 0.58f, 1f), -5, false);
            }
            solarDustOverlayRenderer = CreateSpriteObject("SolarDustOverlay", new Vector3(SolarArrayPos.x, SolarArrayPos.y, -0.05f), new Vector2(2.35f, 1.6f), new Color(0.76f, 0.42f, 0.22f, 0f), -4, false);
            solarLabel = CreateWorldLabel("SolarArrayLabel", new Vector3(SolarArrayPos.x, SolarArrayPos.y + 1.12f, 0), isTurkish ? "GÜNEŞ PANELLERİ (DIŞARI)" : "SOLAR ARRAY (OUTSIDE)", 0.075f, new Color(1f, 0.92f, 0.75f));

            BuildStationIndicators();
            BuildEnvironmentalPool();
        }

        private void BuildStationIndicators()
        {
            stationIndicators.Clear();
            AddStationIndicator("system-maintenance", SystemBayPos, SystemBayPos + new Vector2(0, 0.72f), new Color(0.24f, 0.56f, 0.74f), new Color(0.30f, 0.85f, 1.0f, 1f), new Vector2(1.25f, 1.0f));
            AddStationIndicator("meal-prep", GalleyPos, GalleyPos + new Vector2(0, 0.72f), new Color(0.30f, 0.62f, 0.44f), new Color(0.35f, 0.95f, 0.65f, 1f), new Vector2(1.25f, 1.0f));
            AddStationIndicator("research", ResearchLabPos, ResearchLabPos + new Vector2(0, 0.72f), new Color(0.50f, 0.42f, 0.80f), new Color(0.80f, 0.55f, 1.0f, 1f), new Vector2(1.25f, 1.0f));
            AddStationIndicator("panel-maintenance", SolarArrayPos, SolarArrayPos + new Vector2(0, 1.25f), new Color(0.96f, 0.78f, 0.25f), new Color(1.0f, 0.85f, 0.25f, 1f), new Vector2(2.55f, 1.80f));
        }

        private void AddStationIndicator(string taskId, Vector2 stationPos, Vector2 barPos, Color glowCol, Color themeCol, Vector2 glowSize)
        {
            var glowSr = CreateSpriteObject($"Glow_{taskId}", new Vector3(stationPos.x, stationPos.y, 0.02f), glowSize, new Color(glowCol.r, glowCol.g, glowCol.b, 0.35f), -4, false);
            glowSr.enabled = false;

            var barRoot = new GameObject($"Bar_{taskId}");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = new Vector3(barPos.x, barPos.y, 0);

            const float totalWidth = 0.90f;
            var bgSr = CreateSpriteObject("Bg", Vector3.zero, new Vector2(totalWidth + 0.04f, 0.12f), new Color(0.12f, 0.14f, 0.18f, 0.85f), 8, false);
            bgSr.transform.SetParent(barRoot.transform, false);

            var fillSr = CreateSpriteObject("Fill", Vector3.zero, new Vector2(totalWidth, 0.07f), themeCol, 9, false);
            fillSr.transform.SetParent(barRoot.transform, false);

            barRoot.SetActive(false);

            stationIndicators.Add(new StationActivityIndicator
            {
                TaskId = taskId,
                StationPos = stationPos,
                GlowRenderer = glowSr,
                BarRoot = barRoot,
                BarBg = bgSr,
                BarFill = fillSr,
                ThemeColor = themeCol,
                BarWidth = totalWidth
            });
        }

        private void BuildEnvironmentalPool()
        {
            envParticles.Clear();
            var poolRoot = new GameObject("EnvironmentalPool").transform;
            poolRoot.SetParent(transform, false);

            for (int i = 0; i < 14; i++)
            {
                float px = -6.5f + (i / 13.0f) * 13.0f;
                float py = -3.0f + ((i * 7) % 13) * 0.55f;
                var sr = CreateSpriteObject($"EnvParticle_{i}", new Vector3(px, py, 0), new Vector2(0.8f, 0.08f), Color.white, 14, false);
                sr.transform.SetParent(poolRoot, false);
                sr.enabled = false;
                envParticles.Add(new EnvParticle
                {
                    Transform = sr.transform,
                    Renderer = sr,
                    Speed = 5.5f + (i % 5) * 1.5f,
                    TwinklePhase = (i * 0.73f) % 3.14f
                });
            }
        }

        private SpriteRenderer CreateCustomSpriteObject(string objName, Vector3 pos, Vector2 worldSize, Color color, int sortOrder, Sprite customSprite)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            Vector3 boundsSize = customSprite.bounds.size;
            float sx = boundsSize.x > 0.0001f ? worldSize.x / boundsSize.x : worldSize.x;
            float sy = boundsSize.y > 0.0001f ? worldSize.y / boundsSize.y : worldSize.y;
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = customSprite;
            sr.color = color;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        private SpriteRenderer CreateSpriteObject(string objName, Vector3 pos, Vector2 scale, Color color, int sortOrder, bool circle)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle ? circleSprite : squareSprite;
            sr.color = color;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        private TextMesh CreateWorldLabel(string objName, Vector3 pos, string text, float charSize, Color color)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = charSize;
            tm.fontSize = 36;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            return tm;
        }

        public void SetLanguage(bool turkish)
        {
            isTurkish = turkish;
            if (habitatLabel != null)
                habitatLabel.text = isTurkish ? "SIĞINAK (İÇERİ - GÜVENLİ)" : "HABITAT SHELTER (INSIDE)";
            if (solarLabel != null)
                solarLabel.text = isTurkish ? "GÜNEŞ PANELLERİ (DIŞARI)" : "SOLAR ARRAY (OUTSIDE)";
            if (systemBayLabel != null)
                systemBayLabel.text = isTurkish ? "O2 & SU" : "O2 / WATER";
            if (galleyLabel != null)
                galleyLabel.text = isTurkish ? "MUTFAK" : "GALLEY";
            if (labLabel != null)
                labLabel.text = isTurkish ? "BİLİM LAB" : "SCIENCE LAB";
            if (bunksLabel != null)
                bunksLabel.text = isTurkish ? "DİNLENME / UYKU" : "REST BUNKS";
            if (airlockLabel != null)
                airlockLabel.text = isTurkish ? "HAVA KİLİDİ" : "AIRLOCK";
        }

        public void SetSelectedCrew(string crewId)
        {
            SelectedCrewId = crewId;
        }

        public void ApplyState(MissionState state)
        {
            if (state == null) return;
            latestState = state;

            if (string.IsNullOrEmpty(SelectedCrewId) && state.Crew.Count > 0)
                SelectedCrewId = state.Crew[0].Id;

            if (groundRenderer != null)
            {
                if (terrainSprite != null)
                    groundRenderer.color = state.IsNight ? new Color(0.42f, 0.34f, 0.44f, 1f) : Color.white;
                else
                    groundRenderer.color = state.IsNight ? NightGroundColor : DayGroundColor;
            }

            bool radActive = false;
            bool dustActive = false;
            bool radAlert = false;
            bool dustAlert = false;

            for (int i = 0; i < state.Events.Count; i++)
            {
                var ev = state.Events[i];
                if (ev.Phase == EventPhase.Active)
                {
                    if (ev.Kind == EventKind.Radiation) radActive = true;
                    if (ev.Kind == EventKind.Dust) dustActive = true;
                }
                if (ev.Phase == EventPhase.Warning || ev.Phase == EventPhase.Active)
                {
                    if (ev.Kind == EventKind.Radiation) radAlert = true;
                    if (ev.Kind == EventKind.Dust) dustAlert = true;
                }
            }

            hasRadiationHazard = radAlert;
            hasDustHazard = dustAlert;
            isNightTime = state.IsNight;

            if (hazardOverlayRenderer != null)
            {
                if (radActive) hazardOverlayRenderer.color = RadiationTintColor;
                else if (dustActive) hazardOverlayRenderer.color = DustTintColor;
                else hazardOverlayRenderer.color = Color.clear;
            }

            if (solarPanelRenderer != null)
            {
                float clean = 1f - (float)state.Resources.PanelFouling;
                solarPanelRenderer.color = solarArraySprite != null
                    ? Color.Lerp(new Color(0.64f, 0.46f, 0.32f, 1f), new Color(0.28f, 0.58f, 0.96f, 1f), clean)
                    : Color.Lerp(new Color(0.45f, 0.34f, 0.25f, 1f), new Color(0.18f, 0.38f, 0.68f, 1f), clean);
            }

            if (solarDustOverlayRenderer != null && state.Resources != null)
            {
                float fouling = Mathf.Clamp01((float)state.Resources.PanelFouling);
                solarDustOverlayRenderer.color = new Color(0.78f, 0.44f, 0.22f, fouling * 0.75f);
            }

            EnsureCrewVisuals(state);

            for (int i = 0; i < state.Crew.Count; i++)
            {
                var c = state.Crew[i];
                if (!crewVisuals.TryGetValue(c.Id, out var vis)) continue;

                vis.TargetPos = ComputeTargetPosition(c, i);
                vis.LastStatus = c.Status;
                vis.LastTaskId = c.ActiveTaskId;
                vis.Body.color = GetCrewColor(c);
            }
        }

        private void EnsureCrewVisuals(MissionState state)
        {
            for (int i = 0; i < state.Crew.Count; i++)
            {
                var c = state.Crew[i];
                if (crewVisuals.ContainsKey(c.Id)) continue;

                var root = new GameObject($"Crew_{c.Id}").transform;
                root.SetParent(transform, false);
                Vector2 startPos = IdleSlots[i % IdleSlots.Length];
                root.localPosition = new Vector3(startPos.x, startPos.y, -1f);

                // Shadow under feet
                var shadowSr = CreateSpriteObject("Shadow", new Vector3(0, -0.32f, 0), new Vector2(0.48f, 0.18f), new Color(0f, 0f, 0f, 0.40f), 4, true);
                shadowSr.transform.SetParent(root, false);

                // Rounded boots
                var leftFootSr = CreateSpriteObject("LeftFoot", new Vector3(-0.11f, -0.28f, 0), new Vector2(0.14f, 0.10f), new Color(0.85f, 0.88f, 0.92f, 1f), 5, true);
                leftFootSr.transform.SetParent(root, false);
                var rightFootSr = CreateSpriteObject("RightFoot", new Vector3(0.11f, -0.28f, 0), new Vector2(0.14f, 0.10f), new Color(0.85f, 0.88f, 0.92f, 1f), 5, true);
                rightFootSr.transform.SetParent(root, false);

                // Selection ring
                var ringSr = CreateSpriteObject("Ring", Vector3.zero, new Vector2(0.86f, 0.86f), new Color(1f, 0.85f, 0.2f, 1f), 4, true);
                ringSr.transform.SetParent(root, false);
                ringSr.enabled = false;

                // Body
                var bodySr = astronautSuitSprite != null
                    ? CreateCustomSpriteObject("Body", Vector3.zero, new Vector2(0.70f, 0.70f), Color.white, 6, astronautSuitSprite)
                    : CreateSpriteObject("Body", Vector3.zero, new Vector2(0.56f, 0.56f), Color.white, 6, true);
                bodySr.transform.SetParent(root, false);

                // Visor
                Vector3 visorPos = new Vector3(0, astronautSuitSprite != null ? 0.02f : 0.08f, 0);
                var visorSr = CreateSpriteObject("Visor", visorPos, new Vector2(0.24f, 0.12f), CrewVisorAccents[i % CrewVisorAccents.Length], 7, false);
                visorSr.transform.SetParent(root, false);

                // Status Badge above helmet
                var badgeGo = new GameObject("StatusBadge");
                badgeGo.transform.SetParent(root, false);
                badgeGo.transform.localPosition = new Vector3(0, 0.46f, 0);
                var badgeTm = badgeGo.AddComponent<TextMesh>();
                badgeTm.text = "";
                badgeTm.characterSize = 0.085f;
                badgeTm.fontSize = 36;
                badgeTm.fontStyle = FontStyle.Bold;
                badgeTm.anchor = TextAnchor.MiddleCenter;
                badgeTm.alignment = TextAlignment.Center;
                badgeTm.color = Color.white;

                // Name label below feet
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(root, false);
                labelGo.transform.localPosition = new Vector3(0, -0.48f, 0);
                var tm = labelGo.AddComponent<TextMesh>();
                tm.text = c.Name;
                tm.characterSize = 0.075f;
                tm.fontSize = 32;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;

                // Mars dust puffs (3 recycled sprites)
                var puffs = new DustPuff[3];
                for (int p = 0; p < 3; p++)
                {
                    var pGo = new GameObject($"DustPuff_{c.Id}_{p}");
                    pGo.transform.SetParent(transform, false);
                    var pSr = pGo.AddComponent<SpriteRenderer>();
                    pSr.sprite = circleSprite;
                    pSr.sortingOrder = 4;
                    pSr.enabled = false;
                    puffs[p] = new DustPuff
                    {
                        Transform = pGo.transform,
                        Renderer = pSr,
                        Age = 0f,
                        Lifetime = 0.38f,
                        Active = false
                    };
                }

                crewVisuals[c.Id] = new CrewVisual
                {
                    Id = c.Id,
                    Index = i,
                    Root = root,
                    Shadow = shadowSr,
                    LeftFoot = leftFootSr,
                    RightFoot = rightFootSr,
                    Ring = ringSr,
                    Body = bodySr,
                    Visor = visorSr,
                    StatusBadge = badgeTm,
                    NameLabel = tm,
                    TargetPos = startPos,
                    BodyBaseScale = bodySr.transform.localScale,
                    VisorBasePos = visorPos,
                    LastStatus = c.Status,
                    LastTaskId = c.ActiveTaskId,
                    LastFacing = 1,
                    DustPuffs = puffs,
                    PuffSpawnTimer = 0f,
                    PuffIndex = 0
                };
            }
        }

        private Vector2 ComputeTargetPosition(CrewState crew, int index)
        {
            if (crew.Status == CrewStatus.Working && crew.ActiveTaskId != null)
            {
                if (crew.ActiveTaskId == "panel-maintenance") return SolarArrayPos + new Vector2(0, -0.35f);
                if (crew.ActiveTaskId == "system-maintenance") return SystemBayPos + new Vector2(0, -0.35f);
                if (crew.ActiveTaskId == "meal-prep") return GalleyPos + new Vector2(0, -0.35f);
                if (crew.ActiveTaskId == "research") return ResearchLabPos + new Vector2(0, -0.35f);
            }

            if (crew.Status == CrewStatus.Returning)
            {
                return IdleSlots[index % IdleSlots.Length];
            }

            if (crew.Status == CrewStatus.Sleeping || crew.Status == CrewStatus.Resting)
            {
                return new Vector2(BunksPos.x + (index - 1) * 0.45f, BunksPos.y);
            }

            if (crew.Location == CrewLocation.Outside)
                return SolarArrayPos + new Vector2(0.3f * (index - 1), -1.0f);

            return IdleSlots[index % IdleSlots.Length];
        }

        private static Color GetCrewColor(CrewState crew)
        {
            switch (crew.Status)
            {
                case CrewStatus.Exhausted: return new Color(0.88f, 0.28f, 0.28f, 1f);
                case CrewStatus.Sleeping: return new Color(0.55f, 0.60f, 0.92f, 1f);
                case CrewStatus.Resting: return new Color(0.35f, 0.82f, 0.58f, 1f);
                case CrewStatus.Working: return new Color(0.95f, 0.72f, 0.25f, 1f);
                case CrewStatus.Returning: return new Color(0.95f, 0.50f, 0.22f, 1f);
                default: return new Color(0.85f, 0.92f, 0.98f, 1f);
            }
        }

        private static Vector2 GetNextWaypoint(Vector2 current, Vector2 target)
        {
            // Inside to Outside
            if (current.x < HabitatWallX && target.x >= HabitatWallX)
            {
                if (Vector2.Distance(current, AirlockInnerWaypoint) > 0.12f)
                    return AirlockInnerWaypoint;
                return AirlockOuterWaypoint;
            }

            // Still inside airlock tunnel heading Outside
            if (current.x >= HabitatWallX && target.x >= HabitatWallX && current.x < AirlockOuterWaypoint.x)
            {
                return AirlockOuterWaypoint;
            }

            // Outside to Inside
            if (current.x > HabitatWallX && target.x < HabitatWallX)
            {
                if (current.x >= AirlockOuterWaypoint.x && Vector2.Distance(current, AirlockOuterWaypoint) > 0.12f)
                    return AirlockOuterWaypoint;
                return AirlockInnerWaypoint;
            }

            // Still inside airlock tunnel heading Inside
            if (current.x <= HabitatWallX && target.x < HabitatWallX && current.x > AirlockInnerWaypoint.x)
            {
                return AirlockInnerWaypoint;
            }

            // Both on same side of habitat wall
            return target;
        }

        private void Update()
        {
            UpdateCrewVisuals();
            UpdateAirlock();
            UpdateStationActivityBars();
            UpdateEnvParticles();
            HandleWorldClick();
        }

        private void UpdateCrewVisuals()
        {
            foreach (var kv in crewVisuals)
            {
                var vis = kv.Value;
                Vector2 currentPos = new Vector2(vis.Root.localPosition.x, vis.Root.localPosition.y);
                float distToTarget = Vector2.Distance(currentPos, vis.TargetPos);
                bool isMoving = (distToTarget > 0.08f);

                // Selection Ring
                bool isSelected = (vis.Id == SelectedCrewId);
                vis.Ring.enabled = isSelected;
                if (isSelected)
                {
                    float ringScale = 0.86f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.08f;
                    vis.Ring.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                }

                // Status Badge text
                string badge;
                if (vis.LastStatus == CrewStatus.Exhausted)
                    badge = "⚠️";
                else if (vis.LastStatus == CrewStatus.Returning)
                    badge = "🏃💨";
                else if (isMoving)
                    badge = "🚶";
                else if (vis.LastStatus == CrewStatus.Working)
                {
                    if (vis.LastTaskId == "panel-maintenance") badge = "☀️ ✨";
                    else if (vis.LastTaskId == "system-maintenance") badge = "🔧 💧";
                    else if (vis.LastTaskId == "meal-prep") badge = "🍲 🔥";
                    else if (vis.LastTaskId == "research") badge = "🔬 📡";
                    else badge = "🔧";
                }
                else if (vis.LastStatus == CrewStatus.Sleeping)
                    badge = "😴 Zzz";
                else if (vis.LastStatus == CrewStatus.Resting)
                    badge = "🛋️";
                else
                    badge = "";

                vis.StatusBadge.text = badge;

                // Pathfinding & Movement
                if (isMoving)
                {
                    Vector2 nextWp = GetNextWaypoint(currentPos, vis.TargetPos);
                    Vector2 dir = nextWp - currentPos;
                    if (Mathf.Abs(dir.x) > 0.02f)
                        vis.LastFacing = (dir.x > 0 ? 1 : -1);

                    float walkSpeed = (vis.LastStatus == CrewStatus.Returning) ? 4.5f : 3.2f;
                    Vector2 newPos = Vector2.MoveTowards(currentPos, nextWp, walkSpeed * Time.unscaledDeltaTime);
                    vis.Root.localPosition = new Vector3(newPos.x, newPos.y, -1f);

                    // Procedural foot stride & body bob
                    float stride = Mathf.Sin(Time.unscaledTime * 15f + vis.Index * 2.1f);
                    vis.LeftFoot.transform.localPosition = new Vector3(-0.11f + stride * 0.05f * vis.LastFacing, -0.28f + Mathf.Max(0, stride) * 0.06f, 0);
                    vis.RightFoot.transform.localPosition = new Vector3(0.11f - stride * 0.05f * vis.LastFacing, -0.28f + Mathf.Max(0, -stride) * 0.06f, 0);

                    float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 15f + vis.Index * 2.1f)) * 0.055f;
                    vis.Body.transform.localPosition = new Vector3(0, bob, 0);
                    vis.Body.transform.localRotation = Quaternion.Euler(0, 0, stride * 7f * (vis.LastFacing > 0 ? -1f : 1f));
                    vis.Body.transform.localScale = vis.BodyBaseScale;

                    vis.Visor.transform.localPosition = vis.VisorBasePos + new Vector3(vis.LastFacing * 0.04f, bob, 0);
                    vis.StatusBadge.transform.localPosition = new Vector3(0, 0.46f + bob, 0);

                    // Mars dust puffs behind boots when outside
                    if (newPos.x > 0.4f)
                    {
                        vis.PuffSpawnTimer -= Time.unscaledDeltaTime;
                        if (vis.PuffSpawnTimer <= 0f)
                        {
                            vis.PuffSpawnTimer = 0.18f;
                            var puff = vis.DustPuffs[vis.PuffIndex % vis.DustPuffs.Length];
                            vis.PuffIndex++;
                            puff.Transform.localPosition = new Vector3(newPos.x - vis.LastFacing * 0.12f, newPos.y - 0.28f, -0.5f);
                            puff.Age = 0f;
                            puff.Lifetime = 0.38f;
                            puff.Active = true;
                            puff.Renderer.enabled = true;
                        }
                    }
                }
                else
                {
                    // Rest feet and visor
                    vis.LeftFoot.transform.localPosition = new Vector3(-0.11f, -0.28f, 0);
                    vis.RightFoot.transform.localPosition = new Vector3(0.11f, -0.28f, 0);
                    vis.Visor.transform.localPosition = vis.VisorBasePos;

                    if (vis.LastStatus == CrewStatus.Working)
                    {
                        // Gentle rhythmic working motion (6 Hz pulse)
                        float workPulse = Mathf.Sin(Time.unscaledTime * 6f * Mathf.PI * 2f + vis.Index);
                        vis.Body.transform.localRotation = Quaternion.Euler(0, 0, workPulse * 3.5f);
                        vis.Body.transform.localScale = vis.BodyBaseScale * (1f + workPulse * 0.03f);
                        vis.Body.transform.localPosition = new Vector3(0, workPulse * 0.02f, 0);
                        vis.StatusBadge.transform.localPosition = new Vector3(0, 0.46f, 0);
                    }
                    else if (vis.LastStatus == CrewStatus.Sleeping || vis.LastStatus == CrewStatus.Resting)
                    {
                        // Gentle breathing scale pulse (1.0 +/- 0.04) and floating bob on status text
                        float breath = Mathf.Sin(Time.unscaledTime * 2.5f + vis.Index);
                        vis.Body.transform.localRotation = Quaternion.identity;
                        vis.Body.transform.localScale = vis.BodyBaseScale * (1f + breath * 0.04f);
                        vis.Body.transform.localPosition = Vector3.zero;
                        vis.StatusBadge.transform.localPosition = new Vector3(0, 0.46f + breath * 0.04f, 0);
                    }
                    else
                    {
                        vis.Body.transform.localRotation = Quaternion.identity;
                        vis.Body.transform.localScale = vis.BodyBaseScale;
                        vis.Body.transform.localPosition = Vector3.zero;
                        vis.StatusBadge.transform.localPosition = new Vector3(0, 0.46f, 0);
                    }
                }

                // Update active dust puffs
                for (int p = 0; p < vis.DustPuffs.Length; p++)
                {
                    var puff = vis.DustPuffs[p];
                    if (puff.Active)
                    {
                        puff.Age += Time.unscaledDeltaTime;
                        float t = puff.Age / puff.Lifetime;
                        if (t >= 1.0f)
                        {
                            puff.Active = false;
                            puff.Renderer.enabled = false;
                        }
                        else
                        {
                            float s = Mathf.Lerp(0.10f, 0.28f, t);
                            puff.Transform.localScale = new Vector3(s, s * 0.65f, 1f);
                            puff.Renderer.color = new Color(0.85f, 0.52f, 0.30f, Mathf.Lerp(0.55f, 0f, t));
                        }
                    }
                }
            }
        }

        private void UpdateAirlock()
        {
            // Proximity check: x between -0.6f and 1.3f, y between -1.1f and 0.3f
            bool someoneNearAirlock = false;
            foreach (var kv in crewVisuals)
            {
                Vector3 pos = kv.Value.Root.localPosition;
                if (pos.x >= -0.6f && pos.x <= 1.3f && pos.y >= -1.1f && pos.y <= 0.3f)
                {
                    someoneNearAirlock = true;
                    break;
                }
            }

            float targetDoor = someoneNearAirlock ? 1.0f : 0.0f;
            doorOpenProgress = Mathf.MoveTowards(doorOpenProgress, targetDoor, Time.unscaledDeltaTime * 3.5f);

            if (innerDoorTransform != null)
            {
                innerDoorTransform.localPosition = new Vector3(-0.05f, Mathf.Lerp(-0.35f, 0.08f, doorOpenProgress), 0);
                innerDoorTransform.localScale = new Vector3(0.18f, Mathf.Lerp(0.72f, 0.10f, doorOpenProgress), 1f);
            }
            if (outerDoorTransform != null)
            {
                outerDoorTransform.localPosition = new Vector3(0.85f, Mathf.Lerp(-0.35f, 0.08f, doorOpenProgress), 0);
                outerDoorTransform.localScale = new Vector3(0.18f, Mathf.Lerp(0.72f, 0.10f, doorOpenProgress), 1f);
            }

            // Emergency Beacon Light
            if (beaconLightRenderer != null && beaconHaloRenderer != null)
            {
                if (hasRadiationHazard)
                {
                    float p = Mathf.Sin(Time.unscaledTime * 14f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(new Color(1f, 0.20f, 0.10f, 0.95f), new Color(1f, 0.70f, 0.20f, 1f), p);
                    beaconLightRenderer.color = col;
                    beaconHaloRenderer.color = new Color(col.r, col.g, col.b, 0.45f * p);
                    beaconHaloRenderer.transform.localScale = Vector3.one * (0.42f + p * 0.30f);
                }
                else if (hasDustHazard)
                {
                    float p = Mathf.Sin(Time.unscaledTime * 10f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(new Color(0.95f, 0.45f, 0.05f, 0.95f), new Color(1f, 0.85f, 0.20f, 1f), p);
                    beaconLightRenderer.color = col;
                    beaconHaloRenderer.color = new Color(col.r, col.g, col.b, 0.40f * p);
                    beaconHaloRenderer.transform.localScale = Vector3.one * (0.40f + p * 0.26f);
                }
                else
                {
                    beaconLightRenderer.color = new Color(0.20f, 0.82f, 0.52f, 0.75f);
                    beaconHaloRenderer.color = new Color(0.20f, 0.82f, 0.52f, 0.20f);
                    beaconHaloRenderer.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
                }
            }
        }

        private void UpdateStationActivityBars()
        {
            if (latestState == null) return;

            for (int i = 0; i < stationIndicators.Count; i++)
            {
                var ind = stationIndicators[i];
                TaskState task = null;
                for (int t = 0; t < latestState.Tasks.Count; t++)
                {
                    if (latestState.Tasks[t].Id == ind.TaskId)
                    {
                        task = latestState.Tasks[t];
                        break;
                    }
                }

                bool isActive = false;
                float progress = 0f;
                bool isContinuous = false;

                if (task != null && !string.IsNullOrEmpty(task.AssignedCrewId) && !task.IsCompleted)
                {
                    if (crewVisuals.TryGetValue(task.AssignedCrewId, out var crewVis))
                    {
                        float dist = Vector2.Distance(crewVis.Root.localPosition, ind.StationPos);
                        if (dist < 1.35f && crewVis.LastStatus == CrewStatus.Working)
                        {
                            isActive = true;
                            isContinuous = task.IsContinuous;
                            progress = task.RequiredSeconds > 0
                                ? Mathf.Clamp01((float)(task.ProgressSeconds / task.RequiredSeconds))
                                : 0f;
                        }
                    }
                }

                if (isActive)
                {
                    if (!ind.BarRoot.activeSelf) ind.BarRoot.SetActive(true);
                    if (!ind.GlowRenderer.enabled) ind.GlowRenderer.enabled = true;

                    float pulse = Mathf.Sin(Time.unscaledTime * 5.5f) * 0.5f + 0.5f;
                    ind.GlowRenderer.color = new Color(ind.ThemeColor.r, ind.ThemeColor.g, ind.ThemeColor.b, 0.20f + pulse * 0.25f);

                    if (isContinuous)
                    {
                        float scan = Mathf.PingPong(Time.unscaledTime * 0.85f, 1.0f);
                        float waveWidth = ind.BarWidth * 0.35f;
                        float posX = Mathf.Lerp(-ind.BarWidth * 0.5f + waveWidth * 0.5f, ind.BarWidth * 0.5f - waveWidth * 0.5f, scan);
                        ind.BarFill.transform.localScale = new Vector3(waveWidth, 0.07f, 1f);
                        ind.BarFill.transform.localPosition = new Vector3(posX, 0, 0);
                        ind.BarFill.color = Color.Lerp(ind.ThemeColor, Color.white, pulse * 0.5f);
                    }
                    else
                    {
                        float fillW = ind.BarWidth * Mathf.Max(0.02f, progress);
                        ind.BarFill.transform.localScale = new Vector3(fillW, 0.07f, 1f);
                        ind.BarFill.transform.localPosition = new Vector3(-ind.BarWidth * 0.5f + fillW * 0.5f, 0, 0);
                        ind.BarFill.color = ind.ThemeColor;
                    }
                }
                else
                {
                    if (ind.BarRoot.activeSelf) ind.BarRoot.SetActive(false);
                    if (ind.GlowRenderer.enabled) ind.GlowRenderer.enabled = false;
                }
            }
        }

        private void UpdateEnvParticles()
        {
            if (hasDustHazard)
            {
                for (int i = 0; i < envParticles.Count; i++)
                {
                    var p = envParticles[i];
                    p.Renderer.enabled = true;
                    p.Renderer.sortingOrder = 14;
                    p.Transform.localScale = new Vector3(1.2f, 0.08f, 1f);
                    p.Renderer.color = new Color(0.86f, 0.52f, 0.26f, 0.45f);
                    p.Transform.localRotation = Quaternion.Euler(0, 0, -4f);

                    Vector3 pos = p.Transform.localPosition;
                    pos.x += p.Speed * Time.unscaledDeltaTime;
                    if (pos.x > 7.5f)
                    {
                        pos.x = -7.5f;
                        pos.y = -3.5f + ((i * 3 + (int)(Time.unscaledTime * 2)) % 11) * 0.7f;
                    }
                    p.Transform.localPosition = pos;
                }
            }
            else if (hasRadiationHazard)
            {
                for (int i = 0; i < envParticles.Count; i++)
                {
                    var p = envParticles[i];
                    p.Renderer.enabled = true;
                    p.Renderer.sortingOrder = 14;
                    p.Transform.localScale = new Vector3(0.08f, 1.4f, 1f);
                    Color col = (i % 2 == 0) ? new Color(1.0f, 0.85f, 0.22f, 0.65f) : new Color(0.95f, 0.30f, 0.80f, 0.65f);
                    p.Renderer.color = col;
                    p.Transform.localRotation = Quaternion.Euler(0, 0, 15f);

                    Vector3 pos = p.Transform.localPosition;
                    pos.y -= p.Speed * 2.2f * Time.unscaledDeltaTime;
                    pos.x -= p.Speed * 0.4f * Time.unscaledDeltaTime;
                    if (pos.y < -4.5f)
                    {
                        pos.y = 4.8f;
                        pos.x = -6.5f + ((i * 5 + (int)(Time.unscaledTime * 3)) % 13) * 1.0f;
                    }
                    p.Transform.localPosition = pos;
                }
            }
            else if (isNightTime)
            {
                for (int i = 0; i < envParticles.Count; i++)
                {
                    var p = envParticles[i];
                    p.Renderer.enabled = true;
                    p.Renderer.sortingOrder = -15;
                    p.Transform.localScale = new Vector3(0.08f, 0.08f, 1f);
                    p.Transform.localRotation = Quaternion.identity;

                    Vector3 pos = p.Transform.localPosition;
                    if (pos.y < 3.2f || pos.y > 4.6f)
                    {
                        pos.y = 3.25f + ((i * 3) % 7) * 0.18f;
                        pos.x = -6.2f + (i * 0.95f);
                        p.Transform.localPosition = pos;
                    }

                    float twinkle = Mathf.PingPong(Time.unscaledTime * (1.6f + p.TwinklePhase) + p.TwinklePhase, 0.75f) + 0.25f;
                    p.Renderer.color = new Color(0.92f, 0.95f, 1f, twinkle);
                }
            }
            else
            {
                for (int i = 0; i < envParticles.Count; i++)
                {
                    if (envParticles[i].Renderer.enabled)
                        envParticles[i].Renderer.enabled = false;
                }
            }
        }

        private void HandleWorldClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 world = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCam.transform.position.z));
            Vector2 world2D = new Vector2(world.x, world.y);

            string bestId = null;
            float bestDistSq = 0.65f * 0.65f;
            foreach (var kv in crewVisuals)
            {
                Vector2 pos = kv.Value.Root.position;
                float dSq = (pos - world2D).sqrMagnitude;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestId = kv.Key;
                }
            }

            if (bestId != null)
            {
                SelectedCrewId = bestId;
                CrewSelected?.Invoke(bestId);
                return;
            }

            // Direct World Station Clicking
            if (Vector2.Distance(world2D, SolarArrayPos) <= 1.4f || (world2D.x >= 1.6f && world2D.x <= 4.1f && world2D.y >= -0.7f && world2D.y <= 1.5f))
            {
                StationClicked?.Invoke("panel-maintenance");
            }
            else if (Vector2.Distance(world2D, SystemBayPos) <= 0.85f)
            {
                StationClicked?.Invoke("system-maintenance");
            }
            else if (Vector2.Distance(world2D, GalleyPos) <= 0.85f)
            {
                StationClicked?.Invoke("meal-prep");
            }
            else if (Vector2.Distance(world2D, ResearchLabPos) <= 0.85f)
            {
                StationClicked?.Invoke("research");
            }
            else if (Vector2.Distance(world2D, new Vector2(BunksPos.x, BunksPos.y - 0.3f)) <= 1.0f || Vector2.Distance(world2D, new Vector2(0.38f, -0.35f)) <= 0.85f)
            {
                StationClicked?.Invoke("rest-or-return");
            }
        }
    }
}
