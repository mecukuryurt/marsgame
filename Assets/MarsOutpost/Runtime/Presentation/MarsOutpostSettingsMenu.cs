using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MarsOutpost.Presentation
{
    /// <summary>Runtime-built settings overlay. Add this component to the HUD canvas.</summary>
    public sealed class MarsOutpostSettingsMenu : MonoBehaviour
    {
        private const string Prefix = "MarsOutpost.Settings.";
        private SimulationRunner runner;
        private MarsOutpostAudio audioController;
        private GameObject overlay;
        private Dropdown qualityDropdown, resolutionDropdown;
        private Slider volumeSlider;
        private Toggle muteToggle, fullscreenToggle;
        private Text titleText, audioHeader, graphicsHeader, displayHeader, statusText;
        private UniversalRenderPipelineAsset originalPipeline, runtimePipeline;
        private float originalRenderScale;
        private int originalMsaa;
        private float originalShadowDistance;
        private bool open, priorUserPaused, appliedThisOpen, focusPauseObserved;
        private bool draftMute, draftFullscreen;
        private bool audioBeforeOpenMute;
        private float audioBeforeOpenVolume;
        private RenderPipelineAsset originalQualityPipeline;
        private float draftVolume;
        private int draftQuality, draftResolution;
        private readonly List<Resolution> resolutions = new List<Resolution>();
        private Button entryButton;
        private bool lastLanguageTurkish;

        public bool IsOpen => open;

        private void Awake()
        {
            runner = FindFirstObjectByType<SimulationRunner>();
            audioController = FindFirstObjectByType<MarsOutpostAudio>();
            BuildOverlay();
            LoadSavedSettings();
            overlay.SetActive(false);
            lastLanguageTurkish = OutpostStrings.IsTurkish;
        }

        private void Update()
        {
            if (lastLanguageTurkish != OutpostStrings.IsTurkish)
            {
                lastLanguageTurkish = OutpostStrings.IsTurkish;
                RefreshLabels();
            }
            if (open && runner != null && runner.Stepper != null && runner.Stepper.FocusPaused) focusPauseObserved = true;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (open) Close();
                else
                {
                    var wv = FindFirstObjectByType<MarsOutpostWorldView>();
                    if (wv != null && wv.HandleEscapeCancel()) return;
                    Open();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (open && !hasFocus) focusPauseObserved = true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (open && paused) focusPauseObserved = true;
        }

        private void OnDestroy()
        {
            RestoreGraphics();
            if (overlay != null) Destroy(overlay);
            if (entryButton != null) Destroy(entryButton.gameObject);
        }

        public void Open()
        {
            if (open) return;
            open = true;
            MarsOutpostCameraController.IsMenuBlockingCameraInput = true;
            appliedThisOpen = false; focusPauseObserved = false;
            if (runner == null) runner = FindFirstObjectByType<SimulationRunner>();
            priorUserPaused = runner != null && runner.Stepper != null && runner.Stepper.UserPaused;
            if (runner != null) runner.SetPaused(true);
            audioBeforeOpenMute = audioController != null && audioController.IsMuted;
            audioBeforeOpenVolume = audioController != null ? audioController.Volume : 0.6f;
            ReadDraft();
            overlay.SetActive(true);
            RefreshLabels();
        }

        /// <summary>Closes and discards unapplied choices.</summary>
        public void Close()
        {
            if (!open) return;
            open = false;
            MarsOutpostCameraController.IsMenuBlockingCameraInput = false;
            overlay.SetActive(false);
            if (audioController != null && !appliedThisOpen)
            {
                audioController.SetMuted(audioBeforeOpenMute);
                audioController.SetVolume(audioBeforeOpenVolume);
            }
            bool becamePausedDuringMenu = focusPauseObserved || (runner != null && runner.Stepper != null && runner.Stepper.FocusPaused);
            if (runner != null && !priorUserPaused && !becamePausedDuringMenu) runner.SetPaused(false);
        }

        private void BuildOverlay()
        {
            entryButton = Button(OutpostStrings.TrEn("Ayarlar", "Settings"), transform, new Vector2(-18, -8), new Vector2(118, 32));
            var entryRt = entryButton.GetComponent<RectTransform>(); entryRt.anchorMin = entryRt.anchorMax = new Vector2(1, 1); entryRt.pivot = new Vector2(1, 1);
            entryButton.gameObject.name = "BtnSettings";
            entryButton.onClick.AddListener(Open);
            var root = new GameObject("MarsOutpostSettingsOverlay", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            overlay = root;
            var bg = root.GetComponent<Image>(); bg.color = new Color(0.015f, 0.025f, 0.045f, 0.83f);
            Stretch(root.GetComponent<RectTransform>());

            var panel = CreatePanel("SettingsPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 590), new Color(0.055f, 0.09f, 0.15f, 0.98f));
            titleText = Label("SettingsTitle", panel, new Vector2(24, -16), new Vector2(400, 38), 26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            var close = Button("X", panel, new Vector2(-24, -16), new Vector2(38, 38));
            var closeRt = close.GetComponent<RectTransform>(); closeRt.anchorMin = closeRt.anchorMax = new Vector2(1, 1); closeRt.pivot = new Vector2(1, 1);
            close.onClick.AddListener(Close);

            var scrollGo = new GameObject("SettingsScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(panel, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>(); scrollRt.anchorMin = new Vector2(0, 0); scrollRt.anchorMax = new Vector2(1, 1); scrollRt.offsetMin = new Vector2(22, 76); scrollRt.offsetMax = new Vector2(-22, -66);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.12f); scrollGo.GetComponent<Mask>().showMaskGraphic = false;
            var contentGo = new GameObject("SettingsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>(); contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1); contentRt.pivot = new Vector2(0.5f, 1); contentRt.sizeDelta = new Vector2(0, 540);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(12, 12, 8, 12); layout.spacing = 5; layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandWidth = true;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollGo.GetComponent<ScrollRect>(); scroll.content = contentRt; scroll.horizontal = false; scroll.vertical = true; scroll.viewport = scrollRt;

            audioHeader = Section(contentGo.transform, 38); muteToggle = AddToggle(contentGo.transform, 42); volumeSlider = AddSlider(contentGo.transform, 52);
            graphicsHeader = Section(contentGo.transform, 38); qualityDropdown = AddDropdown(contentGo.transform, 44);
            displayHeader = Section(contentGo.transform, 38); fullscreenToggle = AddToggle(contentGo.transform, 42); resolutionDropdown = AddDropdown(contentGo.transform, 44);
            statusText = Label("SettingsStatus", panel, new Vector2(24, 42), new Vector2(340, 24), 14, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.65f, 0.85f, 1));
            statusText.text = "";
            var statusRect = statusText.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = Vector2.zero;
            statusRect.pivot = Vector2.zero;
            statusRect.anchoredPosition = new Vector2(24, 62);
            var apply = Button(OutpostStrings.TrEn("Uygula", "Apply"), panel, new Vector2(-180, 18), new Vector2(130, 40)); apply.onClick.AddListener(Apply);
            var cancel = Button(OutpostStrings.TrEn("İptal", "Cancel"), panel, new Vector2(-42, 18), new Vector2(110, 40)); cancel.onClick.AddListener(Close);
            var reset = Button(OutpostStrings.TrEn("Varsayılan", "Defaults"), panel, new Vector2(-320, 18), new Vector2(120, 40)); reset.onClick.AddListener(ResetDraft);
            muteToggle.onValueChanged.AddListener(v => draftMute = v);
            volumeSlider.onValueChanged.AddListener(v => draftVolume = v);
            fullscreenToggle.onValueChanged.AddListener(v => draftFullscreen = v);
            qualityDropdown.onValueChanged.AddListener(v => draftQuality = v);
            resolutionDropdown.onValueChanged.AddListener(v => draftResolution = v);
            RefreshLabels();
        }

        private void ReadDraft()
        {
            draftMute = audioController != null ? audioController.IsMuted : PlayerPrefs.GetInt(Prefix + "Muted", 0) != 0;
            draftVolume = audioController != null ? audioController.Volume : PlayerPrefs.GetFloat(Prefix + "Volume", 0.6f);
            draftQuality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", 2), 0, 2);
            draftFullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
            PopulateResolutions();
            var nativeRes = GetNativeSupportedResolution();
            int savedW = PlayerPrefs.GetInt(Prefix + "Width", nativeRes.width), savedH = PlayerPrefs.GetInt(Prefix + "Height", nativeRes.height);
            if (!IsValidResolution(savedW, savedH))
            {
                savedW = nativeRes.width;
                savedH = nativeRes.height;
            }
            draftResolution = FindResolution(savedW, savedH);
            muteToggle.SetIsOnWithoutNotify(draftMute); volumeSlider.SetValueWithoutNotify(draftVolume);
            fullscreenToggle.SetIsOnWithoutNotify(draftFullscreen); qualityDropdown.SetValueWithoutNotify(draftQuality);
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string> { OutpostStrings.TrEn("Düşük", "Low"), OutpostStrings.TrEn("Orta", "Medium"), OutpostStrings.TrEn("Yüksek", "High") });
            qualityDropdown.SetValueWithoutNotify(draftQuality);
            resolutionDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var r in resolutions) options.Add(r.width + " × " + r.height);
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(draftResolution);
        }

        private void LoadSavedSettings()
        {
            if (audioController != null)
            {
                audioController.SetMuted(PlayerPrefs.GetInt(Prefix + "Muted", audioController.IsMuted ? 1 : 0) != 0);
                audioController.SetVolume(PlayerPrefs.GetFloat(Prefix + "Volume", audioController.Volume));
            }
            int quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", 2), 0, 2);
            ApplyGraphics(quality);
            bool fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
            var nativeRes = GetNativeSupportedResolution();
            int targetW = nativeRes.width;
            int targetH = nativeRes.height;

            if (PlayerPrefs.HasKey(Prefix + "Width") && PlayerPrefs.HasKey(Prefix + "Height"))
            {
                int width = PlayerPrefs.GetInt(Prefix + "Width"), height = PlayerPrefs.GetInt(Prefix + "Height");
                if (IsValidResolution(width, height))
                {
                    targetW = width;
                    targetH = height;
                }
                else
                {
                    PlayerPrefs.SetInt(Prefix + "Width", targetW);
                    PlayerPrefs.SetInt(Prefix + "Height", targetH);
                    PlayerPrefs.Save();
                }
            }

            if (!Application.isBatchMode && targetW > 0 && targetH > 0)
                Screen.SetResolution(targetW, targetH, fullscreen);
        }

        public static Resolution GetNativeSupportedResolution()
        {
            if (Screen.currentResolution.width >= 640 && Screen.currentResolution.height >= 480)
                return Screen.currentResolution;

            Resolution best = default;
            foreach (var r in Screen.resolutions)
            {
                if (r.width >= 640 && r.height >= 480 && (r.width * r.height >= best.width * best.height))
                    best = r;
            }
            if (best.width >= 640 && best.height >= 480)
                return best;

            if (Screen.width >= 640 && Screen.height >= 480)
                return new Resolution { width = Screen.width, height = Screen.height };

            return new Resolution { width = 1280, height = 720 };
        }

        public static bool IsValidResolution(int width, int height)
        {
            if (width < 640 || height < 480)
                return false;

            var supported = Screen.resolutions;
            if (supported == null || supported.Length == 0)
                return width <= 7680 && height <= 4320;

            int maxW = 0, maxH = 0;
            for (int i = 0; i < supported.Length; i++)
            {
                if (supported[i].width == width && supported[i].height == height)
                    return true;
                if (supported[i].width > maxW) maxW = supported[i].width;
                if (supported[i].height > maxH) maxH = supported[i].height;
            }
            // Allow standard windowed sizes that fit within monitor bounds
            return maxW > 0 && width <= maxW && height <= maxH;
        }

        private void ResetDraft()
        {
            draftMute = false; draftVolume = 0.6f; draftQuality = 2; draftFullscreen = true;
            PopulateResolutions();
            int nativeWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : Screen.width;
            int nativeHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : Screen.height;
            draftResolution = FindResolution(nativeWidth, nativeHeight);
            muteToggle.SetIsOnWithoutNotify(false); volumeSlider.SetValueWithoutNotify(draftVolume); qualityDropdown.SetValueWithoutNotify(2);
            fullscreenToggle.SetIsOnWithoutNotify(true); resolutionDropdown.SetValueWithoutNotify(draftResolution);
            statusText.text = OutpostStrings.TrEn("Varsayılanlar hazır; uygulayın.", "Defaults ready; press Apply.");
        }

        private void Apply()
        {
            PlayerPrefs.SetInt(Prefix + "Muted", draftMute ? 1 : 0); PlayerPrefs.SetFloat(Prefix + "Volume", draftVolume);
            PlayerPrefs.SetInt(Prefix + "Quality", draftQuality); PlayerPrefs.SetInt(Prefix + "Fullscreen", draftFullscreen ? 1 : 0);
            var chosen = resolutions[Mathf.Clamp(draftResolution, 0, resolutions.Count - 1)];
            PlayerPrefs.SetInt(Prefix + "Width", chosen.width); PlayerPrefs.SetInt(Prefix + "Height", chosen.height); PlayerPrefs.Save();
            if (audioController != null) { audioController.SetMuted(draftMute); audioController.SetVolume(draftVolume); }
            ApplyGraphics(draftQuality);
            if (!Application.isBatchMode && chosen.width > 0 && chosen.height > 0)
                Screen.SetResolution(chosen.width, chosen.height, draftFullscreen);
            audioBeforeOpenMute = draftMute; audioBeforeOpenVolume = draftVolume;
            appliedThisOpen = true;
            statusText.text = OutpostStrings.TrEn("Ayarlar kaydedildi", "Settings saved");
        }

        private void ApplyGraphics(int quality)
        {
            if (runtimePipeline == null)
            {
                originalPipeline = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                if (originalPipeline == null) return;
                originalQualityPipeline = QualitySettings.renderPipeline;
                originalRenderScale = originalPipeline.renderScale; originalMsaa = originalPipeline.msaaSampleCount; originalShadowDistance = originalPipeline.shadowDistance;
                runtimePipeline = Instantiate(originalPipeline); runtimePipeline.name = originalPipeline.name + " (Runtime Settings)";
                GraphicsSettings.renderPipelineAsset = runtimePipeline;
                QualitySettings.renderPipeline = runtimePipeline;
            }
            runtimePipeline.renderScale = quality == 0 ? 0.75f : quality == 1 ? 0.9f : 1.0f;
            runtimePipeline.msaaSampleCount = quality == 0 ? 1 : quality == 1 ? 2 : 4;
            runtimePipeline.shadowDistance = quality == 0 ? 12f : quality == 1 ? 24f : 32f;
        }

        private void RestoreGraphics()
        {
            if (runtimePipeline == null) return;
            GraphicsSettings.renderPipelineAsset = originalPipeline; QualitySettings.renderPipeline = originalQualityPipeline;
            Destroy(runtimePipeline); runtimePipeline = null;
        }

        private void PopulateResolutions()
        {
            resolutions.Clear();
            foreach (var r in Screen.resolutions)
            {
                bool found = false; for (int i = 0; i < resolutions.Count; i++) if (resolutions[i].width == r.width && resolutions[i].height == r.height) { found = true; break; }
                if (!found) resolutions.Add(r);
            }
            // Windowed game sizes need not be listed as monitor fullscreen modes.
            if (Screen.width > 0 && Screen.height > 0 &&
                !resolutions.Exists(r => r.width == Screen.width && r.height == Screen.height))
                resolutions.Add(new Resolution { width = Screen.width, height = Screen.height });
            if (resolutions.Count == 0) resolutions.Add(new Resolution { width = 1280, height = 720 });
        }
        private int FindResolution(int w, int h) { for (int i = 0; i < resolutions.Count; i++) if (resolutions[i].width == w && resolutions[i].height == h) return i; return resolutions.Count - 1; }

        private void RefreshLabels()
        {
            if (titleText == null) return;
            titleText.text = OutpostStrings.TrEn("AYARLAR", "SETTINGS");
            entryButton.GetComponentInChildren<Text>().text = OutpostStrings.TrEn("Ayarlar", "Settings");
            audioHeader.text = OutpostStrings.TrEn("SES", "AUDIO"); graphicsHeader.text = OutpostStrings.TrEn("GRAFİK", "GRAPHICS"); displayHeader.text = OutpostStrings.TrEn("EKRAN", "DISPLAY");
            SetToggleLabel(muteToggle, OutpostStrings.TrEn("Sesi kapat", "Mute sound")); SetSliderLabel(volumeSlider, OutpostStrings.TrEn("Ses seviyesi", "Volume"));
            SetDropdownLabel(qualityDropdown, OutpostStrings.TrEn("Grafik kalitesi", "Graphics quality")); SetToggleLabel(fullscreenToggle, OutpostStrings.TrEn("Tam ekran", "Fullscreen")); SetDropdownLabel(resolutionDropdown, OutpostStrings.TrEn("Çözünürlük", "Resolution"));
        }

        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static Transform CreatePanel(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size, Color color)
        { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = amin; rt.anchorMax = amax; rt.anchoredPosition = pos; rt.sizeDelta = size; go.GetComponent<Image>().color = color; return go.transform; }
        private static Text Label(string name, Transform parent, Vector2 pos, Vector2 size, int fontSize, FontStyle style, TextAnchor anchor, Color color)
        { var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = pos; rt.sizeDelta = size; var t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = fontSize; t.fontStyle = style; t.alignment = anchor; t.color = color; t.text = name; return t; }
        private static Button Button(string text, Transform parent, Vector2 pos, Vector2 size)
        { var go = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); rt.anchoredPosition = pos; rt.sizeDelta = size; go.GetComponent<Image>().color = new Color(0.12f, 0.32f, 0.52f); var b = go.GetComponent<Button>(); b.targetGraphic = go.GetComponent<Image>(); var label = Label("Label", go.transform, Vector2.zero, size, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.pivot = new Vector2(.5f,.5f); label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = Vector2.zero; label.text = text; return b; }
        private static Text Section(Transform parent, float h) { var t = Label("Section", parent, Vector2.zero, new Vector2(500, h), 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(.45f,.8f,1)); t.text = ""; t.gameObject.AddComponent<LayoutElement>().preferredHeight = h; return t; }
        private static Toggle AddToggle(Transform parent, float h) { var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement)); go.transform.SetParent(parent,false); go.GetComponent<RectTransform>().sizeDelta=new Vector2(500,h); go.GetComponent<LayoutElement>().preferredHeight = h; var label = Label("Label", go.transform, new Vector2(4,-2), new Vector2(420,h), 16, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white); var box = new GameObject("Check", typeof(RectTransform), typeof(Image)); box.transform.SetParent(go.transform,false); var rt=box.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(1,.5f); rt.anchorMax=rt.anchorMin; rt.pivot=new Vector2(1,.5f); rt.anchoredPosition=new Vector2(-8,0); rt.sizeDelta=new Vector2(24,24); var check=new GameObject("Mark",typeof(RectTransform),typeof(Image)); check.transform.SetParent(box.transform,false); Stretch(check.GetComponent<RectTransform>()); check.GetComponent<Image>().color=new Color(.35f,.85f,1); var toggle=go.GetComponent<Toggle>(); toggle.targetGraphic=box.GetComponent<Image>(); toggle.graphic=check.GetComponent<Image>(); return toggle; }
        private static Slider AddSlider(Transform parent, float height)
        {
            var row = new GameObject("SliderRow", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            Label("Label", row.transform, new Vector2(4, -2), new Vector2(230, height), 16, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(row.transform, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0, .5f); trackRect.anchorMax = new Vector2(1, .5f);
            trackRect.offsetMin = new Vector2(245, -4); trackRect.offsetMax = new Vector2(-20, 4);
            track.GetComponent<Image>().color = new Color(.15f, .22f, .3f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(.2f, .65f, .95f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(.5f, .5f);
            handleRect.sizeDelta = new Vector2(16, 16);
            handle.GetComponent<Image>().color = Color.white;
            var slider = row.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>(); slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = 0; slider.maxValue = 1;
            return slider;
        }
        private static Dropdown AddDropdown(Transform parent,float h)
        {
            var go=new GameObject("DropdownRow",typeof(RectTransform),typeof(Dropdown),typeof(Image),typeof(LayoutElement)); go.transform.SetParent(parent,false); go.GetComponent<RectTransform>().sizeDelta=new Vector2(500,h); go.GetComponent<LayoutElement>().preferredHeight=h; go.GetComponent<Image>().color=new Color(.09f,.14f,.21f);
            Label("Label",go.transform,new Vector2(8,-2),new Vector2(235,h),15,FontStyle.Normal,TextAnchor.MiddleLeft,Color.white);
            var value=Label("Value",go.transform,new Vector2(250,-2),new Vector2(270,h),15,FontStyle.Normal,TextAnchor.MiddleRight,Color.white);
            var d=go.GetComponent<Dropdown>(); d.captionText=value;
            var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(ScrollRect),typeof(Mask)); template.transform.SetParent(go.transform,false);
            var templateRt=template.GetComponent<RectTransform>(); templateRt.anchorMin=new Vector2(0,0); templateRt.anchorMax=new Vector2(1,0); templateRt.pivot=new Vector2(.5f,1); templateRt.anchoredPosition=new Vector2(0,-h); templateRt.sizeDelta=new Vector2(0,150);
            template.GetComponent<Image>().color=new Color(.05f,.08f,.13f,.98f); template.GetComponent<Mask>().showMaskGraphic=true;
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask)); viewport.transform.SetParent(template.transform,false); Stretch(viewport.GetComponent<RectTransform>()); viewport.GetComponent<Image>().color=Color.white; viewport.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Content",typeof(RectTransform)); content.transform.SetParent(viewport.transform,false); var contentRt=content.GetComponent<RectTransform>(); contentRt.anchorMin=new Vector2(0,1); contentRt.anchorMax=Vector2.one; contentRt.pivot=new Vector2(.5f,1); contentRt.sizeDelta=new Vector2(0,28);
            var item=new GameObject("Item",typeof(RectTransform),typeof(Toggle)); item.transform.SetParent(content.transform,false); var itemRt=item.GetComponent<RectTransform>(); itemRt.anchorMin=new Vector2(0,1); itemRt.anchorMax=new Vector2(1,1); itemRt.pivot=new Vector2(.5f,1); itemRt.sizeDelta=new Vector2(0,28);
            var itemText=Label("Item Label",item.transform,new Vector2(8,-1),new Vector2(430,27),14,FontStyle.Normal,TextAnchor.MiddleLeft,Color.white); var itemToggle=item.GetComponent<Toggle>(); itemToggle.targetGraphic=item.AddComponent<Image>();
            var sr=template.GetComponent<ScrollRect>(); sr.viewport=viewport.GetComponent<RectTransform>(); sr.content=contentRt; sr.horizontal=false; sr.vertical=true;
            d.template=templateRt; d.itemText=itemText; d.itemImage=item.GetComponent<Image>(); d.options.Add(new Dropdown.OptionData("")); template.SetActive(false);
            return d;
        }
        private static void SetToggleLabel(Toggle t,string s) { var tx=t.transform.Find("Label").GetComponent<Text>(); tx.text=s; }
        private static void SetSliderLabel(Slider s,string text) { s.transform.Find("Label").GetComponent<Text>().text=text; }
        private static void SetDropdownLabel(Dropdown d,string text) { d.transform.Find("Label").GetComponent<Text>().text=text; }
    }
}
