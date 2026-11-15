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

        private AudioSource audioSource;
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
            audioSource.volume = Volume;
            audioSource.mute = IsMuted;

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
            if (audioSource != null) audioSource.mute = muted;
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            if (audioSource != null) audioSource.volume = Volume;
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
