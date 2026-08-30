using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    /// <summary>
    /// Level select: 128 levels, 32 per page (8 × 4), 4 pages; the page
    /// persists for the app session (MODES §1.1). Locked levels are disabled
    /// and dimmed; level 1 is always playable.
    /// </summary>
    public sealed class ClassicSelectScreen : AppScreen
    {
        const int Columns = 8;
        const int Rows = 4;
        const int PerPage = Columns * Rows;
        const int Pages = ClassicLevels.Count / PerPage;

        static int _page; // session-persistent, like the original Game singleton field

        GameObject _grid;
        TextMesh _pageLabel;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            float w = UnityEngine.Screen.width;

            var title = Ui.MakeText("title", Root.transform, "CLASSIC", h * 0.05f, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, h * 0.44f);

            Buttons.Add(UiButton.Make(Root.transform, "MENU",
                new Vector2(-w * 0.42f, h * 0.44f), new Vector2(w * 0.12f, h * 0.07f),
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            Buttons.Add(UiButton.Make(Root.transform, "▲",
                new Vector2(w * 0.42f, h * 0.30f), new Vector2(w * 0.08f, h * 0.10f),
                BoardTheme.ButtonBg, BoardTheme.Text, () => Flip(-1)));
            Buttons.Add(UiButton.Make(Root.transform, "▼",
                new Vector2(w * 0.42f, -h * 0.30f), new Vector2(w * 0.08f, h * 0.10f),
                BoardTheme.ButtonBg, BoardTheme.Text, () => Flip(1)));

            _pageLabel = Ui.MakeText("page", Root.transform, "", h * 0.035f, BoardTheme.TextDim, 2);
            Ui.SetPos(_pageLabel.gameObject, w * 0.42f, 0f);

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
            float w = UnityEngine.Screen.width;
            _pageLabel.text = $"{_page + 1}/{Pages}";

            var size = new Vector2(w * 0.075f, h * 0.13f);
            float pitchX = w * 0.088f;
            float pitchY = h * 0.17f;

            for (int n = 0; n < PerPage; n++)
            {
                int levelId = _page * PerPage + n;
                bool unlocked = Queries.IsUnlocked(App.State.Profile, levelId);
                float x = (n % Columns - (Columns - 1) / 2f) * pitchX;
                float y = h * 0.20f - (n / Columns) * pitchY;

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
                    // Padlock stand-in: a small dark block glyph.
                    var lockGlyph = Ui.MakeRect("lock", button.Root.transform,
                        new Vector2(0.28f, 0.20f), BoardTheme.GlyphDark, 22);
                    lockGlyph.transform.localPosition = new Vector3(0f, -0.30f, 0f);
                }
                Buttons.Add(button);
            }
        }
    }
}
