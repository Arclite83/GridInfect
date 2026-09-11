using GridInfect.Core;
using TMPro;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Portrait: the same 32 levels a page, racked 4 across and 8 down instead
    // of 8 and 4, so the page count is unchanged and a tile is a thumb target
    // rather than a sliver. Paging moved off the right edge to a bar at the
    // bottom, where a thumb already is.
    //
    // Every level is open. The chain unlock is gone: a player who wants to
    // poke at level 90 may, and solving still advances on its own for one
    // who does not. Red is beaten.
    public sealed class ClassicSelectScreen : AppScreen
    {
        const int Columns = 4;
        const int Rows = 8;
        const int PerPage = Columns * Rows;
        const int Pages = ClassicLevels.Count / PerPage;

        const float PagerPct = 0.42f;   // paging bar, fraction of height below centre

        static int _page; // session-persistent, like the original Game singleton field

        GameObject _grid;
        TMP_Text _pageLabel;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, Str.LegacyTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);

            Buttons.Add(UiButton.Make(Root.transform, Str.NavMenu, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            float pagerY = -h * PagerPct;
            var pagerSize = new Vector2(L.ShortEdgeUnit * 0.20f, L.BarHeight);
            int arrow = UiButton.IconPx(pagerSize);
            Buttons.Add(UiButton.MakeIcon(Root.transform, "prev", BugGlyph.Prev(BoardPalette.Default, arrow),
                new Vector2(L.Lead(pagerSize.x / 2f), pagerY), pagerSize, () => Flip(-1)));
            Buttons.Add(UiButton.MakeIcon(Root.transform, "next", BugGlyph.Next(BoardPalette.Default, arrow),
                new Vector2(L.Trail(pagerSize.x / 2f), pagerY), pagerSize, () => Flip(1)));

            _pageLabel = Ui.MakeText("page", Root.transform, "", L.BodyText, BoardTheme.TextDim, 2);
            Ui.SetPos(_pageLabel.gameObject, 0f, pagerY);

            BuildGrid();
        }

        void Flip(int delta)
        {
            int page = Mathf.Clamp(_page + delta, 0, Pages - 1);
            if (page == _page) return;
            _page = page;
            BuildGrid();
        }

        void BuildGrid()
        {
            if (_grid != null)
            {
                Buttons.RemoveAll(b => b.Root != null && b.Root.transform.parent == _grid.transform);
                Object.Destroy(_grid);
            }
            _grid = new GameObject("grid");
            _grid.transform.SetParent(Root.transform, false);

            float h = UnityEngine.Screen.height;
            _pageLabel.text = Str.Fmt(Str.CommonPage, _page + 1, Pages);

            // The rack fills the band between the title and the pager. Tiles
            // are square, so the grid stays legible whichever way the numbers
            // fall out of the screen's aspect.
            float top = L.TopBarY - L.HeadingText;
            float bottom = -h * PagerPct + L.BarHeight;
            float pitchX = L.ContentWidth / Columns;
            float pitchY = (top - bottom) / Rows;
            float tile = Mathf.Min(pitchX, pitchY) * 0.86f;
            var size = new Vector2(tile, tile);
            float centreY = (top + bottom) / 2f;

            // Every level is open, so the rack carries one distinction and
            // it is legible across the page: a beaten level is infected.
            for (int n = 0; n < PerPage; n++)
            {
                int levelId = _page * PerPage + n;
                bool solved = Queries.IsClassicSolved(App.State.Profile, levelId);
                float x = (n % Columns - (Columns - 1) / 2f) * pitchX;
                float y = centreY + ((Rows - 1) / 2f - n / Columns) * pitchY;

                int captured = levelId;
                var button = UiButton.Make(_grid.transform, Str.Num(levelId + 1),
                    new Vector2(x, y), size,
                    solved ? BoardTheme.TileSolved() : BoardTheme.TileOpen(),
                    solved ? BoardTheme.TextOnAccent : BoardTheme.Text,
                    () => App.Screens.Show(new BoardScreen(), prepare: () =>
                        App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(captured)).Applied),
                    20, pads: false, padAlpha: 1f, mono: false);
                if (solved) Ui.MarkSolved(button.Root.transform, tile);
                Buttons.Add(button);
            }
        }
    }
}
