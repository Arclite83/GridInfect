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
        enum Mark { Empty, Infected, Gap, Wall, Repel, Trap, Avoid, Relay }

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

            var title = Ui.MakeText("title", Root.transform, Str.RulesTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavMenu, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            float pagerY = -h * PagerPct;
            var pagerSize = new Vector2(L.ShortEdgeUnit * 0.20f, L.BarHeight);
            int arrow = UiButton.IconPx(pagerSize);
            Buttons.Add(UiButton.MakeIcon(Root.transform, "prev", BugGlyph.Chevron(BoardPalette.Default, arrow, true),
                new Vector2(-L.ContentWidth / 2f + pagerSize.x / 2f, pagerY), pagerSize, () => Flip(-1)));
            Buttons.Add(UiButton.MakeIcon(Root.transform, "next", BugGlyph.Chevron(BoardPalette.Default, arrow, false),
                new Vector2(L.ContentWidth / 2f - pagerSize.x / 2f, pagerY), pagerSize, () => Flip(1)));
            _pageLabel = Ui.MakeText("page", Root.transform, "", L.BodyText, BoardTheme.Text, 2);
            Ui.SetPos(_pageLabel.gameObject, 0f, pagerY);

            BuildPage();
        }

        void Flip(int delta)
        {
            _page = (_page + delta + Pages) % Pages;
            BuildPage();
        }

        void BuildPage()
        {
            if (_body != null) Object.Destroy(_body);
            _body = new GameObject("page");
            _body.transform.SetParent(Root.transform, false);
            _pageLabel.text = Str.Fmt(Str.CommonPage, _page + 1, Pages);

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

            Line("goal", Str.RulesGoal, 0f, top, L.BodyText);
            top -= S.Px(26f);

            top = Diagram(top, (top - bottom) * 0.30f);

            float note = S.Px(48f);
            _pitch = Mathf.Min(RowPitch, (top - bottom - note) / BoardRows);
            float y = top - _pitch / 2f;

            Row(new[] { Mark.Empty, Mark.Infected }, "infect", null, Str.RulesCellInfect, ref y);
            Row(new[] { Mark.Gap }, "gap", Str.RulesCellGapName, Str.RulesCellGapLine, ref y);
            Row(new[] { Mark.Wall }, "wall", Str.RulesCellWallName, Str.RulesCellWallLine, ref y);
            Row(new[] { Mark.Repel }, "repel", Str.RulesCellRepelName, Str.RulesCellRepelLine, ref y);
            Row(new[] { Mark.Trap }, "trap", Str.RulesCellTrapName, Str.RulesCellTrapLine, ref y);
            Row(new[] { Mark.Relay }, "relay", Str.RulesCellRelayName, Str.RulesCellRelayLine, ref y);
            Row(new[] { Mark.Avoid }, "avoid", Str.RulesCellAvoidName, Str.RulesCellAvoidLine, ref y);

            // The two things a legend cannot show, both about the rows above.
            // The win check runs before either a repel or a trap fires
            // (RULES §4.1), so a placement that finishes the board is free of
            // both — the original shipped that as its level 26 tutorial. And
            // an avoid cell is never reached at all: the move is refused
            // before anything resolves (RulesV2.CanPlace).
            Line("note1", Str.RulesNote1,
                0f, bottom + note * 0.72f, L.BodyText * 0.95f);
            Line("note2", Str.RulesNote2,
                0f, bottom + note * 0.28f, L.BodyText * 0.95f);
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
            Line("diagram", Str.RulesDiagram, 0f, bottom - S.Px(16f), L.BodyText * 0.95f);
            return bottom - S.Px(30f);
        }

        // ---- page 2: what you drop on it ----

        void BuildBugPage()
        {
            float top = L.TopBarY - S.Px(30f);
            float bottom = -UnityEngine.Screen.height * PagerPct + L.BarHeight;

            Line("intro", Str.RulesIntro, 0f, top, L.BodyText);
            top -= S.Px(26f);

            // One little board per family, each showing the bug and what it
            // lights. A piece needs no name here: the picture is the whole of
            // what there is to say about it.
            float band = (top - bottom) / 3f;
            Spread(top, band, "LRUD", Str.RulesSpreadCardinal);
            Spread(top - band, band, "ul+ur+dl+dr", Str.RulesSpreadDiagonal);
            Spread(top - band * 2f, band, "A", Str.RulesSpreadArea);
        }

        const int SpreadRows = 3;
        const int SpreadCols = 5;

        // A 5 x 3 board with the bug in the middle and its spread lit, worked
        // out from the same PieceSpec the glyph is drawn from — so the
        // picture cannot drift from the piece, and the rays that leave the
        // little board are the ones that would run to the edge of a real one.
        void Spread(float slotTop, float band, string spec, string line)
        {
            var piece = Core.PieceSpec.Parse(spec);

            // The line sits under the board rather than beside it, so the
            // board is as wide as the content and not as wide as whatever the
            // type left over. Height is what bounds the cell now.
            // The gutter is reserved before the cell is sized, or the board
            // grows until the three of them touch and read as one picture.
            float caption = S.Px(30f);
            float gutter = S.Px(18f);
            float cell = Mathf.Clamp((band - caption - gutter) / SpreadRows - S.Px(4f), S.Px(14f), S.Px(46f));
            float pitch = cell + S.Px(4f);
            float grid = (SpreadRows - 1) * pitch + cell;
            float left = -(SpreadCols - 1) * pitch / 2f;

            // Board and line together, centred in this third of the band.
            float y = slotTop - (band - grid - caption) / 2f - cell / 2f;

            int ci = SpreadRows / 2, cj = SpreadCols / 2;
            for (int i = 0; i < SpreadRows; i++)
            {
                for (int j = 0; j < SpreadCols; j++)
                {
                    Draw(Lit(piece, i - ci, j - cj) ? Mark.Infected : Mark.Empty,
                        left + j * pitch, y - i * pitch, cell);
                }
            }
            Sprite(BugGlyph.Piece(piece, BoardPalette.Default, Mathf.RoundToInt(cell * 0.84f)),
                left + cj * pitch, y - ci * pitch);

            Line($"spread:{spec}", line, 0f, y - grid + cell / 2f - caption / 2f, L.BodyText * 0.95f);
        }

        // Whether the bug at the origin infects the cell `di` rows and `dj`
        // columns away: its own cell, the eight around it if it is a blot,
        // and every cell along each arm (TileArms.Di/Dj, the same steps the
        // rules walk).
        static bool Lit(Core.PieceSpec piece, int di, int dj)
        {
            if (di == 0 && dj == 0) return true;
            if (piece.Area && Mathf.Abs(di) <= 1 && Mathf.Abs(dj) <= 1) return true;
            for (int d = 0; d < 8; d++)
            {
                var dir = (Core.Dir)d;
                if (!piece.Has(dir)) continue;
                int si = Core.TileArms.Di(dir), sj = Core.TileArms.Dj(dir);
                for (int k = 1; k <= SpreadCols; k++)
                {
                    if (si * k == di && sj * k == dj) return true;
                }
            }
            return false;
        }

        // ---- rows ----

        void Row(Mark[] marks, string id, string name, string line, ref float y)
        {
            float x = -L.ContentWidth / 2f + S.Px(8f) + Swatch / 2f;
            foreach (Mark mark in marks)
            {
                Draw(mark, x, y, Swatch);
                x += Swatch + S.Px(6f);
            }

            // A row with a name stacks it over the line; the empty/infected
            // pair has only the line, so it sits on the swatches' centre.
            if (!string.IsNullOrEmpty(name))
            {
                Line($"name:{id}", name, TextX, y + S.Px(9f), L.LabelText * 0.9f, TextAnchor.MiddleLeft);
                Line($"line:{id}", line, TextX, y - S.Px(9f), L.BodyText * 0.95f, TextAnchor.MiddleLeft);
            }
            else
            {
                Line($"line:{id}", line, TextX, y, L.LabelText * 0.9f, TextAnchor.MiddleLeft);
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
