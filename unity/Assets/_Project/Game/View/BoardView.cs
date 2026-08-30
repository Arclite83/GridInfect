using GridInfect.Core;
using UnityEngine;
// UnityEngine also declares a Grid component; ours wins explicitly.
using Grid = GridInfect.Core.Grid;

namespace GridInfect.Game
{
    public sealed class BoardView
    {
        public readonly float CellSize;
        public readonly float Pitch;

        readonly GameObject _root;
        readonly GameObject[] _cells = new GameObject[Grid.Cells];
        readonly LevelSession _session;

        public BoardView(Transform parent, LevelSession session)
        {
            _session = session;
            _root = new GameObject("board");
            _root.transform.SetParent(parent, false);

            CellSize = UnityEngine.Screen.height * PresentationConfig.CellHeightPct;
            Pitch = CellSize * PresentationConfig.CellPitch;

            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    int loc = Grid.Loc(i, j);
                    var cell = new GameObject($"cell:{i},{j}");
                    cell.transform.SetParent(_root.transform, false);
                    Vector2 center = CellCenter(i, j);
                    cell.transform.localPosition = new Vector3(center.x, center.y, 0f);
                    _cells[loc] = cell;
                    Paint(i, j, session.Board[loc]);
                }
            }

            session.CellChanged += Paint;
        }

        public void Dispose()
        {
            _session.CellChanged -= Paint;
            if (_root != null) Object.Destroy(_root);
        }

        public Vector2 CellCenter(int i, int j)
        {
            float x = (j - Grid.Width / 2) * Pitch;
            float y = UnityEngine.Screen.height * (PresentationConfig.BoardTopPct - 0.5f) - i * Pitch;
            return new Vector2(x, y);
        }

        public (int i, int j) CellAt(Vector2 world)
        {
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    Vector2 c = CellCenter(i, j);
                    if (Mathf.Abs(world.x - c.x) <= Pitch / 2f && Mathf.Abs(world.y - c.y) <= Pitch / 2f)
                    {
                        return (i, j);
                    }
                }
            }
            return (-1, -1);
        }

        void Paint(int i, int j, byte value)
        {
            var cell = _cells[Grid.Loc(i, j)];
            if (cell == null) return;
            for (int n = cell.transform.childCount - 1; n >= 0; n--)
            {
                Object.Destroy(cell.transform.GetChild(n).gameObject);
            }
            if (value == Cell.Void) return;

            Color bg = value switch
            {
                Cell.Active => BoardTheme.CellActive,
                Cell.Wall => BoardTheme.CellWall,
                Cell.RepelSwitch => BoardTheme.CellSwitch,
                Cell.Infected => BoardTheme.CellInfected,
                Cell.ResetTrap => BoardTheme.CellTrap,
                _ => BoardTheme.CellActive, // 99 is never visible between moves
            };
            Ui.MakeRect("bg", cell.transform, new Vector2(CellSize, CellSize), bg, 0);

            // Shape glyphs so no state is color-only (R-1001).
            switch (value)
            {
                case Cell.Wall:
                    Ui.MakeRect("glyph", cell.transform, new Vector2(CellSize * 0.5f, CellSize * 0.5f), BoardTheme.GlyphDark, 1);
                    break;
                case Cell.RepelSwitch:
                {
                    var diamond = Ui.MakeRect("glyph", cell.transform,
                        new Vector2(CellSize * 0.38f, CellSize * 0.38f), BoardTheme.GlyphLight, 1);
                    diamond.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
                    break;
                }
                case Cell.ResetTrap:
                {
                    var barA = Ui.MakeRect("glyphA", cell.transform,
                        new Vector2(CellSize * 0.55f, CellSize * 0.12f), BoardTheme.GlyphLight, 1);
                    barA.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
                    var barB = Ui.MakeRect("glyphB", cell.transform,
                        new Vector2(CellSize * 0.55f, CellSize * 0.12f), BoardTheme.GlyphLight, 1);
                    barB.transform.localEulerAngles = new Vector3(0f, 0f, -45f);
                    break;
                }
                case Cell.Infected:
                    Ui.MakeRect("glyph", cell.transform, new Vector2(CellSize * 0.18f, CellSize * 0.18f), BoardTheme.GlyphDark, 1);
                    break;
            }
        }
    }
}
