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

        // The device's back button (Android; Escape in the editor). A back
        // chip lives in the top leading corner on every screen that has one
        // (Layout.BackPos), so the default presses whatever enabled chip is
        // there: MENU, the gear on the language screen, WORLDS on a world's
        // level list. A screen whose way back is elsewhere overrides. A
        // screen that has shut its chips (a popup up) answers nothing, which
        // is the same answer a finger on the chip would get.
        public virtual void OnBack()
        {
            Vector2 at = PresentationConfig.Layout.BackPos;
            foreach (var button in Buttons)
            {
                if (button.HitTest(at))
                {
                    button.OnClick?.Invoke();
                    return;
                }
            }
        }
    }

    // Every navigation: 0.25 s down to the scrim, swap screens, 0.25 s back
    // (ASSETS §6). The scrim is the board with the lights off, not black —
    // black is a hole in the app and the substrate is still down there.
    //
    // A navigation may carry `prepare` — the dispatch that loads or generates
    // the level the next screen is going to draw. It runs at the bottom of
    // the fade, one presented frame after the loading card goes up, so an
    // on-device generation (Endless T5 is seconds of solver work) is a screen
    // that says what it is doing rather than a menu that stops answering.
    // The card fades rather than switching on, so a navigation whose work
    // lands in a frame or two never flashes it. Input is shut
    // off for the whole transition (GameApp reads Transitioning), which is
    // what stops a stray tap during the stall landing on whatever button the
    // next screen happens to put under the finger.
    //
    // It may also carry `ready`: is the board this navigation needs already
    // in the cache? While that says no the card simply stays up and the
    // frame keeps running, so the generation happens on the worker thread
    // and the main thread never goes away. Without it, entering a mode whose
    // board had not been warmed blocked the main thread for however long the
    // solver took — seconds at the top of the ramp, long enough for the OS
    // to call the app hung. The wait gives up after ReadyTimeout and
    // generates inline anyway, so a prefetch that failed or was evicted is a
    // slow entry, never a stuck one.
    public sealed class ScreenManager
    {
        public AppScreen Current { get; private set; }
        public bool Transitioning => _phase != Phase.None;

        enum Phase { None, FadeOut, Working, FadeIn }

        // How long the card will wait on the worker before doing the work
        // itself. Longer than any board takes to generate on a phone.
        const float ReadyTimeout = 20f;

        readonly GameApp _app;
        readonly SpriteRenderer _fade;
        readonly LoadingCard _card;

        Phase _phase = Phase.None;
        float _phaseTime;
        float _waited;
        int _workFrames;
        AppScreen _next;
        System.Func<bool> _prepare;
        System.Func<bool> _ready;

        public ScreenManager(GameApp app)
        {
            _app = app;
            var go = Ui.MakeRect("fade", null, new Vector2(UnityEngine.Screen.width * 2f, UnityEngine.Screen.height * 2f),
                BoardPalette.Alpha(LoadingCard.Scrim, 0f), 100);
            Object.DontDestroyOnLoad(go);
            _fade = go.GetComponent<SpriteRenderer>();
            _card = new LoadingCard(101);
        }

        // The scrim and the card follow the skin: the scrim because SetFade
        // reads it fresh, the card because it is written again in place.
        public void Restyle()
        {
            _card.Restyle();
            SetFade(_fade.color.a);
        }

        // `prepare` returns false to call the navigation off: the current
        // screen stays, and the fade simply comes back up on it. `ready` is
        // polled under the LOADING card until the work `prepare` needs has
        // landed on the worker.
        public void Show(AppScreen next, bool instant = false, System.Func<bool> prepare = null,
            System.Func<bool> ready = null)
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
            _ready = ready;
            _phase = Phase.FadeOut;
            _phaseTime = 0f;
            _waited = 0f;
        }

        public void Update(float dt)
        {
            // Always: the card runs its sweep while it is up and its fade
            // out after the work has landed, which is past the end of the
            // phase that put it there.
            _card.Tick(dt);
            if (_phase == Phase.None) return;

            if (_phase == Phase.Working)
            {
                // Frame 0 only puts the card up; the call waits for frame 1,
                // so the player has actually seen it first.
                if (_workFrames++ == 0) return;
                _waited += dt;
                if (_ready != null && _waited < ReadyTimeout && !_ready()) return;
                _card.Hide();
                bool ok = _prepare == null || _prepare();
                _prepare = null;
                _ready = null;
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
                    _card.Show();
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
            _fade.color = BoardPalette.Alpha(LoadingCard.Scrim, alpha);
        }
    }
}
