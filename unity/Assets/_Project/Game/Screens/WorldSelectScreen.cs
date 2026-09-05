using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // The world layer of level select: one full-width row per world with
    // its progress on the right, a page of six rows at a time.
    public sealed class WorldSelectScreen : AppScreen
    {
        const int PerPage = 6;
        const float PagerPct = 0.42f;

        static int _page;

        GameObject _list;
        TextMesh _pageLabel;

        int Pages => (Worlds.Count + PerPage - 1) / PerPage;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "WORLDS", L.HeadingText, BoardTheme.Text, 2);
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

            BuildList();
        }

        void Flip(int delta)
        {
            int page = Mathf.Clamp(_page + delta, 0, Pages - 1);
            if (page == _page) return;
            _page = page;
            BuildList();
        }

        void BuildList()
        {
            if (_list != null)
            {
                Buttons.RemoveAll(b => b.Root != null && b.Root.transform.parent == _list.transform);
                Object.Destroy(_list);
            }
            _list = new GameObject("worlds");
            _list.transform.SetParent(Root.transform, false);
            _pageLabel.text = $"{_page + 1}/{Pages}";

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            int first = _page * PerPage;
            int count = System.Math.Min(PerPage, Worlds.Count - first);
            for (int n = 0; n < count; n++)
            {
                World world = Worlds.All[first + n];
                bool unlocked = Queries.IsWorldUnlocked(profile, world.Id);
                float y = L.StackRowY(n, PerPage, L.ButtonHeight, 0f);
                string captured = world.Id;
                var button = UiButton.Make(_list.transform, $"{world.Index + 1}  {world.Name.ToUpperInvariant()}",
                    new Vector2(0f, y), size,
                    unlocked ? BoardTheme.ButtonBg : BoardTheme.ButtonBgDisabled,
                    unlocked ? BoardTheme.Text : BoardTheme.TextDim,
                    () => App.Screens.Show(new WorldLevelSelectScreen(captured)));
                button.Enabled = unlocked;
                Buttons.Add(button);

                int done = Queries.IsWorldFinished(profile, world.Id) ? world.Count
                    : System.Math.Max(0, Queries.WorldLevelsUnlocked(profile, world.Id) - 1);
                var progress = Ui.MakeText($"progress:{world.Id}", _list.transform,
                    unlocked ? $"{done}/{world.Count}" : "", L.LabelText, BoardTheme.Accent, 2);
                Ui.SetPos(progress.gameObject, L.ContentWidth / 2f - L.Gap * 2.5f, y);
            }
        }
    }

    // The level layer for one world: a rack of square tiles like the Legacy
    // select, sized from the shared layout so it stays a thumb target.
    public sealed class WorldLevelSelectScreen : AppScreen
    {
        const int Columns = 5;
        const float PagerPct = 0.42f;

        readonly string _worldId;

        public WorldLevelSelectScreen(string worldId)
        {
            _worldId = worldId;
        }

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            World world = Worlds.Get(_worldId);

            var title = Ui.MakeText("title", Root.transform, world.Name.ToUpperInvariant(), L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "WORLDS", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new WorldSelectScreen())));

            int rows = (world.Count + Columns - 1) / Columns;
            float top = L.TopBarY - L.HeadingText;
            float bottom = -h * PagerPct + L.BarHeight;
            float pitchX = L.ContentWidth / Columns;
            float pitchY = (top - bottom) / rows;
            float tile = Mathf.Min(pitchX, pitchY) * 0.86f;
            var size = new Vector2(tile, tile);
            float centreY = (top + bottom) / 2f;

            var profile = App.State.Profile;
            for (int n = 0; n < world.Count; n++)
            {
                bool unlocked = Queries.IsWorldLevelUnlocked(profile, _worldId, n);
                float x = (n % Columns - (Columns - 1) / 2f) * pitchX;
                float y = centreY + ((rows - 1) / 2f - n / Columns) * pitchY;
                int captured = n;
                var button = UiButton.Make(Root.transform, (n + 1).ToString(), new Vector2(x, y), size,
                    unlocked ? BoardTheme.ButtonBg : BoardTheme.ButtonBgDisabled,
                    unlocked ? BoardTheme.Text : BoardTheme.TextDim,
                    () => App.Screens.Show(new BoardScreen(), prepare: () =>
                        App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(_worldId, captured)).Applied));
                button.Enabled = unlocked;
                if (!unlocked)
                {
                    var lockGlyph = Ui.MakeSprite("lock", button.Root.transform,
                        BugGlyph.Lock(BoardPalette.Default, Mathf.RoundToInt(tile * 0.9f)), 22);
                    lockGlyph.transform.localPosition = new Vector3(0f, -tile * 0.12f, 0f);
                }
                Buttons.Add(button);
            }
        }
    }
}
