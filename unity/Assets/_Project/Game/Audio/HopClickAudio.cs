using UnityEngine;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // Juice layer "hop audio": one click per hop, pitch +1 semitone per ray
    // depth, capped at +7. Clicks land 40 ms apart, so they need to overlap —
    // a small pool of sources, each set to its own pitch, rather than one
    // source whose pitch would smear across the whole wave.
    //
    // And the one other sound the board makes: the chime on a solve. It is
    // the same instrument. The clicks walk a chromatic ladder up from the
    // root to the fifth; the chime plays that key's triad and tops it with
    // the octave, so it lands as the resolution of what the player has been
    // hearing rather than as a sound from somewhere else. Scheduled to
    // follow the last click of the winning wave, never to overlap it.
    //
    // Both clips are synthesised, like every other asset in this project.
    // tools/render_audio.py renders the same formulas to WAV to listen to
    // off-device; change both together.
    public sealed class HopClickAudio
    {
        const int Sources = 8;
        const int SampleRate = 44100;
        const int ClickSamples = SampleRate * 30 / 1000;   // 30 ms
        const int ChimeSamples = SampleRate * 900 / 1000;  // 0.9 s

        // The click's root, and the chime's. The ladder tops out at +7
        // semitones (Vfx.HopPitchCapSemitones), the fifth: 1768 Hz.
        const float Root = 1180f;

        // Levels. The click was 0.35, a guess written before anything had
        // been heard; the clip itself was also a bare sine with no attack,
        // which is what read as thin. The clip now carries a transient and
        // a lower partial and is normalised to a fixed peak, so the level
        // here is the level.
        const float ClickVolume = 0.6f;
        const float ChimeVolume = 0.6f;
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

        // The chime: root, major third, fifth, octave — 1180, 1486, 1770,
        // 2360 Hz — 90 ms apart, the last held longest. Each note is a sine
        // with a little second harmonic for brightness and a quiet
        // sub-octave for body, a 3 ms attack so it speaks rather than
        // pops, and an exponential tail.
        static AudioClip BuildChime()
        {
            var samples = new float[ChimeSamples];
            float[] notes = { Root, Root * 1.2599f, Root * 1.4983f, Root * 2f };   // +0, +4, +7, +12 semitones
            const float NoteGap = 0.09f;
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                float start = k * NoteGap;
                float tau = k == notes.Length - 1 ? 0.35f : 0.16f;
                float gain = k == notes.Length - 1 ? 1f : 0.8f;
                int first = Mathf.RoundToInt(start * SampleRate);
                for (int n = first; n < ChimeSamples; n++)
                {
                    float t = (n - first) / (float)SampleRate;
                    float attack = Mathf.Min(1f, t / 0.003f);
                    float envelope = gain * attack * Mathf.Exp(-t / tau);
                    float tone = Mathf.Sin(2f * Mathf.PI * f * t)
                        + 0.25f * Mathf.Sin(2f * Mathf.PI * 2f * f * t)
                        + 0.2f * Mathf.Sin(2f * Mathf.PI * (f / 2f) * t);
                    samples[n] += tone * envelope;
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
