using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The world layer of level select: one full-width row per world with
    // its infection meter across the bottom of the row and the count on the
    // right, a page of six rows at a time.
    //
    // Every world is open. Progress is how far the red has spread: a meter
    // cut into each row, and the row itself infected once the world is
    // clear. Nothing is padlocked, so the page reads as a map of what is
    // left rather than a list of what is refused.
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

            var title = Ui.MakeText("title", Root.transform, Str.WorldsTitle, L.HeadingText, BoardTheme.Text, 2);
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
            _pageLabel.text = Str.Fmt(Str.CommonPage, _page + 1, Pages);

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            int first = _page * PerPage;
            int count = System.Math.Min(PerPage, Worlds.Count - first);
            for (int n = 0; n < count; n++)
            {
                World world = Worlds.All[first + n];
                int done = Queries.WorldLevelsSolved(profile, world.Id);
                bool clear = Queries.IsWorldSolved(profile, world.Id);
                float y = L.StackRowY(n, PerPage, L.ButtonHeight, 0f);
                string captured = world.Id;
                var button = UiButton.Make(_list.transform, Str.Fmt(Str.WorldsRow, world.Index + 1, Str.WorldName(world.Id)),
                    new Vector2(0f, y), size,
                    clear ? BoardTheme.TileSolved() : BoardTheme.TileOpen(),
                    clear ? BoardTheme.TextOnAccent : BoardTheme.Text,
                    () => App.Screens.Show(new WorldLevelSelectScreen(captured)), 20,
                    pads: false, padAlpha: 1f, mono: false);
                Buttons.Add(button);

                // The meter takes the bottom of the row, so the type moves off
                // the row's centre line to sit above it.
                button.Label.transform.localPosition = new Vector3(0f, L.ButtonHeight * 0.1f, 0f);

                // Right-anchored: the counts are two widths (9/12, 10/12)
                // and a centred readout put them in two different places.
                var progress = Ui.MakeText($"progress:{world.Id}", button.Root.transform,
                    Str.Fmt(Str.WorldsProgress, done, world.Count), L.LabelText,
                    clear ? BoardTheme.TextOnAccent : BoardTheme.Accent, 22, anchor: L.Trailing);
                Ui.SetPos(progress.gameObject, L.Trail(L.Gap), L.ButtonHeight * 0.1f);

                Meter(button.Root.transform, size, Queries.WorldInfection(profile, world.Id));
            }
        }

        // The infection meter: a track cut across the bottom of the row with
        // the solved fraction filled in. A world with nothing done shows the
        // empty track, so the row still says how much there is to do.
        static void Meter(Transform parent, Vector2 row, float fraction)
        {
            float inset = L.Gap * 0.6f;
            float width = row.x - inset * 2f;
            float height = S.Px(5f);
            float y = -row.y / 2f + inset + height / 2f;

            var track = Ui.MakeGlass("meter", parent, new Vector2(width, height), BoardTheme.MeterTrack(), 21);
            Ui.SetPos(track, 0f, y);
            if (fraction <= 0f) return;

            float filled = Mathf.Max(height, width * Mathf.Clamp01(fraction));
            var fill = Ui.MakeGlass("meter:fill", parent, new Vector2(filled, height), BoardTheme.MeterFill(), 22);
            Ui.SetPos(fill, -width / 2f + filled / 2f, y);
        }
    }

    // The level layer for one world: a rack of square tiles like the Legacy
    // select, sized from the shared layout so it stays a thumb target. Open
    // throughout; a beaten level is infected.
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

            var title = Ui.MakeText("title", Root.transform, Str.WorldName(world.Id), L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavWorlds, L.BackPos, L.BackSize,
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
                bool solved = Queries.IsWorldLevelSolved(profile, _worldId, n);
                float x = (n % Columns - (Columns - 1) / 2f) * pitchX;
                float y = centreY + ((rows - 1) / 2f - n / Columns) * pitchY;
                int captured = n;
                var button = UiButton.Make(Root.transform, Str.Num(n + 1), new Vector2(x, y), size,
                    solved ? BoardTheme.TileSolved() : BoardTheme.TileOpen(),
                    solved ? BoardTheme.TextOnAccent : BoardTheme.Text,
                    () => App.Screens.Show(new BoardScreen(), prepare: () =>
                        App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(_worldId, captured)).Applied),
                    20, pads: false, padAlpha: 1f, mono: false);
                if (solved) Ui.MarkSolved(button.Root.transform, tile);
                Buttons.Add(button);
            }
        }
    }
}
