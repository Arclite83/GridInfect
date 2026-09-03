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
        public readonly System.Collections.Generic.HashSet<int> Unlocked =
            new System.Collections.Generic.HashSet<int>();

        public readonly long[] BestTimesMs = new long[5];

        // World progression: levels playable per world id (0 or absent =
        // locked, except the first world, which is always open at level 0).
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

        public bool Dirty;
    }
}
