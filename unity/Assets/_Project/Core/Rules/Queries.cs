using System.Text;

namespace GridInfect.Core
{
    public static class Queries
    {
        // Every level is open. Legacy and the worlds both start unlocked and
        // unsolved: a player may look at any board they like, and the record
        // of progress is the solved set — the red on a select tile — rather
        // than a gate. The unlock actions and the profile fields they write
        // remain (a logged contract, and what a v4 save migrates from); no
        // query reads them.
        public static int NextClassicId(int levelId) =>
            levelId + 1 < ClassicLevels.Count ? levelId + 1 : -1;

        public static bool IsClassicSolved(Profile profile, int levelId) =>
            profile.SolvedClassic.Contains(levelId);

        public static bool IsWorldLevelSolved(Profile profile, string worldId, int index) =>
            profile.SolvedWorld.TryGetValue(worldId ?? "", out var solved) && solved.Contains(index);

        public static int WorldLevelsSolved(Profile profile, string worldId) =>
            profile.SolvedWorld.TryGetValue(worldId ?? "", out var solved) ? solved.Count : 0;

        // How far the infection has spread through a world, 0..1: what the
        // meter on its row fills to.
        public static float WorldInfection(Profile profile, string worldId)
        {
            World world = Worlds.Get(worldId);
            if (world == null || world.Count == 0) return 0f;
            return (float)WorldLevelsSolved(profile, worldId) / world.Count;
        }

        public static bool IsWorldSolved(Profile profile, string worldId)
        {
            World world = Worlds.Get(worldId);
            return world != null && world.Count > 0 && WorldLevelsSolved(profile, worldId) >= world.Count;
        }

        // A replay: the level in play has already been beaten once, so the
        // Lock tool is on the house (a hint cannot cost what the player has
        // already paid). Read straight off the solved set now that one
        // exists — the old proxy was progression, which could not see the
        // last Legacy level because it opens nothing.
        public static bool IsReplay(GameState state)
        {
            if (state == null) return false;
            switch (state.Mode)
            {
                case GameMode.Classic:
                    return IsClassicSolved(state.Profile, state.ClassicLevelId);
                case GameMode.World:
                    return IsWorldLevelSolved(state.Profile, state.WorldId, state.WorldIndex);
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

        // A date is solved once it has a best time, on the day or from the archive.
        public static bool IsDailySolved(Profile profile, string dateUtc) => DailyBestMs(profile, dateUtc) > 0;

        // The grade band a weekday's dailies fall in (the calendar's column header).
        public static (Solving.Grade min, Solving.Grade max) DailyBand(System.DayOfWeek day) => DailyCalendar.Band(day);

        // Difficulty as a player reads it. The Grade enum's own names (G1..G5)
        // are a stored contract — the world files, the level cache keys, the
        // metrics golden — so the word a person sees lives here and nowhere
        // else. "G" said nothing on its own; a tier is plainly a difficulty
        // band, and T3 does not read as level 3 the way L3 would.
        public static string TierName(Solving.Grade grade) => $"TIER {(int)grade}";

        public static string TierShort(Solving.Grade grade) => $"T{(int)grade}";

        public static string TierBand(Solving.Grade min, Solving.Grade max) =>
            min == max ? TierShort(min) : $"{TierShort(min)}-{(int)max}";

        // The streak as of `dateUtc`: intact if the last completed date is
        // today or yesterday, otherwise broken (shown as 0 until today's solve).
        public static int DailyStreakOn(Profile profile, string dateUtc)
        {
            if (!DailySpec.TryParseDate(dateUtc, out System.DateTime today)) return 0;
            if (!DailySpec.TryParseDate(profile.DailyLastDate, out System.DateTime last)) return 0;
            return last == today || last.AddDays(1) == today ? profile.DailyStreak : 0;
        }

        // A time the way a person says it: "48s", "1m 12s", "1h 03m".
        public static string FormatTime(long ms)
        {
            if (ms < 0) ms = 0;
            long seconds = (ms + 500) / 1000;
            long minutes = seconds / 60;
            long hours = minutes / 60;
            if (hours > 0) return $"{hours}h {minutes % 60:00}m";
            if (minutes > 0) return $"{minutes}m {seconds % 60:00}s";
            return $"{seconds}s";
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
