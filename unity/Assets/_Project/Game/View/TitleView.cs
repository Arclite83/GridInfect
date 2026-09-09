using System.Collections.Generic;
using UnityEngine;
using T = GridInfect.Game.PresentationConfig.Title;

namespace GridInfect.Game
{
    // The wordmark on the title screen (STYLE-GUIDE §12): GRID dense, the
    // bug in the gap, INFECT lit, composited on the live substrate. Its
    // motion, once per launch: GRID is present at rest, the bug lands, and
    // INFECT lights one letter per hop left to right at the board's own hop
    // timing. Every visit after that shows the static, fully lit mark.
    public sealed class TitleView
    {
        public GameObject Root { get; private set; }
        public float Scale { get; private set; }          // device px per logo px
        public float WidthPx => TitleRaster.TotalWidth * Scale;
        public float HeightPx => LogoGlyphs.FontPx * Scale;

        static bool s_played;

        readonly List<SpriteRenderer> _lit = new List<SpriteRenderer>();
        SpriteRenderer _bug, _backing;
        Vector3 _bugScale;
        float _t;
        bool _animating;

        // `widthPx` is the room the mark may take; `maxTypePx` caps the type
        // so a wide screen does not make the title enormous.
        public static TitleView Make(Transform parent, float widthPx, float maxTypePx, Vector2 center, int sortingOrder)
        {
            var view = new TitleView();
            view.Scale = Mathf.Min(widthPx / TitleRaster.TotalWidth, maxTypePx / LogoGlyphs.FontPx);
            view.Root = new GameObject("title");
            view.Root.transform.SetParent(parent, false);
            view.Root.transform.localPosition = new Vector3(center.x, center.y, 0f);
            view.Build(sortingOrder);
            return view;
        }

        // Logo-frame (x right, y down, origin at GRID's pen start on the mid
        // line) -> local px, centred on the mark.
        Vector3 At(float x, float y) => new Vector3((x - TitleRaster.TotalWidth / 2f) * Scale, -y * Scale, 0f);

        SpriteRenderer Place(string name, TitleRaster.Glyph glyph, int order)
        {
            var r = Ui.MakeSprite(name, Root.transform, glyph.Sprite, order);
            r.transform.localPosition = At(glyph.CenterX, glyph.CenterY);
            return r;
        }

        void Build(int order)
        {
            string grid = LogoGlyphs.Grid, infect = LogoGlyphs.Infect;
            for (int i = 0; i < grid.Length; i++)
            {
                Place($"grid:{grid[i]}", TitleRaster.Letter(grid, i, TitleRaster.Material.Dense, Scale), order);
            }
            // INFECT: the dense letter under the lit one. The lit sprite's
            // alpha is what the animation drives; at rest it is opaque and
            // the dense letter under it is never seen.
            for (int i = 0; i < infect.Length; i++)
            {
                Place($"infect:{infect[i]}", TitleRaster.Letter(infect, i, TitleRaster.Material.Dense, Scale), order);
                _lit.Add(Place($"infect:{infect[i]}:lit", TitleRaster.Letter(infect, i, TitleRaster.Material.Lit, Scale), order + 1));
            }
            _backing = Place("bug:glow", TitleRaster.BugBacking(Scale), order);
            _bug = Ui.MakeSprite("bug", Root.transform, TitleRaster.Bug(Scale), order + 2);
            _bug.transform.localPosition = At(TitleRaster.BugX, 0f);
            _bugScale = _bug.transform.localScale;

            if (s_played) return;
            s_played = true;
            _animating = true;
            _t = 0f;
            Apply();
        }

        public void Tick(float dt)
        {
            if (!_animating) return;
            _t += dt;
            if (Apply()) _animating = false;
        }

        // The timeline, from the screen's first frame. Returns true once
        // every letter is lit.
        bool Apply()
        {
            float hop = PresentationConfig.Infection.Hop;

            // The bug lands: from above its size, fading in, easing out.
            float land = Mathf.Clamp01((_t - T.BugLandAt) / T.BugLandDur);
            float ease = 1f - (1f - land) * (1f - land);
            float grow = Mathf.Lerp(T.BugLandScale, 1f, ease);
            if (_bug != null)
            {
                _bug.transform.localScale = _bugScale * grow;
                _bug.color = new Color(1f, 1f, 1f, ease);
            }
            if (_backing != null) _backing.color = new Color(1f, 1f, 1f, ease);

            // Then INFECT, one letter per hop, each taking two hops to fill.
            float start = T.BugLandAt + T.BugLandDur + hop;
            bool done = true;
            for (int i = 0; i < _lit.Count; i++)
            {
                float a = Mathf.Clamp01((_t - (start + hop * i)) / (hop * 2f));
                _lit[i].color = new Color(1f, 1f, 1f, a);
                if (a < 1f) done = false;
            }
            return done && land >= 1f;
        }
    }
}
