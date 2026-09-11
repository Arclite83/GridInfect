using UnityEngine;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // Juice layer "hop audio": one click per hop, pitch +1 semitone per ray
    // depth, capped at +7. Clicks land 40 ms apart, so they need to overlap —
    // a small pool of sources, each set to its own pitch, rather than one
    // source whose pitch would smear across the whole wave.
    //
    // And the one other sound the board makes: the solve. An analogue
    // synth voice in the click's key — the clicks walk a chromatic ladder
    // up from the root, and this lands on the root an octave down: two
    // detuned sawtooths and a sub through a resonant lowpass whose cutoff
    // sweeps open and closes again, the pitch sliding in from a fifth
    // below, a breath of noise on the attack, and a tanh drive over all of
    // it. Half a second. The first cut was a bell and the second a chip
    // blip; neither is what this board sounds like. Scheduled to follow
    // the last click of the winning wave, never to overlap it.
    //
    // Both clips are synthesised, like every other asset in this project.
    // tools/render_audio.py renders the same formulas to WAV to listen to
    // off-device; change both together.
    public sealed class HopClickAudio
    {
        const int Sources = 8;
        const int SampleRate = 44100;
        const int ClickSamples = SampleRate * 30 / 1000;   // 30 ms
        const int ChimeSamples = SampleRate * 450 / 1000;  // 0.45 s

        // The click's root, and the chime's. The ladder tops out at +7
        // semitones (Vfx.HopPitchCapSemitones), the fifth: 1768 Hz.
        const float Root = 1180f;

        // Levels. The click was 0.35, a guess written before anything had
        // been heard; the clip itself was also a bare sine with no attack,
        // which is what read as thin. The clip now carries a transient and
        // a lower partial and is normalised to a fixed peak, so the level
        // here is the level.
        const float ClickVolume = 0.6f;
        const float ChimeVolume = 0.5f;
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

        // The solve. Oscillators: a saw at 590 Hz (the click's root an
        // octave down), one +10 cents beside it, and a sub an octave under
        // both — each additive to 16 kHz, so nothing aliases. Pitch starts a
        // fifth low and settles in ~50 ms. Filter: a Chamberlin state-
        // variable lowpass, Q 3.5, cutoff from 350 Hz opening to 5.2 kHz
        // over 50 ms and closing on a 140 ms tail (capped at 6 kHz, where
        // that filter is still stable at this rate). Noise on the first few
        // ms, then tanh drive and the amplitude envelope: 2 ms in, 60 ms
        // held, a 160 ms fall.
        static AudioClip BuildChime()
        {
            var samples = new float[ChimeSamples];
            const float Target = Root / 2f;
            const float Drive = 3f;
            const float Noise = 0.35f;
            float[] rates = { 1f, 1.006f, 0.5f };
            float[] gains = { 1f, 0.8f, 0.4f };
            var phase = new float[3];
            float low = 0f, band = 0f;
            uint seed = 0x9E3779B9u;
            float driveNorm = 1f / Tanh(Drive);
            for (int n = 0; n < ChimeSamples; n++)
            {
                float t = n / (float)SampleRate;
                float f0 = Target * Mathf.Pow(2f, -(7f / 12f) * Mathf.Exp(-t / 0.03f));
                float x = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float f = f0 * rates[k];
                    phase[k] += f / SampleRate;
                    if (phase[k] >= 1f) phase[k] -= 1f;   // every harmonic is an integer multiple, so this is free; it keeps the sine's argument small
                    int harmonics = Mathf.Min(40, Mathf.Max(1, (int)(16000f / f)));
                    float saw = 0f;
                    for (int h = 1; h <= harmonics; h++)
                    {
                        saw += Mathf.Sin(2f * Mathf.PI * h * phase[k]) / h;
                    }
                    x += gains[k] * saw * (2f / Mathf.PI);
                }
                seed = seed * 1664525u + 1013904223u;
                float white = (seed >> 8) / 8388608f - 1f;
                x += Noise * white * Mathf.Exp(-t * 300f);

                float sweep = Mathf.Min(1f, t / 0.05f) * Mathf.Exp(-Mathf.Max(0f, t - 0.05f) / 0.14f);
                float cutoff = 350f + (5200f - 350f) * sweep;
                float fc = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(cutoff, 6000f) / SampleRate);
                const float Q = 1f / 3.5f;
                low += fc * band;
                float high = x - low - Q * band;
                band += fc * high;
                float y = Tanh(low * Drive) * driveNorm;

                float attack = Mathf.Min(1f, t / 0.002f);
                float envelope = attack * (t < 0.06f ? 1f : Mathf.Exp(-(t - 0.06f) / 0.16f));
                samples[n] = y * envelope;
            }
            Normalise(samples);
            var clip = AudioClip.Create("solve", ChimeSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Tanh(float x)
        {
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
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
