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
        public const float CellPitch = 59f / 54f;   // style guide: 54 px cells on a 5 px gap
        public const float BoardCeilingPct = 0.82f;  // lattice top: 138 px board top + 14 px well pad on 844
        public const float TrayBottomPct = 0.045f;  // tray slots centred 75 px up on 844 (150 px tray)
        public const float TraySlotPitch = (74f + 30f) / 74f;   // 74 px slots on a 30 px gap
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
            public const float TopBarPct = 0.42f;         // HUD row: chips bottom-aligned in the 96 px band

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

        // The visual style (grid-infect-style/STYLE-GUIDE.md, locked
        // 2026-09-04). Every token is a px value on the guide's 390 x 844
        // reference screen; Px() maps it onto this device off the short edge,
        // so the chrome keeps the guide's proportions at any resolution. The
        // board itself still fits whichever axis binds (Layout above).
        public static class Style
        {
            public const float RefWidth = 390f;
            public const float RefHeight = 844f;

            public static float Scale => ShortEdge / RefWidth;
            public static float Px(float refPx) => refPx * Scale;

            // §4 board well, §5 tiles
            public const float Cell = 54f;
            public const float Gap = 5f;
            public const float WellPad = 14f;
            public const float WellRadius = 12f;
            public const float TileRadius = 6f;
            public const float BoardTop = 138f;

            // §6 glyph sizes per context
            public const float GlyphOnTile = 44f;
            public const float TrayNextGlyph = 58f;
            public const float TrayQueuedGlyph = 40f;

            // §7 HUD
            public const float HudHeight = 96f;
            public const float HudInset = 22f;         // chips sit 22 px in from the edge
            public const float HudBottomPad = 12f;     // and 12 px up from the band's bottom
            public const float HudLevel = 26f;
            public const float HudCaption = 11f;
            public const float ChipText = 12f;
            public const float ChipPadX = 14f;
            public const float ChipPadY = 8f;
            public const float ChipRadius = 7f;
            public const float ChipPadDot = 5f;        // the copper pad either side of a chip
            public const float ChipPadGap = 9f;        // pad centre from the chip edge
            public const float BadgeText = 13f;
            public const float BadgeTop = 104f;
            public const float BadgePadX = 12f;
            public const float BadgePadY = 6f;

            // §8 tray
            public const float TrayHeight = 150f;
            public const float TraySlot = 74f;
            public const float TraySlotQueued = 54f;
            public const float TrayGap = 30f;
            public const float TraySlotRadius = 12f;
            public const float TrayCaption = 10f;

            // §3 silkscreen
            public const float Silkscreen = 9f;
            public const float PanelRadius = 12f;
        }

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
            public const float PlacementShakeDur = 0.080f;   // STYLE-GUIDE §9
            public const float PreviewFadeDur = 0.120f;      // the pending trace fading in under the finger
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
