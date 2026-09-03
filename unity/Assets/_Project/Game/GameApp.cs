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
        float _resolveAt = -1f;

        public static long NowMs() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // The Daily's date: UTC, so every device gets the same board.
        public static string TodayUtc() => DailySpec.Format(System.DateTime.UtcNow);

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

            Dispatcher = GridInfectActions.CreateDispatcher();
            _save = new SavePort(Application.persistentDataPath);
            State.Profile = _save.Load();
            Dispatcher.Applied += _ => _save.SaveIfDirty(State.Profile);

            Ads = AdGate.Create();
            Ads.Start();

            Screens = new ScreenManager(this);
            Screens.Show(new MainMenuScreen(), instant: true);
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
            float dt = Time.unscaledDeltaTime;
            Tweens.Update(dt);
            Screens.Update(dt);

            var session = State.Session;
            if (session != null && session.ResolutionPending && Time.unscaledTime >= _resolveAt)
            {
                Do(GridInfectActions.BoardResolve);
            }

            var screen = Screens.Current;
            if (screen == null) return;
            screen.Tick(dt);

            if (Screens.Transitioning) return;

            if (Input.GetMouseButtonDown(0))
            {
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
                    if (hit != null) hit.OnClick?.Invoke();
                    else screen.OnPress(world);
                }
            }
            else if (Input.GetMouseButton(0))
            {
                screen.OnDrag(ToWorld(Input.mousePosition));
            }
            else if (Input.GetMouseButtonUp(0))
            {
                screen.OnRelease(ToWorld(Input.mousePosition));
            }
        }

        public Vector2 ToWorld(Vector3 screenPos)
        {
            Vector3 world = _camera.ScreenToWorldPoint(screenPos);
            return new Vector2(world.x, world.y);
        }
    }
}
