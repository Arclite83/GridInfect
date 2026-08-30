using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    /// <summary>
    /// One tray piece: a body square plus a bar per arm — the piece IS its
    /// arms, readable without art. Hit area is the full cell-sized square
    /// (PORT_NOTES §4: hit tests are sprite rects, not visible shape).
    /// </summary>
    public sealed class PieceView
    {
        public readonly int Index;
        public readonly GameObject Root;
        public Vector2 TraySlot;

        readonly float _size;

        public PieceView(Transform parent, int index, Tile tile, float cellSize, Vector2 traySlot)
        {
            Index = index;
            TraySlot = traySlot;
            _size = cellSize;

            Root = new GameObject($"piece:{index}:{tile}");
            Root.transform.SetParent(parent, false);
            Root.transform.localPosition = new Vector3(traySlot.x, traySlot.y, 0f);

            Ui.MakeRect("body", Root.transform, new Vector2(cellSize * 0.56f, cellSize * 0.56f), BoardTheme.PieceBody, 6);
            for (int d = 0; d < 4; d++)
            {
                var dir = (Dir)d;
                if (!TileArms.Has(tile, dir)) continue;
                float len = cellSize * 0.44f, thick = cellSize * 0.2f;
                bool horizontal = dir == Dir.L || dir == Dir.R;
                var arm = Ui.MakeRect("arm:" + dir, Root.transform,
                    horizontal ? new Vector2(len, thick) : new Vector2(thick, len),
                    BoardTheme.PieceArm, 5);
                float offset = cellSize * 0.28f;
                arm.transform.localPosition = new Vector3(
                    TileArms.Dj(dir) * offset, -TileArms.Di(dir) * offset, 0f);
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
