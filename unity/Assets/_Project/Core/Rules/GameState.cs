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

        public bool Muted;

        public bool Dirty;
    }
}
