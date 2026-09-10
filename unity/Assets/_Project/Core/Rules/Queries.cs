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

        // The two clean-sweep marks. Nothing in the game is gated on
        // progress any more except the board skins, which are the reward for
        // one: blue for every world, breadboard for all 128 Legacy levels.
        // Which skin needs which is the adapter's business (SettingsScreen),
        // as unlock gating has always been presentation policy.
        public static bool AllWorldsSolved(Profile profile)
        {
            if (Worlds.Count == 0) return false;
            foreach (World world in Worlds.All)
            {
                if (!IsWorldSolved(profile, world.Id)) return false;
            }
            return true;
        }

        public static bool AllClassicSolved(Profile profile) =>
            profile.SolvedClassic.Count >= ClassicLevels.Count;

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

        // Difficulty as a player reads it used to live here. The Grade enum's
        // own names (G1..G5) are still a stored contract — the world files,
        // the level cache keys, the metrics golden — but the *word* a person
        // reads is prose, and Core holds keys and never prose
        // (docs/I18N.md 5). It is Str.TierName / Str.TierBandLabel now.

        // The streak as of `dateUtc`: intact if the last completed date is
        // today or yesterday, otherwise broken (shown as 0 until today's solve).
        public static int DailyStreakOn(Profile profile, string dateUtc)
        {
            if (!DailySpec.TryParseDate(dateUtc, out System.DateTime today)) return 0;
            if (!DailySpec.TryParseDate(profile.DailyLastDate, out System.DateTime last)) return 0;
            return last == today || last.AddDays(1) == today ? profile.DailyStreak : 0;
        }

        // Free Play's clock, the one place a time is still shown: "1:07:420".
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
