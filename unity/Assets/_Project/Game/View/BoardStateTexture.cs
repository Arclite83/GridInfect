using GridInfect.Core;
using UnityEngine;
using Grid = GridInfect.Core.Grid;
using Vfx = GridInfect.Game.PresentationConfig.Infection;

namespace GridInfect.Game
{
    // The one piece of state the board shader reads: COLS x ROWS,
    // point-filtered, RGBAFloat, one texel per cell.
    //
    //   R  cell value, the game's own wire vocabulary (Cell.*). The spec's
    //      four-state table (empty / infected / immune / conflict) does not
    //      cover the repel switch or the reset trap, and a lossy remap would
    //      need a second enum kept in sync with Rules; the wire value is a
    //      superset, so it goes across unchanged.
    //   G  transition start time on the board clock
    //   B  entry direction packed (dr + 1) * 3 + (dc + 1) — 4 is the seed
    //   A  transition kind (Kind.*), the spec's reserved channel
    //
    // Writes are one Color per changed cell plus one Apply per frame. Nothing
    // here allocates or rebuilds geometry, and nothing reads back.
    public sealed class BoardStateTexture
    {
        public static class Kind
        {
            public const float None = 0f;
            public const float Infecting = 1f;
            public const float Receding = 2f;
            public const float Conflict = 3f;
        }

        public const int SeedDir = 4;   // (dr, dc) = (0, 0)

        public readonly Texture2D Texture;

        readonly Color[] _pixels = new Color[Grid.Cells];
        bool _dirty;

        public BoardStateTexture()
        {
            Texture = new Texture2D(Grid.Width, Grid.Height, TextureFormat.RGBAFloat, false, true)
            {
                name = "board-state",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        public void Dispose()
        {
            if (Texture != null) Object.Destroy(Texture);
        }

        public static int PackDir(int dr, int dc) => (dr + 1) * 3 + (dc + 1);

        // Row 0 is the top row on screen; the shader indexes the same way.
        static int Index(int i, int j) => i * Grid.Width + j;

        public void Set(int i, int j, byte value, float startTime, int packedDir, float kind)
        {
            _pixels[Index(i, j)] = new Color(value, startTime, packedDir, kind);
            _dirty = true;
        }

        // A cell whose state simply *is* what it is: level load, undo resync,
        // full reset. Everything reads as settled on the very first frame.
        public void SetSettled(int i, int j, byte value)
        {
            float kind = value == Cell.Infected ? Kind.Infecting : Kind.None;
            Set(i, j, value, Vfx.SettledLongAgo, SeedDir, kind);
        }

        public float StartTimeAt(int i, int j) => _pixels[Index(i, j)].g;

        public byte ValueAt(int i, int j) => (byte)Mathf.RoundToInt(_pixels[Index(i, j)].r);

        public int PackedDirAt(int i, int j) => Mathf.RoundToInt(_pixels[Index(i, j)].b);

        public void Fill(LevelSession session)
        {
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    SetSettled(i, j, session.Board[Grid.Loc(i, j)]);
                }
            }
        }

        public void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            Texture.SetPixels(_pixels);
            Texture.Apply(false);
        }
    }
}
