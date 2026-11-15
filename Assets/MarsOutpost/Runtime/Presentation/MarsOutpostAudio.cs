using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarsOutpost.Presentation
{
    /// <summary>
    /// Procedural Web/Desktop-safe SFX controller for MarsOutpost (no external paid assets).
    /// Generates task complete, hazard/alert warning, and command rejected clips at runtime.
    /// </summary>
    public sealed class MarsOutpostAudio : MonoBehaviour
    {
        [SerializeField] private SimulationRunner runner;

        private AudioSource audioSource, ambienceSource, musicSource;
        private AudioClip ambienceClip, musicClip;
        private float fade = 0f;
        private bool focused = true;
        public float AmbienceVolume { get; private set; } = 0.45f;
        public float MusicVolume { get; private set; } = 0.35f;
        public float SfxVolume { get; private set; } = 0.8f;
        private AudioClip taskCompleteClip;
        private AudioClip warningClip;
        private AudioClip errorClip;

        private readonly HashSet<string> completedTasksSeen = new HashSet<string>();
        private readonly Dictionary<string, EventPhase> eventPhasesSeen = new Dictionary<string, EventPhase>();
        private int lastAlertCount;
        private EarthAdviceStatus lastAdviceStatus = EarthAdviceStatus.NotRequested;
        private double lastElapsedSeen = -1;

        public bool IsMuted { get; private set; }
        public float Volume { get; private set; } = 0.6f;

        private void Awake()
        {
            if (runner == null) runner = GetComponent<SimulationRunner>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = Volume * SfxVolume;
            audioSource.mute = IsMuted;
            ambienceSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            ambienceClip = CreateLoop("MarsWindAndHum", false);
            musicClip = CreateLoop("MarsOriginalPad", true);
            foreach (var source in new[] { ambienceSource, musicSource })
            {
                source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
                source.ignoreListenerPause = true; source.volume = 0f;
            }
            ambienceSource.clip = ambienceClip; musicSource.clip = musicClip;
            ambienceSource.Play(); musicSource.Play();

            taskCompleteClip = CreateToneSequenceClip("SFX_TaskComplete", new[] { 523.25f, 659.25f }, 0.12f, 0.24f);
            warningClip = CreateToneSequenceClip("SFX_Warning", new[] { 880.0f, 660.0f }, 0.14f, 0.28f);
            errorClip = CreateToneSequenceClip("SFX_Error", new[] { 196.0f }, 0.14f, 0.22f);
        }

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.StateChanged += HandleStateChanged;
                runner.CommandRejected += HandleCommandRejected;
                if (runner.State != null)
                    SyncBaseline(runner.State);
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.StateChanged -= HandleStateChanged;
                runner.CommandRejected -= HandleCommandRejected;
            }
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            // Smoothly fade all buses in Update rather than cutting loop waveforms.
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            // Applied once per bus in Update.
        }

        public void SetAmbienceVolume(float value) => AmbienceVolume = Mathf.Clamp01(value);
        public void SetMusicVolume(float value) => MusicVolume = Mathf.Clamp01(value);
        public void SetSfxVolume(float value) => SfxVolume = Mathf.Clamp01(value);

        private void OnApplicationFocus(bool hasFocus) { focused = hasFocus; }
        private void Update()
        {
            float target = IsMuted ? 0f : focused ? 1f : 0.2f;
            fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime * 2f);
            if (audioSource != null) { audioSource.mute = false; audioSource.volume = Volume * SfxVolume * fade; }
            if (ambienceSource != null) ambienceSource.volume = Volume * AmbienceVolume * fade;
            if (musicSource != null) musicSource.volume = Volume * MusicVolume * fade;
        }

        // All oscillators use whole cycles per loop, so the wrap is continuous.
        // Deterministic harmonic noise and a slow original chord require no external assets.
        private static AudioClip CreateLoop(string name, bool music)
        {
            const int rate = 22050, seconds = 8, count = rate * seconds;
            var data = new float[count];
            var phases = new float[32];
            var random = new System.Random(271828);
            for (int j = 0; j < phases.Length; j++) phases[j] = (float)(random.NextDouble() * Math.PI * 2.0);
            for (int i = 0; i < count; i++)
            {
                double phase = 2.0 * Math.PI * i / count;
                double sample = 0;
                if (music)
                {
                    // Integer cycle counts produce near A2/C3/E3 and a sparse upper tone.
                    sample = 0.035 * Math.Sin(phase * 880) + 0.026 * Math.Sin(phase * 1046) +
                             0.022 * Math.Sin(phase * 1318);
                    sample *= 0.65 + 0.35 * Math.Cos(phase);
                    sample += 0.014 * (0.5 - 0.5 * Math.Cos(phase * 2)) * Math.Sin(phase * 1760);
                }
                else
                {
                    for (int j = 0; j < phases.Length; j++)
                        sample += Math.Sin(phase * (1601 + j * 137) + phases[j]) / (32.0 * (1.0 + j * 0.12));
                    sample *= 0.12 * (0.65 + 0.35 * Math.Cos(phase));
                    sample += 0.018 * Math.Sin(phase * 480);
                }
                data[i] = (float)sample;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void SyncBaseline(MissionState state)
        {
            completedTasksSeen.Clear();
            for (int i = 0; i < state.Tasks.Count; i++)
                if (state.Tasks[i].IsCompleted) completedTasksSeen.Add(state.Tasks[i].Id);

            eventPhasesSeen.Clear();
            for (int i = 0; i < state.Events.Count; i++)
                eventPhasesSeen[state.Events[i].Id] = state.Events[i].Phase;

            lastAlertCount = state.LowOxygenAlertCount;
            lastAdviceStatus = state.EarthAdviceStatus;
            lastElapsedSeen = state.ElapsedSeconds;
        }

        private void HandleStateChanged(MissionState state)
        {
            if (state == null) return;

            if (lastElapsedSeen < 0 || state.ElapsedSeconds < lastElapsedSeen)
            {
                SyncBaseline(state);
                return;
            }
            lastElapsedSeen = state.ElapsedSeconds;

            bool playComplete = false;
            for (int i = 0; i < state.Tasks.Count; i++)
            {
                var t = state.Tasks[i];
                if (t.IsCompleted && completedTasksSeen.Add(t.Id))
                    playComplete = true;
                else if (!t.IsCompleted && completedTasksSeen.Contains(t.Id))
                    completedTasksSeen.Remove(t.Id);
            }

            bool playWarning = false;
            for (int i = 0; i < state.Events.Count; i++)
            {
                var ev = state.Events[i];
                if (eventPhasesSeen.TryGetValue(ev.Id, out var prevPhase))
                {
                    if (prevPhase != ev.Phase && (ev.Phase == EventPhase.Warning || ev.Phase == EventPhase.Active))
                        playWarning = true;
                }
                eventPhasesSeen[ev.Id] = ev.Phase;
            }

            if (state.LowOxygenAlertCount > lastAlertCount)
                playWarning = true;
            lastAlertCount = state.LowOxygenAlertCount;

            if (lastAdviceStatus != EarthAdviceStatus.Delivered && state.EarthAdviceStatus == EarthAdviceStatus.Delivered)
                playComplete = true;
            lastAdviceStatus = state.EarthAdviceStatus;

            if (playWarning) PlayClip(warningClip);
            else if (playComplete) PlayClip(taskCompleteClip);
        }

        private void HandleCommandRejected(CommandResult _)
        {
            PlayClip(errorClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (IsMuted || Volume <= 0.001f || audioSource == null || clip == null) return;
            // AudioSource.volume already applies the user's master volume.
            audioSource.PlayOneShot(clip, 1f);
        }

        private void OnDestroy()
        {
            if (ambienceSource != null) ambienceSource.Stop();
            if (musicSource != null) musicSource.Stop();
            if (ambienceSource != null) Destroy(ambienceSource);
            if (musicSource != null) Destroy(musicSource);
            if (ambienceClip != null) Destroy(ambienceClip);
            if (musicClip != null) Destroy(musicClip);
            if (taskCompleteClip != null) Destroy(taskCompleteClip);
            if (warningClip != null) Destroy(warningClip);
            if (errorClip != null) Destroy(errorClip);
        }

        private static AudioClip CreateToneSequenceClip(string clipName, float[] frequencies, float noteSeconds, float gain)
        {
            const int sampleRate = 22050;
            int samplesPerNote = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteSeconds));
            int totalSamples = samplesPerNote * frequencies.Length;
            var data = new float[totalSamples];

            for (int n = 0; n < frequencies.Length; n++)
            {
                float freq = frequencies[n];
                int offset = n * samplesPerNote;
                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Sin(Mathf.PI * (i / (float)samplesPerNote));
                    data[offset + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * gain * env;
                }
            }

            var clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
