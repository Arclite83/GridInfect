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
    public sealed class ScreenManager
    {
        public AppScreen Current { get; private set; }
        public bool Transitioning => _phase != Phase.None;

        enum Phase { None, FadeOut, FadeIn }

        readonly GameApp _app;
        readonly SpriteRenderer _fade;
        Phase _phase = Phase.None;
        float _phaseTime;
        AppScreen _next;

        public ScreenManager(GameApp app)
        {
            _app = app;
            var go = Ui.MakeRect("fade", null, new Vector2(UnityEngine.Screen.width * 2f, UnityEngine.Screen.height * 2f),
                new Color(0f, 0f, 0f, 0f), 100);
            Object.DontDestroyOnLoad(go);
            _fade = go.GetComponent<SpriteRenderer>();
        }

        public void Show(AppScreen next, bool instant = false)
        {
            if (instant || Current == null)
            {
                Current?.Exit();
                Current = next;
                Current.Enter(_app);
                return;
            }
            _next = next;
            _phase = Phase.FadeOut;
            _phaseTime = 0f;
        }

        public void Update(float dt)
        {
            if (_phase == Phase.None) return;
            float half = PresentationConfig.SceneFade / 2f;
            _phaseTime += dt;
            float t = Mathf.Clamp01(_phaseTime / half);

            if (_phase == Phase.FadeOut)
            {
                SetFade(t);
                if (t >= 1f)
                {
                    Current?.Exit();
                    Current = _next;
                    _next = null;
                    Current.Enter(_app);
                    _phase = Phase.FadeIn;
                    _phaseTime = 0f;
                }
            }
            else
            {
                SetFade(1f - t);
                if (t >= 1f) _phase = Phase.None;
            }
        }

        void SetFade(float alpha)
        {
            var c = _fade.color;
            c.a = alpha;
            _fade.color = c;
        }
    }
}
