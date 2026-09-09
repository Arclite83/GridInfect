using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The rules, on two pages: what is on the board, and what you drop on it.
    //
    // It is a legend, not a manual. Everything here is something a player
    // cannot work out by looking — the rest (resolution order, ring order,
    // relay chaining) is true and left out. Each swatch is drawn from the
    // board's own materials and marks (BoardTheme.Cell*, BugGlyph), so the
    // picture beside a line is the thing it describes.
    //
    // The chips read as one sentence each and all end the same way: repel,
    // trap and relay react when hit. Avoid (Cell.Forbidden) is the odd one —
    // not a chip but bare board, and a ray may not touch it at all. That is
    // a real mechanical difference, not a presentation one: a trap hit on
    // the winning move is free, because the win check runs first; an avoid
    // cell is never hit at all, because the drop that would hit it is
    // refused before anything resolves (RulesV2.CanPlace).
    //
    // It is not called CLEAR: this page uses "clears" as a verb for what a
    // repel and a trap do, and a tile of the same name fought it.
    public sealed class RulesScreen : AppScreen
    {
        enum Mark { Empty, Infected, Gap, Wall, Repel, Trap, Avoid, Relay, Bug, Diagonal, Blot, Locked }

        const int Pages = 2;
        const float PagerPct = 0.42f;

        static int _page;

        GameObject _body;
        TextMesh _pageLabel;
        float _pitch;

        // Swatch box and row pitch in the guide's reference px. The swatch
        // follows the pitch down once the band tightens past it, or the rows
        // would fit and their pictures would still overlap.
        float Swatch => Mathf.Min(S.Px(38f), _pitch * 0.72f);
        static float RowPitch => S.Px(54f);
        static float TextX => -L.ContentWidth / 2f + S.Px(100f);

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "HOW TO PLAY", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            float pagerY = -h * PagerPct;
            var pagerSize = new Vector2(L.ShortEdgeUnit * 0.20f, L.BarHeight);
            Buttons.Add(UiButton.Make(Root.transform, "◀",
                new Vector2(-L.ContentWidth / 2f + pagerSize.x / 2f, pagerY), pagerSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => Flip(-1)));
            Buttons.Add(UiButton.Make(Root.transform, "▶",
                new Vector2(L.ContentWidth / 2f - pagerSize.x / 2f, pagerY), pagerSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => Flip(1)));
            _pageLabel = Ui.MakeText("page", Root.transform, "", L.BodyText, BoardTheme.Text, 2);
            Ui.SetPos(_pageLabel.gameObject, 0f, pagerY);

            BuildPage();
        }

        void Flip(int delta)
        {
            int page = Mathf.Clamp(_page + delta, 0, Pages - 1);
            if (page == _page) return;
            _page = page;
            BuildPage();
        }

        void BuildPage()
        {
            if (_body != null) Object.Destroy(_body);
            _body = new GameObject("page");
            _body.transform.SetParent(Root.transform, false);
            _pageLabel.text = $"{_page + 1}/{Pages}";

            if (_page == 0) BuildBoardPage();
            else BuildBugPage();
        }

        // ---- page 1: what is on the board ----

        const int BoardRows = 7;

        void BuildBoardPage()
        {
            // Everything on this page is measured inside one band, from under
            // the title down to the top of the pager. The rows used to flow
            // down from the title in width-scaled pitches while the note was
            // pinned to the bottom of the screen: two different units, so on
            // any screen shorter than the guide's 390x844 they walked into
            // each other. Now the diagram takes a share, the note takes a
            // share, and the rows divide what is left — a short screen
            // tightens instead of overlapping.
            float top = L.TopBarY - S.Px(30f);
            float bottom = -UnityEngine.Screen.height * PagerPct + L.BarHeight;

            Line("goal", "Infect every cell on the board to win.", 0f, top, L.BodyText);
            top -= S.Px(26f);

            top = Diagram(top, (top - bottom) * 0.30f);

            float note = S.Px(30f);
            _pitch = Mathf.Min(RowPitch, (top - bottom - note) / BoardRows);
            float y = top - _pitch / 2f;

            Row(new[] { Mark.Empty, Mark.Infected }, "", "Infect it.", ref y);
            Row(new[] { Mark.Gap }, "GAP", "Not a cell: rays cross it.", ref y);
            Row(new[] { Mark.Wall }, "WALL", "Blocks a ray.", ref y);
            Row(new[] { Mark.Repel }, "REPEL", "Clears back when hit.", ref y);
            Row(new[] { Mark.Trap }, "TRAP", "Clears the board when hit.", ref y);
            Row(new[] { Mark.Relay }, "RELAY", "Fires its own rays when hit.", ref y);
            Row(new[] { Mark.Avoid }, "AVOID", "Rays must not touch it.", ref y);

            // What the last row feels like in the hand, which is the part a
            // legend cannot show: the chips above it answer a ray, and this
            // one is never reached at all — the move is refused before
            // anything resolves (RulesV2.CanPlace).
            Line("note", "A drop that would hit one bounces back.", 0f, bottom + note / 2f, L.BodyText * 0.95f);
        }

        // A ray runs the width of the board and steps over a gap on the way:
        // the one rule that is not guessable from a still board, so it gets
        // the picture rather than a line of type.
        float Diagram(float top, float maxHeight)
        {
            // Shrinks with the band so the picture is the part that gives,
            // not the rows under it.
            float cell = Mathf.Clamp((maxHeight - S.Px(30f)) / 3f - S.Px(4f), S.Px(15f), S.Px(30f));
            float pitch = cell + S.Px(4f);
            float y = top - cell / 2f;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    bool lit = row == 1 && col != 2;
                    var mark = row == 1 && col == 2 ? Mark.Gap : lit ? Mark.Infected : Mark.Empty;
                    Draw(mark, (col - 1.5f) * pitch, y - row * pitch, cell);
                }
            }
            // The bug that did it, over the middle of the lit row.
            var glyph = Ui.MakeSprite("bug", _body.transform,
                BugGlyph.Piece(Core.PieceSpec.Parse("LR"), BoardPalette.Default,
                    Mathf.RoundToInt(cell * 0.82f)), 14);
            glyph.transform.localPosition = new Vector3(-0.5f * pitch, y - pitch, 0f);

            float bottom = y - 2f * pitch - cell / 2f;
            Line("diagram", "A ray runs to the edge, over gaps.", 0f, bottom - S.Px(16f), L.BodyText * 0.95f);
            return bottom - S.Px(30f);
        }

        // ---- page 2: what you drop on it ----

        void BuildBugPage()
        {
            float y = L.TopBarY - S.Px(34f);
            Line("intro", "Drag one onto any cell.", 0f, y, L.BodyText);
            Line("intro2", "Its rays do the rest.", 0f, y - S.Px(16f), L.BodyText);

            // Four rows and three lines, centred in the band: hung off the
            // title like page 1 they would leave half a screen of nothing.
            _pitch = RowPitch;
            y = S.Px(96f);
            Row(new[] { Mark.Bug }, "RAYS", "Each ray infects to the edge.", ref y);
            Row(new[] { Mark.Diagonal }, "DIAGONAL", "Same, corner to corner.", ref y);
            Row(new[] { Mark.Blot }, "BLOT", "Takes the eight around it.", ref y);
            Row(new[] { Mark.Locked }, "LOCKED", "A hint placed it. It won't lift.", ref y);

            y -= S.Px(12f);
            Line("f1", "Pick a bug up any time. It's free.", 0f, y, L.BodyText * 0.95f);
            Line("f2", "Nothing stops a blot. Not a wall.", 0f, y - S.Px(17f), L.BodyText * 0.95f);
            Line("f3", "Win on a trap and it still counts.", 0f, y - S.Px(34f), L.BodyText * 0.95f);
        }

        // ---- rows ----

        void Row(Mark[] marks, string name, string line, ref float y)
        {
            float x = -L.ContentWidth / 2f + S.Px(8f) + Swatch / 2f;
            foreach (Mark mark in marks)
            {
                Draw(mark, x, y, Swatch);
                x += Swatch + S.Px(6f);
            }

            // A row with a name stacks it over the line; the empty/infected
            // pair has only the line, so it sits on the swatches' centre.
            if (name.Length > 0)
            {
                Line($"name:{name}", name, TextX, y + S.Px(9f), L.LabelText * 0.9f, TextAnchor.MiddleLeft);
                Line($"line:{name}", line, TextX, y - S.Px(9f), L.BodyText * 0.95f, TextAnchor.MiddleLeft);
            }
            else
            {
                Line($"line:{line}", line, TextX, y, L.LabelText * 0.9f, TextAnchor.MiddleLeft);
            }
            y -= _pitch;
        }

        void Line(string name, string text, float x, float y, float size,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var mesh = Ui.MakeText(name, _body.transform, text, size, BoardTheme.Text, 12, anchor: anchor);
            Ui.SetPos(mesh.gameObject, x, y);
        }

        // One swatch: the board's material, and the board's mark over it.
        void Draw(Mark mark, float x, float y, float size)
        {
            var p = BoardPalette.Default;
            var box = new Vector2(size, size);
            int glyphPx = Mathf.RoundToInt(size * 0.84f);

            switch (mark)
            {
                case Mark.Avoid:
                    // No tile: that is the point of it.
                    Sprite(BugGlyph.Avoid(p, glyphPx), x, y);
                    return;
                case Mark.Bug:
                case Mark.Diagonal:
                case Mark.Blot:
                case Mark.Locked:
                    Tile(BoardTheme.CellEmpty(), box, x, y);
                    Sprite(BugGlyph.Piece(Core.PieceSpec.Parse(
                        mark == Mark.Diagonal ? "ul+ur+dl+dr" : mark == Mark.Blot ? "A" : "LRUD"), p, glyphPx), x, y);
                    if (mark == Mark.Locked) Sprite(BugGlyph.Lock(p, glyphPx), x, y);
                    return;
                case Mark.Gap:
                    Tile(BoardTheme.CellGap(), box, x, y);
                    return;
                case Mark.Wall:
                    Tile(BoardTheme.CellWall(), box, x, y);
                    Sprite(BugGlyph.Blocker(p, glyphPx), x, y);
                    return;
                case Mark.Repel:
                    Tile(BoardTheme.CellChip(p.RepelSwitch, 0.6f), box, x, y);
                    Sprite(BugGlyph.Repel(p, glyphPx), x, y);
                    return;
                case Mark.Trap:
                    Tile(BoardTheme.CellChip(p.ResetTrap, 0.7f), box, x, y);
                    Sprite(BugGlyph.Trap(p, glyphPx), x, y);
                    return;
                case Mark.Relay:
                    Tile(BoardTheme.CellEmpty(), box, x, y);
                    Sprite(BugGlyph.Relay((byte)((1 << (int)Core.Dir.U) | (1 << (int)Core.Dir.R)), p, glyphPx), x, y);
                    return;
                case Mark.Infected:
                    Tile(BoardTheme.CellInfected(), box, x, y);
                    Sprite(BugGlyph.CellDot(p, true, glyphPx), x, y);
                    return;
                default:
                    Tile(BoardTheme.CellEmpty(), box, x, y);
                    Sprite(BugGlyph.CellDot(p, false, glyphPx), x, y);
                    return;
            }
        }

        void Tile(GlassStyle style, Vector2 box, float x, float y)
        {
            var go = Ui.MakeGlass("cell", _body.transform, box, style, 10);
            Ui.SetPos(go, x, y);
        }

        void Sprite(Sprite sprite, float x, float y)
        {
            var renderer = Ui.MakeSprite("mark", _body.transform, sprite, 12);
            renderer.transform.localPosition = new Vector3(x, y, 0f);
        }
    }
}
