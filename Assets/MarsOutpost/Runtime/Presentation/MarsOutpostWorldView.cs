using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using MarsOutpost;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Full 3D Mars Outpost World View in 2.5D perspective.
    /// Features a 96x64 sculpted Mars surface mesh from NASA Mastcam imagery,
    /// cutaway pressurized habitat dome with animated sliding 3D airlock,
    /// distinct interactive 3D stations with floating billboard progress bars,
    /// deadlock-free waypoint queue pathfinding, articulated 3D astronauts,
    /// and dynamic day/night/storm lighting and environmental particle systems.
    /// </summary>
    public sealed class MarsOutpostWorldView : MonoBehaviour
    {
        // -------------------------------------------------------------
        // Exact 3D Coordinate Contract (Y = 0 ground plane)
        // -------------------------------------------------------------
        private static readonly Vector3 HabitatCenter = new Vector3(-2.0f, 0f, 0f);
        private const float HabitatWallX = 1.20f;

        private static readonly Vector3 AirlockInnerPos = new Vector3(1.20f, 0f, -0.35f);
        private static readonly Vector3 AirlockOuterPos = new Vector3(2.45f, 0f, -0.35f);

        private static readonly Vector3 SystemBayPos = new Vector3(-4.0f, 0f, 2.05f);
        private static readonly Vector3 GalleyPos = new Vector3(-2.0f, 0f, 2.05f);
        private static readonly Vector3 ResearchLabPos = new Vector3(0.0f, 0f, 2.05f);
        private static readonly Vector3 BunksPos = new Vector3(-3.65f, 0f, -1.95f);
        private static readonly Vector3 SolarArrayPos = new Vector3(4.65f, 0f, 0.25f);
        private static readonly Vector3 SolarWorkPos = new Vector3(4.35f, 0f, -1.15f);

        private static readonly Vector3[] IdleSlots =
        {
            new Vector3(-3.2f, 0f, -0.55f),
            new Vector3(-1.6f, 0f, -0.85f),
            new Vector3(0.0f, 0f, -0.55f)
        };

        private static readonly Vector3[] RockLocations =
        {
            new Vector3(-6.5f, 0f, -5.2f),
            new Vector3(-8.2f, 0f, -3.8f),
            new Vector3(-7.5f, 0f, -1.2f),
            new Vector3(-8.8f, 0f, 1.5f),
            new Vector3(-6.2f, 0f, 4.5f),
            new Vector3(-8.0f, 0f, 6.2f),
            new Vector3(-3.5f, 0f, 5.8f),
            new Vector3(-1.2f, 0f, 6.5f),
            new Vector3(1.5f, 0f, 5.5f),
            new Vector3(4.2f, 0f, 5.8f),
            new Vector3(6.8f, 0f, 4.8f),
            new Vector3(8.5f, 0f, 2.2f),
            new Vector3(7.2f, 0f, -0.5f),
            new Vector3(6.5f, 0f, -3.2f),
            new Vector3(8.2f, 0f, -4.5f),
            new Vector3(4.8f, 0f, -5.5f),
            new Vector3(1.8f, 0f, -5.2f),
            new Vector3(-2.5f, 0f, -5.8f)
        };

        private static readonly Color[] CrewVisorAccents =
        {
            new Color(0.96f, 0.72f, 0.22f, 1f), // Crew 0: Gold
            new Color(0.28f, 0.80f, 0.96f, 1f), // Crew 1: Cyan
            new Color(0.42f, 0.88f, 0.54f, 1f)  // Crew 2: Emerald
        };

        // -------------------------------------------------------------
        // Serialized Sprite Fields (Preserved for Asset Inspector Compatibility)
        // -------------------------------------------------------------
        [Header("Art Sprite Assets (Optional Fallback to Procedural Shapes)")]
        [SerializeField] private Sprite terrainSprite;
        [SerializeField] private Sprite habitatSprite;
        [SerializeField] private Sprite lifeSupportSprite;
        [SerializeField] private Sprite galleySprite;
        [SerializeField] private Sprite labSprite;
        [SerializeField] private Sprite solarArraySprite;
        [SerializeField] private Sprite astronautSuitSprite;

        // -------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------
        public string SelectedCrewId { get; private set; }
        public event Action<string> CrewSelected;
        public event Action<string> StationClicked;

        // Dome Roof Toggle API
        public bool IsDomeRoofVisible { get; private set; }
        public event Action<bool> DomeRoofChanged;

        public void SetDomeRoofVisible(bool visible)
        {
            if (IsDomeRoofVisible == visible) return;
            IsDomeRoofVisible = visible;
            if (domeRoofGroup != null)
                domeRoofGroup.SetActive(visible);
            DomeRoofChanged?.Invoke(visible);
        }

        public void ToggleDomeRoof()
        {
            SetDomeRoofVisible(!IsDomeRoofVisible);
        }

        // -------------------------------------------------------------
        // Internal State & Visual Hierarchy References
        // -------------------------------------------------------------
        private Camera mainCam;
        private Light sceneDirLight;
        private Light sceneFillLight;
        private bool isTurkish = true;
        private MissionState latestState;

        private bool hasRadiationHazard;
        private bool hasDustHazard;
        private bool isNightTime;

        // Habitat & Airlock components
        private GameObject domeRoofGroup;
        private Transform innerDoorTransform;
        private Transform outerDoorTransform;
        private float doorOpenProgress = 0f;
        private Renderer beaconSphereRenderer;
        private Light beaconLight;
        private Light interiorDomeLight;

        // Solar Array components
        private Renderer solarPanelRenderer;
        private Renderer solarDustPlateRenderer;

        // Animated Station parts
        private readonly List<Transform> o2Bubbles = new List<Transform>();
        private Transform marsHoloSphere;
        private Transform marsHoloRing;
        private Light galleyGrowLight;

        // World labels (Billboarded TextMesh)
        private TextMesh habitatLabel;
        private TextMesh solarLabel;
        private TextMesh systemBayLabel;
        private TextMesh galleyLabel;
        private TextMesh labLabel;
        private TextMesh bunksLabel;
        private TextMesh airlockLabel;

        // Station progress indicators
        private readonly List<StationActivityIndicator> stationIndicators = new List<StationActivityIndicator>();

        // Crew visual state
        private readonly Dictionary<string, CrewVisual> crewVisuals = new Dictionary<string, CrewVisual>();

        // Environmental 3D Particles
        private readonly List<EnvParticle3D> envParticles = new List<EnvParticle3D>();

        // Materials cache
        private Material terrainMat;
        private Material metalMat;
        private Material deckMat;
        private Material glassMat;
        private Material beaconMat;
        private Material solarMat;
        private Material solarDustMat;
        private Material suitMat;
        private Material bootMat;
        private Material ringMat;

        // -------------------------------------------------------------
        // Inner Data Classes
        // -------------------------------------------------------------
        private sealed class DustPuff3D
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Material Material;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private sealed class CrewVisual
        {
            public string Id;
            public int Index;
            public Transform Root;
            public Transform VisualPivot;
            public Transform RingTransform;
            public Renderer RingRenderer;
            public Transform LeftHipPivot;
            public Transform RightHipPivot;
            public Transform LeftShoulderPivot;
            public Transform RightShoulderPivot;
            public Transform HeadPivot;
            public Renderer VisorRenderer;
            public Renderer TorsoRenderer;
            public TextMesh StatusBadge;
            public TextMesh NameLabel;
            public Vector3 TargetPos;
            public CrewStatus LastStatus;
            public string LastTaskId;
            public float StridePhase;
            public DustPuff3D[] DustPuffs;
            public float PuffSpawnTimer;
            public int PuffIndex;

            // Deadlock-free Waypoint Routing
            public readonly List<Vector3> Waypoints = new List<Vector3>();
            public string RouteGoalKey = "";
        }

        private sealed class StationActivityIndicator
        {
            public string TaskId;
            public Vector3 StationPos;
            public GameObject BarRoot;
            public Transform BarFill;
            public Material FillMaterial;
            public Color ThemeColor;
            public float BarWidth;
        }

        private sealed class EnvParticle3D
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Material Material;
            public float Speed;
            public float Phase;
        }

        // -------------------------------------------------------------
        // Lifecycle & Initialization
        // -------------------------------------------------------------
        private void Awake()
        {
            mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.nearClipPlane = 0.5f;
                mainCam.farClipPlane = 160f;
                mainCam.backgroundColor = new Color(0.28f, 0.14f, 0.10f, 1f);
                var camCtrl = mainCam.GetComponent<MarsOutpostCameraController>()
                           ?? mainCam.gameObject.AddComponent<MarsOutpostCameraController>();
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.32f, 0.16f, 0.11f, 1f);
            RenderSettings.fogStartDistance = 36f;
            RenderSettings.fogEndDistance = 78f;

            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].name == "MarsSunLight") sceneDirLight = lights[i];
                else if (lights[i].name == "MarsFillLight") sceneFillLight = lights[i];
                else if (sceneDirLight == null && lights[i].type == LightType.Directional) sceneDirLight = lights[i];
            }

            BuildMaterials();
            Build3DWorldGeometry();
        }

        // -------------------------------------------------------------
        // Shader & Material Helpers (URP / Batchmode Safe)
        // -------------------------------------------------------------
        private static Shader cachedLitShader;
        private static bool shaderResolved;

        private static Shader GetSafeLitShader()
        {
            if (!shaderResolved)
            {
                shaderResolved = true;
                cachedLitShader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Sprites/Default");
            }
            return cachedLitShader;
        }

        private Material CreateMaterial(Color color, float metallic = 0.1f, float smoothness = 0.5f, bool transparent = false, Color? emission = null)
        {
            Shader s = GetSafeLitShader();
            Material mat = (s != null) ? new Material(s) : new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emission.Value);
            }

            if (transparent)
            {
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            return mat;
        }

        private void BuildMaterials()
        {
            terrainMat = CreateTerrainMaterial();
            metalMat = CreateMaterial(new Color(0.70f, 0.74f, 0.80f), metallic: 0.75f, smoothness: 0.65f);
            deckMat = CreateMaterial(new Color(0.18f, 0.22f, 0.27f), metallic: 0.55f, smoothness: 0.35f);
            glassMat = CreateMaterial(new Color(0.25f, 0.60f, 0.82f, 0.28f), metallic: 0.2f, smoothness: 0.95f, transparent: true);
            beaconMat = CreateMaterial(new Color(0.20f, 0.85f, 0.55f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.20f, 0.85f, 0.55f) * 1.5f);
            solarMat = CreateMaterial(new Color(0.12f, 0.30f, 0.65f), metallic: 0.85f, smoothness: 0.85f);
            solarDustMat = CreateMaterial(new Color(0.78f, 0.44f, 0.22f, 0f), metallic: 0f, smoothness: 0.1f, transparent: true);
            // Off-white fabric main suit (metallic 0.08, smoothness 0.35 - NOT washed-out 0.95 peak white!)
            suitMat = CreateMaterial(new Color(0.84f, 0.86f, 0.89f), metallic: 0.08f, smoothness: 0.35f);
            // Dark graphite belt/joints/shoulder collars/knee guards/gloves/boots
            bootMat = CreateMaterial(new Color(0.22f, 0.25f, 0.30f), metallic: 0.55f, smoothness: 0.45f);
            ringMat = CreateMaterial(new Color(1f, 0.85f, 0.2f, 0.75f), metallic: 0f, smoothness: 0.8f, transparent: true, emission: new Color(1f, 0.85f, 0.2f) * 1.8f);
        }

        private Material CreateTerrainMaterial()
        {
            Material mat = CreateMaterial(Color.white, metallic: 0.04f, smoothness: 0.14f);
            Texture2D tex = null;
            if (terrainSprite != null && terrainSprite.texture != null)
                tex = terrainSprite.texture;
            else
                tex = CreateFallbackTerrainTexture();

            tex.anisoLevel = 8;
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);

            // High-detail tiled Mars regolith micro-texture + normal map to eliminate 72m stretch blur
            CreateRegolithDetailTextures(out var detailAlbedo, out var detailNormal);
            if (mat.HasProperty("_DetailAlbedoMap"))
            {
                mat.SetTexture("_DetailAlbedoMap", detailAlbedo);
                mat.SetTextureScale("_DetailAlbedoMap", new Vector2(26f, 26f));
            }
            if (mat.HasProperty("_DetailAlbedoMapScale"))
                mat.SetFloat("_DetailAlbedoMapScale", 0.90f);
            if (mat.HasProperty("_DetailNormalMap"))
            {
                mat.SetTexture("_DetailNormalMap", detailNormal);
                mat.SetTextureScale("_DetailNormalMap", new Vector2(26f, 26f));
            }
            if (mat.HasProperty("_DetailNormalMapScale"))
                mat.SetFloat("_DetailNormalMapScale", 0.55f);
            mat.EnableKeyword("_DETAIL_MULX2");

            return mat;
        }

        private static void CreateRegolithDetailTextures(out Texture2D albedoTex, out Texture2D normalTex)
        {
            const int size = 256;
            albedoTex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: false);
            albedoTex.wrapMode = TextureWrapMode.Repeat;
            albedoTex.filterMode = FilterMode.Trilinear;
            albedoTex.anisoLevel = 8;

            normalTex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: true);
            normalTex.wrapMode = TextureWrapMode.Repeat;
            normalTex.filterMode = FilterMode.Trilinear;
            normalTex.anisoLevel = 8;

            var height = new float[size * size];
            var albedoPixels = new Color[size * size];
            var normalPixels = new Color[size * size];

            // Seamless periodic multi-octave regolith heightfield (ripples + fine sand + pebbles)
            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                float ay = ny * Mathf.PI * 2f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    float ax = nx * Mathf.PI * 2f;

                    // Seamless 2D periodic coordinates via 4D torus projection into PerlinNoise
                    float n1 = Mathf.PerlinNoise(12.3f + Mathf.Cos(ax) * 1.4f + Mathf.Sin(ay) * 0.4f, 45.7f + Mathf.Sin(ax) * 1.4f + Mathf.Cos(ay) * 1.4f);
                    float n2 = Mathf.PerlinNoise(78.1f + Mathf.Cos(ax * 3f) * 2.2f, 19.4f + Mathf.Sin(ay * 3f) * 2.2f);
                    float ripple = Mathf.Sin(ay * 6f + n1 * 3.0f) * 0.5f + 0.5f;

                    // Deterministic fine grain hash
                    uint h = (uint)(x * 73856093 ^ y * 19349663);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    float grain = ((h & 0xFF) / 255.0f) - 0.5f;

                    float hVal = n1 * 0.45f + n2 * 0.30f + ripple * 0.15f + grain * 0.22f;
                    height[y * size + x] = hVal;

                    // Centered around 0.5 for _DETAIL_MULX2 (0.5 = neutral, <0.5 darker pebble/crevice, >0.5 lighter sand)
                    float shade = Mathf.Clamp(0.50f + (hVal - 0.50f) * 0.42f, 0.28f, 0.72f);
                    albedoPixels[y * size + x] = new Color(shade, shade * 0.98f, shade * 0.95f, 1f);
                }
            }

            const float strength = 2.2f;
            for (int y = 0; y < size; y++)
            {
                int ym = (y - 1 + size) % size;
                int yp = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size;
                    int xp = (x + 1) % size;

                    float dx = (height[y * size + xp] - height[y * size + xm]) * strength;
                    float dy = (height[yp * size + x] - height[ym * size + x]) * strength;
                    Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                    normalPixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            albedoTex.SetPixels(albedoPixels);
            albedoTex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            normalTex.SetPixels(normalPixels);
            normalTex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        }

        private static Texture2D CreateFallbackTerrainTexture()
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            Color c1 = new Color(0.78f, 0.44f, 0.28f);
            Color c2 = new Color(0.68f, 0.36f, 0.22f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.25f, y * 0.25f);
                    pixels[y * 32 + x] = Color.Lerp(c1, c2, noise);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // -------------------------------------------------------------
        // Procedural 3D Mesh Generation & Terrain Sculpting
        // -------------------------------------------------------------
        public static float GetTerrainHeight(float x, float z)
        {
            // Flat outpost foundation: x in [-5.8, 6.4], z in [-3.8, 3.8]
            const float fxMin = -5.8f, fxMax = 6.4f, fzMin = -3.8f, fzMax = 3.8f;
            float distOutX = Mathf.Max(0f, Mathf.Max(fxMin - x, x - fxMax));
            float distOutZ = Mathf.Max(0f, Mathf.Max(fzMin - z, z - fzMax));
            float distOut = Mathf.Sqrt(distOutX * distOutX + distOutZ * distOutZ);

            if (distOut <= 0.001f)
                return 0f;

            // Smooth transition over 1.8m from foundation to natural undulating Mars terrain
            float blend = Mathf.Clamp01(distOut / 1.8f);

            // Natural undulating dunes
            float dunes = Mathf.Sin(x * 0.28f + z * 0.18f) * 0.22f + Mathf.Cos(x * 0.14f - z * 0.22f) * 0.18f;

            // Micro-craters: (-7.2, -2.5), (7.8, -2.0), (7.8, 3.8), (-7.5, 4.2)
            float craterDisp = 0f;
            Vector2[] craters =
            {
                new Vector2(-7.2f, -2.5f),
                new Vector2(7.8f, -2.0f),
                new Vector2(7.8f, 3.8f),
                new Vector2(-7.5f, 4.2f)
            };
            for (int i = 0; i < craters.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(x, z), craters[i]);
                const float r = 1.6f;
                if (d < r)
                {
                    float t = d / r;
                    float bowl = -0.38f * (1f - t * t);
                    float rim = 0.16f * Mathf.Sin(t * Mathf.PI);
                    craterDisp += bowl + rim;
                }
            }

            // Gale Crater / Mount Sharp mountain ridge rising cleanly at z > 12f up to y = 4.2f
            float ridge = 0f;
            if (z > 12.0f)
            {
                float rz = Mathf.Clamp01((z - 12.0f) / 22.0f);
                float smoothT = rz * rz * (3f - 2f * rz);
                ridge = smoothT * 4.2f;
                ridge += Mathf.Sin(x * 0.35f) * 0.75f * smoothT + Mathf.Cos(x * 0.85f + 1.2f) * 0.45f * smoothT;
            }

            return (dunes + craterDisp + ridge) * blend;
        }

        private void Build3DWorldGeometry()
        {
            // 1. 96x64 Subdivided Mars Terrain Mesh
            BuildTerrainMesh();

            // 2. Scatter 18 Procedural Faceted 3D Mars Boulders
            BuildPerimeterRocks();

            // 3. Compacted Rover Track Walkway
            BuildRoverTrack();

            // 4. Cutaway Pressurized Habitat Dome & Deck
            BuildHabitatDome();

            // 5. 3D Airlock Chamber & Sliding Doors
            BuildAirlockChamber();

            // 6. Distinct 3D Stations
            BuildStations();

            // 7. Floating 3D Station Progress Bars
            BuildStationProgressBars();

            // 8. 3D Environmental Particle Pool
            BuildEnvironmentalParticlePool();
        }

        private void BuildTerrainMesh()
        {
            var terrainGo = new GameObject("MarsTerrainMesh");
            terrainGo.transform.SetParent(transform, false);

            const int xRes = 120;
            const int zRes = 96;
            const float minX = -36f, maxX = 36f;
            const float minZ = -28f, maxZ = 44f;

            var vertices = new Vector3[(xRes + 1) * (zRes + 1)];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[xRes * zRes * 6];

            int vertIdx = 0;
            for (int iz = 0; iz <= zRes; iz++)
            {
                float tz = (float)iz / zRes;
                float z = Mathf.Lerp(minZ, maxZ, tz);

                for (int ix = 0; ix <= xRes; ix++)
                {
                    float tx = (float)ix / xRes;
                    float x = Mathf.Lerp(minX, maxX, tx);
                    float y = GetTerrainHeight(x, z);

                    vertices[vertIdx] = new Vector3(x, y, z);
                    // UV maps texture clamped between 0.02 and 0.62 so Mount Sharp ridge never samples sky
                    float v = Mathf.Lerp(0.02f, 0.62f, tz);
                    uvs[vertIdx] = new Vector2(tx, v);

                    // Vertex colors: warm ochre plains to darker stratified rock on ridge slopes
                    float ridgeWeight = Mathf.Clamp01((z - 11.5f) / 22.0f);
                    colors[vertIdx] = Color.Lerp(new Color(0.85f, 0.55f, 0.40f), new Color(0.52f, 0.32f, 0.25f), ridgeWeight);

                    vertIdx++;
                }
            }

            int triIdx = 0;
            for (int iz = 0; iz < zRes; iz++)
            {
                for (int ix = 0; ix < xRes; ix++)
                {
                    int row1 = iz * (xRes + 1);
                    int row2 = (iz + 1) * (xRes + 1);

                    triangles[triIdx++] = row1 + ix;
                    triangles[triIdx++] = row2 + ix;
                    triangles[triIdx++] = row1 + ix + 1;

                    triangles[triIdx++] = row1 + ix + 1;
                    triangles[triIdx++] = row2 + ix;
                    triangles[triIdx++] = row2 + ix + 1;
                }
            }

            var mesh = new Mesh
            {
                name = "MarsSurfaceMesh",
                vertices = vertices,
                uv = uvs,
                colors = colors,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = terrainGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = terrainGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = terrainMat;
            mr.receiveShadows = true;

            var mc = terrainGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        private void BuildPerimeterRocks()
        {
            var rocksRoot = new GameObject("MarsPerimeterRocks").transform;
            rocksRoot.SetParent(transform, false);

            var rockMat = CreateMaterial(new Color(0.50f, 0.34f, 0.28f), metallic: 0.25f, smoothness: 0.15f);

            for (int i = 0; i < RockLocations.Length; i++)
            {
                Vector3 loc = RockLocations[i];
                loc.y = GetTerrainHeight(loc.x, loc.z) + 0.15f;

                var rock = CreateCubePrimitive($"Rock_{i}", rocksRoot, loc, new Vector3(0.55f + (i % 3) * 0.18f, 0.38f + (i % 2) * 0.14f, 0.50f + (i % 4) * 0.12f), rockMat);
                rock.transform.localRotation = Quaternion.Euler((i * 37) % 360, (i * 61) % 360, (i * 23) % 45);
            }
        }

        private void BuildRoverTrack()
        {
            var trackMat = CreateMaterial(new Color(0.36f, 0.25f, 0.20f), metallic: 0.2f, smoothness: 0.1f);
            var track = CreateCubePrimitive("RoverTrackWalkway", transform, new Vector3(3.20f, 0.005f, -0.55f), new Vector3(1.80f, 0.01f, 0.70f), trackMat);
            track.transform.localRotation = Quaternion.Euler(0, -12f, 0);
        }

        private void BuildHabitatDome()
        {
            var habRoot = new GameObject("HabitatStructure").transform;
            habRoot.SetParent(transform, false);

            // Titanium Base Pad: 6.5 x 0.20 x 6.1 centered at (-2.0, -0.10, 0)
            CreateCubePrimitive("HabitatBasePad", habRoot, new Vector3(HabitatCenter.x, -0.10f, HabitatCenter.z), new Vector3(6.5f, 0.20f, 6.1f), metalMat);

            // Dark slate interior deck: 6.3 x 0.02 x 5.9 centered at (-2.0, 0.01, 0)
            CreateCubePrimitive("HabitatDeck", habRoot, new Vector3(HabitatCenter.x, 0.01f, HabitatCenter.z), new Vector3(6.3f, 0.02f, 5.9f), deckMat);

            // Perimeter Cutaway Hull Walls
            // West Wall: along x = -5.20f
            CreateCubePrimitive("WestWall", habRoot, new Vector3(-5.20f, 0.35f, 0f), new Vector3(0.18f, 0.70f, 6.0f), metalMat);

            // South Wall: along z = -3.00f with panoramic glowing cyan viewport window
            CreateCubePrimitive("SouthWallBase", habRoot, new Vector3(HabitatCenter.x, 0.25f, -3.00f), new Vector3(6.4f, 0.50f, 0.18f), metalMat);
            var windowMat = CreateMaterial(new Color(0.25f, 0.85f, 0.95f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.25f, 0.85f, 0.95f) * 1.2f);
            CreateCubePrimitive("SouthViewportWindow", habRoot, new Vector3(HabitatCenter.x, 0.28f, -2.91f), new Vector3(4.2f, 0.22f, 0.04f), windowMat);

            // East Wall: along x = 1.20f (split with airlock opening at z in [-0.90, 0.20])
            CreateCubePrimitive("EastWallSouth", habRoot, new Vector3(HabitatWallX, 0.35f, -1.95f), new Vector3(0.18f, 0.70f, 2.10f), metalMat);
            CreateCubePrimitive("EastWallNorth", habRoot, new Vector3(HabitatWallX, 0.35f, 1.45f), new Vector3(0.18f, 0.70f, 2.90f), metalMat);

            // Taller North Bulkhead Wall: along z = 3.00f
            CreateCubePrimitive("NorthBulkhead", habRoot, new Vector3(HabitatCenter.x, 0.55f, 3.00f), new Vector3(6.4f, 1.10f, 0.20f), metalMat);

            // Flanged Pressure Seal Rim Collar atop perimeter walls
            var collarMat = CreateMaterial(new Color(0.78f, 0.82f, 0.88f), metallic: 0.85f, smoothness: 0.75f);
            CreateCubePrimitive("WestCollar", habRoot, new Vector3(-5.20f, 0.72f, 0f), new Vector3(0.26f, 0.06f, 6.04f), collarMat);
            CreateCubePrimitive("NorthCollar", habRoot, new Vector3(HabitatCenter.x, 1.12f, 3.00f), new Vector3(6.44f, 0.06f, 0.26f), collarMat);
            CreateCubePrimitive("SouthCollar", habRoot, new Vector3(HabitatCenter.x, 0.52f, -3.00f), new Vector3(6.44f, 0.06f, 0.26f), collarMat);
            CreateCubePrimitive("EastCollarSouth", habRoot, new Vector3(HabitatWallX, 0.72f, -1.95f), new Vector3(0.26f, 0.06f, 2.14f), collarMat);
            CreateCubePrimitive("EastCollarNorth", habRoot, new Vector3(HabitatWallX, 0.72f, 1.45f), new Vector3(0.26f, 0.06f, 2.94f), collarMat);

            // Beveled Corner Life-Support Scrubbers/Vents in all 4 corners
            BuildCornerScrubber(habRoot, new Vector3(-5.02f, 0.40f, 2.82f), "NW");
            BuildCornerScrubber(habRoot, new Vector3(-5.02f, 0.32f, -2.82f), "SW");
            BuildCornerScrubber(habRoot, new Vector3(1.02f, 0.40f, 2.82f), "NE");
            BuildCornerScrubber(habRoot, new Vector3(1.02f, 0.32f, -2.82f), "SE");

            // Illuminated Interior Trim Strip
            var trimMat = CreateMaterial(new Color(0.28f, 0.85f, 0.95f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.28f, 0.85f, 0.95f) * 1.5f);
            CreateCubePrimitive("TrimWest", habRoot, new Vector3(-5.09f, 0.04f, 0f), new Vector3(0.04f, 0.04f, 5.8f), trimMat);
            CreateCubePrimitive("TrimNorth", habRoot, new Vector3(HabitatCenter.x, 0.04f, 2.89f), new Vector3(6.1f, 0.04f, 0.04f), trimMat);
            CreateCubePrimitive("TrimSouth", habRoot, new Vector3(HabitatCenter.x, 0.04f, -2.89f), new Vector3(6.1f, 0.04f, 0.04f), trimMat);
            CreateCubePrimitive("TrimEastSouth", habRoot, new Vector3(1.09f, 0.04f, -1.95f), new Vector3(0.04f, 0.04f, 1.9f), trimMat);
            CreateCubePrimitive("TrimEastNorth", habRoot, new Vector3(1.09f, 0.04f, 1.45f), new Vector3(0.04f, 0.04f, 2.7f), trimMat);

            // Interior Airlock Bulkhead Arch Framing
            var bulkheadMat = CreateMaterial(new Color(0.65f, 0.68f, 0.74f), metallic: 0.7f, smoothness: 0.6f);
            CreateCubePrimitive("InnerAirlockBulkheadHeader", habRoot, new Vector3(HabitatWallX, 0.98f, -0.35f), new Vector3(0.22f, 0.14f, 1.15f), bulkheadMat);
            CreateCubePrimitive("InnerAirlockBulkheadJambSouth", habRoot, new Vector3(HabitatWallX, 0.48f, -0.92f), new Vector3(0.22f, 0.96f, 0.08f), bulkheadMat);
            CreateCubePrimitive("InnerAirlockBulkheadJambNorth", habRoot, new Vector3(HabitatWallX, 0.48f, 0.22f), new Vector3(0.22f, 0.96f, 0.08f), bulkheadMat);

            // Toggleable 3D Dome Roof Canopy Group
            BuildDomeRoofGroup(habRoot);

            // Interior Warm PointLight
            var lightGo = new GameObject("InteriorDomeLight");
            lightGo.transform.SetParent(habRoot, false);
            lightGo.transform.localPosition = new Vector3(HabitatCenter.x, 2.05f, HabitatCenter.z);
            interiorDomeLight = lightGo.AddComponent<Light>();
            interiorDomeLight.type = LightType.Point;
            interiorDomeLight.color = new Color(1.0f, 0.92f, 0.82f);
            interiorDomeLight.range = 9.5f;
            interiorDomeLight.intensity = 1.4f;

            habitatLabel = CreateWorldLabel("HabitatLabel", new Vector3(-0.6f, 2.55f, 3.05f), isTurkish ? "[MARS SIGINAK - GUVENLI]" : "[HABITAT SHELTER - SAFE]", 0.062f, Color.white);
        }

        private void BuildCornerScrubber(Transform parent, Vector3 pos, string name)
        {
            var scrubberRoot = new GameObject($"AirScrubber_{name}").transform;
            scrubberRoot.SetParent(parent, false);
            scrubberRoot.localPosition = pos;

            var scrubberMat = CreateMaterial(new Color(0.35f, 0.38f, 0.44f), metallic: 0.7f, smoothness: 0.5f);
            var ledMat = CreateMaterial(new Color(0.20f, 0.90f, 0.60f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.20f, 0.90f, 0.60f) * 1.5f);

            CreateCylinderPrimitive("Canister", scrubberRoot, Vector3.zero, new Vector3(0.28f, 0.38f, 0.28f), scrubberMat);
            CreateCubePrimitive("VentGrill", scrubberRoot, new Vector3(0, 0.20f, 0), new Vector3(0.32f, 0.06f, 0.32f), metalMat);
            CreateSpherePrimitive("StatusLed", scrubberRoot, new Vector3(0, 0.24f, 0), new Vector3(0.06f, 0.06f, 0.06f), ledMat);
        }

        private void BuildDomeRoofGroup(Transform parent)
        {
            domeRoofGroup = new GameObject("DomeRoofGroup");
            domeRoofGroup.transform.SetParent(parent, false);

            var frameMat = CreateMaterial(new Color(0.75f, 0.78f, 0.84f), metallic: 0.8f, smoothness: 0.7f);

            // 4 Arched Canopy Ribs resting atop perimeter collars along X
            float[] archXPositions = { -4.4f, -2.8f, -1.2f, 0.4f };
            for (int a = 0; a < archXPositions.Length; a++)
            {
                float ax = archXPositions[a];
                var archRoot = new GameObject($"DomeArch_{a}").transform;
                archRoot.SetParent(domeRoofGroup.transform, false);

                const int segments = 10;
                for (int i = 0; i < segments; i++)
                {
                    float t0 = (float)i / segments;
                    float t1 = (float)(i + 1) / segments;

                    float z0 = Mathf.Lerp(-2.95f, 2.95f, t0);
                    float z1 = Mathf.Lerp(-2.95f, 2.95f, t1);

                    // Spring from collar height (~0.72) up to apex (2.45)
                    float y0 = 0.70f + Mathf.Sin(t0 * Mathf.PI) * 1.75f;
                    float y1 = 0.70f + Mathf.Sin(t1 * Mathf.PI) * 1.75f;

                    Vector3 p0 = new Vector3(ax, y0, z0);
                    Vector3 p1 = new Vector3(ax, y1, z1);

                    Vector3 mid = (p0 + p1) * 0.5f;
                    float len = Vector3.Distance(p0, p1);

                    var seg = CreateCubePrimitive($"ArchSeg_{i}", archRoot, mid, new Vector3(0.07f, 0.07f, len), frameMat);
                    seg.transform.localRotation = Quaternion.LookRotation(p1 - p0);
                }
            }

            // Central longitudinal spine beam connecting the arch apexes
            CreateCubePrimitive("DomeSpine", domeRoofGroup.transform, new Vector3(HabitatCenter.x, 2.45f, 0f), new Vector3(5.2f, 0.08f, 0.08f), frameMat);

            // Geodesic translucent canopy panels between the ribs
            for (int p = 0; p < 3; p++)
            {
                float xMid = Mathf.Lerp(-4.4f, 0.4f, (p + 0.5f) / 3.0f);
                for (int zSeg = 0; zSeg < 4; zSeg++)
                {
                    float tz = (zSeg + 0.5f) / 4.0f;
                    float zMid = Mathf.Lerp(-2.5f, 2.5f, tz);
                    float yMid = 0.75f + Mathf.Sin(tz * Mathf.PI) * 1.68f;

                    var glassPanel = CreateCubePrimitive($"GlassCanopy_{p}_{zSeg}", domeRoofGroup.transform, new Vector3(xMid, yMid, zMid), new Vector3(1.50f, 0.02f, 1.20f), glassMat);
                    float pitchAngle = (tz - 0.5f) * -52f;
                    glassPanel.transform.localRotation = Quaternion.Euler(pitchAngle, 0, 0);
                }
            }

            domeRoofGroup.SetActive(IsDomeRoofVisible);
        }

        private void BuildAirlockChamber()
        {
            var airlockRoot = new GameObject("AirlockChamber").transform;
            airlockRoot.SetParent(transform, false);

            // Floor threshold centered at x = 1.825, z = -0.35
            CreateCubePrimitive("AirlockFloor", airlockRoot, new Vector3(1.825f, 0.02f, -0.35f), new Vector3(1.25f, 0.04f, 1.10f), deckMat);

            // Side walls & hazard trim
            CreateCubePrimitive("AirlockNorthWall", airlockRoot, new Vector3(1.825f, 0.48f, 0.20f), new Vector3(1.25f, 0.96f, 0.10f), metalMat);
            CreateCubePrimitive("AirlockSouthWall", airlockRoot, new Vector3(1.825f, 0.48f, -0.90f), new Vector3(1.25f, 0.96f, 0.10f), metalMat);
            CreateCubePrimitive("AirlockRoof", airlockRoot, new Vector3(1.825f, 0.98f, -0.35f), new Vector3(1.25f, 0.10f, 1.10f), metalMat);

            // Hazard pillars with yellow/black striping accent
            var hazardMat = CreateMaterial(new Color(0.95f, 0.82f, 0.18f), metallic: 0.3f, smoothness: 0.5f);
            CreateCubePrimitive("HazardPillar1", airlockRoot, new Vector3(1.25f, 0.48f, -0.85f), new Vector3(0.08f, 0.96f, 0.08f), hazardMat);
            CreateCubePrimitive("HazardPillar2", airlockRoot, new Vector3(2.40f, 0.48f, -0.85f), new Vector3(0.08f, 0.96f, 0.08f), hazardMat);

            // Sliding Armored Doors: InnerDoor (x = 1.20) and OuterDoor (x = 2.45)
            var doorMat = CreateMaterial(new Color(0.85f, 0.65f, 0.22f), metallic: 0.85f, smoothness: 0.75f);
            var innerDoorGo = CreateCubePrimitive("InnerAirlockDoor", airlockRoot, new Vector3(HabitatWallX, 0.48f, -0.35f), new Vector3(0.08f, 0.88f, 0.70f), doorMat);
            var outerDoorGo = CreateCubePrimitive("OuterAirlockDoor", airlockRoot, new Vector3(2.45f, 0.48f, -0.35f), new Vector3(0.08f, 0.88f, 0.70f), doorMat);

            innerDoorTransform = innerDoorGo.transform;
            outerDoorTransform = outerDoorGo.transform;

            // Roof Emergency Beacon
            var beaconGo = CreateSpherePrimitive("EmergencyBeaconSphere", airlockRoot, new Vector3(1.825f, 1.12f, -0.35f), new Vector3(0.22f, 0.22f, 0.22f), beaconMat);
            beaconSphereRenderer = beaconGo.GetComponent<Renderer>();

            var bLightGo = new GameObject("BeaconLight");
            bLightGo.transform.SetParent(beaconGo.transform, false);
            beaconLight = bLightGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(0.20f, 0.85f, 0.55f);
            beaconLight.range = 4.0f;
            beaconLight.intensity = 0.8f;

            airlockLabel = CreateWorldLabel("AirlockLabel", new Vector3(1.825f, 1.45f, -0.35f), isTurkish ? "[HAVA KILIDI]" : "[AIRLOCK]", 0.055f, new Color(0.95f, 0.90f, 0.75f));
        }

        private void BuildStations()
        {
            // -------------------------------------------------------------
            // Station 1: O2 & Water Recycler (SystemBayPos = (-4.0, 0, 2.05))
            // -------------------------------------------------------------
            var sysRoot = new GameObject("SystemBayStation");
            sysRoot.transform.SetParent(transform, false);
            sysRoot.transform.localPosition = SystemBayPos;
            var sysCol = sysRoot.AddComponent<BoxCollider>();
            sysCol.size = new Vector3(1.6f, 1.8f, 1.2f);
            sysCol.center = new Vector3(0, 0.9f, 0);

            // Base console
            var consoleMat = CreateMaterial(new Color(0.22f, 0.44f, 0.58f), metallic: 0.6f, smoothness: 0.5f);
            CreateCubePrimitive("ConsoleBase", sysRoot.transform, new Vector3(0, 0.32f, 0), new Vector3(1.3f, 0.64f, 0.80f), consoleMat);

            // Twin glowing translucent cyan vertical cylinders
            var glassCylMat = CreateMaterial(new Color(0.25f, 0.85f, 1.0f, 0.45f), metallic: 0.1f, smoothness: 0.95f, transparent: true);
            var cylCapMat = CreateMaterial(new Color(0.75f, 0.80f, 0.85f), metallic: 0.8f, smoothness: 0.7f);

            CreateCylinderPrimitive("CylinderLeft", sysRoot.transform, new Vector3(-0.25f, 0.85f, 0), new Vector3(0.26f, 0.42f, 0.26f), glassCylMat);
            CreateCylinderPrimitive("CylinderRight", sysRoot.transform, new Vector3(0.25f, 0.85f, 0), new Vector3(0.26f, 0.42f, 0.26f), glassCylMat);
            CreateCubePrimitive("TopCapLeft", sysRoot.transform, new Vector3(-0.25f, 1.30f, 0), new Vector3(0.32f, 0.08f, 0.32f), cylCapMat);
            CreateCubePrimitive("TopCapRight", sysRoot.transform, new Vector3(0.25f, 1.30f, 0), new Vector3(0.32f, 0.08f, 0.32f), cylCapMat);
            CreateCubePrimitive("ConnectingPipe", sysRoot.transform, new Vector3(0, 1.30f, 0), new Vector3(0.50f, 0.06f, 0.06f), cylCapMat);

            // 6 Rising animated 3D oxygen bubbles
            var bubbleMat = CreateMaterial(new Color(0.75f, 0.95f, 1.0f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.4f, 0.9f, 1.0f) * 1.5f);
            for (int b = 0; b < 6; b++)
            {
                float xOff = (b < 3) ? -0.25f : 0.25f;
                var bub = CreateSpherePrimitive($"Bubble_{b}", sysRoot.transform, new Vector3(xOff, 0.65f + (b % 3) * 0.22f, 0), new Vector3(0.06f, 0.06f, 0.06f), bubbleMat);
                o2Bubbles.Add(bub.transform);
            }

            systemBayLabel = CreateWorldLabel("SystemBayLabel", SystemBayPos + new Vector3(0, 1.65f, 0), isTurkish ? "[O2 & SU]" : "[O2 / WATER]", 0.055f, new Color(0.85f, 0.95f, 1f));

            // -------------------------------------------------------------
            // Station 2: Galley & Hydroponics (GalleyPos = (-2.0, 0, 2.05))
            // -------------------------------------------------------------
            var galleyRoot = new GameObject("GalleyStation");
            galleyRoot.transform.SetParent(transform, false);
            galleyRoot.transform.localPosition = GalleyPos;
            var galleyCol = galleyRoot.AddComponent<BoxCollider>();
            galleyCol.size = new Vector3(1.6f, 1.8f, 1.2f);
            galleyCol.center = new Vector3(0, 0.9f, 0);

            // Kitchen counter
            var counterMat = CreateMaterial(new Color(0.30f, 0.58f, 0.42f), metallic: 0.45f, smoothness: 0.55f);
            CreateCubePrimitive("CounterBase", galleyRoot.transform, new Vector3(0, 0.32f, 0), new Vector3(1.3f, 0.64f, 0.80f), counterMat);

            // Oven viewport window
            var ovenMat = CreateMaterial(new Color(1.0f, 0.55f, 0.15f), metallic: 0.2f, smoothness: 0.8f, emission: new Color(1.0f, 0.55f, 0.15f) * 1.6f);
            CreateCubePrimitive("OvenViewport", galleyRoot.transform, new Vector3(-0.25f, 0.35f, -0.41f), new Vector3(0.35f, 0.25f, 0.04f), ovenMat);

            // Vertical hydroponic grow rack with 3 shelves
            var rackMat = CreateMaterial(new Color(0.24f, 0.28f, 0.32f), metallic: 0.7f, smoothness: 0.5f);
            CreateCubePrimitive("RackFrame", galleyRoot.transform, new Vector3(0.28f, 0.95f, 0), new Vector3(0.55f, 0.70f, 0.65f), rackMat);

            // Leafy crop clusters
            var cropMat = CreateMaterial(new Color(0.28f, 0.85f, 0.38f), metallic: 0.1f, smoothness: 0.4f);
            for (int s = 0; s < 3; s++)
            {
                float sy = 0.72f + s * 0.22f;
                CreateSpherePrimitive($"CropCluster_{s}", galleyRoot.transform, new Vector3(0.28f, sy, 0), new Vector3(0.42f, 0.12f, 0.50f), cropMat);
            }

            // Purple UV grow light
            var growLightGo = new GameObject("GrowLight");
            growLightGo.transform.SetParent(galleyRoot.transform, false);
            growLightGo.transform.localPosition = new Vector3(0.28f, 1.35f, 0);
            galleyGrowLight = growLightGo.AddComponent<Light>();
            galleyGrowLight.type = LightType.Point;
            galleyGrowLight.color = new Color(0.85f, 0.25f, 0.95f);
            galleyGrowLight.range = 2.5f;
            galleyGrowLight.intensity = 0.8f;

            galleyLabel = CreateWorldLabel("GalleyLabel", GalleyPos + new Vector3(0, 1.65f, 0), isTurkish ? "[MUTFAK]" : "[GALLEY]", 0.055f, new Color(0.86f, 1f, 0.90f));

            // -------------------------------------------------------------
            // Station 3: Science Research Lab (ResearchLabPos = (0.0, 0, 2.05))
            // -------------------------------------------------------------
            var labRoot = new GameObject("ResearchLabStation");
            labRoot.transform.SetParent(transform, false);
            labRoot.transform.localPosition = ResearchLabPos;
            var labCol = labRoot.AddComponent<BoxCollider>();
            labCol.size = new Vector3(1.6f, 1.8f, 1.2f);
            labCol.center = new Vector3(0, 0.9f, 0);

            // Tech console desk
            var labMat = CreateMaterial(new Color(0.42f, 0.36f, 0.68f), metallic: 0.6f, smoothness: 0.6f);
            CreateCubePrimitive("DeskBase", labRoot.transform, new Vector3(0, 0.32f, 0), new Vector3(1.3f, 0.64f, 0.80f), labMat);

            // Telemetry screen
            var screenMat = CreateMaterial(new Color(0.20f, 0.70f, 1.0f), metallic: 0.2f, smoothness: 0.9f, emission: new Color(0.20f, 0.70f, 1.0f) * 1.4f);
            CreateCubePrimitive("Screen", labRoot.transform, new Vector3(0, 0.75f, 0.35f), new Vector3(0.70f, 0.35f, 0.05f), screenMat);

            // Floating slowly-rotating 3D holographic red Mars sphere
            var marsMat = CreateMaterial(new Color(0.92f, 0.42f, 0.26f), metallic: 0.2f, smoothness: 0.7f, emission: new Color(0.92f, 0.42f, 0.26f) * 1.5f);
            var marsGo = CreateSpherePrimitive("HoloMarsSphere", labRoot.transform, new Vector3(0, 1.15f, 0), new Vector3(0.28f, 0.28f, 0.28f), marsMat);
            marsHoloSphere = marsGo.transform;

            // Glowing cyan orbital ring tilted around Mars
            var ringHoloMat = CreateMaterial(new Color(0.30f, 0.90f, 1.0f), metallic: 0.1f, smoothness: 0.9f, emission: new Color(0.30f, 0.90f, 1.0f) * 1.8f);
            var ringGo = CreateCylinderPrimitive("HoloOrbitalRing", labRoot.transform, new Vector3(0, 1.15f, 0), new Vector3(0.48f, 0.015f, 0.48f), ringHoloMat);
            ringGo.transform.localRotation = Quaternion.Euler(35f, 0, 20f);
            marsHoloRing = ringGo.transform;

            labLabel = CreateWorldLabel("LabLabel", ResearchLabPos + new Vector3(0, 1.65f, 0), isTurkish ? "[BILIM LAB]" : "[SCIENCE LAB]", 0.055f, new Color(0.92f, 0.88f, 1f));

            // -------------------------------------------------------------
            // Station 4: Crew Rest Bunks (BunksPos = (-3.65, 0, -1.95))
            // -------------------------------------------------------------
            var bunksRoot = new GameObject("BunksStation");
            bunksRoot.transform.SetParent(transform, false);
            bunksRoot.transform.localPosition = BunksPos;
            var bunksCol = bunksRoot.AddComponent<BoxCollider>();
            bunksCol.size = new Vector3(2.6f, 0.8f, 1.6f);
            bunksCol.center = new Vector3(0, 0.4f, 0);

            var bunkFrameMat = CreateMaterial(new Color(0.24f, 0.28f, 0.35f), metallic: 0.6f, smoothness: 0.4f);
            var pillowMat = CreateMaterial(new Color(0.95f, 0.95f, 0.98f), metallic: 0.05f, smoothness: 0.2f);
            var blanketMat = CreateMaterial(new Color(0.18f, 0.48f, 0.65f), metallic: 0.1f, smoothness: 0.3f);

            // 3 well-spaced bunks at bx = -0.85, 0.0, +0.85
            float[] bunkXOffsets = { -0.85f, 0.0f, 0.85f };
            for (int b = 0; b < bunkXOffsets.Length; b++)
            {
                float bx = bunkXOffsets[b];
                CreateCubePrimitive($"BunkFrame_{b}", bunksRoot.transform, new Vector3(bx, 0.12f, 0), new Vector3(0.58f, 0.22f, 1.25f), bunkFrameMat);
                CreateCubePrimitive($"Pillow_{b}", bunksRoot.transform, new Vector3(bx, 0.26f, -0.44f), new Vector3(0.48f, 0.08f, 0.24f), pillowMat);
                CreateCubePrimitive($"Blanket_{b}", bunksRoot.transform, new Vector3(bx, 0.25f, 0.12f), new Vector3(0.52f, 0.07f, 0.88f), blanketMat);
            }

            bunksLabel = CreateWorldLabel("BunksLabel", BunksPos + new Vector3(0, 0.95f, 0), isTurkish ? "[UYKU / DINLENME]" : "[REST BUNKS]", 0.055f, new Color(0.75f, 0.88f, 1f));

            // -------------------------------------------------------------
            // Station 5: Exterior Solar Array (SolarArrayPos = (4.65, 0, 0.25))
            // -------------------------------------------------------------
            var solarRoot = new GameObject("SolarArrayStation");
            solarRoot.transform.SetParent(transform, false);
            solarRoot.transform.localPosition = SolarArrayPos;
            var solarCol = solarRoot.AddComponent<BoxCollider>();
            solarCol.size = new Vector3(3.2f, 2.2f, 2.2f);
            solarCol.center = new Vector3(0, 1.0f, 0);

            // Ground base mounting plate
            CreateCubePrimitive("PedestalBasePlate", solarRoot.transform, new Vector3(0, 0.04f, 0), new Vector3(0.65f, 0.08f, 0.65f), metalMat);

            // Heavy steel pedestal & gimbal mount
            CreateCylinderPrimitive("Pedestal", solarRoot.transform, new Vector3(0, 0.45f, 0), new Vector3(0.20f, 0.45f, 0.20f), metalMat);
            CreateCubePrimitive("GimbalHub", solarRoot.transform, new Vector3(0, 0.85f, 0), new Vector3(0.26f, 0.26f, 0.26f), metalMat);

            // Rear diagonal support brace for pedestal
            Vector3 rearBase = new Vector3(0, 0.12f, -0.28f);
            Vector3 rearHub = new Vector3(0, 0.80f, 0.16f);
            var rearBrace = CreateCubePrimitive("RearDiagonalBrace", solarRoot.transform, (rearBase + rearHub) * 0.5f, new Vector3(0.08f, 0.08f, Vector3.Distance(rearBase, rearHub)), metalMat);
            rearBrace.transform.localRotation = Quaternion.LookRotation(rearHub - rearBase);

            // Structural aluminum rear support frame under PV wings
            Quaternion wingAngle = Quaternion.Euler(28f, 12f, 0);
            var rearSpine = CreateCubePrimitive("RearSupportSpine", solarRoot.transform, new Vector3(0, 0.90f, 0.10f), new Vector3(3.20f, 0.07f, 0.09f), metalMat);
            rearSpine.transform.localRotation = wingAngle;

            var leftRearRib = CreateCubePrimitive("LeftRearRib", solarRoot.transform, new Vector3(-0.95f, 0.90f, 0.10f), new Vector3(0.09f, 0.07f, 1.24f), metalMat);
            leftRearRib.transform.localRotation = wingAngle;

            var rightRearRib = CreateCubePrimitive("RightRearRib", solarRoot.transform, new Vector3(0.95f, 0.90f, 0.10f), new Vector3(0.09f, 0.07f, 1.24f), metalMat);
            rightRearRib.transform.localRotation = wingAngle;

            // Heavy angled tubular support struts connecting gimbal hub to wings
            Vector3 hubMount = new Vector3(0, 0.68f, 0);
            Vector3 leftAnchor = new Vector3(-0.85f, 0.88f, 0.06f);
            var strutLeft = CreateCubePrimitive("LeftSupportStrut", solarRoot.transform, (hubMount + leftAnchor) * 0.5f, new Vector3(0.08f, 0.08f, Vector3.Distance(hubMount, leftAnchor)), metalMat);
            strutLeft.transform.localRotation = Quaternion.LookRotation(leftAnchor - hubMount);

            Vector3 rightAnchor = new Vector3(0.85f, 0.88f, 0.06f);
            var strutRight = CreateCubePrimitive("RightSupportStrut", solarRoot.transform, (hubMount + rightAnchor) * 0.5f, new Vector3(0.08f, 0.08f, Vector3.Distance(hubMount, rightAnchor)), metalMat);
            strutRight.transform.localRotation = Quaternion.LookRotation(rightAnchor - hubMount);

            // Two angled sapphire PV wing frames (thickened from 0.04f to 0.07f)
            var leftWing = CreateCubePrimitive("LeftWing", solarRoot.transform, new Vector3(-0.95f, 0.95f, 0.10f), new Vector3(1.50f, 0.07f, 1.20f), solarMat);
            leftWing.transform.localRotation = wingAngle;
            solarPanelRenderer = leftWing.GetComponent<Renderer>();

            var rightWing = CreateCubePrimitive("RightWing", solarRoot.transform, new Vector3(0.95f, 0.95f, 0.10f), new Vector3(1.50f, 0.07f, 1.20f), solarMat);
            rightWing.transform.localRotation = wingAngle;

            // Dynamic 3D dusty overlay plate atop PV wings
            var dustPlate = CreateCubePrimitive("SolarDustPlate", solarRoot.transform, new Vector3(0, 0.985f, 0.10f), new Vector3(3.30f, 0.05f, 1.22f), solarDustMat);
            dustPlate.transform.localRotation = wingAngle;
            solarDustPlateRenderer = dustPlate.GetComponent<Renderer>();

            // Power conduit cable running west towards habitat
            CreateCubePrimitive("PowerCable", solarRoot.transform, new Vector3(-1.10f, 0.02f, -0.60f), new Vector3(2.2f, 0.03f, 0.06f), metalMat);

            solarLabel = CreateWorldLabel("SolarLabel", SolarArrayPos + new Vector3(0, 2.15f, 0), isTurkish ? "[GUNES PANELLERI]" : "[SOLAR ARRAY]", 0.065f, new Color(1f, 0.92f, 0.75f));
        }

        private void BuildStationProgressBars()
        {
            stationIndicators.Clear();
            AddStationProgressBar("system-maintenance", SystemBayPos, SystemBayPos + new Vector3(0, 1.95f, 0), new Color(0.25f, 0.85f, 1.0f));
            AddStationProgressBar("meal-prep", GalleyPos, GalleyPos + new Vector3(0, 1.95f, 0), new Color(0.35f, 0.95f, 0.65f));
            AddStationProgressBar("research", ResearchLabPos, ResearchLabPos + new Vector3(0, 1.95f, 0), new Color(0.85f, 0.55f, 1.0f));
            AddStationProgressBar("panel-maintenance", SolarArrayPos, SolarArrayPos + new Vector3(0, 2.45f, 0), new Color(1.0f, 0.85f, 0.25f));
        }

        private void AddStationProgressBar(string taskId, Vector3 stationPos, Vector3 barPos, Color themeColor)
        {
            var barRoot = new GameObject($"ProgressBar_{taskId}");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = barPos;

            const float totalWidth = 0.92f;
            var bgMat = CreateMaterial(new Color(0.10f, 0.12f, 0.16f, 0.85f), metallic: 0f, smoothness: 0.2f, transparent: true);
            var fillMat = CreateMaterial(themeColor, metallic: 0f, smoothness: 0.8f, emission: themeColor * 1.5f);

            CreateCubePrimitive("Bg", barRoot.transform, Vector3.zero, new Vector3(totalWidth + 0.04f, 0.10f, 0.02f), bgMat);
            var fillGo = CreateCubePrimitive("Fill", barRoot.transform, new Vector3(-totalWidth * 0.5f, 0, -0.015f), new Vector3(0.02f, 0.06f, 0.02f), fillMat);

            barRoot.SetActive(false);

            stationIndicators.Add(new StationActivityIndicator
            {
                TaskId = taskId,
                StationPos = stationPos,
                BarRoot = barRoot,
                BarFill = fillGo.transform,
                FillMaterial = fillMat,
                ThemeColor = themeColor,
                BarWidth = totalWidth
            });
        }

        private void BuildEnvironmentalParticlePool()
        {
            envParticles.Clear();
            var poolRoot = new GameObject("EnvironmentalParticlePool").transform;
            poolRoot.SetParent(transform, false);

            var partMat = CreateMaterial(Color.white, metallic: 0f, smoothness: 0.5f, transparent: true);

            for (int i = 0; i < 20; i++)
            {
                var go = CreateSpherePrimitive($"EnvPart_{i}", poolRoot, Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), partMat);
                var mr = go.GetComponent<MeshRenderer>();
                mr.enabled = false;

                envParticles.Add(new EnvParticle3D
                {
                    Transform = go.transform,
                    Renderer = mr,
                    Material = new Material(partMat),
                    Speed = 6.0f + (i % 5) * 1.8f,
                    Phase = (i * 0.65f) % Mathf.PI
                });
                mr.sharedMaterial = envParticles[i].Material;
            }
        }

        // -------------------------------------------------------------
        // Primitive Creation Helpers
        // -------------------------------------------------------------
        private GameObject CreateCubePrimitive(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            return go;
        }

        private GameObject CreateSpherePrimitive(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            return go;
        }

        private GameObject CreateCylinderPrimitive(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            return go;
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

        // -------------------------------------------------------------
        // Public API Implementations
        // -------------------------------------------------------------
        public void SetLanguage(bool turkish)
        {
            isTurkish = turkish;
            if (habitatLabel != null)
                habitatLabel.text = isTurkish ? "[MARS SIGINAK - GUVENLI]" : "[HABITAT SHELTER - SAFE]";
            if (solarLabel != null)
                solarLabel.text = isTurkish ? "[GUNES PANELLERI]" : "[SOLAR ARRAY]";
            if (systemBayLabel != null)
                systemBayLabel.text = isTurkish ? "[O2 & SU]" : "[O2 / WATER]";
            if (galleyLabel != null)
                galleyLabel.text = isTurkish ? "[MUTFAK]" : "[GALLEY]";
            if (labLabel != null)
                labLabel.text = isTurkish ? "[BILIM LAB]" : "[SCIENCE LAB]";
            if (bunksLabel != null)
                bunksLabel.text = isTurkish ? "[UYKU / DINLENME]" : "[REST BUNKS]";
            if (airlockLabel != null)
                airlockLabel.text = isTurkish ? "[HAVA KILIDI]" : "[AIRLOCK]";
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

            bool radAlert = false;
            bool dustAlert = false;

            for (int i = 0; i < state.Events.Count; i++)
            {
                var ev = state.Events[i];
                if (ev.Phase == EventPhase.Warning || ev.Phase == EventPhase.Active)
                {
                    if (ev.Kind == EventKind.Radiation) radAlert = true;
                    if (ev.Kind == EventKind.Dust) dustAlert = true;
                }
            }

            hasRadiationHazard = radAlert;
            hasDustHazard = dustAlert;
            isNightTime = state.IsNight;

            // Terrain texture & day/night tinting
            if (terrainMat != null)
            {
                if (terrainMat.mainTexture == null && terrainSprite != null && terrainSprite.texture != null)
                {
                    terrainMat.mainTexture = terrainSprite.texture;
                    if (terrainMat.HasProperty("_BaseMap"))
                        terrainMat.SetTexture("_BaseMap", terrainSprite.texture);
                }
                terrainMat.color = isNightTime ? new Color(0.70f, 0.65f, 0.75f) : Color.white;
                if (terrainMat.HasProperty("_BaseColor"))
                    terrainMat.SetColor("_BaseColor", terrainMat.color);
            }

            // Solar Array fouling overlay
            if (state.Resources != null)
            {
                float fouling = Mathf.Clamp01((float)state.Resources.PanelFouling);
                if (solarDustPlateRenderer != null)
                {
                    solarDustMat.SetColor("_BaseColor", new Color(0.78f, 0.44f, 0.22f, fouling * 0.82f));
                }
                if (solarPanelRenderer != null)
                {
                    Color cleanCol = new Color(0.12f, 0.32f, 0.68f);
                    Color dirtyCol = new Color(0.55f, 0.40f, 0.30f);
                    solarMat.SetColor("_BaseColor", Color.Lerp(cleanCol, dirtyCol, fouling));
                }
            }

            EnsureCrewVisuals(state);

            for (int i = 0; i < state.Crew.Count; i++)
            {
                var c = state.Crew[i];
                if (!crewVisuals.TryGetValue(c.Id, out var vis)) continue;

                Vector3 targetPos = ComputeTargetPosition(c, i);
                string goalKey = $"{targetPos.x:F2}_{targetPos.z:F2}_{c.Status}";

                if (vis.RouteGoalKey != goalKey)
                {
                    vis.RouteGoalKey = goalKey;
                    vis.TargetPos = targetPos;
                    RebuildRoute(vis, vis.Root.localPosition, targetPos);
                }

                vis.LastStatus = c.Status;
                vis.LastTaskId = c.ActiveTaskId;

                Color suitCol = GetCrewColor(c);
                if (vis.TorsoRenderer != null && vis.TorsoRenderer.sharedMaterial != null)
                {
                    // Subtle status tint blended with NASA off-white suit fabric so suit maintains 3D form & shading
                    Color baseSuit = new Color(0.84f, 0.86f, 0.89f);
                    Color tinted = Color.Lerp(baseSuit, suitCol, 0.28f);
                    vis.TorsoRenderer.sharedMaterial.color = tinted;
                    if (vis.TorsoRenderer.sharedMaterial.HasProperty("_BaseColor"))
                        vis.TorsoRenderer.sharedMaterial.SetColor("_BaseColor", tinted);
                }
            }
        }

        // -------------------------------------------------------------
        // Articulated 3D Astronaut Hierarchy Creation
        // -------------------------------------------------------------
        private void EnsureCrewVisuals(MissionState state)
        {
            for (int i = 0; i < state.Crew.Count; i++)
            {
                var c = state.Crew[i];
                if (crewVisuals.ContainsKey(c.Id)) continue;

                var root = new GameObject($"Crew_{c.Id}").transform;
                root.SetParent(transform, false);

                Vector3 startPos = IdleSlots[i % IdleSlots.Length];
                startPos.y = GetTerrainHeight(startPos.x, startPos.z);
                root.localPosition = startPos;

                // Physics Collider for accurate 3D click raycasting
                var capCol = root.gameObject.AddComponent<CapsuleCollider>();
                capCol.height = 1.35f;
                capCol.radius = 0.32f;
                capCol.center = new Vector3(0, 0.65f, 0);

                // Glowing Ground Selection Ring
                var ringGo = CreateCylinderPrimitive("SelectionRing", root, new Vector3(0, 0.03f, 0), new Vector3(0.85f, 0.02f, 0.85f), ringMat);
                var ringRenderer = ringGo.GetComponent<Renderer>();
                ringRenderer.enabled = false;

                // Visual Pivot that rotates towards movement direction
                var visualPivot = new GameObject("VisualPivot").transform;
                visualPivot.SetParent(root, false);

                // 1. Torso: Off-white fabric main suit
                var torsoMat = new Material(suitMat);
                var torsoGo = CreateCubePrimitive("Torso", visualPivot, new Vector3(0, 0.52f, 0), new Vector3(0.34f, 0.42f, 0.24f), torsoMat);
                var torsoRenderer = torsoGo.GetComponent<Renderer>();

                // Dark graphite utility belt & neck collar
                CreateCubePrimitive("WaistBelt", visualPivot, new Vector3(0, 0.34f, 0), new Vector3(0.36f, 0.07f, 0.26f), bootMat);
                CreateCubePrimitive("NeckCollar", visualPivot, new Vector3(0, 0.72f, 0), new Vector3(0.26f, 0.05f, 0.22f), bootMat);

                // Colored mission stripe & chest control module
                Color visorAccent = CrewVisorAccents[i % CrewVisorAccents.Length];
                var stripeMat = CreateMaterial(visorAccent, metallic: 0.1f, smoothness: 0.6f, emission: visorAccent * 1.3f);
                CreateCubePrimitive("ChestModule", visualPivot, new Vector3(0, 0.56f, 0.125f), new Vector3(0.16f, 0.10f, 0.03f), bootMat);
                CreateCubePrimitive("ChestStripe", visualPivot, new Vector3(0, 0.56f, 0.142f), new Vector3(0.12f, 0.04f, 0.015f), stripeMat);

                // 2. Sculpted PLSS Backpack with Twin O2 Canisters, Valve Block & Antenna
                var plssMat = CreateMaterial(new Color(0.72f, 0.75f, 0.80f), metallic: 0.6f, smoothness: 0.5f);
                CreateCubePrimitive("PLSS", visualPivot, new Vector3(0, 0.55f, -0.16f), new Vector3(0.28f, 0.38f, 0.18f), plssMat);
                CreateCylinderPrimitive("O2CanisterLeft", visualPivot, new Vector3(-0.08f, 0.55f, -0.26f), new Vector3(0.08f, 0.16f, 0.08f), metalMat);
                CreateCylinderPrimitive("O2CanisterRight", visualPivot, new Vector3(0.08f, 0.55f, -0.26f), new Vector3(0.08f, 0.16f, 0.08f), metalMat);
                CreateCubePrimitive("PLSSValveBlock", visualPivot, new Vector3(0, 0.72f, -0.22f), new Vector3(0.12f, 0.07f, 0.08f), bootMat);
                CreateCylinderPrimitive("AntennaRod", visualPivot, new Vector3(0.10f, 0.86f, -0.22f), new Vector3(0.02f, 0.22f, 0.02f), metalMat);

                // 3. Head + Helmet + Visor
                var headPivot = new GameObject("HeadPivot").transform;
                headPivot.SetParent(visualPivot, false);
                headPivot.localPosition = new Vector3(0, 0.82f, 0);

                var helmetMat = CreateMaterial(new Color(0.88f, 0.90f, 0.92f), metallic: 0.18f, smoothness: 0.85f);
                CreateSpherePrimitive("Helmet", headPivot, Vector3.zero, new Vector3(0.32f, 0.32f, 0.32f), helmetMat);

                var visorMat = CreateMaterial(visorAccent, metallic: 0.95f, smoothness: 0.92f, emission: visorAccent * 1.3f);
                var visorGo = CreateSpherePrimitive("Visor", headPivot, new Vector3(0, 0.02f, 0.08f), new Vector3(0.20f, 0.14f, 0.16f), visorMat);
                var visorRenderer = visorGo.GetComponent<Renderer>();

                // 4. Left and Right Shoulders & Arms with Graphite Collars, Suit Sleeves, and Gloves
                var leftShoulder = new GameObject("LeftShoulderPivot").transform;
                leftShoulder.SetParent(visualPivot, false);
                leftShoulder.localPosition = new Vector3(-0.21f, 0.70f, 0);
                CreateSpherePrimitive("LeftShoulderCap", leftShoulder, Vector3.zero, new Vector3(0.12f, 0.10f, 0.12f), bootMat);
                CreateCubePrimitive("LeftArmStripe", leftShoulder, new Vector3(-0.04f, -0.06f, 0), new Vector3(0.02f, 0.04f, 0.08f), stripeMat);
                CreateCylinderPrimitive("LeftArm", leftShoulder, new Vector3(0, -0.16f, 0), new Vector3(0.09f, 0.18f, 0.09f), suitMat);
                CreateSpherePrimitive("LeftGlove", leftShoulder, new Vector3(0, -0.34f, 0), new Vector3(0.10f, 0.10f, 0.10f), bootMat);

                var rightShoulder = new GameObject("RightShoulderPivot").transform;
                rightShoulder.SetParent(visualPivot, false);
                rightShoulder.localPosition = new Vector3(0.21f, 0.70f, 0);
                CreateSpherePrimitive("RightShoulderCap", rightShoulder, Vector3.zero, new Vector3(0.12f, 0.10f, 0.12f), bootMat);
                CreateCubePrimitive("RightArmStripe", rightShoulder, new Vector3(0.04f, -0.06f, 0), new Vector3(0.02f, 0.04f, 0.08f), stripeMat);
                CreateCylinderPrimitive("RightArm", rightShoulder, new Vector3(0, -0.16f, 0), new Vector3(0.09f, 0.18f, 0.09f), suitMat);
                CreateSpherePrimitive("RightGlove", rightShoulder, new Vector3(0, -0.34f, 0), new Vector3(0.10f, 0.10f, 0.10f), bootMat);

                // 5. Left and Right Hips & Legs with Knee Guards and Mars Boots
                var leftHip = new GameObject("LeftHipPivot").transform;
                leftHip.SetParent(visualPivot, false);
                leftHip.localPosition = new Vector3(-0.11f, 0.36f, 0);
                CreateCylinderPrimitive("LeftLeg", leftHip, new Vector3(0, -0.18f, 0), new Vector3(0.11f, 0.20f, 0.11f), suitMat);
                CreateCubePrimitive("LeftKneeGuard", leftHip, new Vector3(0, -0.18f, 0.065f), new Vector3(0.09f, 0.08f, 0.03f), bootMat);
                CreateCubePrimitive("LeftBoot", leftHip, new Vector3(0, -0.36f, 0.03f), new Vector3(0.13f, 0.10f, 0.18f), bootMat);

                var rightHip = new GameObject("RightHipPivot").transform;
                rightHip.SetParent(visualPivot, false);
                rightHip.localPosition = new Vector3(0.11f, 0.36f, 0);
                CreateCylinderPrimitive("RightLeg", rightHip, new Vector3(0, -0.18f, 0), new Vector3(0.11f, 0.20f, 0.11f), suitMat);
                CreateCubePrimitive("RightKneeGuard", rightHip, new Vector3(0, -0.18f, 0.065f), new Vector3(0.09f, 0.08f, 0.03f), bootMat);
                CreateCubePrimitive("RightBoot", rightHip, new Vector3(0, -0.36f, 0.03f), new Vector3(0.13f, 0.10f, 0.18f), bootMat);

                // 6. Overhead Clean TextMesh Labels (Mounted cleanly ABOVE helmet)
                var badgeTm = CreateWorldLabel("StatusBadge", new Vector3(0, 1.48f, 0), "", 0.068f, Color.white);
                badgeTm.transform.SetParent(root, false);

                var nameTm = CreateWorldLabel("NameLabel", new Vector3(0, 1.24f, 0), c.Name, 0.052f, Color.white);
                nameTm.transform.SetParent(root, false);

                // 7. Mars dust puff pool (3 recycled spheres)
                var puffMat = CreateMaterial(new Color(0.85f, 0.52f, 0.30f, 0.55f), metallic: 0f, smoothness: 0.1f, transparent: true);
                var puffs = new DustPuff3D[3];
                for (int p = 0; p < 3; p++)
                {
                    var pGo = CreateSpherePrimitive($"DustPuff_{c.Id}_{p}", transform, Vector3.zero, new Vector3(0.15f, 0.15f, 0.15f), puffMat);
                    var pMr = pGo.GetComponent<MeshRenderer>();
                    pMr.enabled = false;
                    puffs[p] = new DustPuff3D
                    {
                        Transform = pGo.transform,
                        Renderer = pMr,
                        Material = new Material(puffMat),
                        Age = 0f,
                        Lifetime = 0.38f,
                        Active = false
                    };
                    pMr.sharedMaterial = puffs[p].Material;
                }

                var vis = new CrewVisual
                {
                    Id = c.Id,
                    Index = i,
                    Root = root,
                    VisualPivot = visualPivot,
                    RingTransform = ringGo.transform,
                    RingRenderer = ringRenderer,
                    LeftHipPivot = leftHip,
                    RightHipPivot = rightHip,
                    LeftShoulderPivot = leftShoulder,
                    RightShoulderPivot = rightShoulder,
                    HeadPivot = headPivot,
                    VisorRenderer = visorRenderer,
                    TorsoRenderer = torsoRenderer,
                    StatusBadge = badgeTm,
                    NameLabel = nameTm,
                    TargetPos = startPos,
                    LastStatus = c.Status,
                    LastTaskId = c.ActiveTaskId,
                    StridePhase = 0f,
                    DustPuffs = puffs,
                    PuffSpawnTimer = 0f,
                    PuffIndex = 0
                };

                crewVisuals[c.Id] = vis;
            }
        }

        private Vector3 ComputeTargetPosition(CrewState crew, int index)
        {
            if (crew.Status == CrewStatus.Working && crew.ActiveTaskId != null)
            {
                if (crew.ActiveTaskId == "panel-maintenance") return SolarWorkPos;
                if (crew.ActiveTaskId == "system-maintenance") return new Vector3(-4.0f, 0f, 0.72f);
                if (crew.ActiveTaskId == "meal-prep") return new Vector3(-2.0f, 0f, 0.72f);
                if (crew.ActiveTaskId == "research") return new Vector3(0.0f, 0f, 0.72f);
            }

            if (crew.Status == CrewStatus.Returning)
            {
                return IdleSlots[index % IdleSlots.Length];
            }

            if (crew.Status == CrewStatus.Sleeping || crew.Status == CrewStatus.Resting)
            {
                float[] bunkOffsets = { -0.85f, 0f, 0.85f };
                return new Vector3(BunksPos.x + bunkOffsets[index % 3], 0f, BunksPos.z);
            }

            if (crew.Location == CrewLocation.Outside)
            {
                return new Vector3(4.65f + (index - 1) * 1.35f, 0f, -1.35f);
            }

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
                default: return new Color(0.92f, 0.94f, 0.96f, 1f);
            }
        }

        // -------------------------------------------------------------
        // Bug #1 Permanent Fix: Deadlock-Free Waypoint Queue
        // -------------------------------------------------------------
        private static void RebuildRoute(CrewVisual vis, Vector3 currentPos, Vector3 targetPos)
        {
            vis.Waypoints.Clear();
            bool currentlyInside = currentPos.x < HabitatWallX;
            bool targetInside = targetPos.x < HabitatWallX;

            if (currentlyInside != targetInside)
            {
                if (currentlyInside)
                {
                    // Inside -> Outside
                    vis.Waypoints.Add(AirlockInnerPos);
                    vis.Waypoints.Add(AirlockOuterPos);
                    vis.Waypoints.Add(targetPos);
                }
                else
                {
                    // Outside -> Inside
                    vis.Waypoints.Add(AirlockOuterPos);
                    vis.Waypoints.Add(AirlockInnerPos);
                    vis.Waypoints.Add(targetPos);
                }
            }
            else
            {
                // Both on same side of the airlock boundary
                vis.Waypoints.Add(targetPos);
            }
        }

        // -------------------------------------------------------------
        // Main Update Loop
        // -------------------------------------------------------------
        private void Update()
        {
            UpdateCrewVisuals();
            UpdateAirlock();
            UpdateStationAnimations();
            UpdateStationActivityBars();
            UpdateEnvParticles();
            UpdateLighting();
            HandleWorldClick();
        }

        private void LateUpdate()
        {
            UpdateBillboards();
        }

        private void UpdateCrewVisuals()
        {
            foreach (var kv in crewVisuals)
            {
                var vis = kv.Value;
                Vector3 currentPos = vis.Root.localPosition;

                // Selection Ring pulse
                bool isSelected = (vis.Id == SelectedCrewId);
                vis.RingRenderer.enabled = isSelected;
                if (isSelected)
                {
                    float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 5.0f) * 0.08f;
                    vis.RingTransform.localScale = new Vector3(pulse, 0.02f, pulse);
                }

                // Clean BMP/ASCII status badge text
                string badge = "";
                if (vis.LastStatus == CrewStatus.Exhausted)
                    badge = "[!]";
                else if (vis.LastStatus == CrewStatus.Returning)
                    badge = ">>";
                else if (vis.LastStatus == CrewStatus.Sleeping)
                    badge = "Zzz";
                else if (vis.LastStatus == CrewStatus.Resting)
                    badge = "[~]";
                else if (vis.LastStatus == CrewStatus.Working)
                    badge = "[+]";

                vis.StatusBadge.text = badge;

                // Deadlock-free sequential waypoint traversal
                if (vis.Waypoints.Count > 0)
                {
                    Vector3 nextWp = vis.Waypoints[0];
                    Vector2 cur2D = new Vector2(currentPos.x, currentPos.z);
                    Vector2 next2D = new Vector2(nextWp.x, nextWp.z);
                    float distToWp = Vector2.Distance(cur2D, next2D);

                    // Once waypoint reached, pop it forever!
                    if (distToWp < 0.06f && vis.Waypoints.Count > 1)
                    {
                        vis.Waypoints.RemoveAt(0);
                        nextWp = vis.Waypoints[0];
                        next2D = new Vector2(nextWp.x, nextWp.z);
                        distToWp = Vector2.Distance(cur2D, next2D);
                    }

                    bool isMoving = (distToWp > 0.05f || vis.Waypoints.Count > 1);
                    if (isMoving)
                    {
                        // Fast, smooth walk back when returning
                        float speed = (vis.LastStatus == CrewStatus.Returning) ? 2.6f : 1.85f;
                        Vector2 move2D = Vector2.MoveTowards(cur2D, next2D, speed * Time.unscaledDeltaTime);
                        float groundY = GetTerrainHeight(move2D.x, move2D.y);
                        vis.Root.localPosition = new Vector3(move2D.x, groundY, move2D.y);

                        // Smoothly rotate visual pivot toward walk direction
                        Vector2 dir2D = (next2D - cur2D);
                        if (dir2D.sqrMagnitude > 0.0001f)
                        {
                            float targetAngle = Mathf.Atan2(dir2D.x, dir2D.y) * Mathf.Rad2Deg;
                            vis.VisualPivot.rotation = Quaternion.Slerp(vis.VisualPivot.rotation, Quaternion.Euler(0, targetAngle, 0), Time.unscaledDeltaTime * 10f);
                        }

                        // Articulated walking stride
                        vis.StridePhase += Time.unscaledDeltaTime * (speed * 7.5f);
                        float stride = Mathf.Sin(vis.StridePhase);
                        vis.LeftHipPivot.localRotation = Quaternion.Euler(stride * 34f, 0, 0);
                        vis.RightHipPivot.localRotation = Quaternion.Euler(-stride * 34f, 0, 0);
                        vis.LeftShoulderPivot.localRotation = Quaternion.Euler(-stride * 26f, 0, 0);
                        vis.RightShoulderPivot.localRotation = Quaternion.Euler(stride * 26f, 0, 0);

                        float bounce = Mathf.Abs(Mathf.Sin(vis.StridePhase)) * 0.045f;
                        vis.VisualPivot.localPosition = new Vector3(0, bounce, 0);

                        // Dust puffs behind boots when outside
                        if (move2D.x > 2.3f)
                        {
                            vis.PuffSpawnTimer -= Time.unscaledDeltaTime;
                            if (vis.PuffSpawnTimer <= 0f)
                            {
                                vis.PuffSpawnTimer = 0.20f;
                                var puff = vis.DustPuffs[vis.PuffIndex % vis.DustPuffs.Length];
                                vis.PuffIndex++;
                                puff.Transform.position = new Vector3(move2D.x, groundY + 0.05f, move2D.y);
                                puff.Age = 0f;
                                puff.Active = true;
                                puff.Renderer.enabled = true;
                            }
                        }
                    }
                    else
                    {
                        // Stationary pose
                        vis.StridePhase = 0f;
                        vis.LeftHipPivot.localRotation = Quaternion.identity;
                        vis.RightHipPivot.localRotation = Quaternion.identity;
                        vis.LeftShoulderPivot.localRotation = Quaternion.identity;
                        vis.RightShoulderPivot.localRotation = Quaternion.identity;

                        if (vis.LastStatus == CrewStatus.Working)
                        {
                            // Working animation: lean slightly forward facing the station (+Z) without Euler readback
                            float workT = Time.unscaledTime * 5.0f + vis.Index * 1.5f;
                            float armTink = Mathf.Sin(workT);
                            vis.VisualPivot.localRotation = Quaternion.Euler(8f, 0f, 0f);
                            vis.LeftShoulderPivot.localRotation = Quaternion.Euler(-25f + armTink * 18f, 15f, 0f);
                            vis.RightShoulderPivot.localRotation = Quaternion.Euler(-25f - armTink * 18f, -15f, 0f);
                            vis.VisualPivot.localPosition = Vector3.zero;
                        }
                        else if (vis.LastStatus == CrewStatus.Sleeping || vis.LastStatus == CrewStatus.Resting)
                        {
                            // Lying down horizontally on bunk mattress without clipping frame
                            vis.VisualPivot.localRotation = Quaternion.Euler(-85f, 90f, 0f);
                            float breath = Mathf.Sin(Time.unscaledTime * 2.5f + vis.Index) * 0.02f;
                            vis.VisualPivot.localPosition = new Vector3(0, 0.28f + breath, 0);
                        }
                        else
                        {
                            // Idle: stand upright and face camera
                            vis.VisualPivot.localRotation = Quaternion.Slerp(vis.VisualPivot.localRotation, Quaternion.Euler(0, 180f, 0), Time.unscaledDeltaTime * 6f);
                            vis.VisualPivot.localPosition = Vector3.zero;
                        }
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
                            float s = Mathf.Lerp(0.08f, 0.26f, t);
                            puff.Transform.localScale = new Vector3(s, s * 0.7f, s);
                            puff.Material.SetColor("_BaseColor", new Color(0.85f, 0.52f, 0.30f, Mathf.Lerp(0.55f, 0f, t)));
                        }
                    }
                }
            }
        }

        private void UpdateAirlock()
        {
            // Detect if any astronaut is near the airlock chamber
            bool someoneNearAirlock = false;
            foreach (var kv in crewVisuals)
            {
                Vector3 pos = kv.Value.Root.localPosition;
                if (pos.x >= 0.8f && pos.x <= 2.85f && pos.z >= -1.2f && pos.z <= 0.5f)
                {
                    someoneNearAirlock = true;
                    break;
                }
            }

            float targetProgress = someoneNearAirlock ? 1.0f : 0.0f;
            doorOpenProgress = Mathf.MoveTowards(doorOpenProgress, targetProgress, Time.unscaledDeltaTime * 3.5f);

            // Sliding door animations
            if (innerDoorTransform != null)
                innerDoorTransform.localPosition = new Vector3(HabitatWallX, 0.48f, -0.35f - doorOpenProgress * 0.45f);
            if (outerDoorTransform != null)
                outerDoorTransform.localPosition = new Vector3(2.45f, 0.48f, -0.35f + doorOpenProgress * 0.45f);

            // Roof Emergency Beacon
            if (beaconSphereRenderer != null)
            {
                if (hasRadiationHazard)
                {
                    float p = Mathf.Sin(Time.unscaledTime * 14f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(new Color(1f, 0.15f, 0.10f), new Color(1f, 0.65f, 0.20f), p);
                    beaconMat.SetColor("_BaseColor", col);
                    if (beaconMat.HasProperty("_EmissionColor"))
                        beaconMat.SetColor("_EmissionColor", col * 3.0f);
                    if (beaconLight != null)
                    {
                        beaconLight.color = col;
                        beaconLight.intensity = 1.8f * p;
                    }
                }
                else if (hasDustHazard)
                {
                    float p = Mathf.Sin(Time.unscaledTime * 10f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(new Color(1f, 0.45f, 0.05f), new Color(1f, 0.85f, 0.25f), p);
                    beaconMat.SetColor("_BaseColor", col);
                    if (beaconMat.HasProperty("_EmissionColor"))
                        beaconMat.SetColor("_EmissionColor", col * 2.5f);
                    if (beaconLight != null)
                    {
                        beaconLight.color = col;
                        beaconLight.intensity = 1.6f * p;
                    }
                }
                else
                {
                    Color col = new Color(0.20f, 0.85f, 0.55f);
                    beaconMat.SetColor("_BaseColor", col);
                    if (beaconMat.HasProperty("_EmissionColor"))
                        beaconMat.SetColor("_EmissionColor", col * 1.5f);
                    if (beaconLight != null)
                    {
                        beaconLight.color = col;
                        beaconLight.intensity = 0.8f;
                    }
                }
            }
        }

        private void UpdateStationAnimations()
        {
            // 1. Oxygen Bubbles in SystemBay
            for (int b = 0; b < o2Bubbles.Count; b++)
            {
                var bub = o2Bubbles[b];
                float y = 0.55f + Mathf.Repeat(Time.unscaledTime * 0.70f + b * 0.25f, 0.70f);
                float wobble = Mathf.Sin(Time.unscaledTime * 3.5f + b) * 0.02f;
                bub.localPosition = new Vector3(bub.localPosition.x + wobble * Time.unscaledDeltaTime, y, bub.localPosition.z);
            }

            // 2. Holographic Mars & Ring in Science Lab
            if (marsHoloSphere != null)
            {
                marsHoloSphere.Rotate(Vector3.up, 28f * Time.unscaledDeltaTime, Space.World);
                float bob = Mathf.Sin(Time.unscaledTime * 2.0f) * 0.03f;
                marsHoloSphere.localPosition = new Vector3(0, 1.15f + bob, 0);
            }
            if (marsHoloRing != null)
            {
                marsHoloRing.Rotate(Vector3.forward, 36f * Time.unscaledDeltaTime, Space.Self);
                float bob = Mathf.Sin(Time.unscaledTime * 2.0f) * 0.03f;
                marsHoloRing.localPosition = new Vector3(0, 1.15f + bob, 0);
            }

            // 3. Galley Grow Light pulse
            if (galleyGrowLight != null)
            {
                galleyGrowLight.intensity = 0.80f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.15f;
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
                        Vector2 crewPlanar = new Vector2(crewVis.Root.position.x, crewVis.Root.position.z);
                        Vector2 stationPlanar = new Vector2(ind.StationPos.x, ind.StationPos.z);
                        float dist = Vector2.Distance(crewPlanar, stationPlanar);
                        if (dist < 1.8f && crewVis.LastStatus == CrewStatus.Working)
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

                    float pulse = Mathf.Sin(Time.unscaledTime * 5.5f) * 0.5f + 0.5f;

                    if (isContinuous)
                    {
                        float scan = Mathf.PingPong(Time.unscaledTime * 0.85f, 1.0f);
                        float waveWidth = ind.BarWidth * 0.35f;
                        float posX = Mathf.Lerp(-ind.BarWidth * 0.5f + waveWidth * 0.5f, ind.BarWidth * 0.5f - waveWidth * 0.5f, scan);
                        ind.BarFill.localScale = new Vector3(waveWidth, 0.06f, 0.02f);
                        ind.BarFill.localPosition = new Vector3(posX, 0, -0.015f);
                        ind.FillMaterial.SetColor("_BaseColor", Color.Lerp(ind.ThemeColor, Color.white, pulse * 0.4f));
                    }
                    else
                    {
                        float fillW = ind.BarWidth * Mathf.Max(0.04f, progress);
                        ind.BarFill.localScale = new Vector3(fillW, 0.06f, 0.02f);
                        ind.BarFill.localPosition = new Vector3(-ind.BarWidth * 0.5f + fillW * 0.5f, 0, -0.015f);
                        ind.FillMaterial.SetColor("_BaseColor", ind.ThemeColor);
                    }
                }
                else
                {
                    if (ind.BarRoot.activeSelf) ind.BarRoot.SetActive(false);
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
                    p.Transform.localScale = new Vector3(0.95f, 0.06f, 0.06f);
                    p.Material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.28f, 0.45f));

                    Vector3 pos = p.Transform.localPosition;
                    pos.x += p.Speed * Time.unscaledDeltaTime;
                    if (pos.x > 9.5f)
                    {
                        pos.x = -9.5f;
                        pos.y = 0.3f + ((i * 3 + (int)(Time.unscaledTime * 2)) % 9) * 0.28f;
                        pos.z = -3.5f + ((i * 7) % 11) * 0.85f;
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
                    p.Transform.localScale = new Vector3(0.06f, 1.15f, 0.06f);
                    Color col = (i % 2 == 0) ? new Color(1.0f, 0.85f, 0.25f, 0.65f) : new Color(0.95f, 0.28f, 0.85f, 0.65f);
                    p.Material.SetColor("_BaseColor", col);

                    Vector3 pos = p.Transform.localPosition;
                    pos.y -= p.Speed * 2.8f * Time.unscaledDeltaTime;
                    pos.x -= p.Speed * 0.6f * Time.unscaledDeltaTime;
                    if (pos.y < -0.5f)
                    {
                        pos.y = 5.2f;
                        pos.x = -7.5f + ((i * 5 + (int)(Time.unscaledTime * 3)) % 15) * 1.1f;
                        pos.z = -3.0f + ((i * 3) % 9) * 0.95f;
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
                    p.Transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

                    Vector3 pos = p.Transform.localPosition;
                    if (pos.z < 12.0f || pos.y < 5.0f)
                    {
                        pos.x = -12.0f + (i / 19.0f) * 24.0f;
                        pos.y = 5.5f + ((i * 3) % 7) * 0.55f;
                        pos.z = 14.0f + ((i * 2) % 6) * 1.2f;
                        p.Transform.localPosition = pos;
                    }

                    float twinkle = Mathf.PingPong(Time.unscaledTime * (1.6f + p.Phase) + p.Phase, 0.75f) + 0.25f;
                    p.Material.SetColor("_BaseColor", new Color(0.92f, 0.95f, 1f, twinkle));
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

        private void UpdateLighting()
        {
            if (sceneDirLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].name == "MarsSunLight") sceneDirLight = lights[i];
                    else if (lights[i].name == "MarsFillLight") sceneFillLight = lights[i];
                    else if (sceneDirLight == null && lights[i].type == LightType.Directional) sceneDirLight = lights[i];
                }
            }

            if (sceneDirLight != null)
            {
                Color targetCol;
                float targetIntensity;
                Quaternion targetRot;

                if (hasRadiationHazard)
                {
                    targetCol = new Color(1.0f, 0.38f, 0.30f);
                    targetIntensity = 1.35f;
                    targetRot = Quaternion.Euler(45f, -35f, 0);
                }
                else if (hasDustHazard)
                {
                    targetCol = new Color(0.85f, 0.52f, 0.28f);
                    targetIntensity = 0.42f;
                    targetRot = Quaternion.Euler(45f, -35f, 0);
                }
                else if (isNightTime)
                {
                    targetCol = new Color(0.25f, 0.35f, 0.60f);
                    targetIntensity = 0.22f;
                    targetRot = Quaternion.Euler(25f, -35f, 0);
                }
                else
                {
                    targetCol = new Color(1.0f, 0.84f, 0.68f);
                    targetIntensity = 1.55f;
                    targetRot = Quaternion.Euler(46f, -32f, 0);
                }

                sceneDirLight.color = Color.Lerp(sceneDirLight.color, targetCol, Time.unscaledDeltaTime * 3f);
                sceneDirLight.intensity = Mathf.Lerp(sceneDirLight.intensity, targetIntensity, Time.unscaledDeltaTime * 3f);
                sceneDirLight.transform.rotation = Quaternion.Slerp(sceneDirLight.transform.rotation, targetRot, Time.unscaledDeltaTime * 3f);
            }

            if (sceneFillLight != null)
            {
                float targetFill = isNightTime ? 0.08f : (hasDustHazard ? 0.15f : 0.32f);
                sceneFillLight.intensity = Mathf.Lerp(sceneFillLight.intensity, targetFill, Time.unscaledDeltaTime * 3f);
            }

            // Adjust trilight ambient lighting smoothly
            Color targetSky = isNightTime ? new Color(0.10f, 0.10f, 0.16f)
                : hasDustHazard ? new Color(0.42f, 0.26f, 0.18f)
                : hasRadiationHazard ? new Color(0.48f, 0.22f, 0.32f)
                : new Color(0.36f, 0.30f, 0.34f);

            Color targetEquator = isNightTime ? new Color(0.14f, 0.14f, 0.20f)
                : hasDustHazard ? new Color(0.52f, 0.32f, 0.20f)
                : hasRadiationHazard ? new Color(0.55f, 0.28f, 0.30f)
                : new Color(0.58f, 0.38f, 0.28f);

            Color targetGround = isNightTime ? new Color(0.06f, 0.06f, 0.10f)
                : hasDustHazard ? new Color(0.26f, 0.14f, 0.10f)
                : hasRadiationHazard ? new Color(0.28f, 0.12f, 0.12f)
                : new Color(0.24f, 0.14f, 0.10f);

            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, targetSky, Time.unscaledDeltaTime * 3f);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, targetEquator, Time.unscaledDeltaTime * 3f);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, targetGround, Time.unscaledDeltaTime * 3f);

            // Interior habitat point light glows brighter at night
            if (interiorDomeLight != null)
            {
                interiorDomeLight.intensity = isNightTime ? 2.6f : 1.4f;
            }
        }

        private void UpdateBillboards()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Quaternion camRot = mainCam.transform.rotation;

            if (habitatLabel != null) habitatLabel.transform.rotation = camRot;
            if (solarLabel != null) solarLabel.transform.rotation = camRot;
            if (systemBayLabel != null) systemBayLabel.transform.rotation = camRot;
            if (galleyLabel != null) galleyLabel.transform.rotation = camRot;
            if (labLabel != null) labLabel.transform.rotation = camRot;
            if (bunksLabel != null) bunksLabel.transform.rotation = camRot;
            if (airlockLabel != null) airlockLabel.transform.rotation = camRot;

            for (int i = 0; i < stationIndicators.Count; i++)
            {
                if (stationIndicators[i].BarRoot != null)
                    stationIndicators[i].BarRoot.transform.rotation = camRot;
            }

            foreach (var kv in crewVisuals)
            {
                if (kv.Value.StatusBadge != null) kv.Value.StatusBadge.transform.rotation = camRot;
                if (kv.Value.NameLabel != null) kv.Value.NameLabel.transform.rotation = camRot;
            }
        }

        // -------------------------------------------------------------
        // 3D Physics Raycast Click Handling
        // -------------------------------------------------------------
        private void HandleWorldClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (Keyboard.current != null && Keyboard.current.altKey.isPressed) return;
            var camCtrl = mainCam != null ? mainCam.GetComponent<MarsOutpostCameraController>() : null;
            if (camCtrl != null && (camCtrl.IsDraggingCamera || camCtrl.HasDraggedRecently)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            Ray ray = mainCam.ScreenPointToRay(screenPos);

            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 100f);
            Vector3 hitPoint = Vector3.zero;
            bool hasHitPoint = false;

            if (hasHit)
            {
                hitPoint = hit.point;
                hasHitPoint = true;

                // 1. Check if clicked an astronaut directly
                foreach (var kv in crewVisuals)
                {
                    if (hit.collider.transform == kv.Value.Root || hit.collider.transform.IsChildOf(kv.Value.Root))
                    {
                        SelectedCrewId = kv.Key;
                        CrewSelected?.Invoke(kv.Key);
                        return;
                    }
                }

                // 2. Check if clicked a station root / child collider
                Transform cur = hit.collider.transform;
                while (cur != null && cur != transform)
                {
                    string curName = cur.name;
                    if (curName.Contains("SystemBay") || curName.Contains("LifeSupport"))
                    {
                        StationClicked?.Invoke("system-maintenance");
                        return;
                    }
                    if (curName.Contains("Galley"))
                    {
                        StationClicked?.Invoke("meal-prep");
                        return;
                    }
                    if (curName.Contains("ResearchLab") || curName.Contains("Lab"))
                    {
                        StationClicked?.Invoke("research");
                        return;
                    }
                    if (curName.Contains("SolarArray") || curName.Contains("Solar"))
                    {
                        StationClicked?.Invoke("panel-maintenance");
                        return;
                    }
                    if (curName.Contains("Bunks") || curName.Contains("Habitat") || curName.Contains("Airlock"))
                    {
                        StationClicked?.Invoke("rest-or-return");
                        return;
                    }
                    cur = cur.parent;
                }
            }
            else
            {
                // Fallback: Plane raycast at Y = 0
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                    hasHitPoint = true;
                }
            }

            if (hasHitPoint)
            {
                Vector2 hitPlanar = new Vector2(hitPoint.x, hitPoint.z);

                // Check astronaut proximity
                string bestCrewId = null;
                float bestDistSq = 0.85f * 0.85f;
                foreach (var kv in crewVisuals)
                {
                    Vector2 crewPlanar = new Vector2(kv.Value.Root.position.x, kv.Value.Root.position.z);
                    float dSq = (crewPlanar - hitPlanar).sqrMagnitude;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestCrewId = kv.Key;
                    }
                }
                if (bestCrewId != null)
                {
                    SelectedCrewId = bestCrewId;
                    CrewSelected?.Invoke(bestCrewId);
                    return;
                }

                // Check station proximity
                if (Vector2.Distance(hitPlanar, new Vector2(SolarArrayPos.x, SolarArrayPos.z)) <= 2.2f)
                {
                    StationClicked?.Invoke("panel-maintenance");
                }
                else if (Vector2.Distance(hitPlanar, new Vector2(SystemBayPos.x, SystemBayPos.z)) <= 1.4f)
                {
                    StationClicked?.Invoke("system-maintenance");
                }
                else if (Vector2.Distance(hitPlanar, new Vector2(GalleyPos.x, GalleyPos.z)) <= 1.4f)
                {
                    StationClicked?.Invoke("meal-prep");
                }
                else if (Vector2.Distance(hitPlanar, new Vector2(ResearchLabPos.x, ResearchLabPos.z)) <= 1.4f)
                {
                    StationClicked?.Invoke("research");
                }
                else if (Vector2.Distance(hitPlanar, new Vector2(BunksPos.x, BunksPos.z)) <= 1.6f ||
                         Vector2.Distance(hitPlanar, new Vector2(AirlockInnerPos.x, AirlockInnerPos.z)) <= 1.1f)
                {
                    StationClicked?.Invoke("rest-or-return");
                }
            }
        }
    }
}
