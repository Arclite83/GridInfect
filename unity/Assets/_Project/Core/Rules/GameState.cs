using System;

namespace GridInfect.Core
{
    /// <summary>
    /// Root state aggregate. Two sub-aggregates with disjoint owners:
    /// the current <see cref="LevelSession"/> (board play, owned by the
    /// level/piece/board actions) and the persistent <see cref="Profile"/>
    /// (progress and settings, owned by the progress/freeplay/settings
    /// actions). Wall-clock time never enters except through action inputs,
    /// so a log replay is deterministic.
    /// </summary>
    public sealed class GameState
    {
        public GameMode Mode = GameMode.Classic;
        public Difficulty Difficulty = Difficulty.Beginner;

        /// <summary>Classic level id (0..127) currently loaded; -1 when none.</summary>
        public int ClassicLevelId = -1;

        /// <summary>Generated free-play levels for the current run (null outside FreePlay).</summary>
        public LevelDef[] FreePlayDefs;

        /// <summary>Index into FreePlayDefs of the level being played.</summary>
        public int FreePlayIndex;

        /// <summary>Free-play timer, running when StartedMs is set (wall clock arrives via inputs).</summary>
        public FreePlayRun FreePlayRun;

        public LevelSession Session { get; private set; }

        public Profile Profile = new Profile();

        /// <summary>Fired when a new session replaces the old (adapters re-subscribe board events).</summary>
        public event Action<LevelSession> SessionChanged;

        public void SetSession(LevelSession session)
        {
            Session = session;
            SessionChanged?.Invoke(session);
        }
    }

    /// <summary>One timed Free Play run: five generated levels against the wall clock (MODES.md §2).</summary>
    public sealed class FreePlayRun
    {
        public long StartedMs;      // wall-clock ms at BEGIN (from the freeplay.begin input)
        public long CompletedMs;    // wall-clock ms at the 5th solve; 0 while running
        public bool Completed => CompletedMs != 0;
    }

    /// <summary>
    /// Persistent player progress — the save model (pure data, no IO;
    /// serialization in <see cref="SaveCodec"/>, file placement in the Unity
    /// layer). Array indexes are Difficulty ordinals — order is contract.
    /// </summary>
    public sealed class Profile
    {
        /// <summary>Classic ids whose levels are unlocked. Id 0 is always playable regardless.</summary>
        public readonly System.Collections.Generic.HashSet<int> Unlocked =
            new System.Collections.Generic.HashSet<int>();

        /// <summary>Best Free Play time per difficulty, ms; 0 = no record yet.</summary>
        public readonly long[] BestTimesMs = new long[5];

        /// <summary>Completed Free Play runs per difficulty (drives the unlock ladder).</summary>
        public readonly int[] FreePlayCounts = new int[5];

        public bool Muted;

        /// <summary>
        /// Set by any profile-mutating action; the persistence adapter
        /// write-through saves and clears it after each dispatch.
        /// </summary>
        public bool Dirty;
    }
}
