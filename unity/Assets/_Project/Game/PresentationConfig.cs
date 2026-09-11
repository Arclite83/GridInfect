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

        // The tutorial: the beat after a step's winning wave has landed
        // before the next step (or the last popup) takes the screen, and the
        // ghost of a bug on its cell — faint, breathing a little so it
        // reads as an instruction and not as a bug already placed.
        public const float TutorialHold = 0.6f;
        public const float TutorialMarkAlpha = 0.42f;
        public const float TutorialMarkPulse = 0.12f;
        public const float TutorialMarkRate = 3.2f;     // radians per second
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
        public const float ButtonDebounce = 0.25f;  // after a chip's handler returns, not before it runs
        public const float SolveCooldown = 0.6f;    // the hint: its handler runs the deducer and lands a piece

        // The largest frame delta animation is allowed to see. A synchronous
        // level generation can stall the main thread for seconds; without a
        // clamp that whole stall lands on the next frame's tweens and fades.
        public const float MaxFrameDelta = 0.1f;

        // Board layout metrics (LevelMenuScene::init), made orientation-
        // agnostic: the board fits whichever axis binds, so it composes on a
        // phone held upright without anything running off an edge.
        public const float CellHeightPct = 0.11f;   // cap: cell = 11% of screen height
        public const float BoardWidthPct = 0.97f;   // the board spans at most 97% of the width
        public const float CellPitch = 59f / 54f;   // style guide: 54 px cells on a 5 px gap
        // 54 px is the style guide's *design* cell, not a ceiling. Capping the
        // fit at it threw away the room a short, wide screen has going spare,
        // so the cell may grow to 72 px before CellHeightPct takes over.
        public const float CellMaxPx = 72f;
        public const float BoardCeilingPct = 0.88f;  // lattice top: 88 px board top + 14 px well pad on 844
        public const float TrayBottomPct = 0.057f;  // tray slots centred 48 px up on 844 (96 px tray)
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
            public const float TopBarPct = 0.42f;         // HUD row: chips bottom-aligned in the 56 px band

            public static float ShortEdgeUnit => ShortEdge;
            public static float ContentWidth => W * ContentWidthPct;
            // A popup plate: inset half a row height either side of the
            // full-width rows. At ContentWidth a message landing over the
            // menu shared both edges with the stack under it and read as
            // one more row of it rather than as something on top.
            public static float PlateWidth => ContentWidth - ButtonHeight;
            public static float TopBarY => H * TopBarPct;

            public static float ButtonHeight => ShortEdge * 0.11f;
            // The top-bar chips. 0.075 was a 29 px chip on the reference
            // screen: under the 44 px touch minimum and too short for its
            // own label. 0.085 is 33 px; the hit box (UiButton.HitBounds)
            // makes up the rest.
            public static float BarHeight => ShortEdge * 0.085f;
            // A square icon chip (gear, help, pager arrow): 39 px reference.
            public static float IconChip => ShortEdge * 0.10f;
            public static float Gap => ShortEdge * 0.035f;

            // A chip is bigger than its box, and nothing placed next to one
            // may be measured from the box alone.
            //
            // Sideways: it wears a copper pad either side, a ChipPadDot dot
            // centred ChipPadGap out from its edge, so it reaches
            // ChipPadGap + ChipPadDot/2 past itself. ChipPadSpan is the room
            // two neighbours must leave between their boxes — it clears both
            // pads and leaves a dot's width of dark between them, rather than
            // stacking the two dots on top of each other.
            //
            // Above and below: a plain chip stops at its box, but a lit one
            // (BoardTheme.Chip haloes an accent) throws ChipGlow of light
            // past it, which is what a line of type above it has to clear.
            public static float ChipPadSpan => Style.Px(2f * (Style.ChipPadGap + Style.ChipPadDot));
            public static float ChipGlow => Style.Px(Style.ChipGlow);

            // Centres this far apart for two chips of this width side by side.
            public static float ChipPitch(float chipWidth) => chipWidth + ChipPadSpan;

            public static float TitleText => ShortEdge * 0.095f;
            public static float HeadingText => ShortEdge * 0.05f;
            public static float LabelText => ShortEdge * 0.04f;
            // 0.037 is 14.4 px on the reference screen. A line longer than
            // its room shrinks to fit (Ui.FitText), so this is the size a
            // line that fits is drawn at, not a cap set by the longest one.
            public static float BodyText => ShortEdge * 0.037f;

            // Reading direction (docs/I18N.md): +1 left to right, -1 right
            // to left. Chrome positions itself from the leading and trailing
            // edges through Lead/Trail rather than from left and right, and
            // resolves its text anchors through Leading/Trailing, so a
            // right-to-left language mirrors the chrome by construction. The
            // board is not chrome: its coordinates never pass through this.
            public static float Dir => Str.IsRtl ? -1f : 1f;
            public static UnityEngine.TextAnchor Leading =>
                Str.IsRtl ? UnityEngine.TextAnchor.MiddleRight : UnityEngine.TextAnchor.MiddleLeft;
            public static UnityEngine.TextAnchor Trailing =>
                Str.IsRtl ? UnityEngine.TextAnchor.MiddleLeft : UnityEngine.TextAnchor.MiddleRight;

            // x of a point `inset` in from the content's leading / trailing edge.
            public static float Lead(float inset) => Dir * (-ContentWidth / 2f + inset);
            public static float Trail(float inset) => Dir * (ContentWidth / 2f - inset);

            // x of column `col` of `count` at `pitch`, centred, first column
            // on the leading side.
            public static float ColumnX(int col, int count, float pitch) =>
                Dir * (col - (count - 1) / 2f) * pitch;

            // A back button lives in the top leading corner on every screen
            // that has one, sized so a thumb can reach it on the tallest phone.
            public static UnityEngine.Vector2 BackSize =>
                new UnityEngine.Vector2(ShortEdge * 0.20f, BarHeight);
            public static UnityEngine.Vector2 BackPos =>
                new UnityEngine.Vector2(Lead(ShortEdge * 0.10f), TopBarY);

            // n stacked rows of `rowHeight`, centred on `centreY`; row 0 on top.
            public static float StackRowY(int n, int count, float rowHeight, float centreY)
            {
                float pitch = rowHeight + Gap;
                float top = centreY + (count - 1) * pitch / 2f;
                return top - n * pitch;
            }
        }

        public const int TargetFrameRate = 60;      // R-1104

        // The title screen's motion (STYLE-GUIDE §12), once per launch: the
        // bug lands this long after the menu appears, and INFECT lights one
        // letter per Infection.Hop after it.
        public static class Title
        {
            public const float BugLandAt = 0.25f;
            public const float BugLandDur = 0.12f;
            public const float BugLandScale = 1.6f;     // the bug arrives from this size
        }

        // The first-open offer's entrance. It used to be built already lit:
        // the dim was on and the plate was mid-slide on the menu's very
        // first frame, which is the one frame the player is reading the
        // title. Now the menu stands on its own, the screen goes dark under
        // the plate, and the plate lands the way the title bug does.
        public static class Offer
        {
            // Long enough for the title's own animation to finish (the bug
            // lands, then INFECT lights a letter per hop: ~0.69 s). The one
            // launch that shows this offer is the one launch that plays the
            // wordmark, and darkening the screen over it wastes both.
            public const float Wait = 0.75f;    // the menu, alone
            public const float Dim = 0.28f;     // the screen going dark behind it
            public const float Rise = 0.34f;    // the plate coming up
            public const float RiseAt = 0.12f;  // after the dim starts
            // The back in the ease's tail, as a fraction of the travel — and
            // the travel is most of the screen, so this is small on purpose:
            // 0.8 puts the overshoot around 10 px on a 390x844.
            public const float Overshoot = 0.8f;
            public const float Fall = 0.22f;    // SKIP: the plate dropping back out
        }

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
            public const float BoardTop = 88f;

            // §6 glyph sizes per context
            public const float GlyphOnTile = 44f;
            public const float TrayNextGlyph = 58f;
            public const float TrayQueuedGlyph = 40f;

            // §7 HUD
            public const float HudHeight = 56f;
            public const float HudInset = 22f;         // chips sit 22 px in from the edge
            public const float HudBottomPad = 10f;     // and 10 px up from the band's bottom
            public const float HudLevel = 26f;
            public const float HudCaption = 12f;       // the mode readout; was 11
            public const float ChipText = 12f;
            public const float ChipPadX = 14f;
            public const float ChipPadY = 8f;
            public const float ChipRadius = 7f;
            public const float ChipPadDot = 5f;        // the copper pad either side of a chip
            public const float ChipPadGap = 9f;        // pad centre from the chip edge
            public const float ChipGlow = 14f;         // a lit chip's halo, past its box on every side
            public const float BadgeText = 13f;
            public const float BadgeTop = 52f;
            public const float BadgePadX = 12f;
            public const float BadgePadY = 6f;

            // §8 tray
            public const float TrayHeight = 96f;
            public const float TraySlot = 74f;
            public const float TraySlotQueued = 54f;
            public const float TrayGap = 30f;
            public const float TraySlotRadius = 12f;
            public const float TrayCaption = 11f;      // was 10

            // §3 silkscreen: decorative, the one size allowed under the floor
            public const float Silkscreen = 9f;
            public const float PanelRadius = 12f;

            // §11 legibility floor. No informational type under 11 px on the
            // reference screen, and every pressable chip answers a 44 px
            // square around its centre whatever its drawn size — the chip
            // may stay chip-sized, the finger may not.
            public const float SmallText = 11f;
            public const float MinTouch = 44f;
        }

        // Infection VFX (docs/infection-vfx-spec.md "Locked parameters").
        // Blocks, hop, bias, glow hold and glow fade are fixed; trace and
        // bleed are the two remaining tunables. Keep this table and the spec in
        // step by hand.
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
