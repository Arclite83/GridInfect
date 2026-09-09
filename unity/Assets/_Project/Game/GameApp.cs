using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindAnyObjectByType<GameApp>() != null) return;
            var go = new GameObject("GridInfect");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }
    }

    public sealed class GameApp : MonoBehaviour
    {
        public Dispatcher<GameState> Dispatcher { get; private set; }
        public GameState State => Dispatcher.State;
        public readonly TweenRunner Tweens = new TweenRunner();
        public ScreenManager Screens { get; private set; }

        Camera _camera;
        SavePort _save;
        LevelCachePort _levels;
        float _resolveAt = -1f;

        // Touch gating. A transition swallows input outright; the two cool-
        // downs cover the frames either side of it — the tap that lands the
        // instant a screen appears, and the second half of a double-tap that
        // was only ever meant to be one press.
        bool _wasTransitioning;
        float _inputBlockedUntil;
        float _clickBlockedUntil;

        public static long NowMs() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // The Daily's date: UTC, so every device gets the same board.
        public static string TodayUtc() => DailySpec.Format(System.DateTime.UtcNow);

        // The seed the next Endless run will start from. Picked ahead of
        // time so the cache can have its opening boards ready before the
        // player asks (Warmup); TakeEndlessSeed hands it over and picks the
        // one after.
        public ulong EndlessSeed { get; private set; }

        public ulong TakeEndlessSeed()
        {
            ulong seed = EndlessSeed;
            EndlessSeed = (ulong)NowMs() ^ (seed << 7);
            Warmup.EndlessOpeners(LevelCache.Shared, EndlessSeed, 30);
            return seed;
        }

        // Local until a friends board lands (stage 4 leaves the hook).
        public IDailyScoreSink DailyScores { get; set; } = new LocalDailyScoreSink();

        // Ads, consent and remove-ads behind the Services boundary (stage 6).
        public AdGate Ads { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = PresentationConfig.TargetFrameRate;

            _camera = Camera.main;
            if (_camera == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _camera = go.AddComponent<Camera>();
            }
            _camera.orthographic = true;
            _camera.orthographicSize = UnityEngine.Screen.height / 2f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BoardTheme.Background;

            // The PCB under every screen, and the bloom that gives the
            // infection its halo. Both outlive any one screen.
            Substrate.Ensure(BoardPalette.Default);
            BoardBloom.Ensure(_camera, BoardPalette.Default);

            Dispatcher = GridInfectActions.CreateDispatcher();
            _save = new SavePort(Application.persistentDataPath);
            State.Profile = _save.Load();
            ApplySkin();   // the saved colours, before the first screen builds

            // The level cache: what the device has generated so far, and the
            // worker that generates ahead of the player. Today's daily first,
            // then the recent archive, then Endless's opening boards; in
            // Endless, the next board while the current one is played.
            _levels = new LevelCachePort(Application.persistentDataPath);
            _levels.Load(LevelCache.Shared);
            EndlessSeed = (ulong)NowMs();
            Warmup.AtBoot(LevelCache.Shared, System.DateTime.UtcNow, State.Profile, EndlessSeed);

            Dispatcher.Applied += _ =>
            {
                _save.SaveIfDirty(State.Profile);
                Warmup.AfterAction(LevelCache.Shared, State);
            };

            Ads = AdGate.Create();
            Ads.Start();

            Screens = new ScreenManager(this);
            Screens.Show(new MainMenuScreen(), instant: true);
        }

        // A skin change touches everything that baked a colour when it was
        // made: the palette, the glyph sprites cached against it, the
        // substrate's material and the camera clear. Glass is per-screen and
        // re-reads the palette on build, so the caller shows a screen after.
        public void ApplySkin()
        {
            // A skin that is no longer earned falls back rather than being
            // worn anyway: clearing progress takes the reward with it, and it
            // comes back when the sweep does. The profile keeps the choice.
            var skin = (BoardPalette.SkinId)State.Profile.Skin;
            if (!SkinEarned(skin)) skin = BoardPalette.SkinId.Default;
            BoardPalette.SetSkin(skin);
            BugGlyph.ClearCache();
            Substrate.Restyle(BoardPalette.Default);
            if (_camera != null) _camera.backgroundColor = BoardTheme.Background;
        }

        // The one thing left in the game behind progress: blue for every
        // world beaten, breadboard for all 128 Legacy levels. Gating is
        // presentation policy (ARCHITECTURE §3), so it lives here and not in
        // settings.skin, which will set anything a test asks for.
        public bool SkinEarned(BoardPalette.SkinId skin)
        {
            switch (skin)
            {
                case BoardPalette.SkinId.Blue: return Queries.AllWorldsSolved(State.Profile);
                case BoardPalette.SkinId.Breadboard: return Queries.AllClassicSolved(State.Profile);
                default: return true;
            }
        }

        public ActionResult Do(string action, Dictionary<string, object> input = null)
        {
            var result = Dispatcher.Dispatch(action, input);
            if (!result.Applied) Debug.Log($"[actions] rejected: {result.Rejection}");
            return result;
        }

        public void ScheduleResolve()
        {
            _resolveAt = Time.unscaledTime + PresentationConfig.ResolveDelay;
        }

        public bool FastForwardResolve()
        {
            if (State.Session != null && State.Session.ResolutionPending)
            {
                Do(GridInfectActions.BoardResolve);
                return true;
            }
            return false;
        }

        void Update()
        {
            // A level generation or a first-frame hitch produces one enormous
            // delta. Clamped, it costs a fraction of a second of animation;
            // unclamped it fast-forwards every tween and fade past its end.
            float dt = Mathf.Min(Time.unscaledDeltaTime, PresentationConfig.MaxFrameDelta);
            Tweens.Update(dt);
            Screens.Update(dt);
            Work.Shared.Pump();                        // completions of background jobs land here
            _levels.SaveIfDirty(LevelCache.Shared);   // a board landed on the worker: keep it

            var session = State.Session;
            if (session != null && session.ResolutionPending && Time.unscaledTime >= _resolveAt)
            {
                Do(GridInfectActions.BoardResolve);
            }

            var screen = Screens.Current;
            if (screen == null) return;
            screen.Tick(dt);

            bool transitioning = Screens.Transitioning;
            if (_wasTransitioning && !transitioning)
            {
                // A press made during the blackout arrives on the first frame
                // after it, where the new screen has already claimed the pixels
                // under the finger. Give it a beat to be let go of.
                _inputBlockedUntil = Time.unscaledTime + PresentationConfig.PostTransitionInputBlock;
            }
            _wasTransitioning = transitioning;
            if (transitioning) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (Time.unscaledTime < _inputBlockedUntil) return;

                // Any touch fast-forwards a pending resolution first. If that
                // resolution just solved the level, swallow the touch: the
                // popup that appeared must not eat a click meant for the board.
                bool solvedNow = FastForwardResolve() && session != null && session.Solved;
                if (!solvedNow)
                {
                    Vector2 world = ToWorld(Input.mousePosition);
                    UiButton hit = null;
                    foreach (var button in screen.Buttons)
                    {
                        if (button.HitTest(world)) hit = button; // last wins = drawn on top
                    }
                    if (hit == null)
                    {
                        screen.OnPress(world);   // the board: never debounced, it is a drag
                    }
                    else if (Time.realtimeSinceStartup >= _clickBlockedUntil)
                    {
                        // One button press per cooldown, whichever button: a
                        // double-tap on a menu row must not both navigate and
                        // fire again on whatever replaces it. The window is
                        // stamped from the wall clock *after* the handler
                        // returns: a handler that stalls the frame (the hint
                        // runs the deducer) must not spend its own window, or
                        // the second tap of a double-tap lands the moment the
                        // frame resumes.
                        hit.OnClick?.Invoke();
                        _clickBlockedUntil = Time.realtimeSinceStartup + hit.Cooldown;
                    }
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (Time.unscaledTime >= _inputBlockedUntil) screen.OnDrag(ToWorld(Input.mousePosition));
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (Time.unscaledTime >= _inputBlockedUntil) screen.OnRelease(ToWorld(Input.mousePosition));
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) _levels?.SaveIfDirty(LevelCache.Shared);
        }

        void OnApplicationQuit()
        {
            _levels?.SaveIfDirty(LevelCache.Shared);
        }

        public Vector2 ToWorld(Vector3 screenPos)
        {
            Vector3 world = _camera.ScreenToWorldPoint(screenPos);
            return new Vector2(world.x, world.y);
        }
    }
}
