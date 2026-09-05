using System.Collections.Generic;
using UnityEngine;

namespace GridInfect.Game
{
    public abstract class AppScreen
    {
        protected GameApp App { get; private set; }
        public GameObject Root { get; private set; }
        public readonly List<UiButton> Buttons = new List<UiButton>();

        public void Enter(GameApp app)
        {
            App = app;
            Root = new GameObject(GetType().Name);
            Build();
        }

        public void Exit()
        {
            OnExit();
            if (Root != null) Object.Destroy(Root);
        }

        protected abstract void Build();
        protected virtual void OnExit() { }

        public virtual void Tick(float dt) { }

        public virtual bool OnPress(Vector2 world) => false;
        public virtual void OnDrag(Vector2 world) { }
        public virtual void OnRelease(Vector2 world) { }
    }

    // Every navigation: 0.25 s to black, swap screens, 0.25 s back (ASSETS §6).
    //
    // A navigation may carry `prepare` — the dispatch that loads or generates
    // the level the next screen is going to draw. It runs at full black, one
    // presented frame after the LOADING card goes up, so an on-device
    // generation (Endless G5 is seconds of solver work) is a screen that says
    // what it is doing rather than a menu that stops answering. Input is shut
    // off for the whole transition (GameApp reads Transitioning), which is
    // what stops a stray tap during the stall landing on whatever button the
    // next screen happens to put under the finger.
    public sealed class ScreenManager
    {
        public AppScreen Current { get; private set; }
        public bool Transitioning => _phase != Phase.None;

        enum Phase { None, FadeOut, Working, FadeIn }

        readonly GameApp _app;
        readonly SpriteRenderer _fade;
        readonly TextMesh _loading;
        Phase _phase = Phase.None;
        float _phaseTime;
        int _workFrames;
        AppScreen _next;
        System.Func<bool> _prepare;

        public ScreenManager(GameApp app)
        {
            _app = app;
            var go = Ui.MakeRect("fade", null, new Vector2(UnityEngine.Screen.width * 2f, UnityEngine.Screen.height * 2f),
                new Color(0f, 0f, 0f, 0f), 100);
            Object.DontDestroyOnLoad(go);
            _fade = go.GetComponent<SpriteRenderer>();

            // On the black of the fade, so the lit tip white, not the ink.
            _loading = Ui.MakeText("loading", null, "LOADING",
                PresentationConfig.Layout.HeadingText, BoardTheme.TextOnAccent, 101);
            Object.DontDestroyOnLoad(_loading.gameObject);
            _loading.gameObject.SetActive(false);
        }

        // `prepare` returns false to call the navigation off: the current
        // screen stays, and the fade simply comes back up on it.
        public void Show(AppScreen next, bool instant = false, System.Func<bool> prepare = null)
        {
            if (instant || Current == null)
            {
                if (prepare != null && !prepare()) return;
                Current?.Exit();
                Current = next;
                Current.Enter(_app);
                return;
            }
            _next = next;
            _prepare = prepare;
            _phase = Phase.FadeOut;
            _phaseTime = 0f;
        }

        public void Update(float dt)
        {
            if (_phase == Phase.None) return;

            if (_phase == Phase.Working)
            {
                // Frame 0 only puts the card up; the blocking call waits for
                // frame 1, so the player has actually seen it before the main
                // thread goes away.
                if (_workFrames++ == 0) return;
                _loading.gameObject.SetActive(false);
                bool ok = _prepare == null || _prepare();
                _prepare = null;
                if (ok)
                {
                    Swap();
                }
                else
                {
                    _next = null;   // rejected: fade back in on the screen we never left
                }
                _phase = Phase.FadeIn;
                _phaseTime = 0f;
                return;
            }

            float half = PresentationConfig.SceneFade / 2f;
            _phaseTime += dt;
            float t = Mathf.Clamp01(_phaseTime / half);

            if (_phase == Phase.FadeOut)
            {
                SetFade(t);
                if (t < 1f) return;
                if (_prepare != null)
                {
                    _loading.gameObject.SetActive(true);
                    _phase = Phase.Working;
                    _workFrames = 0;
                    return;
                }
                Swap();
                _phase = Phase.FadeIn;
                _phaseTime = 0f;
            }
            else
            {
                SetFade(1f - t);
                if (t >= 1f) _phase = Phase.None;
            }
        }

        void Swap()
        {
            Current?.Exit();
            Current = _next;
            _next = null;
            Current.Enter(_app);
        }

        void SetFade(float alpha)
        {
            var c = _fade.color;
            c.a = alpha;
            _fade.color = c;
        }
    }
}
