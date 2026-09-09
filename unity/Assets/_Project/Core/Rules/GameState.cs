using System;

namespace GridInfect.Core
{
    // Wall-clock time and RNG seeds enter only through action inputs, so a log replays deterministically.
    public sealed class GameState
    {
        public GameMode Mode = GameMode.Classic;
        public Difficulty Difficulty = Difficulty.Beginner;

        public int ClassicLevelId = -1;

        public string WorldId;      // GameMode.World: the world and level in play
        public int WorldIndex = -1;

        public DailyRun DailyRun;       // GameMode.Daily
        public EndlessRun EndlessRun;   // GameMode.Endless

        public int TutorialIndex = -1;  // GameMode.Tutorial: the step in play

        // The current level's stored solution in a winning order: the
        // vector for Legacy, the generator's for everything else. The Lock
        // tool's fallback source; set by every loader.
        public (int piece, int cell)[] Solution;
        public (int piece, int cell)[][] FreePlaySolutions;

        public LevelDef[] FreePlayDefs;

        public int FreePlayIndex;

        public FreePlayRun FreePlayRun;

        public LevelSession Session { get; private set; }

        public Profile Profile = new Profile();

        public event Action<LevelSession> SessionChanged;

        public void SetSession(LevelSession session)
        {
            Session = session;
            SessionChanged?.Invoke(session);
        }
    }

    public sealed class FreePlayRun
    {
        public long StartedMs;      // wall-clock ms at BEGIN (from the freeplay.begin input)
        public long CompletedMs;    // wall-clock ms at the 5th solve; 0 while running
        public bool Completed => CompletedMs != 0;
    }

    public sealed class Profile
    {
        // Levels beaten. Every level is open from a fresh install, so this is
        // the whole of progression: it is what paints a select tile infected
        // and what makes a hint free on a second visit. Legacy by id, worlds
        // by index per world id.
        public readonly System.Collections.Generic.HashSet<int> SolvedClassic =
            new System.Collections.Generic.HashSet<int>();
        public readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<int>> SolvedWorld =
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<int>>(System.StringComparer.Ordinal);

        // Retired gates. Nothing reads these: they are still written by the
        // unlock actions, which are a logged contract, and a v4 save's solved
        // sets are derived from them on load.
        public readonly System.Collections.Generic.HashSet<int> Unlocked =
            new System.Collections.Generic.HashSet<int>();

        public readonly long[] BestTimesMs = new long[5];

        public readonly System.Collections.Generic.Dictionary<string, int> WorldUnlocked =
            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);

        public readonly int[] FreePlayCounts = new int[5];

        // Daily: personal best per UTC date, streak of consecutive dates,
        // the last date completed. Endless: best streak per grade (index = grade - 1).
        public readonly System.Collections.Generic.Dictionary<string, long> DailyBestMs =
            new System.Collections.Generic.Dictionary<string, long>(System.StringComparer.Ordinal);
        public int DailyStreak;
        public string DailyLastDate = "";
        public readonly int[] EndlessBest = new int[5];

        // Lock wallet (stage 5): start 5, free grants capped at LocksCap,
        // rewarded-ad grants uncapped (NEXT_PASS: they are revenue).
        public const int LocksStart = 5;
        public const int LocksCap = 10;
        public int Locks = LocksStart;

        public bool Muted;

        // The tutorial: whether the first-open offer has been answered (either
        // way), and the highest step beaten plus one, so the series resumes
        // where it was left and its forward chevron opens only what is
        // earned. Neither is progress the erase button forgets: they are
        // about knowing the game, not beating it.
        public bool TutorialSeen;
        public int TutorialStep;

        // Board skin: 0 the ship green, 1 blue, 2 breadboard tan
        // (BoardPalette.SkinId). A preference like Muted, not progress.
        public int Skin;

        public bool Dirty;
    }
}
