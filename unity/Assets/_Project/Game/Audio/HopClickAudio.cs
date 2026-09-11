using UnityEngine;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // Juice layer "hop audio": one click per hop, pitch +1 semitone per ray
    // depth, capped at +7. Clicks land 40 ms apart, so they need to overlap —
    // a small pool of sources, each set to its own pitch, rather than one
    // source whose pitch would smear across the whole wave.
    //
    // Both sounds are struck glass, because the tiles are glass. A free
    // bar's modes are inharmonic — 1 : 2.756 : 5.404 : 8.933 — which is
    // what makes a strike read as glass rather than as a note, and the
    // higher modes die fastest. The click is a tap: 35 ms, the modes gone
    // almost at once. The solve is two strikes: a short grace at the
    // fifth, then 55 ms later the main strike an octave above the click's
    // root with a quieter strike at the root under it for body, its
    // fundamental doubled a few cents apart so it shimmers as it fades —
    // under 0.4 s, snappy. Scheduled to follow the last click of the
    // winning wave, never to overlap it.
    //
    // Both clips are synthesised, like every other asset in this project.
    // tools/render_audio.py renders the same formulas to WAV to listen to
    // off-device; change both together.
    public sealed class HopClickAudio
    {
        const int Sources = 8;
        const int SampleRate = 44100;
        const int ClickSamples = SampleRate * 35 / 1000;   // 35 ms
        const int StrikeSamples = SampleRate * 320 / 1000;   // the main strike, 0.32 s
        const int GraceSamples = SampleRate * 120 / 1000;    // the grace, 0.12 s
        const int GraceLead = SampleRate * 55 / 1000;        // the main strike lands this far after the grace
        const int ChimeSamples = GraceLead + StrikeSamples;

        // The click's root, and the chime's. The ladder tops out at +7
        // semitones (Vfx.HopPitchCapSemitones), the fifth: 1768 Hz.
        const float Root = 1180f;

        // Levels. Each clip is normalised to a fixed peak, so the level
        // here is the level. tools/render_audio.py is where they were set.
        const float ClickVolume = 0.6f;
        const float ChimeVolume = 0.55f;
        const float Peak = 0.9f;

        readonly GameObject _root;
        readonly AudioSource[] _sources = new AudioSource[Sources];
        readonly AudioSource _chime;
        readonly float[] _queueTime = new float[64];
        readonly int[] _queueDepth = new int[64];
        int _queued;
        int _next;
        float _chimeAt = float.NaN;

        public bool Enabled = true;
        public bool Muted;

        public HopClickAudio(Transform parent)
        {
            _root = new GameObject("hop-audio");
            _root.transform.SetParent(parent, false);

            var clip = BuildClick();
            for (int n = 0; n < Sources; n++)
            {
                _sources[n] = MakeSource(clip, ClickVolume);
            }
            _chime = MakeSource(BuildChime(), ChimeVolume);
        }

        AudioSource MakeSource(AudioClip clip, float volume)
        {
            var source = _root.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root);
        }

        // Scheduled against the board clock so a click lands with its hop even
        // when the frame rate does not divide 40 ms.
        public void Schedule(float boardTime, int depth)
        {
            if (_queued >= _queueTime.Length) return;
            _queueTime[_queued] = boardTime;
            _queueDepth[_queued] = depth;
            _queued++;
        }

        // The solve, at this board time — the caller passes the end of the
        // winning wave, so the chime follows the last click rather than
        // landing on top of it. One at a time: a second call before the
        // first has played moves it.
        public void ScheduleChime(float boardTime)
        {
            _chimeAt = boardTime;
        }

        public void Tick(float boardTime)
        {
            int keep = 0;
            for (int n = 0; n < _queued; n++)
            {
                if (boardTime >= _queueTime[n])
                {
                    if (Enabled && !Muted) Play(_queueDepth[n]);
                    continue;
                }
                _queueTime[keep] = _queueTime[n];
                _queueDepth[keep] = _queueDepth[n];
                keep++;
            }
            _queued = keep;

            if (!float.IsNaN(_chimeAt) && boardTime >= _chimeAt)
            {
                _chimeAt = float.NaN;
                if (!Muted) _chime.Play();
            }
        }

        void Play(int depth)
        {
            int semitones = Mathf.Clamp(depth, 0, Vfx.HopPitchCapSemitones);
            var source = _sources[_next];
            _next = (_next + 1) % Sources;
            source.pitch = Mathf.Pow(2f, semitones / 12f);
            source.Play();
        }

        // ---- synthesis ----

        static readonly float[] Modes = { 1f, 2.756f, 5.404f, 8.933f };

        // One strike: the four modes at their gains and decays, an optional
        // second fundamental a few cents sharp (the shimmer), a burst of
        // noise on the first millisecond (the snap), and a 0.4 ms attack so
        // the first sample is not a step.
        static void Strike(float[] samples, float f0, float[] gains, float[] taus, float noise, float shimmer, uint seed, float mix)
        {
            for (int n = 0; n < samples.Length; n++)
            {
                float t = n / (float)SampleRate;
                float x = 0f;
                for (int k = 0; k < Modes.Length; k++)
                {
                    float f = f0 * Modes[k];
                    if (f > 18000f || gains[k] <= 0f) continue;
                    x += gains[k] * Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t / taus[k]);
                }
                if (shimmer > 0f)
                {
                    x += shimmer * Mathf.Sin(2f * Mathf.PI * f0 * 1.004f * t) * Mathf.Exp(-t / taus[0]);
                }
                seed = seed * 1664525u + 1013904223u;
                float white = (seed >> 8) / 8388608f - 1f;
                x += noise * white * Mathf.Exp(-t * 2500f);
                float attack = Mathf.Min(1f, t / 0.0004f);
                samples[n] += x * attack * mix;
            }
        }

        // The hop: a tap on glass at the root.
        static AudioClip BuildClick()
        {
            var samples = new float[ClickSamples];
            Strike(samples, Root,
                new[] { 1f, 0.55f, 0.3f, 0.12f }, new[] { 0.016f, 0.008f, 0.004f, 0.002f },
                noise: 0.5f, shimmer: 0f, seed: 0x9E3779B9u, mix: 1f);
            Normalise(samples);
            var clip = AudioClip.Create("hop-click", ClickSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // The solve: the grace at the fifth, then the strike an octave up
        // with the root under it. Each strike is normalised on its own
        // before the mix, as the render tool does it, so the three sit at
        // the ratios written here.
        static AudioClip BuildChime()
        {
            var grace = new float[GraceSamples];
            Strike(grace, Root * 1.4983f,
                new[] { 1f, 0.4f, 0.2f, 0f }, new[] { 0.05f, 0.03f, 0.015f, 0.01f },
                noise: 0.3f, shimmer: 0f, seed: 0x2545F491u, mix: 1f);
            Normalise(grace);
            var top = new float[StrikeSamples];
            Strike(top, Root * 2f,
                new[] { 1f, 0.45f, 0.22f, 0f }, new[] { 0.19f, 0.07f, 0.03f, 0.01f },
                noise: 0.35f, shimmer: 0.35f, seed: 0x2545F491u, mix: 1f);
            Normalise(top);
            var body = new float[StrikeSamples];
            Strike(body, Root,
                new[] { 1f, 0.3f, 0.1f, 0f }, new[] { 0.09f, 0.04f, 0.02f, 0.01f },
                noise: 0f, shimmer: 0f, seed: 0x2545F491u, mix: 1f);
            Normalise(body);
            var samples = new float[ChimeSamples];
            for (int n = 0; n < GraceSamples; n++) samples[n] = 0.7f * grace[n];
            for (int n = 0; n < StrikeSamples; n++) samples[GraceLead + n] += top[n] + 0.45f * body[n];
            Normalise(samples);
            var clip = AudioClip.Create("solve", ChimeSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // Scale the clip so its loudest sample sits at Peak. The level a
        // source plays at is then ClickVolume / ChimeVolume and nothing
        // else — a partial added to a formula cannot push the clip into
        // clipping or quietly halve it.
        static void Normalise(float[] samples)
        {
            float max = 0f;
            for (int n = 0; n < samples.Length; n++) max = Mathf.Max(max, Mathf.Abs(samples[n]));
            if (max <= 0f) return;
            float k = Peak / max;
            for (int n = 0; n < samples.Length; n++) samples[n] *= k;
        }
    }
}
