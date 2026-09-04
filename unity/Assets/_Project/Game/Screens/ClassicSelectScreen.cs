using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Portrait: the same 32 levels a page, racked 4 across and 8 down instead
    // of 8 and 4, so the page count is unchanged and a tile is a thumb target
    // rather than a sliver. Paging moved off the right edge to a bar at the
    // bottom, where a thumb already is.
    public sealed class ClassicSelectScreen : AppScreen
    {
        const int Columns = 4;
        const int Rows = 8;
        const int PerPage = Columns * Rows;
        const int Pages = ClassicLevels.Count / PerPage;

        const float PagerPct = 0.42f;   // paging bar, fraction of height below centre

        static int _page; // session-persistent, like the original Game singleton field

        GameObject _grid;
        TextMesh _pageLabel;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "LEGACY", L.HeadingText, BoardTheme.Text, 2);
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
            _pageLabel.text = $"{_page + 1}/{Pages}";

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

            for (int n = 0; n < PerPage; n++)
            {
                int levelId = _page * PerPage + n;
                bool unlocked = Queries.IsUnlocked(App.State.Profile, levelId);
                float x = (n % Columns - (Columns - 1) / 2f) * pitchX;
                float y = centreY + ((Rows - 1) / 2f - n / Columns) * pitchY;

                int captured = levelId;
                var button = UiButton.Make(_grid.transform, (levelId + 1).ToString(),
                    new Vector2(x, y), size,
                    unlocked ? BoardTheme.ButtonBg : BoardTheme.ButtonBgDisabled,
                    unlocked ? BoardTheme.Text : BoardTheme.TextDim,
                    () =>
                    {
                        App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(captured));
                        App.Screens.Show(new BoardScreen());
                    });
                button.Enabled = unlocked;
                if (!unlocked)
                {
                    // The padlock mark (R-1001), over the tile's lower half.
                    var lockGlyph = Ui.MakeSprite("lock", button.Root.transform,
                        BugGlyph.Lock(BoardPalette.Default, Mathf.RoundToInt(tile * 0.9f)), 22);
                    lockGlyph.transform.localPosition = new Vector3(0f, -tile * 0.12f, 0f);
                }
                Buttons.Add(button);
            }
        }
    }
}
