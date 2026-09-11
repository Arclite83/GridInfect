using UnityEngine;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // Juice layer "hop audio": one click per hop, pitch +1 semitone per ray
    // depth, capped at +7. Clicks land 40 ms apart, so they need to overlap —
    // a small pool of sources, each set to its own pitch, rather than one
    // source whose pitch would smear across the whole wave.
    //
    // And the one other sound the board makes: the confirm on a solve. It
    // is the same instrument's key — the clicks walk a chromatic ladder up
    // from the root to the fifth, and this is the root and its octave — but
    // it is a hardware sound, not a musical one: two gated pulse-wave
    // blips, the second held a moment, done in a quarter of a second. The
    // first cut was a four-note bell with long tails, and a bell is not
    // what a circuit board says when it is done. Scheduled to follow the
    // last click of the winning wave, never to overlap it.
    //
    // Both clips are synthesised, like every other asset in this project.
    // tools/render_audio.py renders the same formulas to WAV to listen to
    // off-device; change both together.
    public sealed class HopClickAudio
    {
        const int Sources = 8;
        const int SampleRate = 44100;
        const int ClickSamples = SampleRate * 30 / 1000;   // 30 ms
        const int ChimeSamples = SampleRate * 300 / 1000;  // 0.3 s

        // The click's root, and the chime's. The ladder tops out at +7
        // semitones (Vfx.HopPitchCapSemitones), the fifth: 1768 Hz.
        const float Root = 1180f;

        // Levels. The click was 0.35, a guess written before anything had
        // been heard; the clip itself was also a bare sine with no attack,
        // which is what read as thin. The clip now carries a transient and
        // a lower partial and is normalised to a fixed peak, so the level
        // here is the level.
        const float ClickVolume = 0.6f;
        const float ChimeVolume = 0.45f;
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

        // The click: a 1180 Hz body under a 14 ms decay, an octave-down
        // partial for weight (shorter, so it is felt in the attack and not
        // heard as a hum), a 3.3 kHz snap, and one millisecond of noise at
        // the front, which is the "click" itself — a sine that starts from
        // zero has no attack, and an attack is what a phone speaker can
        // actually reproduce of a sound this short. Nothing under 590 Hz: a
        // phone speaker has nothing there to give.
        static AudioClip BuildClick()
        {
            var samples = new float[ClickSamples];
            uint noise = 0x9E3779B9u;   // fixed seed: the same clip every launch
            for (int n = 0; n < ClickSamples; n++)
            {
                float t = n / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 70f);
                float body = Mathf.Sin(2f * Mathf.PI * Root * t);
                float sub = 0.5f * Mathf.Sin(2f * Mathf.PI * (Root / 2f) * t) * Mathf.Exp(-t * 120f);
                float snap = 0.4f * Mathf.Sin(2f * Mathf.PI * 3300f * t) * Mathf.Exp(-t * 400f);
                noise = noise * 1664525u + 1013904223u;
                float white = (noise >> 8) / 8388608f - 1f;   // [-1, 1)
                float tick = 0.6f * white * Mathf.Exp(-t * 1500f);
                samples[n] = (body + sub + snap + tick) * envelope;
            }
            Normalise(samples);
            var clip = AudioClip.Create("hop-click", ClickSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // The confirm: root then octave, 1180 and 2360 Hz, as pulse waves
        // (odd harmonics, the chip-tone spectrum) under hard gates — 1 ms
        // on, 6 ms off — so each blip starts and stops like a switch. The
        // first is 45 ms; the second starts 70 ms in and holds 60 ms before
        // a short fall. Nothing rings.
        static AudioClip BuildChime()
        {
            var samples = new float[ChimeSamples];
            // (frequency, start s, gate length s, release s)
            float[] freq = { Root, Root * 2f };
            float[] start = { 0f, 0.07f };
            float[] hold = { 0.045f, 0.06f };
            float[] release = { 0.006f, 0.05f };
            for (int k = 0; k < freq.Length; k++)
            {
                float f = freq[k];
                int first = Mathf.RoundToInt(start[k] * SampleRate);
                int last = Mathf.Min(ChimeSamples, first + Mathf.RoundToInt((hold[k] + release[k] * 6f) * SampleRate));
                for (int n = first; n < last; n++)
                {
                    float t = (n - first) / (float)SampleRate;
                    float attack = Mathf.Min(1f, t / 0.001f);
                    float gate = t <= hold[k] ? 1f : Mathf.Exp(-(t - hold[k]) / release[k]);
                    float w = 2f * Mathf.PI * f * t;
                    float tone = Mathf.Sin(w) + Mathf.Sin(3f * w) / 3f + Mathf.Sin(5f * w) / 5f;
                    samples[n] += tone * attack * gate;
                }
            }
            Normalise(samples);
            var clip = AudioClip.Create("solve-chime", ChimeSamples, 1, SampleRate, false);
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
