namespace GridInfect.Game
{
    public static class PresentationConfig
    {
        // The resolution beat: consequences (win check / reset / repels) land
        // this long after a successful drop. Presentation only — input during
        // the beat fast-forwards resolution, it never cancels it (R-107).
        public const float ResolveDelay = 0.30f;

        public const float DropSnap = 0.10f;        // drop -> cell center
        public const float TrayReturn = 0.15f;      // illegal drop -> tray slot
        public const float SceneFade = 0.50f;       // every navigation
        public const float PopupSlide = 0.15f;      // COMPLETE popup / BEGIN dismiss
        public const float PageSlide = 0.20f;       // classic level-select paging

        // Touching a locked given: it leans this far off its cell and back,
        // so a piece that cannot be lifted still answers the finger.
        public const float LockedNudge = 0.06f;
        public const float LockedNudgePct = 0.12f;  // of a cell

        // Touch gating (see GameApp.Update). The block runs from the end of a
        // transition so a press made during the blackout cannot act on the
        // screen that replaced the one it was aimed at; the debounce is one
        // button activation per window, so a double-tap navigates once.
        public const float PostTransitionInputBlock = 0.15f;
        public const float ButtonDebounce = 0.25f;

        // The largest frame delta animation is allowed to see. A synchronous
        // level generation can stall the main thread for seconds; without a
        // clamp that whole stall lands on the next frame's tweens and fades.
        public const float MaxFrameDelta = 0.1f;

        // Board layout metrics (LevelMenuScene::init), made orientation-
        // agnostic: the board fits whichever axis binds, so it composes on a
        // phone held upright without anything running off an edge.
        public const float CellHeightPct = 0.11f;   // cap: cell = 11% of screen height
        public const float BoardWidthPct = 0.94f;   // the board spans at most 94% of the width
        public const float CellPitch = 1.05f;       // 5% gutters both axes
        public const float BoardCeilingPct = 0.855f; // board stays below the title and HUD
        public const float TrayBottomPct = 0.075f;  // tray pieces 7.5% from the bottom
        public const float TraySlotPitch = 1.1f * 1.1f;
        public const int TraySlots = 8;             // LevelDef.MaxPieces; real levels top out at 6

        // Anything square-ish — button boxes, glyphs, type — is sized off the
        // short edge, so a control keeps its proportions when the screen turns.
        // Positions stay fractions of the axis they belong to.
        public static float ShortEdge =>
            UnityEngine.Mathf.Min(UnityEngine.Screen.width, UnityEngine.Screen.height);

        // The chrome's shared metrics. Every screen measures from these rather
        // than inventing its own fractions, which is what let each of them
        // drift into a landscape-only shape in the first place.
        public static class Layout
        {
            static float H => UnityEngine.Screen.height;
            static float W => UnityEngine.Screen.width;

            public const float ContentWidthPct = 0.88f;   // full-width controls
            public const float TopBarPct = 0.44f;         // title row, from centre

            public static float ShortEdgeUnit => ShortEdge;
            public static float ContentWidth => W * ContentWidthPct;
            public static float TopBarY => H * TopBarPct;

            public static float ButtonHeight => ShortEdge * 0.11f;
            public static float BarHeight => ShortEdge * 0.075f;
            public static float Gap => ShortEdge * 0.035f;

            public static float TitleText => ShortEdge * 0.095f;
            public static float HeadingText => ShortEdge * 0.05f;
            public static float LabelText => ShortEdge * 0.04f;
            public static float BodyText => ShortEdge * 0.035f;

            // A back button lives in the top-left corner on every screen that
            // has one, sized so a thumb can reach it on the tallest phone.
            public static UnityEngine.Vector2 BackSize =>
                new UnityEngine.Vector2(ShortEdge * 0.20f, BarHeight);
            public static UnityEngine.Vector2 BackPos =>
                new UnityEngine.Vector2(-ContentWidth / 2f + ShortEdge * 0.10f, TopBarY);

            // n stacked rows of `rowHeight`, centred on `centreY`; row 0 on top.
            public static float StackRowY(int n, int count, float rowHeight, float centreY)
            {
                float pitch = rowHeight + Gap;
                float top = centreY + (count - 1) * pitch / 2f;
                return top - n * pitch;
            }
        }

        public const int TargetFrameRate = 60;      // R-1104

        // Infection VFX (docs/infection-vfx-spec.md "Locked parameters").
        // Blocks, hop, bias, glow hold and glow fade are fixed; trace and
        // bleed are the two remaining tunables. InfectionVfxSpecTests keeps
        // this table and the spec from drifting apart.
        public static class Infection
        {
            public const int Blocks = 16;           // blocks per cell
            public const float Hop = 0.040f;        // 40 ms between ray steps
            public const float Bias = 0.30f;        // noise -> entry-edge lean
            public const float GlowHold = 0.150f;   // 150 ms at full emission
            public const float GlowFade = 0.300f;   // 300 ms cooling to rest
            public const float TraceDur = 0.090f;   // 90 ms trace pulse
            public const float BleedDur = 0.260f;   // 260 ms bleed dissolve

            // Derived juice timings; each has an on/off switch on BoardView.
            public const float ArrivalPulseGain = 1.4f;
            public const float ArrivalPulseDur = 0.060f;
            public const float ConflictShakePx = 2f;
            public const float ConflictShakeDur = 0.120f;
            public const float ConflictFlashDur = 0.500f;
            public const float SparkLife = 0.200f;
            public const float TraceDimLevel = 0.30f;
            public const float GhostTrailDur = 0.200f;

            public const int HopPitchCapSemitones = 7;

            // Written into the state texture for anything that did not arrive
            // through a wave: far enough in the past that every curve reads as
            // fully settled on the first frame.
            public const float SettledLongAgo = -1000f;
        }
    }
}
