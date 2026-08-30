using System.Text;

namespace GridInfect.Core
{
    /// <summary>
    /// Read-only projections over game state. Mechanical, zero business
    /// rules beyond stating recorded policy; nothing here writes anything.
    /// </summary>
    public static class Queries
    {
        /// <summary>Level 1 (id 0) is always playable; everything else needs its unlock flag.</summary>
        public static bool IsUnlocked(Profile profile, int levelId) =>
            levelId == 0 || profile.Unlocked.Contains(levelId);

        /// <summary>The classic id a solve unlocks, or -1 on the last level.</summary>
        public static int NextClassicId(int levelId) =>
            levelId + 1 < ClassicLevels.Count ? levelId + 1 : -1;

        /// <summary>Elapsed run time at 'nowMs'; negative means the clock moved backward (cheat guard trips).</summary>
        public static long ElapsedMs(FreePlayRun run, long nowMs) =>
            run == null ? 0 : (run.Completed ? run.CompletedMs : nowMs) - run.StartedMs;

        /// <summary>
        /// Whether a Free Play difficulty is available: Beginner always, each
        /// next after 3 completions of the previous (MODES.md §2.4).
        /// </summary>
        public static bool IsDifficultyUnlocked(Profile profile, Difficulty difficulty) =>
            difficulty == Difficulty.Beginner ||
            profile.FreePlayCounts[(int)difficulty - 1] >= FreePlayUnlockThreshold;

        public const int FreePlayUnlockThreshold = 3;

        /// <summary>Completions of the previous difficulty still needed to unlock this one (0 when open).</summary>
        public static int RunsRemainingToUnlock(Profile profile, Difficulty difficulty)
        {
            if (difficulty == Difficulty.Beginner) return 0;
            int have = profile.FreePlayCounts[(int)difficulty - 1];
            return have >= FreePlayUnlockThreshold ? 0 : FreePlayUnlockThreshold - have;
        }

        /// <summary>
        /// The original time format (MODES.md §2.2): minutes printed only when
        /// &gt; 0 (taken mod 60), seconds mod 60 zero-padded to 2, milliseconds
        /// zero-padded to 3, colon-separated — e.g. "07:123", "1:07:123".
        /// </summary>
        public static string FormatDuration(long ms)
        {
            if (ms < 0) ms = 0;
            long minutes = (ms / 60000) % 60;
            long seconds = (ms / 1000) % 60;
            long millis = ms % 1000;
            var sb = new StringBuilder(12);
            if (minutes > 0)
            {
                sb.Append(minutes).Append(':');
            }
            if (seconds < 10) sb.Append('0');
            sb.Append(seconds).Append(':');
            if (millis < 100) sb.Append('0');
            if (millis < 10) sb.Append('0');
            sb.Append(millis);
            return sb.ToString();
        }

        /// <summary>"--:--:---" placeholder when no best time exists (Free Play menu).</summary>
        public static string FormatBestTime(long bestMs) =>
            bestMs <= 0 ? "--:--:---" : FormatDuration(bestMs);
    }
}
