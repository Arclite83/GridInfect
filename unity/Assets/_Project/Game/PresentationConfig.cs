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
