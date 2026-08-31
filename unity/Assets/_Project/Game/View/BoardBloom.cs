using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GridInfect.Game
{
    // URP Bloom and tonemapping, created from code like everything else here.
    //
    // The threshold is the whole point: it sits at 1, so only output the board
    // shader pushes into HDR blooms — the hot fill, the bleed edge band, the
    // active trace and the seed marker. Every LDR colour on screen (cooled
    // fill, cell border, immune hatch, UI text) is rejected, which is what
    // acceptance criterion 9 asks for. If the effect ever needs the threshold
    // lowered to read, the effect is wrong, not the threshold.
    //
    // Tonemapping is not decoration here. The hot fill is the palette colour
    // times 2.2, which without a curve hard-clips per channel: #00D9FF lands on
    // pure #00FFFF and the authored hue is gone exactly where the eye is
    // looking. Neutral rolls the highlights off instead, so the fill keeps its
    // colour and the bloom does the work of reading as bright.
    public static class BoardBloom
    {
        static GameObject _volume;

        public static void Ensure(Camera camera, BoardPalette palette)
        {
            if (camera != null)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }

            if (_volume != null) return;

            _volume = new GameObject("bloom");
            Object.DontDestroyOnLoad(_volume);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(palette.BloomThreshold);
            bloom.intensity.Override(palette.BloomIntensity);
            bloom.scatter.Override(palette.BloomScatter);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volume = _volume.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }
    }
}
