using TMPro;
using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // What a navigation puts up while it works (ScreenManager): one row of
    // the board, infecting left to right at the board's own hop, on the
    // scrim.
    //
    // It was the word LOADING on flat black, switched on and off on the
    // frame it was asked for. Flat black is the one surface in the game that
    // is not the game — everything else is a printed board under glass — and
    // a card that appears is a card the player reads as a stall. This one
    // fades up, runs the mechanic it is waiting for, and fades away: six
    // cells, each taking two hops to fill, then a beat at full and a clear,
    // over and over for as long as the work takes. Nothing here is a
    // progress bar, because nothing here knows the progress: a seed scan
    // finishes when it finishes. It says the game is alive, which is the
    // only honest thing it has to say.
    public sealed class LoadingCard
    {
        // The board's own width. A row of six is the shape the player is
        // waiting for, at two thirds the board's cell.
        const int Cells = 6;
        const float CellPx = 34f;      // reference px; the board's is 54
        const float CellGapPx = 4f;
        const float WellPadPx = 10f;
        const float CaptionGapPx = 18f;

        const float FadeIn = 0.18f;
        const float FadeOut = 0.12f;
        const float Hold = 0.16f;      // the full row, before it clears
        const float Clear = 0.18f;     // the row going out together

        // One cell lit over two hops, one hop apart — the title's timing and
        // the board's.
        static float Hop => PresentationConfig.Infection.Hop;
        static float Sweep => Hop * (Cells - 1) + Hop * 2f;
        static float Loop => Sweep + Hold + Clear;

        // A box of glass and the stack it was built from, so it can be
        // written again at another alpha (Glass.SetAlpha).
        struct Box
        {
            public GameObject Go;
            public GlassStyle Style;
        }

        readonly GameObject _root;
        readonly Box[] _plate;              // the well and the six dormant cells
        readonly Box[] _lit = new Box[Cells];
        readonly TMP_Text _caption;

        float _alpha;         // 0 gone, 1 up
        float _target;
        float _t;             // the sweep's clock, free-running while up

        // The scrim a navigation fades to. Not black: black is a hole in the
        // app, and the substrate is still down there. This is the board with
        // the lights off, and it follows the skin because ScreenManager reads
        // it fresh every frame it draws the fade.
        public static Color Scrim => Color.Lerp(Color.black, BoardPalette.Default.MaskLo, 0.22f);

        public LoadingCard(int sortingOrder)
        {
            _root = new GameObject("loading");
            Object.DontDestroyOnLoad(_root);

            float cell = S.Px(CellPx), gap = S.Px(CellGapPx), pad = S.Px(WellPadPx);
            float pitch = cell + gap;
            float width = Cells * cell + (Cells - 1) * gap;

            var palette = BoardPalette.Default;
            _plate = new Box[Cells + 1];
            _plate[0] = Make("well", new Vector2(width + pad * 2f, cell + pad * 2f),
                GlassStyle.Well(palette), sortingOrder, 0f);

            var dormant = BoardTheme.CellEmpty();
            var infected = BoardTheme.CellInfected();
            for (int n = 0; n < Cells; n++)
            {
                // The sweep runs from the leading side, like any progress.
                // The card is built once at boot, so a language change
                // mid-session keeps the old direction until the next launch.
                float x = PresentationConfig.Layout.ColumnX(n, Cells, pitch);
                _plate[n + 1] = Make($"cell:{n}", new Vector2(cell, cell), dormant, sortingOrder + 1, x);
                // The lit cell rides on top of the dormant one, exactly as a
                // board cell does: what the sweep drives is its alpha.
                _lit[n] = Make($"cell:{n}:lit", new Vector2(cell, cell), infected, sortingOrder + 2, x);
            }

            _caption = Ui.MakeText("caption", _root.transform, Str.LoadingCaption, S.Px(S.HudCaption), palette.Tip,
                sortingOrder + 3, mono: true);
            Ui.SetPos(_caption.gameObject, 0f, -(cell / 2f + pad + S.Px(CaptionGapPx)));

            Apply();
            _root.SetActive(false);
        }

        Box Make(string name, Vector2 box, GlassStyle style, int sortingOrder, float x)
        {
            var go = Ui.MakeGlass(name, _root.transform, box, style, sortingOrder);
            Ui.SetPos(go, x, 0f);
            return new Box { Go = go, Style = style };
        }

        public void Show()
        {
            if (_target < 1f) _t = 0f;   // a card coming up starts its sweep at the left
            _target = 1f;
            _root.SetActive(true);
        }

        public void Hide()
        {
            _target = 0f;
        }

        public void Tick(float dt)
        {
            if (!_root.activeSelf) return;

            _alpha = _target > _alpha
                ? Mathf.Min(_target, _alpha + dt / FadeIn)
                : Mathf.Max(_target, _alpha - dt / FadeOut);
            _t += dt;
            Apply();

            if (_alpha <= 0f && _target <= 0f) _root.SetActive(false);
        }

        // A skin change rebuilds every piece of glass on a screen, because
        // each one baked its colours when it was made. This card outlives
        // screens, so its stacks are replaced in place instead.
        public void Restyle()
        {
            var palette = BoardPalette.Default;
            _plate[0].Style = GlassStyle.Well(palette);
            var dormant = BoardTheme.CellEmpty();
            var infected = BoardTheme.CellInfected();
            for (int n = 0; n < Cells; n++)
            {
                _plate[n + 1].Style = dormant;
                _lit[n].Style = infected;
            }
            Apply();
        }

        void Apply()
        {
            foreach (var box in _plate) Glass.SetAlpha(box.Go, box.Style, _alpha);

            float phase = Loop <= 0f ? 0f : Mathf.Repeat(_t, Loop);
            float clearing = 1f - Mathf.Clamp01((phase - Sweep - Hold) / Clear);
            for (int n = 0; n < Cells; n++)
            {
                float lit = Mathf.Clamp01((phase - Hop * n) / (Hop * 2f));
                Glass.SetAlpha(_lit[n].Go, _lit[n].Style, lit * clearing * _alpha);
            }

            if (_caption != null) _caption.color = BoardPalette.Alpha(BoardPalette.Default.Tip, 0.75f * _alpha);
        }
    }
}
