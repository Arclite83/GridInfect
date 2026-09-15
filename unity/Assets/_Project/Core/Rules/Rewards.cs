namespace GridInfect.Core
{
    // Where a free solve comes from (NEXT_PASS "Economy"). Three faucets,
    // each a pure function of progress so the adapter can ask before it
    // dispatches and the log replays the same answer: the daily streak's
    // ladder, a world's midpoint and its last level on first clear, and
    // every tenth Legacy level on first clear. Every one of them lands
    // through locks.grant, which stops at the wallet's cap; only a rewarded
    // ad goes past it.
    //
    // The rates were picked against the cap, not against generosity: a
    // grant that lands on a full wallet is a no-op, and a faucet fast
    // enough to keep the wallet pinned at ten makes the counter stop
    // meaning anything. Two per world (every eleven levels or so) keeps
    // most grants live at a stuck rate of one solve in ten levels.
    public static class Rewards
    {
        // locks.grant reasons. "rewarded" (GrantLocksAction.Rewarded) is the
        // ad and is not one of these.
        public const string Streak = "streak";
        public const string World = "world";
        public const string Legacy = "legacy";

        // The streak ladder: once, the first time a streak ever reaches 3
        // (so a new player sees the mechanism inside the first week), then
        // every 7th day of any streak. `bestBefore` is the profile's best
        // streak before this day's solve moved it.
        public const int StreakFirst = 3;
        public const int StreakEvery = 7;

        public static bool StreakGrant(int streak, int bestBefore) =>
            (streak == StreakFirst && bestBefore < StreakFirst)
            || (streak > 0 && streak % StreakEvery == 0);

        // Whether the one-off grant at 3 has been taken: the pad the streak
        // bar draws under its third segment goes with it.
        public static bool StreakFirstTaken(Profile profile) => profile.DailyStreakBest >= StreakFirst;

        // The streak as the bar shows it: its position in the current
        // seven, so day 9 lights two. Zero for no streak.
        public static int StreakCycle(int streak) => streak <= 0 ? 0 : (streak - 1) % StreakEvery + 1;

        // A world grants twice, on first clear: at its midpoint and at its
        // last level. Two per world of twenty-odd.
        public static bool WorldLevelGrant(World world, int index) =>
            world != null && world.Count >= 2 && (index == world.Count - 1 || index == world.Count / 2 - 1);

        // Legacy has no worlds: every tenth level, on first clear.
        public const int LegacyEvery = 10;

        public static bool LegacyGrant(int levelId) => levelId >= 0 && (levelId + 1) % LegacyEvery == 0;
    }
}
