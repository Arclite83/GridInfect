using System.Text;

namespace GridInfect.Core
{
    public static class Queries
    {
        public static bool IsUnlocked(Profile profile, int levelId) =>
            levelId == 0 || profile.Unlocked.Contains(levelId);

        public static int NextClassicId(int levelId) =>
            levelId + 1 < ClassicLevels.Count ? levelId + 1 : -1;

        // Levels playable in a world: the first world always offers its first
        // level; anything else needs progress.unlockWorld / unlockWorldLevel.
        public static int WorldLevelsUnlocked(Profile profile, string worldId)
        {
            int n = profile.WorldUnlocked.TryGetValue(worldId ?? "", out int v) ? v : 0;
            if (n == 0 && Worlds.Count > 0 && worldId == Worlds.First.Id) n = 1;
            World w = Worlds.Get(worldId);
            return w != null && n > w.Count ? w.Count : n;
        }

        public static bool IsWorldUnlocked(Profile profile, string worldId) => WorldLevelsUnlocked(profile, worldId) > 0;

        public static bool IsWorldLevelUnlocked(Profile profile, string worldId, int index) =>
            index >= 0 && index < WorldLevelsUnlocked(profile, worldId);

        public static bool IsWorldFinished(Profile profile, string worldId)
        {
            World w = Worlds.Get(worldId);
            return w != null && profile.WorldUnlocked.TryGetValue(worldId, out int v) && v > w.Count;
        }

        public static long ElapsedMs(FreePlayRun run, long nowMs) =>
            run == null ? 0 : (run.Completed ? run.CompletedMs : nowMs) - run.StartedMs;

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
