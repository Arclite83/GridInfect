using GridInfect.Core;
using UnityEngine;
using Style = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // A piece is its bug glyph (STYLE-GUIDE §6), rasterised from the spec:
    // one lit lead per arm, a wire lead per diagonal, stop bars for a short
    // arm, the pulse arcs for an area bug. It draws at the tray size in its
    // slot and at the tile size once it sits on the board.
    public sealed class PieceView
    {
        public readonly int Index;
        public readonly GameObject Root;
        public Vector2 TraySlot;

        readonly PieceSpec _spec;
        readonly BoardPalette _palette;
        readonly SpriteRenderer _glyph;
        readonly float _slotSize;       // the hit box in the tray
        float _size;                    // the current glyph size in px
        GameObject _lockMark;

        public PieceView(Transform parent, int index, Tile tile, float slotSize, int trayGlyphPx, Vector2 traySlot)
            : this(parent, index, PieceSpec.FromTile(tile), slotSize, trayGlyphPx, traySlot)
        {
        }

        public PieceView(Transform parent, int index, PieceSpec spec, float slotSize, int trayGlyphPx, Vector2 traySlot)
        {
            Index = index;
            TraySlot = traySlot;
            _spec = spec;
            _palette = BoardPalette.Default;
            _slotSize = slotSize;

            Root = new GameObject($"piece:{index}:{spec.Encode()}");
            Root.transform.SetParent(parent, false);
            Root.transform.localPosition = new Vector3(traySlot.x, traySlot.y, 0f);

            _glyph = Ui.MakeSprite("glyph", Root.transform, null, 6);
            SetGlyphSize(trayGlyphPx);
        }

        // 58 px in the tray, 44 px on a 54 px tile: same source, two LODs.
        public void SetGlyphSize(int px)
        {
            px = Mathf.Max(8, px);
            if (_size == px) return;
            _size = px;
            _glyph.sprite = BugGlyph.Piece(_spec, _palette, px);
            if (_lockMark != null) _lockMark.GetComponent<SpriteRenderer>().sprite = BugGlyph.Lock(_palette, px);
        }

        // The lock mark (R-1001: a shape, never colour alone), over the core
        // while the piece is locked.
        public void SetLocked(bool locked)
        {
            if (locked && _lockMark == null)
            {
                _lockMark = Ui.MakeSprite("lock", Root.transform, BugGlyph.Lock(_palette, (int)_size), 7).gameObject;
            }
            else if (!locked && _lockMark != null)
            {
                Object.Destroy(_lockMark);
                _lockMark = null;
            }
        }

        // The hit box follows the glyph's LOD: a 44 px glyph on a 54 px tile
        // and a 58 px glyph in a 74 px slot are both the box at 54/44 of the
        // glyph, so a placed piece never claims its neighbour's tile.
        public bool HitTest(Vector2 world)
        {
            Vector3 p = Root.transform.localPosition;
            float half = Mathf.Min(_size * (Style.Cell / Style.GlyphOnTile), _slotSize) / 2f;
            return Mathf.Abs(world.x - p.x) <= half && Mathf.Abs(world.y - p.y) <= half;
        }

        public void SetPos(Vector2 pos) => Root.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
    }
}
