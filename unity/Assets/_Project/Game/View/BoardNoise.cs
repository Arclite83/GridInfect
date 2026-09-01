using UnityEngine;

namespace GridInfect.Game
{
    // The blot pattern. Generated at exactly one texel per block over the whole
    // board, so a quantised board UV lands on its own texel and the field is
    // one continuous piece of noise with no restart at a cell boundary
    // (docs/infection-vfx-spec.md, acceptance criterion 4).
    //
    // Procedural for the same reason everything else here is: the project boots
    // from a fresh clone with no imported art. A board type that wants its own
    // blot swaps the texture; nothing else changes.
    public static class BoardNoise
    {
        // Octave lattices are deliberately coprime-ish with the cell grid, so
        // no octave can line up with a cell boundary and print a seam.
        static readonly int[] LatticeX = { 7, 17, 37 };
        static readonly int[] LatticeY = { 4, 9, 20 };
        static readonly float[] Weight = { 0.55f, 0.30f, 0.15f };

        static Texture2D _shared;
        static int _sharedKey;

        // The field is deterministic and board-sized, so every level shares one
        // texture instead of regenerating 17k texels on each load.
        public static Texture2D Shared(int width, int height, int seed)
        {
            int key = (width * 397 ^ height) * 397 ^ seed;
            if (_shared == null || _sharedKey != key)
            {
                if (_shared != null) Object.Destroy(_shared);
                _shared = Generate(width, height, seed);
                _sharedKey = key;
            }
            return _shared;
        }

        public static Texture2D Generate(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "board-blot",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[width * height];
            var values = new float[width * height];

            for (int octave = 0; octave < LatticeX.Length; octave++)
            {
                float[] lattice = Lattice(LatticeX[octave] + 1, LatticeY[octave] + 1, seed + octave * 7919);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float u = (x + 0.5f) / width * LatticeX[octave];
                        float v = (y + 0.5f) / height * LatticeY[octave];
                        values[y * width + x] += Weight[octave] *
                            Bilinear(lattice, LatticeX[octave] + 1, LatticeY[octave] + 1, u, v);
                    }
                }
            }

            // Rank-normalise, rather than rescale min..max. Summed value noise
            // is bell-shaped: nearly every block lands near the middle, so a
            // dissolve threshold sweeping 0..1 does almost nothing, then flips
            // the whole cell at once, and the bleed reads as static instead of
            // ink. A flat histogram makes the filled fraction track p directly,
            // which is also what makes the 0.12 edge band 12% of a cell rather
            // than most of it. Spatial structure is untouched — only the values
            // are re-spread.
            var order = new int[values.Length];
            for (int n = 0; n < order.Length; n++) order[n] = n;
            var keys = (float[])values.Clone();
            System.Array.Sort(keys, order);
            float last = order.Length > 1 ? order.Length - 1 : 1;
            for (int rank = 0; rank < order.Length; rank++)
            {
                float g = rank / last;
                pixels[order[rank]] = new Color(g, g, g, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        static float[] Lattice(int w, int h, int seed)
        {
            var rng = new System.Random(seed);
            var lattice = new float[w * h];
            for (int n = 0; n < lattice.Length; n++) lattice[n] = (float)rng.NextDouble();
            return lattice;
        }

        static float Bilinear(float[] lattice, int w, int h, float u, float v)
        {
            int x0 = Mathf.Clamp((int)u, 0, w - 2);
            int y0 = Mathf.Clamp((int)v, 0, h - 2);
            float fx = Smooth(Mathf.Clamp01(u - x0));
            float fy = Smooth(Mathf.Clamp01(v - y0));
            float a = Mathf.Lerp(lattice[y0 * w + x0], lattice[y0 * w + x0 + 1], fx);
            float b = Mathf.Lerp(lattice[(y0 + 1) * w + x0], lattice[(y0 + 1) * w + x0 + 1], fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
