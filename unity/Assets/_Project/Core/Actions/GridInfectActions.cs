using System.Collections.Generic;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// The action registry for Grid Infect — the complete list of writers of
    /// meaningful state. Documented as the registry artifact in
    /// ARCHITECTURE.md; the gate test asserts this list and that document
    /// stay in sync with the registered names.
    /// </summary>
    public static class GridInfectActions
    {
        public const string LevelLoad = "level.load";
        public const string LevelGenerate = "level.generate";
        public const string LevelReset = "level.reset";
        public const string PiecePlace = "piece.place";
        public const string PieceClear = "piece.clear";
        public const string BoardResolve = "board.resolve";
        public const string ProgressUnlock = "progress.unlock";
        public const string SettingsMute = "settings.mute";
        public const string FreePlayBegin = "freeplay.begin";
        public const string FreePlayAdvance = "freeplay.advance";
        public const string FreePlayComplete = "freeplay.complete";
        public const string FreePlayAbort = "freeplay.abort";

        public static void RegisterAll(ActionRegistry<GameState> registry)
        {
            registry.Register(new LoadLevelAction());
            registry.Register(new GenerateLevelsAction());
            registry.Register(new ResetLevelAction());
            registry.Register(new PlacePieceAction());
            registry.Register(new ClearPieceAction());
            registry.Register(new ResolveBoardAction());
            registry.Register(new UnlockLevelAction());
            registry.Register(new SetMutedAction());
            registry.Register(new BeginFreePlayAction());
            registry.Register(new AdvanceFreePlayAction());
            registry.Register(new CompleteFreePlayAction());
            registry.Register(new AbortFreePlayAction());
        }

        /// <summary>A fully wired dispatcher over fresh state — the one-call entry point for adapters and tests.</summary>
        public static Dispatcher<GameState> CreateDispatcher()
        {
            var registry = new ActionRegistry<GameState>();
            RegisterAll(registry);
            return new Dispatcher<GameState>(new GameState(), registry);
        }
    }

    /// <summary>Typed input builders — the only place input payload keys are spelled by callers.</summary>
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
    }
}
