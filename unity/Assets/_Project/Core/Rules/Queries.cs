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

        // Whether progress.unlockAll has already been run (the dev row reads
        // it to say so rather than offer the press again).
        public static bool EverythingUnlocked(Profile profile)
        {
            if (profile.Unlocked.Count < ClassicLevels.Count) return false;
            foreach (World world in Worlds.All)
            {
                if (!IsWorldFinished(profile, world.Id)) return false;
            }
            return true;
        }

        // A replay: the level in play has already been beaten once, so the
        // Lock tool is on the house (a hint cannot cost what the player has
        // already paid). Read off progression rather than a second record —
        // solving N is what opens N + 1. The one blind spot is the last
        // Legacy level, which opens nothing and so never reads as replayed.
        public static bool IsReplay(GameState state)
        {
            if (state == null) return false;
            switch (state.Mode)
            {
                case GameMode.Classic:
                    int next = NextClassicId(state.ClassicLevelId);
                    return next >= 0 && state.Profile.Unlocked.Contains(next);
                case GameMode.World:
                    // A later level is open, or the world is finished — the
                    // last level's unlock lands past Count, where the clamp in
                    // WorldLevelsUnlocked can no longer see it.
                    return state.WorldIndex >= 0 &&
                           (WorldLevelsUnlocked(state.Profile, state.WorldId) > state.WorldIndex + 1 ||
                            IsWorldFinished(state.Profile, state.WorldId));
                case GameMode.Daily:
                    return state.DailyRun != null && DailyBestMs(state.Profile, state.DailyRun.DateUtc) > 0;
                default:
                    return false;   // Free Play and Endless are never a second visit
            }
        }

        public static long ElapsedMs(FreePlayRun run, long nowMs) =>
            run == null ? 0 : (run.Completed ? run.CompletedMs : nowMs) - run.StartedMs;

        public static long ElapsedMs(DailyRun run, long nowMs) =>
            run == null ? 0 : (run.Completed ? run.CompletedMs : nowMs) - run.StartedMs;

        public static long DailyBestMs(Profile profile, string dateUtc) =>
            profile.DailyBestMs.TryGetValue(dateUtc ?? "", out long ms) ? ms : 0;

        // The streak as of `dateUtc`: intact if the last completed date is
        // today or yesterday, otherwise broken (shown as 0 until today's solve).
        public static int DailyStreakOn(Profile profile, string dateUtc)
        {
            if (!DailySpec.TryParseDate(dateUtc, out System.DateTime today)) return 0;
            if (!DailySpec.TryParseDate(profile.DailyLastDate, out System.DateTime last)) return 0;
            return last == today || last.AddDays(1) == today ? profile.DailyStreak : 0;
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
