using UnityEngine;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // Juice layer "hop audio": one click per hop, pitch +1 semitone per ray
    // depth, capped at +7. Clicks land 40 ms apart, so they need to overlap —
    // a small pool of sources, each set to its own pitch, rather than one
    // source whose pitch would smear across the whole wave.
    //
    // The clip is synthesised, like every other asset in this project.
    public sealed class HopClickAudio
    {
        const int Sources = 8;
        const int SampleRate = 44100;
        const int ClickSamples = SampleRate / 40;   // 25 ms

        readonly GameObject _root;
        readonly AudioSource[] _sources = new AudioSource[Sources];
        readonly float[] _queueTime = new float[64];
        readonly int[] _queueDepth = new int[64];
        int _queued;
        int _next;

        public bool Enabled = true;
        public bool Muted;

        public HopClickAudio(Transform parent)
        {
            _root = new GameObject("hop-audio");
            _root.transform.SetParent(parent, false);

            var clip = BuildClick();
            for (int n = 0; n < Sources; n++)
            {
                var source = _root.AddComponent<AudioSource>();
                source.clip = clip;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.35f;
                _sources[n] = source;
            }
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
        }

        void Play(int depth)
        {
            int semitones = Mathf.Clamp(depth, 0, Vfx.HopPitchCapSemitones);
            var source = _sources[_next];
            _next = (_next + 1) % Sources;
            source.pitch = Mathf.Pow(2f, semitones / 12f);
            source.Play();
        }

        static AudioClip BuildClick()
        {
            var samples = new float[ClickSamples];
            for (int n = 0; n < ClickSamples; n++)
            {
                float t = n / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 90f);
                float body = Mathf.Sin(2f * Mathf.PI * 1180f * t);
                float snap = Mathf.Sin(2f * Mathf.PI * 3300f * t) * Mathf.Exp(-t * 400f) * 0.4f;
                samples[n] = (body + snap) * envelope;
            }
            var clip = AudioClip.Create("hop-click", ClickSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
