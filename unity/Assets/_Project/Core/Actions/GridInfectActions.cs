using System.Collections.Generic;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    // The complete list of writers of meaningful state; a gate test keeps this,
    // the registry, and ARCHITECTURE.md in sync.
    public static class GridInfectActions
    {
        public const string LevelLoad = "level.load";
        public const string LevelGenerate = "level.generate";
        public const string LevelReset = "level.reset";
        public const string PiecePlace = "piece.place";
        public const string PieceClear = "piece.clear";
        public const string BoardResolve = "board.resolve";
        public const string ProgressUnlock = "progress.unlock";
        public const string ProgressUnlockAll = "progress.unlockAll";
        public const string SettingsMute = "settings.mute";
        public const string FreePlayBegin = "freeplay.begin";
        public const string FreePlayAdvance = "freeplay.advance";
        public const string FreePlayComplete = "freeplay.complete";
        public const string FreePlayAbort = "freeplay.abort";
        public const string WorldLoad = "world.load";
        public const string ProgressUnlockWorld = "progress.unlockWorld";
        public const string ProgressUnlockWorldLevel = "progress.unlockWorldLevel";
        public const string DailyBegin = "daily.begin";
        public const string DailyComplete = "daily.complete";
        public const string EndlessBegin = "endless.begin";
        public const string EndlessAdvance = "endless.advance";
        public const string EndlessAbort = "endless.abort";
        public const string PieceLock = "piece.lock";
        public const string LocksGrant = "locks.grant";

        public static void RegisterAll(ActionRegistry<GameState> registry)
        {
            registry.Register(new LoadLevelAction());
            registry.Register(new GenerateLevelsAction());
            registry.Register(new ResetLevelAction());
            registry.Register(new PlacePieceAction());
            registry.Register(new ClearPieceAction());
            registry.Register(new ResolveBoardAction());
            registry.Register(new UnlockLevelAction());
            registry.Register(new UnlockEverythingAction());
            registry.Register(new SetMutedAction());
            registry.Register(new BeginFreePlayAction());
            registry.Register(new AdvanceFreePlayAction());
            registry.Register(new CompleteFreePlayAction());
            registry.Register(new AbortFreePlayAction());
            registry.Register(new LoadWorldLevelAction());
            registry.Register(new UnlockWorldAction());
            registry.Register(new UnlockWorldLevelAction());
            registry.Register(new BeginDailyAction());
            registry.Register(new CompleteDailyAction());
            registry.Register(new BeginEndlessAction());
            registry.Register(new AdvanceEndlessAction());
            registry.Register(new AbortEndlessAction());
            registry.Register(new LockPieceAction());
            registry.Register(new GrantLocksAction());
        }

        public static Dispatcher<GameState> CreateDispatcher()
        {
            var registry = new ActionRegistry<GameState>();
            RegisterAll(registry);
            return new Dispatcher<GameState>(new GameState(), registry);
        }
    }

    public static class Inputs
    {
        public static Dictionary<string, object> LevelLoad(int levelId) =>
            new Dictionary<string, object> { ["levelId"] = levelId };

        public static Dictionary<string, object> LevelGenerate(Difficulty difficulty, long seed, int count = GenerateLevelsAction.LevelsPerRun) =>
            new Dictionary<string, object> { ["difficulty"] = (int)difficulty, ["seed"] = seed, ["count"] = count };

        public static Dictionary<string, object> PiecePlace(int piece, int i, int j) =>
            new Dictionary<string, object> { ["piece"] = piece, ["i"] = i, ["j"] = j };

        public static Dictionary<string, object> PieceClear(int piece) =>
            new Dictionary<string, object> { ["piece"] = piece };

        public static Dictionary<string, object> Unlock(int levelId) =>
            new Dictionary<string, object> { ["levelId"] = levelId };

        public static Dictionary<string, object> Muted(bool muted) =>
            new Dictionary<string, object> { ["muted"] = muted };

        public static Dictionary<string, object> Now(long nowMs) =>
            new Dictionary<string, object> { ["nowMs"] = nowMs };

        public static Dictionary<string, object> WorldLoad(string worldId, int index) =>
            new Dictionary<string, object> { ["worldId"] = worldId, ["index"] = index };

        public static Dictionary<string, object> UnlockWorld(string worldId) =>
            new Dictionary<string, object> { ["worldId"] = worldId };

        public static Dictionary<string, object> UnlockWorldLevel(string worldId, int index) =>
            new Dictionary<string, object> { ["worldId"] = worldId, ["index"] = index };

        public static Dictionary<string, object> DailyBegin(string dateUtc, long nowMs) =>
            new Dictionary<string, object> { ["dateUtc"] = dateUtc, ["nowMs"] = nowMs };

        public static Dictionary<string, object> EndlessBegin(Solving.Grade grade, long seed) =>
            new Dictionary<string, object> { ["grade"] = (int)grade, ["seed"] = seed };

        public static Dictionary<string, object> LocksGrant(int amount, string reason) =>
            new Dictionary<string, object> { ["amount"] = amount, ["reason"] = reason };
    }
}
