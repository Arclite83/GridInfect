using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    public sealed class PieceView
    {
        public readonly int Index;
        public readonly GameObject Root;
        public Vector2 TraySlot;

        readonly float _size;
        GameObject _lockGlyph;

        public PieceView(Transform parent, int index, Tile tile, float cellSize, Vector2 traySlot)
            : this(parent, index, PieceSpec.FromTile(tile), cellSize, traySlot)
        {
        }

        // Drawn from the spec (RULES_V2 §1): an arm per direction, diagonal
        // arms turned 45°, a short arm drawn short with its reach as a
        // notch count, the area as a wide frame — each a shape, never a
        // colour alone (R-1001).
        public PieceView(Transform parent, int index, PieceSpec spec, float cellSize, Vector2 traySlot)
        {
            Index = index;
            TraySlot = traySlot;
            _size = cellSize;

            Root = new GameObject($"piece:{index}:{spec.Encode()}");
            Root.transform.SetParent(parent, false);
            Root.transform.localPosition = new Vector3(traySlot.x, traySlot.y, 0f);

            if (spec.Area)
            {
                Ui.MakeRect("area", Root.transform, new Vector2(cellSize * 0.9f, cellSize * 0.9f), BoardTheme.PieceArm, 4);
                Ui.MakeRect("areaHole", Root.transform, new Vector2(cellSize * 0.74f, cellSize * 0.74f), BoardTheme.Background, 5);
            }
            Ui.MakeRect("body", Root.transform, new Vector2(cellSize * 0.56f, cellSize * 0.56f), BoardTheme.PieceBody, 6);
            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!spec.Has(dir)) continue;
                int reach = spec.ReachOf(dir);
                float len = cellSize * (reach == 0 ? 0.44f : reach == 1 ? 0.22f : 0.33f);
                float thick = cellSize * 0.2f;
                bool diagonal = TileArms.IsDiagonal(dir);
                bool horizontal = dir == Dir.L || dir == Dir.R;
                var arm = Ui.MakeRect("arm:" + dir, Root.transform,
                    horizontal || diagonal ? new Vector2(len, thick) : new Vector2(thick, len),
                    BoardTheme.PieceArm, 5);
                float offset = cellSize * (reach == 0 ? 0.28f : reach == 1 ? 0.2f : 0.24f);
                float dx = TileArms.Dj(dir), dy = -TileArms.Di(dir);
                if (diagonal)
                {
                    // The arm's long axis runs along the diagonal.
                    arm.transform.localEulerAngles = new Vector3(0f, 0f, dx * dy > 0 ? 45f : -45f);
                    offset *= 0.9f;
                }
                arm.transform.localPosition = new Vector3(dx * offset, dy * offset, 0f);
                for (int n = 0; n < reach; n++)
                {
                    // Reach notches: one small block per cell the arm reaches.
                    var notch = Ui.MakeRect("reach", Root.transform, new Vector2(thick * 0.35f, thick * 0.35f), BoardTheme.GlyphDark, 6);
                    float along = offset + (n + 1) * cellSize * 0.07f;
                    notch.transform.localPosition = new Vector3(dx * along, dy * along, 0f);
                }
            }
        }

        // The lock icon (R-1001: a shape, never colour alone): a small dark
        // block on the piece body, shown while the piece is locked.
        public void SetLocked(bool locked)
        {
            if (locked && _lockGlyph == null)
            {
                _lockGlyph = Ui.MakeRect("lock", Root.transform, new Vector2(_size * 0.22f, _size * 0.16f), BoardTheme.GlyphDark, 7);
                _lockGlyph.transform.localPosition = new Vector3(0f, -_size * 0.06f, 0f);
            }
            else if (!locked && _lockGlyph != null)
            {
                Object.Destroy(_lockGlyph);
                _lockGlyph = null;
            }
        }

        public bool HitTest(Vector2 world)
        {
            Vector3 p = Root.transform.localPosition;
            return Mathf.Abs(world.x - p.x) <= _size / 2f && Mathf.Abs(world.y - p.y) <= _size / 2f;
        }

        public void SetPos(Vector2 pos) => Root.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
    }
}
