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

        // Board layout metrics (LevelMenuScene::init).
        public const float CellHeightPct = 0.11f;   // cell sprite = 11% of screen height
        public const float CellPitch = 1.05f;       // 5% gutters both axes
        public const float BoardTopPct = 0.80f;     // row 0 centered at 80% screen height
        public const float TrayBottomPct = 0.075f;  // tray pieces 7.5% from the bottom
        public const float TraySlotPitch = 1.1f * 1.1f;
        public const int TraySlots = 8;
        public const int TrayCenterSlot = 3;

        public const int TargetFrameRate = 60;      // R-1104
    }
}
