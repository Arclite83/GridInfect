using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    public sealed class BoardScreen : AppScreen
    {
        BoardView _board;
        PieceView[] _pieces;
        LevelSession _bound;
        TextMesh _title;
        TextMesh _hud;
        UiButton _backButton;
        UiButton _resetButton;
        GameObject _popup;
        GameObject _beginCover;
        int _dragIndex = -1;
        bool _popupOpen;

        static float ScreenW => UnityEngine.Screen.width;
        static float ScreenH => UnityEngine.Screen.height;

        // Boxes and type come off the short edge so a control keeps its shape
        // when the screen turns; positions stay fractions of their own axis.
        static float Short => PresentationConfig.ShortEdge;

        protected override void Build()
        {
            // Same top bar as every other screen: back on the left, title
            // centred, the screen's one action on the right.
            _backButton = UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, GoBack);
            Buttons.Add(_backButton);
            _resetButton = UiButton.Make(Root.transform, "RESET",
                new Vector2(-L.BackPos.x, L.TopBarY), L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, ResetLevel);
            Buttons.Add(_resetButton);

            _title = Ui.MakeText("title", Root.transform, "", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(_title.gameObject, 0f, L.TopBarY);

            _hud = Ui.MakeText("hud", Root.transform, "", L.BodyText, BoardTheme.Accent, 2);
            Ui.SetPos(_hud.gameObject, 0f, L.TopBarY - L.HeadingText - L.Gap);

            App.State.SessionChanged += OnSessionChanged;
            Bind(App.State.Session);

            if (App.State.Mode == GameMode.FreePlay && App.State.FreePlayRun == null)
            {
                ShowBeginCover();
            }
        }

        protected override void OnExit()
        {
            App.State.SessionChanged -= OnSessionChanged;
            Unbind();
        }

        // ---- session binding ----

        void OnSessionChanged(LevelSession session)
        {
            ClosePopup();
            Bind(session);
        }

        void Bind(LevelSession session)
        {
            Unbind();
            _bound = session;
            if (session == null) return;

            _board = new BoardView(Root.transform, session);
            _pieces = new PieceView[session.Pieces.Length];
            for (int k = 0; k < session.Pieces.Length; k++)
            {
                _pieces[k] = new PieceView(Root.transform, k, session.Pieces[k].Tile,
                    TrayTileSize(session.Pieces.Length), TraySlot(k));
            }
            session.LevelSolved += OnSolved;
            session.PiecesUnbound += OnPiecesUnbound;

            _title.text = App.State.Mode == GameMode.Classic ? $"LEVEL {App.State.ClassicLevelId + 1}"
                : App.State.Mode == GameMode.World ? $"{Worlds.Get(App.State.WorldId).Name.ToUpperInvariant()}  {App.State.WorldIndex + 1}"
                : App.State.Mode == GameMode.Daily ? $"DAILY  {App.State.DailyRun.DateUtc}"
                : App.State.Mode == GameMode.Endless ? $"ENDLESS  GRADE {(int)App.State.EndlessRun.Grade}"
                : $"{App.State.Difficulty}".ToUpperInvariant();
            _hud.text = "";
        }

        void Unbind()
        {
            if (_bound != null)
            {
                _bound.LevelSolved -= OnSolved;
                _bound.PiecesUnbound -= OnPiecesUnbound;
                _bound = null;
            }
            _board?.Dispose();
            _board = null;
            if (_pieces != null)
            {
                foreach (var piece in _pieces)
                {
                    if (piece?.Root != null) Object.Destroy(piece.Root);
                }
                _pieces = null;
            }
            _dragIndex = -1;
        }

        // The original racked the tray into eight fixed slots and centred on
        // slot 3. Six columns of board leave no room for that, so the tray
        // centres on the pieces the level actually has and shrinks only if
        // they would not fit the width — which, at a real maximum of six
        // pieces, they always do.
        float TrayTileSize(int count)
        {
            float board = _board != null ? _board.CellSize : ScreenH * PresentationConfig.CellHeightPct;
            float byWidth = ScreenW * PresentationConfig.BoardWidthPct
                            / (count * PresentationConfig.TraySlotPitch);
            return Mathf.Min(board, byWidth);
        }

        Vector2 TraySlot(int k)
        {
            int count = _bound != null ? _bound.Pieces.Length : PresentationConfig.TraySlots;
            float size = TrayTileSize(count);
            float x = (k - (count - 1) / 2f) * size * PresentationConfig.TraySlotPitch;
            float y = -ScreenH / 2f + ScreenH * PresentationConfig.TrayBottomPct + size / 2f;
            return new Vector2(x, y);
        }

        // ---- input ----

        public override bool OnPress(Vector2 world)
        {
            if (_popupOpen || _beginCover != null || _bound == null) return true;

            for (int k = _pieces.Length - 1; k >= 0; k--)
            {
                if (!_pieces[k].HitTest(world)) continue;

                if (_bound.Pieces[k].Placed)
                {
                    // Touching a placed piece unplaces it instantly (RULES §3).
                    _board.BeginUndo();
                    var clear = App.Do(GridInfectActions.PieceClear, Inputs.PieceClear(k));
                    _board.EndBatch();
                    if (!clear.Applied) return true;
                }
                _dragIndex = k;
                App.Tweens.Cancel(_pieces[k].Root.transform);
                _pieces[k].SetPos(world);
                return true;
            }
            return false;
        }

        public override void OnDrag(Vector2 world)
        {
            if (_dragIndex >= 0) _pieces[_dragIndex].SetPos(world);
        }

        public override void OnRelease(Vector2 world)
        {
            if (_dragIndex < 0 || _bound == null) return;
            int index = _dragIndex;
            _dragIndex = -1;

            var (i, j) = _board.CellAt(world);
            if (i >= 0)
            {
                // The wave is open across the dispatch: every CellChanged the
                // spread raises inside it is scheduled off this seed, and the
                // action is still applied on the frame the touch landed.
                _board.BeginWave(i, j);
                var place = App.Do(GridInfectActions.PiecePlace, Inputs.PiecePlace(index, i, j));
                _board.EndBatch(place.Applied);
                if (place.Applied)
                {
                    Vector2 center = _board.CellCenter(i, j);
                    App.Tweens.MoveTo(_pieces[index].Root.transform,
                        new Vector3(center.x, center.y, 0f), PresentationConfig.DropSnap);
                    App.ScheduleResolve();
                    return;
                }
            }
            // Illegal drop: back to the tray slot (the piece was already
            // cleared on touch, so board state is consistent).
            App.Tweens.MoveTo(_pieces[index].Root.transform,
                new Vector3(_pieces[index].TraySlot.x, _pieces[index].TraySlot.y, 0f),
                PresentationConfig.TrayReturn);
        }

        // ---- session reactions ----

        void OnPiecesUnbound()
        {
            if (_pieces == null) return;
            for (int k = 0; k < _pieces.Length; k++)
            {
                App.Tweens.MoveTo(_pieces[k].Root.transform,
                    new Vector3(_pieces[k].TraySlot.x, _pieces[k].TraySlot.y, 0f),
                    PresentationConfig.TrayReturn);
            }
        }

        void OnSolved()
        {
            if (_popupOpen) return;
            if (App.State.Mode == GameMode.Classic)
            {
                int levelId = App.State.ClassicLevelId;
                int next = Queries.NextClassicId(levelId);
                if (next >= 0)
                {
                    App.Do(GridInfectActions.ProgressUnlock, Inputs.Unlock(next));
                }
                ShowSolvedPopup(next >= 0
                    ? () => App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(next))
                    : (System.Action)null,
                    () => App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(levelId)));
            }
            else if (App.State.Mode == GameMode.World)
            {
                // Solving level N unlocks N+1; the last level finishes the
                // world (index == Count) and opens the next one.
                string worldId = App.State.WorldId;
                int index = App.State.WorldIndex;
                World world = Worlds.Get(worldId);
                App.Do(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(worldId, index + 1));
                System.Action next = null;
                if (index + 1 < world.Count)
                {
                    next = () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(worldId, index + 1));
                }
                else
                {
                    World following = Worlds.Next(worldId);
                    if (following != null)
                    {
                        App.Do(GridInfectActions.ProgressUnlockWorld, Inputs.UnlockWorld(following.Id));
                        next = () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(following.Id, 0));
                    }
                }
                ShowSolvedPopup(next, () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(worldId, index)));
            }
            else if (App.State.Mode == GameMode.Daily)
            {
                var run = App.State.DailyRun;
                if (!run.Completed)
                {
                    App.Do(GridInfectActions.DailyComplete, Inputs.Now(GameApp.NowMs()));
                    App.DailyScores.Submit(run.DateUtc, Queries.ElapsedMs(run, GameApp.NowMs()), run.ParMs);
                }
                long elapsed = Queries.ElapsedMs(run, GameApp.NowMs());
                long best = Queries.DailyBestMs(App.State.Profile, run.DateUtc);
                OpenPopup($"SOLVED IN {Queries.FormatDuration(elapsed)}\nPAR {Queries.FormatDuration(run.ParMs)}   BEST {Queries.FormatDuration(best)}\nSTREAK {App.State.Profile.DailyStreak}");
                AddPopupButton("MENU", new Vector2(0f, -Short * 0.06f),
                    new Vector2(L.ContentWidth / 3f, L.BarHeight), () => App.Screens.Show(new DailyScreen()));
            }
            else if (App.State.Mode == GameMode.Endless)
            {
                App.Do(GridInfectActions.EndlessAdvance); // no pause between levels; the streak is in the HUD
            }
            else
            {
                if (App.State.FreePlayIndex < App.State.FreePlayDefs.Length - 1)
                {
                    App.Do(GridInfectActions.FreePlayAdvance); // no pause between levels
                }
                else
                {
                    App.Do(GridInfectActions.FreePlayComplete, Inputs.Now(GameApp.NowMs()));
                    long duration = Queries.ElapsedMs(App.State.FreePlayRun, GameApp.NowMs());
                    ShowCompletedPopup(duration);
                }
            }
        }

        // ---- free play chrome ----

        void ShowBeginCover()
        {
            _beginCover = new GameObject("beginCover");
            _beginCover.transform.SetParent(Root.transform, false);
            var cover = Ui.MakeRect("bg", _beginCover.transform, new Vector2(ScreenW, ScreenH * 0.86f), BoardTheme.Background, 30);
            Ui.SetPos(cover, 0f, -ScreenH * 0.07f); // board area only; title stays visible
            var begin = UiButton.Make(_beginCover.transform, "BEGIN",
                new Vector2(0f, 0f), new Vector2(L.ContentWidth * 0.55f, L.ButtonHeight),
                BoardTheme.Accent, BoardTheme.GlyphDark, () =>
                {
                    App.Do(GridInfectActions.FreePlayBegin, Inputs.Now(GameApp.NowMs()));
                    Buttons.RemoveAll(b => b.Root != null && b.Root.transform.parent == _beginCover.transform);
                    Object.Destroy(_beginCover);
                    _beginCover = null;
                }, 31);
            Buttons.Add(begin);
        }

        public override void Tick(float dt)
        {
            if (_board != null)
            {
                _board.Muted = App.State.Profile.Muted;
                _board.Tick(dt);
            }

            if (App.State.Mode == GameMode.Daily)
            {
                var daily = App.State.DailyRun;
                if (daily == null || daily.Completed) return;
                long elapsedDaily = Queries.ElapsedMs(daily, GameApp.NowMs());
                if (elapsedDaily < 0) elapsedDaily = 0; // a backward clock is refused at daily.complete
                _hud.text = $"{Queries.FormatDuration(elapsedDaily)}   PAR {Queries.FormatDuration(daily.ParMs)}";
                return;
            }
            if (App.State.Mode == GameMode.Endless)
            {
                var endless = App.State.EndlessRun;
                if (endless != null) _hud.text = $"SOLVED {endless.Index}   STREAK {endless.Streak}   BEST {App.State.Profile.EndlessBest[(int)endless.Grade - 1]}";
                return;
            }
            if (App.State.Mode != GameMode.FreePlay) return;
            var run = App.State.FreePlayRun;
            if (run == null || run.Completed) return;

            long elapsed = Queries.ElapsedMs(run, GameApp.NowMs());
            if (elapsed < 0)
            {
                // Cheat guard (MODES §2.2): clock moved backward — abort to menu.
                App.Do(GridInfectActions.FreePlayAbort);
                App.Screens.Show(new FreePlayMenuScreen());
                return;
            }
            _hud.text = $"{App.State.Difficulty}   {App.State.FreePlayIndex + 1}/5   {Queries.FormatDuration(elapsed)}"
                .ToUpperInvariant();
        }

        // ---- popups ----

        void ShowSolvedPopup(System.Action next, System.Action replay)
        {
            OpenPopup("COMPLETE");
            float y = -Short * 0.06f;
            float step = L.ContentWidth / 3f;
            var size = new Vector2(step * 0.9f, L.BarHeight);
            AddPopupButton("MENU", new Vector2(-step, y), size, GoBack);
            AddPopupButton("REPLAY", new Vector2(0f, y), size, replay);
            if (next != null)
            {
                AddPopupButton("NEXT", new Vector2(step, y), size, next);
            }
        }

        void ShowCompletedPopup(long durationMs)
        {
            OpenPopup($"COMPLETED IN:\n{Queries.FormatDuration(durationMs)}");
            AddPopupButton("MENU", new Vector2(0f, -Short * 0.06f),
                new Vector2(L.ContentWidth / 3f, L.BarHeight),
                () => App.Screens.Show(new FreePlayMenuScreen()));
        }

        void OpenPopup(string message)
        {
            _popupOpen = true;
            _backButton.Enabled = false;
            _resetButton.Enabled = false;

            _popup = new GameObject("popup");
            _popup.transform.SetParent(Root.transform, false);
            Ui.MakeRect("dim", _popup.transform, new Vector2(ScreenW, ScreenH), BoardTheme.PanelDim, 40);

            var panel = new GameObject("panel");
            panel.transform.SetParent(_popup.transform, false);
            Ui.MakeRect("bg", panel.transform, new Vector2(L.ContentWidth, Short * 0.36f), BoardTheme.ButtonBg, 41);
            var text = Ui.MakeText("message", panel.transform, message, L.HeadingText, BoardTheme.Text, 42);
            Ui.SetPos(text.gameObject, 0f, Short * 0.08f);

            // Slide in (0.15 s, linear).
            panel.transform.localPosition = new Vector3(0f, -ScreenH, 0f);
            App.Tweens.MoveTo(panel.transform, Vector3.zero, PresentationConfig.PopupSlide);
            _popupPanel = panel;
        }

        GameObject _popupPanel;

        void AddPopupButton(string label, Vector2 center, Vector2 size, System.Action onClick)
        {
            var button = UiButton.Make(_popupPanel.transform, label, center, size,
                BoardTheme.Accent, BoardTheme.GlyphDark, onClick, 43);
            Buttons.Add(button);
        }

        void ClosePopup()
        {
            if (!_popupOpen) return;
            _popupOpen = false;
            _backButton.Enabled = true;
            _resetButton.Enabled = true;
            Buttons.RemoveAll(b => b.Root == null ||
                (b != _backButton && b != _resetButton));
            if (_popup != null) Object.Destroy(_popup);
            _popup = null;
            _popupPanel = null;
        }

        // ---- top bar ----

        void GoBack()
        {
            if (App.State.Mode == GameMode.FreePlay)
            {
                App.Do(GridInfectActions.FreePlayAbort);
                App.Screens.Show(new FreePlayMenuScreen());
            }
            else if (App.State.Mode == GameMode.World)
            {
                App.Screens.Show(new WorldLevelSelectScreen(App.State.WorldId));
            }
            else if (App.State.Mode == GameMode.Daily)
            {
                App.Screens.Show(new DailyScreen());
            }
            else if (App.State.Mode == GameMode.Endless)
            {
                App.Do(GridInfectActions.EndlessAbort);
                App.Screens.Show(new EndlessScreen());
            }
            else
            {
                App.Screens.Show(new ClassicSelectScreen());
            }
        }

        void ResetLevel()
        {
            if (_bound == null) return;
            if (_bound.Solved)
            {
                // Replay on a solved level reloads it fresh (MODES §1.1).
                if (App.State.Mode == GameMode.Classic)
                {
                    App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(App.State.ClassicLevelId));
                }
                else if (App.State.Mode == GameMode.World)
                {
                    App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(App.State.WorldId, App.State.WorldIndex));
                }
            }
            else
            {
                _board.BeginReset();
                App.Do(GridInfectActions.LevelReset);
                _board.EndBatch();
            }
        }
    }
}
