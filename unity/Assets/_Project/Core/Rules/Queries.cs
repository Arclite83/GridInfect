using System.Text;

namespace GridInfect.Core
{
    public static class Queries
    {
        public static bool IsUnlocked(Profile profile, int levelId) =>
            levelId == 0 || profile.Unlocked.Contains(levelId);

        public static int NextClassicId(int levelId) =>
            levelId + 1 < ClassicLevels.Count ? levelId + 1 : -1;

        public static long ElapsedMs(FreePlayRun run, long nowMs) =>
            run == null ? 0 : (run.Completed ? run.CompletedMs : nowMs) - run.StartedMs;

        public static bool IsDifficultyUnlocked(Profile profile, Difficulty difficulty) =>
            difficulty == Difficulty.Beginner ||
            profile.FreePlayCounts[(int)difficulty - 1] >= FreePlayUnlockThreshold;

        public const int FreePlayUnlockThreshold = 3;

        public static int RunsRemainingToUnlock(Profile profile, Difficulty difficulty)
        {
            if (difficulty == Difficulty.Beginner) return 0;
            int have = profile.FreePlayCounts[(int)difficulty - 1];
            return have >= FreePlayUnlockThreshold ? 0 : FreePlayUnlockThreshold - have;
        }

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

        public static string FormatBestTime(long bestMs) =>
            bestMs <= 0 ? "--:--:---" : FormatDuration(bestMs);
    }
}
